using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class AppDataCoordinator : IAppDataCoordinator
{
    private readonly ITaskCacheService _taskCacheService;
    private readonly IRingtoneCatalogService _ringtoneCatalogService;
    private readonly IUserSettingsService _userSettingsService;

    public AppDataCoordinator(
        ITaskCacheService taskCacheService,
        IRingtoneCatalogService ringtoneCatalogService,
        IUserSettingsService userSettingsService)
    {
        _taskCacheService = taskCacheService;
        _ringtoneCatalogService = ringtoneCatalogService;
        _userSettingsService = userSettingsService;
    }

    public async Task InitializeAsync()
    {
        // UserSettingsService resolves its Ringtone navigation property from
        // IRingtoneCatalogService, so the catalog must be loaded first.
        await Task.WhenAll(
            _taskCacheService.InitializeAsync(),
            _ringtoneCatalogService.InitializeAsync()).ConfigureAwait(false);
        await _userSettingsService.InitializeAsync().ConfigureAwait(false);
    }

    public async Task ReloadAsync()
    {
        await Task.WhenAll(
            _taskCacheService.ReloadAsync(),
            _ringtoneCatalogService.ReloadAsync()).ConfigureAwait(false);
        await _userSettingsService.ReloadAsync().ConfigureAwait(false);
    }
}
