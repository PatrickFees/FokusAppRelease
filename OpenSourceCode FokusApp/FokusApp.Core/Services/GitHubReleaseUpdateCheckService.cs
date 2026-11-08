using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FokusApp.Core.Services;

public sealed class GitHubReleaseUpdateCheckService : IUpdateCheckService
{
    private const string LatestReleaseRequestUri =
        "https://api.github.com/repos/PatrickFees/FokusAppRelease/releases/latest";

    private readonly HttpClient _httpClient;
    private readonly IAppEnvironmentInfo _environmentInfo;
    private readonly IAppErrorCaptureService _errorCaptureService;

    public GitHubReleaseUpdateCheckService(
        HttpClient httpClient,
        IAppEnvironmentInfo environmentInfo,
        IAppErrorCaptureService errorCaptureService)
    {
        _httpClient = httpClient;
        _environmentInfo = environmentInfo;
        _errorCaptureService = errorCaptureService;
    }

    public async Task<AppUpdateInfo?> GetAvailableUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var release = await _httpClient
                .GetFromJsonAsync<GitHubReleaseResponse>(LatestReleaseRequestUri, cancellationToken)
                .ConfigureAwait(false);

            var apkAsset = release?.Assets?.FirstOrDefault(
                asset => asset.Name?.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) == true);

            if (release?.TagName is null ||
                apkAsset?.BrowserDownloadUrl is null ||
                !TryNormalizeVersion(release.TagName, out var latestVersion) ||
                !TryNormalizeVersion(_environmentInfo.AppVersionNumber ?? string.Empty, out var currentVersion) ||
                latestVersion <= currentVersion)
            {
                return null;
            }

            return new AppUpdateInfo
            {
                Version = latestVersion.ToString(3),
                DownloadUrl = apkAsset.BrowserDownloadUrl
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException or JsonException)
        {
            await _errorCaptureService.CaptureAsync(
                ex,
                new AppErrorCaptureOptions { Source = "GitHubReleaseUpdateCheckService" }).ConfigureAwait(false);
            return null;
        }
    }

    private static bool TryNormalizeVersion(string raw, out Version version)
    {
        var trimmed = raw.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(trimmed, out var parsed))
        {
            version = default!;
            return false;
        }

        version = new Version(Math.Max(parsed.Major, 0), Math.Max(parsed.Minor, 0), Math.Max(parsed.Build, 0));
        return true;
    }

    private sealed class GitHubReleaseResponse
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubReleaseAsset>? Assets { get; set; }
    }

    private sealed class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }
}
