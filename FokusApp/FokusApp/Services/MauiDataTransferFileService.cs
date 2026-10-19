using FokusApp.Core.Interfaces;

namespace FokusApp.Services;

public sealed class MauiDataTransferFileService : IDataTransferFileService
{
    public async Task ShareExportFileAsync(string fileName, string title, string content)
    {
        var targetPath = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllTextAsync(targetPath, content).ConfigureAwait(false);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = title,
            File = new ShareFile(targetPath)
        }).ConfigureAwait(false);
    }

    public async Task<Stream?> OpenImportFileAsync(string pickerTitle)
    {
        var pickResult = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = pickerTitle,
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.Android] = ["application/json"],
                [DevicePlatform.iOS] = ["public.json"],
                [DevicePlatform.MacCatalyst] = ["public.json"],
                [DevicePlatform.WinUI] = [".json"]
            })
        }).ConfigureAwait(false);

        if (pickResult is null)
        {
            return null;
        }

        return await pickResult.OpenReadAsync().ConfigureAwait(false);
    }
}
