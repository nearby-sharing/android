using Android.Content;
using Android.Provider;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using static Android.Provider.MediaStore;
using static Java.Util.Jar.Attributes;

namespace Nearby_Sharing_Windows;

internal static class FileUtils
{
    public static CdpFileProvider CreateNearShareFileFromContentUriAsync(this ContentResolver contentResolver, AndroidUri contentUri)
    {
        var fileName = contentResolver.QueryContentName(contentUri);

        using var fd = contentResolver.OpenAssetFileDescriptor(contentUri, "r") ?? throw new IOException("Could not open file");
        var stream = fd.CreateInputStream() ?? throw new IOException("Could not open input stream");

        return CdpFileProvider.FromStream(fileName, stream);
    }

    public static string QueryContentName(this ContentResolver resolver, AndroidUri contentUri)
    {
        using var returnCursor = resolver.Query(contentUri, new[] { IOpenableColumns.DisplayName }, null, null, null) ?? throw new InvalidOperationException("Could not open content cursor");
        returnCursor.MoveToFirst();
        return returnCursor.GetString(0) ?? throw new IOException("Could not query content name");
    }

    public static Stream CreateDownloadFile(this Activity activity, string fileName, ulong size)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29) || true)
        {
            var filePath = Path.Combine(activity.GetDownloadDirectory().FullName, fileName);
            return File.Create(filePath);
        }

        // ToDo: Register as Download ...
        // We need a seekable stream!

        var resolver = activity.ContentResolver ?? throw new InvalidOperationException("Could not get ContentResolver");

        ContentValues contentValues = new();
        contentValues.Put(Downloads.InterfaceConsts.Title, fileName);
        contentValues.Put(Downloads.InterfaceConsts.DisplayName, fileName);
        contentValues.Put(Downloads.InterfaceConsts.MimeType, "*/*");
        contentValues.Put(Downloads.InterfaceConsts.Size, (long)size);
        contentValues.Put(Downloads.InterfaceConsts.RelativePath, Path.Combine("Download", fileName));
        contentValues.Put(Downloads.InterfaceConsts.IsDownload, true);

        // Insert into the database
        var contentUri = resolver.Insert(Downloads.ExternalContentUri, contentValues) ?? throw new IOException("Could not insert file into database");

        using var fd = resolver.OpenAssetFileDescriptor(contentUri, "wt") ?? throw new IOException("Could not open file");
        return fd.CreateOutputStream() ?? throw new IOException("Could not open input stream");
    }

    public static DirectoryInfo GetDownloadDirectory(this Activity activity)
    {
        DirectoryInfo rootDir = new(Path.Combine(activity.GetExternalMediaDirs()?.FirstOrDefault()?.AbsolutePath ?? "/sdcard/"));
        return rootDir.CreateSubdirectory("Download");
    }
}
