using System.Text;
using Android.Content;
using Android.Provider;
using AndroidEnvironment = Android.OS.Environment;

namespace FokusApp.Platforms.Android;

public static class AndroidDownloadSaver
{
    public static async Task SaveAsync(string fileName, string mimeType, string content)
    {
        var resolver = Platform.AppContext.ContentResolver
            ?? throw new InvalidOperationException("The content resolver is not available.");

        var values = new ContentValues();
        values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
        values.Put(MediaStore.IMediaColumns.MimeType, mimeType);
        values.Put(MediaStore.IMediaColumns.RelativePath, AndroidEnvironment.DirectoryDownloads);

        var uri = resolver.Insert(MediaStore.Downloads.ExternalContentUri, values)
            ?? throw new InvalidOperationException("The file could not be created in the Downloads folder.");

        await using var stream = resolver.OpenOutputStream(uri)
            ?? throw new InvalidOperationException("The file in the Downloads folder could not be opened.");
        await stream.WriteAsync(Encoding.UTF8.GetBytes(content)).ConfigureAwait(false);
    }
}
