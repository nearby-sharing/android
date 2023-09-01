using Android.Content;
using Android.Provider;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using Environment = Android.OS.Environment;

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
        var filePath = Path.Combine(activity.GetDownloadDirectory().FullName, fileName);
        return File.Create(filePath);
    }

    public static DirectoryInfo GetDownloadDirectory(this Activity activity)
    {
        var publicDownloadDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads)?.AbsolutePath;
        DirectoryInfo downloadDir = new(publicDownloadDir ?? Path.Combine(activity.GetExternalMediaDirs()?.FirstOrDefault()?.AbsolutePath ?? "/sdcard/", "Download"));
        if (!downloadDir.Exists)
            downloadDir.Create();

        return downloadDir;
    }

    public static string GetLogFilePattern(this Activity activity)
    {
        DirectoryInfo downloadDir = new(Path.Combine(activity.GetExternalMediaDirs()?.FirstOrDefault()?.AbsolutePath ?? "/sdcard/", "logs"));
        if (!downloadDir.Exists)
            downloadDir.Create();

        return Path.Combine( downloadDir.FullName, "nearshare-android-{Date}.log.txt");
    }
}
