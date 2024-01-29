using Android.Content;
using Android.Provider;
using Microsoft.Win32.SafeHandles;
using ShortDev.Microsoft.ConnectedDevices.NearShare;

namespace Nearby_Sharing_Windows;

internal static class FileUtils
{
    public static CdpFileProvider CreateNearShareFileFromContentUri(this ContentResolver contentResolver, AndroidUri contentUri)
    {
        var fileName = contentResolver.QueryContentName(contentUri);

        using var fd = contentResolver.OpenAssetFileDescriptor(contentUri, "r") ?? throw new IOException("Could not open file");
        var stream = fd.CreateInputStream() ?? throw new IOException("Could not open input stream");

        return CdpFileProvider.FromStream(fileName, stream);
    }

    public static string QueryContentName(this ContentResolver resolver, AndroidUri contentUri)
    {
        using var returnCursor = resolver.Query(contentUri, [IOpenableColumns.DisplayName], null, null, null) ?? throw new InvalidOperationException("Could not open content cursor");
        returnCursor.MoveToFirst();
        return returnCursor.GetString(0) ?? throw new IOException("Could not query content name");
    }

    public static Stream CreateMediaStoreStream(this ContentResolver resolver, string fileName)
    {
        ContentValues contentValues = new();
        contentValues.Put(MediaStore.IMediaColumns.DisplayName, fileName);
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
            contentValues.Put(MediaStore.IMediaColumns.RelativePath, "Download/Nearby Sharing/");

        var mediaUri = resolver.Insert(
            MediaStore.Files.GetContentUri("external") ?? throw new InvalidOperationException("Could not get external content uri"),
            contentValues) ?? throw new InvalidOperationException("Could not insert into MediaStore");

        var fileDescriptor = resolver.OpenFileDescriptor(mediaUri, "rwt") ?? throw new InvalidOperationException("Could not file");
        SafeFileHandle handle = new(fileDescriptor.Fd, ownsHandle: true);
        return new FileStream(handle, FileAccess.ReadWrite);
    }

    public static string GetLogFilePattern(this Activity activity)
    {
        DirectoryInfo downloadDir = new(Path.Combine(activity.GetExternalMediaDirs()?.FirstOrDefault()?.AbsolutePath ?? "/sdcard/", "logs"));
        if (!downloadDir.Exists)
            downloadDir.Create();

        return Path.Combine(downloadDir.FullName, "nearshare-android-{Date}.log.txt");
    }
}
