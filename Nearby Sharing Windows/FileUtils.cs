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

        var stream = contentResolver.OpenInputStream(contentUri) ?? throw new IOException("Could not open input stream");
        return CdpFileProvider.FromStream(fileName, stream);
    }

    public static string QueryContentName(this ContentResolver resolver, AndroidUri contentUri)
    {
        using var returnCursor = resolver.Query(contentUri, [IOpenableColumns.DisplayName], null, null, null) ?? throw new InvalidOperationException("Could not open content cursor");
        returnCursor.MoveToFirst();
        return returnCursor.GetString(0) ?? throw new IOException("Could not query content name");
    }

    public static (AndroidUri uri, FileStream stream) CreateMediaStoreStream(this ContentResolver resolver, string fileName)
    {
        ContentValues contentValues = new();
        contentValues.Put(MediaStore.IMediaColumns.DisplayName, fileName);

        FileStream stream;
        AndroidUri mediaUri;
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            stream = CreateDownloadFile(fileName);

            contentValues.Put(MediaStore.IMediaColumns.Data, stream.Name);
            mediaUri = resolver.Insert(contentValues);
        }
        else
        {
            contentValues.Put(MediaStore.IMediaColumns.RelativePath, "Download/Nearby Sharing/");
            mediaUri = resolver.Insert(contentValues);

            stream = resolver.OpenFileStream(mediaUri);
        }

        return (mediaUri, stream);
    }

    public static FileStream OpenFileStream(this ContentResolver resolver, AndroidUri mediaUri)
    {
        var fileDescriptor = resolver.OpenFileDescriptor(mediaUri, "rwt") ?? throw new InvalidOperationException("Could not open file descriptor");

        SafeFileHandle handle = new(fileDescriptor.Fd, ownsHandle: false);
        return new(handle, FileAccess.ReadWrite);
    }

    static AndroidUri Insert(this ContentResolver resolver, ContentValues contentValues)
    {
        return resolver.Insert(
            MediaStore.Files.GetContentUri("external") ?? throw new InvalidOperationException("Could not get external content uri"),
            contentValues
        ) ?? throw new InvalidOperationException("Could not insert into MediaStore");
    }

    static FileStream CreateDownloadFile(string fileName)
    {
        var downloadDir = AndroidEnvironment.GetExternalStoragePublicDirectory(AndroidEnvironment.DirectoryDownloads)?.AbsolutePath
            ?? throw new NullReferenceException("Could not get download directory");

        string filePath = Path.Combine(downloadDir, fileName);
        if (!File.Exists(filePath))
            return File.Create(filePath);

        var fileNameCore = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (int i = 1; File.Exists(filePath); i++)
        {
            filePath = Path.Combine(downloadDir, $"{fileNameCore} ({i}){extension}");
        }
        return File.Create(filePath);
    }

    public static string GetLogFilePattern(this Activity activity)
    {
        DirectoryInfo downloadDir = new(Path.Combine(activity.GetExternalMediaDirs()?.FirstOrDefault()?.AbsolutePath ?? "/sdcard/", "logs"));
        if (!downloadDir.Exists)
            downloadDir.Create();

        return Path.Combine(downloadDir.FullName, "nearshare-android-{Date}.log.txt");
    }
}
