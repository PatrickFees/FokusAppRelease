using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.Services;

public class StartupDataCheckerTests
{
    private const string DefaultRingtone = "Raw/Happy with Clap.mp3";

    private static readonly DateTimeOffset Now = new(2026, 3, 10, 14, 30, 0, TimeSpan.Zero);

    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtonesRepository _ringtonesRepository = Substitute.For<IRingtonesRepository>();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly StartupDataChecker _checker;
    private List<RingtonesEntity> _storedRingtones = [];
    private List<RingtonesEntity> _addedRingtones = [];
    private List<TaskEntity> _addedTasks = [];
    private SettingEntity? _addedSetting;

    public StartupDataCheckerTests()
    {
        _settingRepository.AnySettingExistsAsync().Returns(false);
        _ringtonesRepository.AddRangeAsync(Arg.Do<IEnumerable<RingtonesEntity>>(CaptureRingtones));
        _ringtonesRepository.GetAllAsync().Returns(_ => _storedRingtones);
        _settingRepository.AddAsync(Arg.Do<SettingEntity>(setting => _addedSetting = setting));
        _taskRepository.AddRangeAsync(Arg.Do<IEnumerable<TaskEntity>>(CaptureTasks));
        _checker = new StartupDataChecker(_taskRepository, _settingRepository, _ringtonesRepository, _timeProvider);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_SettingsAlreadyExist_DoesNotCreateAnyData()
    {
        _settingRepository.AnySettingExistsAsync().Returns(true);

        await _checker.EnsureFirstStartDataAsync();

        await _ringtonesRepository.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<RingtonesEntity>>());
        await _settingRepository.DidNotReceive().AddAsync(Arg.Any<SettingEntity>());
        await _taskRepository.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<TaskEntity>>());
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_FirstStart_CreatesFourRingtonesWithUniqueIds()
    {
        await _checker.EnsureFirstStartDataAsync();

        var ringtoneIds = _addedRingtones.Select(ringtone => ringtone.Id);
        var uniqueIdCount = ringtoneIds.Distinct().Count();
        var filePaths = _addedRingtones.Select(ringtone => ringtone.RingtoneFilePath);
        Assert.Equal(4, _addedRingtones.Count);
        Assert.Equal(4, uniqueIdCount);
        Assert.All(filePaths, filePath => Assert.NotEmpty(filePath ?? string.Empty));
        Assert.Contains(DefaultRingtone, filePaths);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_FirstStart_CreatesSettingsWithDefaultRingtone()
    {
        await _checker.EnsureFirstStartDataAsync();

        var defaultRingtone = _addedRingtones.Single(ringtone => ringtone.RingtoneFilePath == DefaultRingtone);
        Assert.NotNull(_addedSetting);
        Assert.NotEqual(Guid.Empty, _addedSetting.Id);
        Assert.Equal(0, _addedSetting.Theme);
        Assert.Equal(0, _addedSetting.Language);
        Assert.Equal(defaultRingtone.Id, _addedSetting.RingtoneId);
        Assert.Same(defaultRingtone, _addedSetting.Ringtone);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_DefaultRingtoneNotStored_FallsBackToFirstStoredRingtone()
    {
        var other = CreateRingtone("Raw/Anderer.mp3");
        _ringtonesRepository.GetAllAsync().Returns(_ => [other]);

        await _checker.EnsureFirstStartDataAsync();

        Assert.Equal(other.Id, _addedSetting?.RingtoneId);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_DefaultRingtonePathDiffersInCase_StillFindsDefault()
    {
        var upperCaseDefaultPath = DefaultRingtone.ToUpperInvariant();
        var other = CreateRingtone("Raw/Anderer.mp3");
        var defaultWithOtherCase = CreateRingtone(upperCaseDefaultPath);
        _ringtonesRepository.GetAllAsync().Returns(_ => [other, defaultWithOtherCase]);

        await _checker.EnsureFirstStartDataAsync();

        Assert.Equal(defaultWithOtherCase.Id, _addedSetting?.RingtoneId);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_NoRingtoneStored_ThrowsAndCreatesNoSettingsOrTasks()
    {
        var noRingtones = new List<RingtonesEntity>();
        _ringtonesRepository.GetAllAsync().Returns(_ => noRingtones);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _checker.EnsureFirstStartDataAsync());

        await _settingRepository.DidNotReceive().AddAsync(Arg.Any<SettingEntity>());
        await _taskRepository.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<TaskEntity>>());
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_FirstStart_CreatesOneOpenAndOneDoneExampleTask()
    {
        await _checker.EnsureFirstStartDataAsync();

        var openTask = Assert.Single(_addedTasks, task => !task.IsDone);
        var doneTask = Assert.Single(_addedTasks, task => task.IsDone);
        Assert.Equal(2, _addedTasks.Count);
        Assert.Equal(TaskPriority.Dringend, openTask.Priority);
        Assert.Equal(TaskPriority.Wenig, doneTask.Priority);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_FirstStart_ExampleTasksAreNormalWorkTasksWithDefaults()
    {
        await _checker.EnsureFirstStartDataAsync();

        var taskIds = _addedTasks.Select(task => task.Id);
        var uniqueIdCount = taskIds.Distinct().Count();
        Assert.Equal(2, uniqueIdCount);
        Assert.All(_addedTasks, AssertIsNormalWorkTaskWithDefaults);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_FirstStart_ExampleTasksUseCurrentTimeAndDate()
    {
        var expectedDate = new DateTime(2026, 3, 10);

        await _checker.EnsureFirstStartDataAsync();

        Assert.All(_addedTasks, task =>
        {
            Assert.Equal(Now.DateTime, task.CreationDate);
            Assert.Equal(expectedDate, task.TaskDate);
        });
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_FirstStart_CreatesRingtonesBeforeSettingsBeforeTasks()
    {
        await _checker.EnsureFirstStartDataAsync();

        Received.InOrder(() =>
        {
            _ringtonesRepository.AddRangeAsync(Arg.Any<IEnumerable<RingtonesEntity>>());
            _settingRepository.AddAsync(Arg.Any<SettingEntity>());
            _taskRepository.AddRangeAsync(Arg.Any<IEnumerable<TaskEntity>>());
        });
    }

    private static void AssertIsNormalWorkTaskWithDefaults(TaskEntity task)
    {
        var note = task.Note ?? string.Empty;
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.NotEmpty(note);
        Assert.True(task.IsWorkTask);
        Assert.False(task.IsRegularTask);
        Assert.False(task.IsPositive);
        Assert.Equal(5, task.BreakMinutes);
    }

    private static RingtonesEntity CreateRingtone(string filePath)
    {
        return new RingtonesEntity
        {
            Id = Guid.NewGuid(),
            RingtoneFilePath = filePath
        };
    }

    private void CaptureRingtones(IEnumerable<RingtonesEntity> ringtones)
    {
        _addedRingtones = ringtones.ToList();
        _storedRingtones = _addedRingtones;
    }

    private void CaptureTasks(IEnumerable<TaskEntity> tasks)
    {
        _addedTasks = tasks.ToList();
    }
}
