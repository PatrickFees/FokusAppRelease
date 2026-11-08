using FocusAppTests.MauiShims;
using FokusApp.Services;
using NSubstitute;

namespace FocusAppTests.Services;

[Collection(SerialMauiShimsCollection.Name)]
public class MauiAdapterTests : IDisposable
{
    private readonly string _cacheDirectory = Path.Combine(Path.GetTempPath(), $"fokusapp-adapter-{Guid.NewGuid():N}");
    private readonly string _previousCacheDirectory = FileSystem.CacheDirectory;
    private readonly IDeviceInfo _deviceInfo = Substitute.For<IDeviceInfo>();
    private readonly IAppInfo _appInfo = Substitute.For<IAppInfo>();
    private readonly IShare _share = Substitute.For<IShare>();
    private readonly IFilePicker _filePicker = Substitute.For<IFilePicker>();

    public MauiAdapterTests()
    {
        Directory.CreateDirectory(_cacheDirectory);
        FileSystem.CacheDirectory = _cacheDirectory;
        DeviceInfo.Current = _deviceInfo;
        AppInfo.Current = _appInfo;
        Share.Default = _share;
        FilePicker.Default = _filePicker;
        Application.Current = null;
    }

    public void Dispose()
    {
        FileSystem.CacheDirectory = _previousCacheDirectory;
        Application.Current = null;
        if (Directory.Exists(_cacheDirectory))
        {
            Directory.Delete(_cacheDirectory, recursive: true);
        }
    }

    [Fact]
    public void AppEnvironmentInfo_DeviceAndApp_ExposesPlatformOsVersionAndAppVersion()
    {
        _deviceInfo.Platform.Returns(DevicePlatform.Android);
        _deviceInfo.VersionString.Returns("15");
        _appInfo.VersionString.Returns("1.2.3");
        _appInfo.BuildString.Returns("42");
        var info = new MauiAppEnvironmentInfo();

        Assert.Equal("Android", info.Platform);
        Assert.Equal("15", info.OsVersion);
        Assert.Equal("1.2.3 (42)", info.AppVersion);
    }

    [Fact]
    public async Task ShareExportFile_Content_IsWrittenToTheCacheAndSharedWithTheTitle()
    {
        ShareFileRequest? request = null;
        _ = _share.RequestAsync(Arg.Do<ShareFileRequest>(captured => request = captured));
        var service = new MauiDataTransferFileService();

        await service.ShareExportFileAsync("export.json", "Exportieren", "{\"a\":1}");

        var expectedPath = Path.Combine(_cacheDirectory, "export.json");
        Assert.Equal("{\"a\":1}", await File.ReadAllTextAsync(expectedPath));
        Assert.NotNull(request);
        Assert.Equal("Exportieren", request.Title);
        Assert.Equal(expectedPath, request.File!.FullPath);
    }

    [Fact]
    public async Task ShareExportFile_ExistingFile_IsOverwritten()
    {
        var service = new MauiDataTransferFileService();
        await service.ShareExportFileAsync("export.json", "Exportieren", "alt");

        await service.ShareExportFileAsync("export.json", "Exportieren", "neu");

        var content = await File.ReadAllTextAsync(Path.Combine(_cacheDirectory, "export.json"));
        Assert.Equal("neu", content);
    }

    [Fact]
    public async Task ShareText_TitleAndText_OpensTheShareMenuWithBoth()
    {
        ShareTextRequest? request = null;
        _ = _share.RequestAsync(Arg.Do<ShareTextRequest>(captured => request = captured));
        var service = new MauiDataTransferFileService();

        await service.ShareTextAsync("Teilen", "https://example.com/app.apk");

        Assert.NotNull(request);
        Assert.Equal("Teilen", request.Title);
        Assert.Equal("https://example.com/app.apk", request.Text);
    }

    [Fact]
    public async Task OpenImportFile_PickerCancelled_ReturnsNull()
    {
        _filePicker.PickAsync(Arg.Any<PickOptions>()).Returns((FileResult?)null);
        var service = new MauiDataTransferFileService();

        var stream = await service.OpenImportFileAsync("Datei wählen");

        Assert.Null(stream);
    }

    [Fact]
    public async Task OpenImportFile_FilePicked_ReturnsItsStream()
    {
        using var content = new MemoryStream([1, 2, 3]);
        var picked = new FileResult(() => Task.FromResult<Stream>(content));
        _filePicker.PickAsync(Arg.Any<PickOptions>()).Returns(picked);
        var service = new MauiDataTransferFileService();

        var stream = await service.OpenImportFileAsync("Datei wählen");

        Assert.Same(content, stream);
    }

    [Fact]
    public async Task OpenImportFile_Always_OffersJsonFilesForEveryPlatformWithTheGivenTitle()
    {
        PickOptions? options = null;
        _filePicker.PickAsync(Arg.Do<PickOptions>(captured => options = captured)).Returns((FileResult?)null);
        var service = new MauiDataTransferFileService();

        await service.OpenImportFileAsync("Datei wählen");

        Assert.NotNull(options);
        Assert.Equal("Datei wählen", options.PickerTitle);
        var types = options.FileTypes!.Values;
        Assert.Equal(["application/json"], types[DevicePlatform.Android]);
        Assert.Equal(["public.json"], types[DevicePlatform.iOS]);
        Assert.Equal(["public.json"], types[DevicePlatform.MacCatalyst]);
        Assert.Equal([".json"], types[DevicePlatform.WinUI]);
        Assert.Equal(4, types.Count);
    }
}
