using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class AppDataCoordinator : IAppDataCoordinator
{
    private readonly ITaskCacheService _taskCacheService;
    private readonly IRingtoneCatalogService _ringtoneCatalogService;
    private readonly ISettingStore _settingStore;
    private readonly ITaskSeriesService _taskSeriesService;
    private readonly ITaskReminderService _taskReminderService;

    public AppDataCoordinator(
        ITaskCacheService taskCacheService,
        IRingtoneCatalogService ringtoneCatalogService,
        ISettingStore settingStore,
        ITaskSeriesService taskSeriesService,
        ITaskReminderService taskReminderService)
    {
        _taskCacheService = taskCacheService;
        _ringtoneCatalogService = ringtoneCatalogService;
        _settingStore = settingStore;
        _taskSeriesService = taskSeriesService;
        _taskReminderService = taskReminderService;
    }

    public async Task InitializeAsync()
    {

        await Task.WhenAll(
            _taskCacheService.InitializeAsync(),
            _ringtoneCatalogService.InitializeAsync()).ConfigureAwait(false);
        await _settingStore.InitializeAsync().ConfigureAwait(false);

        await _taskSeriesService.EnsureTasksAsync().ConfigureAwait(false);
        await _taskReminderService.StartAsync().ConfigureAwait(false);
    }

    public async Task ReloadAsync()
    {
        await Task.WhenAll(
            _taskCacheService.ReloadAsync(),
            _ringtoneCatalogService.ReloadAsync()).ConfigureAwait(false);
        await _settingStore.ReloadAsync().ConfigureAwait(false);
        await _taskSeriesService.EnsureTasksAsync().ConfigureAwait(false);
        await _taskReminderService.RefreshAsync().ConfigureAwait(false);
    }
}
