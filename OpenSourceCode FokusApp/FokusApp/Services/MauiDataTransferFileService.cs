using FokusApp.Core.Interfaces;

namespace FokusApp.Services;

public sealed class MauiDataTransferFileService : IDataTransferFileService
{
    public async Task ShareExportFileAsync(string fileName, string title, string content, string mimeType = "application/json")
    {
#if WINDOWS

        await MainThread.InvokeOnMainThreadAsync(() => SaveWithPickerAsync(fileName, content)).ConfigureAwait(false);
#elif ANDROID

        await Platforms.Android.AndroidDownloadSaver.SaveAsync(fileName, mimeType, content).ConfigureAwait(false);
#else
        var targetPath = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllTextAsync(targetPath, content).ConfigureAwait(false);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = title,
            File = new ShareFile(targetPath)
        }).ConfigureAwait(false);
#endif
    }

    public async Task ShareTextAsync(string title, string text)
    {
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = title,
            Text = text
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

#if WINDOWS
    private static async Task SaveWithPickerAsync(string fileName, string content)
    {
        var picker = new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedFileName = Path.GetFileNameWithoutExtension(fileName),
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
        };
        var extension = Path.GetExtension(fileName);
        if (string.Equals(extension, ".ics", StringComparison.OrdinalIgnoreCase))
        {
            picker.FileTypeChoices.Add("iCalendar", [".ics"]);
        }
        else
        {
            picker.FileTypeChoices.Add("JSON", [".json"]);
        }

        if (Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView is Microsoft.UI.Xaml.Window window)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
        }

        var file = await picker.PickSaveFileAsync();
        if (file is not null)
        {
            await Windows.Storage.FileIO.WriteTextAsync(file, content);
        }
    }
#endif
}
