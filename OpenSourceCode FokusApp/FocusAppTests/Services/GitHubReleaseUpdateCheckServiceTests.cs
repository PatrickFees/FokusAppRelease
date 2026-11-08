using System.Net;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class GitHubReleaseUpdateCheckServiceTests
{
    private readonly IAppEnvironmentInfo _environmentInfo = Substitute.For<IAppEnvironmentInfo>();
    private readonly IAppErrorCaptureService _errorCaptureService = Substitute.For<IAppErrorCaptureService>();

    public GitHubReleaseUpdateCheckServiceTests()
    {
        _environmentInfo.AppVersionNumber.Returns("1.4.0");
    }

    [Fact]
    public async Task GetAvailableUpdateAsync_NewerTagWithApkAsset_ReturnsItsVersionAndDownloadUrl()
    {
        var service = CreateService(ReleaseResponse("v1.5.0", ("FokusApp-1.5.0.apk", "https://example.com/FokusApp-1.5.0.apk")));

        var update = await service.GetAvailableUpdateAsync();

        Assert.NotNull(update);
        Assert.Equal("1.5.0", update!.Version);
        Assert.Equal("https://example.com/FokusApp-1.5.0.apk", update.DownloadUrl);
    }

    [Theory]
    [InlineData("v1.4.0")]
    [InlineData("v1.3.9")]
    [InlineData("1.4")]
    public async Task GetAvailableUpdateAsync_SameOrOlderTag_ReturnsNull(string tagName)
    {
        var service = CreateService(ReleaseResponse(tagName, ("FokusApp-x.apk", "https://example.com/x.apk")));

        var update = await service.GetAvailableUpdateAsync();

        Assert.Null(update);
    }

    [Fact]
    public async Task GetAvailableUpdateAsync_NoApkAsset_ReturnsNull()
    {
        var service = CreateService(ReleaseResponse("v1.5.0", ("notes.txt", "https://example.com/notes.txt")));

        var update = await service.GetAvailableUpdateAsync();

        Assert.Null(update);
    }

    [Fact]
    public async Task GetAvailableUpdateAsync_UnparsableTag_ReturnsNull()
    {
        var service = CreateService(ReleaseResponse("latest-beta", ("FokusApp-x.apk", "https://example.com/x.apk")));

        var update = await service.GetAvailableUpdateAsync();

        Assert.Null(update);
    }

    [Fact]
    public async Task GetAvailableUpdateAsync_RequestFails_ReturnsNullAndCapturesTheError()
    {
        var service = CreateService(new FakeHttpMessageHandler(_ => throw new HttpRequestException("offline")));

        var update = await service.GetAvailableUpdateAsync();

        Assert.Null(update);
        await _errorCaptureService.Received(1).CaptureAsync(
            Arg.Any<HttpRequestException>(),
            Arg.Any<AppErrorCaptureOptions>());
    }

    private GitHubReleaseUpdateCheckService CreateService(string jsonBody) =>
        CreateService(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonBody)
        }));

    private GitHubReleaseUpdateCheckService CreateService(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler), _environmentInfo, _errorCaptureService);

    private static string ReleaseResponse(string tagName, (string Name, string Url) asset) =>
        "{ \"tag_name\": \"" + tagName + "\", \"assets\": [ { \"name\": \"" + asset.Name +
        "\", \"browser_download_url\": \"" + asset.Url + "\" } ] }";

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            _respond = respond;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_respond(request));
    }
}
