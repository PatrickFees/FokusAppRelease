using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class AppDataCoordinatorTests
{
    private readonly ITaskCacheService _taskCacheService = Substitute.For<ITaskCacheService>();
    private readonly IRingtoneCatalogService _ringtoneCatalogService = Substitute.For<IRingtoneCatalogService>();
    private readonly IUserSettingsService _userSettingsService = Substitute.For<IUserSettingsService>();
    private readonly AppDataCoordinator _coordinator;

    public AppDataCoordinatorTests()
    {
        _coordinator = new AppDataCoordinator(_taskCacheService, _ringtoneCatalogService, _userSettingsService);
    }

    [Fact]
    public async Task InitializeAsync_Always_InitializesAllThreeServices()
    {
        await _coordinator.InitializeAsync();

        await _taskCacheService.Received(1).InitializeAsync();
        await _ringtoneCatalogService.Received(1).InitializeAsync();
        await _userSettingsService.Received(1).InitializeAsync();
    }

    [Fact]
    public async Task InitializeAsync_TaskCacheStillLoading_StartsCatalogButWaitsWithSettings()
    {
        var taskCacheGate = new TaskCompletionSource();
        _taskCacheService.InitializeAsync().Returns(taskCacheGate.Task);

        var initializeTask = _coordinator.InitializeAsync();

        await _ringtoneCatalogService.Received(1).InitializeAsync();
        await _userSettingsService.DidNotReceive().InitializeAsync();
        taskCacheGate.SetResult();
        await initializeTask;
        await _userSettingsService.Received(1).InitializeAsync();
    }

    [Fact]
    public async Task InitializeAsync_CatalogStillLoading_WaitsWithSettings()
    {
        var catalogGate = new TaskCompletionSource();
        _ringtoneCatalogService.InitializeAsync().Returns(catalogGate.Task);

        var initializeTask = _coordinator.InitializeAsync();

        await _userSettingsService.DidNotReceive().InitializeAsync();
        catalogGate.SetResult();
        await initializeTask;
        await _userSettingsService.Received(1).InitializeAsync();
    }

    [Fact]
    public async Task InitializeAsync_TaskCacheFails_ThrowsAndSkipsSettings()
    {
        var failure = new InvalidOperationException("Datenbank nicht erreichbar");
        _taskCacheService.InitializeAsync().Returns(Task.FromException(failure));

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => _coordinator.InitializeAsync());

        Assert.Same(failure, thrown);
        await _userSettingsService.DidNotReceive().InitializeAsync();
    }

    [Fact]
    public async Task ReloadAsync_Always_ReloadsAllThreeServices()
    {
        await _coordinator.ReloadAsync();

        await _taskCacheService.Received(1).ReloadAsync();
        await _ringtoneCatalogService.Received(1).ReloadAsync();
        await _userSettingsService.Received(1).ReloadAsync();
    }

    [Fact]
    public async Task ReloadAsync_CatalogStillLoading_WaitsWithSettings()
    {
        var catalogGate = new TaskCompletionSource();
        _ringtoneCatalogService.ReloadAsync().Returns(catalogGate.Task);

        var reloadTask = _coordinator.ReloadAsync();

        await _taskCacheService.Received(1).ReloadAsync();
        await _userSettingsService.DidNotReceive().ReloadAsync();
        catalogGate.SetResult();
        await reloadTask;
        await _userSettingsService.Received(1).ReloadAsync();
    }
}
