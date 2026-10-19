using FokusApp.Core.DataBase.Repositories.Implementations;
using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using Microsoft.Extensions.Time.Testing;

namespace FocusAppTests.DataBase;

public class StartupDataCheckerIntegrationTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 14, 30, 0, TimeSpan.Zero);

    private readonly SqliteTestDatabase _database = new();
    private readonly TaskRepository _taskRepository;
    private readonly SettingRepository _settingRepository;
    private readonly RingtonesRepository _ringtonesRepository;
    private readonly StartupDataChecker _checker;

    public StartupDataCheckerIntegrationTests()
    {
        _database.MigrateApp();
        _taskRepository = new TaskRepository(_database.AppFactory);
        _settingRepository = new SettingRepository(_database.AppFactory);
        _ringtonesRepository = new RingtonesRepository(_database.AppFactory);
        var timeProvider = new FakeTimeProvider(Now);
        _checker = new StartupDataChecker(_taskRepository, _settingRepository, _ringtonesRepository, timeProvider);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task EnsureFirstStartDataAsync_EmptyDatabase_CreatesRingtonesSettingAndTwoTasks()
    {
        await _checker.EnsureFirstStartDataAsync();

        var ringtones = await _ringtonesRepository.GetAllAsync();
        var settings = await _settingRepository.GetAllAsync();
        var tasks = await _taskRepository.GetAllAsync();
        Assert.Equal(4, ringtones.Count);
        Assert.Single(settings);
        Assert.Equal(2, tasks.Count);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_EmptyDatabase_SettingReferencesDefaultRingtone()
    {
        await _checker.EnsureFirstStartDataAsync();

        var settings = await _settingRepository.GetAllAsync();
        var setting = Assert.Single(settings);
        Assert.Equal("Raw/Happy with Clap.mp3", setting.Ringtone.RingtoneFilePath);
        Assert.Equal(0, setting.Theme);
        Assert.Equal(0, setting.Language);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_EmptyDatabase_StartTasksUseFakeClockAndExpectedCategories()
    {
        await _checker.EnsureFirstStartDataAsync();

        var open = await _taskRepository.GetNormalTasksAsync();
        var done = await _taskRepository.GetDoneTasksAsync();
        var openTask = Assert.Single(open);
        var doneTask = Assert.Single(done);
        Assert.Equal(Now.DateTime, openTask.CreationDate);
        Assert.Equal(Now.Date, openTask.TaskDate);
        Assert.Equal(TaskPriority.Dringend, openTask.Priority);
        Assert.Equal(TaskPriority.Wenig, doneTask.Priority);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_CalledTwice_DoesNotDuplicateData()
    {
        await _checker.EnsureFirstStartDataAsync();

        await _checker.EnsureFirstStartDataAsync();

        var ringtones = await _ringtonesRepository.GetAllAsync();
        var settings = await _settingRepository.GetAllAsync();
        var tasks = await _taskRepository.GetAllAsync();
        Assert.Equal(4, ringtones.Count);
        Assert.Single(settings);
        Assert.Equal(2, tasks.Count);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_SettingAlreadyExists_LeavesDatabaseUntouched()
    {
        var ringtone = TestEntities.CreateRingtone("Raw/Eigener.mp3");
        await _ringtonesRepository.AddAsync(ringtone);
        var setting = TestEntities.CreateSetting(ringtone.Id);
        await _settingRepository.AddAsync(setting);

        await _checker.EnsureFirstStartDataAsync();

        var ringtones = await _ringtonesRepository.GetAllAsync();
        var tasks = await _taskRepository.GetAllAsync();
        Assert.Single(ringtones);
        Assert.Empty(tasks);
    }
}
