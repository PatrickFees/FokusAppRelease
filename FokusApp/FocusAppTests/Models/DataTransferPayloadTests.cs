using System.Text.Json;
using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;

namespace FocusAppTests.Models;

public class DataTransferPayloadTests
{
    private static readonly Guid RingtoneId = Guid.NewGuid();
    private static readonly Guid SettingId = Guid.NewGuid();
    private static readonly Guid TaskId = Guid.NewGuid();
    private static readonly Guid TimerId = Guid.NewGuid();

    [Fact]
    public void IsValid_CompletePayload_ReturnsTrue()
    {
        var payload = CreateValidPayload();

        var result = payload.IsValid();

        Assert.True(result);
    }

    [Fact]
    public void IsValid_PayloadWithoutTasksAndTimers_ReturnsTrue()
    {
        var payload = CreateValidPayload();
        payload.Tasks!.Clear();
        payload.FocusTimers!.Clear();

        var result = payload.IsValid();

        Assert.True(result);
    }

    [Fact]
    public void IsValid_NullTasks_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.Tasks = null;

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_NullFocusTimers_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.FocusTimers = null;

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_NullRingtones_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.Ringtones = null;

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_NoSettingsAtAll_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.Settings = null;
        payload.LegacySettings = null;

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_EmptySettingsList_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.Settings!.Clear();

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_EmptyRingtonesList_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.Ringtones!.Clear();

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_LegacySettingsOnly_ReturnsTrue()
    {
        var payload = CreateValidPayload();
        payload.LegacySettings = payload.Settings;
        payload.Settings = null;

        var result = payload.IsValid();

        Assert.True(result);
    }

    [Fact]
    public void IsValid_SettingsAndLegacySettingsBothPresent_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.LegacySettings = payload.Settings;

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_NullTaskEntry_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.Tasks!.Add(null!);

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_NullFocusTimerEntry_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.FocusTimers!.Add(null!);

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_NullRingtoneEntry_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.Ringtones!.Add(null!);

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_NullSettingEntry_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.Settings!.Add(null!);

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_DuplicateTaskIds_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        var duplicate = CreateTaskModel(TaskId);
        payload.Tasks!.Add(duplicate);

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_EmptyTaskId_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        var emptyId = CreateTaskModel(Guid.Empty);
        payload.Tasks!.Add(emptyId);

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_DuplicateFocusTimerIds_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        var duplicate = CreateTimerModel(TimerId, 10, null);
        payload.FocusTimers!.Add(duplicate);

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_EmptyFocusTimerId_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        var emptyId = CreateTimerModel(Guid.Empty, 10, null);
        payload.FocusTimers!.Add(emptyId);

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_DuplicateRingtoneIds_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        var duplicate = new RingtoneDataTransferModel(RingtoneId, "Raw/Zwei.mp3");
        payload.Ringtones!.Add(duplicate);

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_DuplicateSettingIds_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        var duplicate = new SettingDataTransferModel(SettingId, 0, 0, RingtoneId);
        payload.Settings!.Add(duplicate);

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_SettingReferencesUnknownRingtone_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        var unknownRingtoneId = Guid.NewGuid();
        payload.Settings = [new SettingDataTransferModel(SettingId, 0, 0, unknownRingtoneId)];

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(-1, false)]
    [InlineData(3, false)]
    public void IsValid_SettingTheme_MustBeUnspecifiedLightOrDark(int theme, bool expected)
    {
        var payload = CreateValidPayload();
        payload.Settings = [new SettingDataTransferModel(SettingId, theme, 0, RingtoneId)];

        var result = payload.IsValid();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData((int)AppLanguage.German, true)]
    [InlineData((int)AppLanguage.English, true)]
    [InlineData(2, false)]
    [InlineData(-1, false)]
    public void IsValid_SettingLanguage_MustBeKnownLanguage(int language, bool expected)
    {
        var payload = CreateValidPayload();
        payload.Settings = [new SettingDataTransferModel(SettingId, 0, language, RingtoneId)];

        var result = payload.IsValid();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_RingtoneWithoutFilePath_ReturnsFalse(string? filePath)
    {
        var payload = CreateValidPayload();
        payload.Ringtones = [new RingtoneDataTransferModel(RingtoneId, filePath)];

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_TaskWithUndefinedPriority_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        var task = CreateTaskModel(TaskId) with { Priority = (TaskPriority)99 };
        payload.Tasks = [task];

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(120, true)]
    [InlineData(-1, false)]
    [InlineData(121, false)]
    public void IsValid_TaskTimerMinutes_MustBeWithinZeroAndMaximum(int minutes, bool expected)
    {
        var payload = CreateValidPayload();
        var task = CreateTaskModel(TaskId) with { TimerMinutes = minutes };
        payload.Tasks = [task];

        var result = payload.IsValid();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(120, true)]
    [InlineData(-1, false)]
    [InlineData(121, false)]
    public void IsValid_TaskBreakMinutes_MustBeWithinZeroAndMaximum(int minutes, bool expected)
    {
        var payload = CreateValidPayload();
        var task = CreateTaskModel(TaskId) with { BreakMinutes = minutes };
        payload.Tasks = [task];

        var result = payload.IsValid();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValid_FocusTimerWithoutTask_ReturnsTrue()
    {
        var payload = CreateValidPayload();
        payload.FocusTimers = [CreateTimerModel(TimerId, 25, null)];

        var result = payload.IsValid();

        Assert.True(result);
    }

    [Fact]
    public void IsValid_FocusTimerReferencesUnknownTask_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        var unknownTaskId = Guid.NewGuid();
        payload.FocusTimers = [CreateTimerModel(TimerId, 25, unknownTaskId)];

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(120, true)]
    [InlineData(121, false)]
    [InlineData(180, false)]
    public void IsValid_FocusTimerMinutes_MustNotExceedMaximum(int minutes, bool expected)
    {
        var payload = CreateValidPayload();
        payload.FocusTimers = [CreateTimerModel(TimerId, minutes, TaskId)];

        var result = payload.IsValid();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetSettingsForImport_SettingsPresent_ReturnsSettings()
    {
        var payload = CreateValidPayload();

        var result = payload.GetSettingsForImport();

        var setting = Assert.Single(result);
        Assert.Equal(SettingId, setting.Id);
    }

    [Fact]
    public void GetSettingsForImport_OnlyLegacySettings_ReturnsLegacySettings()
    {
        var payload = CreateValidPayload();
        payload.LegacySettings = payload.Settings;
        payload.Settings = null;

        var result = payload.GetSettingsForImport();

        var setting = Assert.Single(result);
        Assert.Equal(SettingId, setting.Id);
    }

    [Fact]
    public void GetSettingsForImport_NoSettings_ReturnsEmptyList()
    {
        var payload = CreateValidPayload();
        payload.Settings = null;

        var result = payload.GetSettingsForImport();

        Assert.Empty(result);
    }

    [Fact]
    public void CreateTaskEntities_Tasks_AreMappedToEntities()
    {
        var payload = CreateValidPayload();

        var entities = payload.CreateTaskEntities().ToList();

        var entity = Assert.Single(entities);
        Assert.Equal(TaskId, entity.Id);
        Assert.Equal("Aufgabe", entity.Note);
    }

    [Fact]
    public void CreateTaskEntities_TaskWithoutTimerMinutes_TakesMinutesFromLegacyFocusTimer()
    {
        var payload = CreateValidPayload();
        var task = CreateTaskModel(TaskId) with { TimerMinutes = 0 };
        payload.Tasks = [task];
        payload.FocusTimers = [CreateTimerModel(TimerId, 45, TaskId)];

        var entities = payload.CreateTaskEntities().ToList();

        var entity = Assert.Single(entities);
        Assert.Equal(45, entity.TimerMinutes);
    }

    [Fact]
    public void CreateTaskEntities_TaskWithOwnTimerMinutes_KeepsOwnMinutes()
    {
        var payload = CreateValidPayload();
        var task = CreateTaskModel(TaskId) with { TimerMinutes = 25 };
        payload.Tasks = [task];
        payload.FocusTimers = [CreateTimerModel(TimerId, 45, TaskId)];

        var entities = payload.CreateTaskEntities().ToList();

        var entity = Assert.Single(entities);
        Assert.Equal(25, entity.TimerMinutes);
    }

    [Fact]
    public void CreateTaskEntities_SeveralFocusTimersForOneTask_UsesFirstOne()
    {
        var payload = CreateValidPayload();
        var task = CreateTaskModel(TaskId) with { TimerMinutes = 0 };
        var firstTimer = CreateTimerModel(Guid.NewGuid(), 30, TaskId);
        var secondTimer = CreateTimerModel(Guid.NewGuid(), 50, TaskId);
        payload.Tasks = [task];
        payload.FocusTimers = [firstTimer, secondTimer];

        var entities = payload.CreateTaskEntities().ToList();

        var entity = Assert.Single(entities);
        Assert.Equal(30, entity.TimerMinutes);
    }

    [Fact]
    public void CreateTaskEntities_FocusTimerWithoutTask_IsIgnored()
    {
        var payload = CreateValidPayload();
        var task = CreateTaskModel(TaskId) with { TimerMinutes = 0 };
        payload.Tasks = [task];
        payload.FocusTimers = [CreateTimerModel(TimerId, 45, null)];

        var entities = payload.CreateTaskEntities().ToList();

        var entity = Assert.Single(entities);
        Assert.Equal(0, entity.TimerMinutes);
    }

    [Fact]
    public void CreateTaskEntities_FocusTimerOfOtherTask_DoesNotAffectTask()
    {
        var payload = CreateValidPayload();
        var otherTaskId = Guid.NewGuid();
        var task = CreateTaskModel(TaskId) with { TimerMinutes = 0 };
        var otherTask = CreateTaskModel(otherTaskId) with { TimerMinutes = 0 };
        payload.Tasks = [task, otherTask];
        payload.FocusTimers = [CreateTimerModel(TimerId, 45, otherTaskId)];

        var entities = payload.CreateTaskEntities().ToList();

        var entityOfTask = entities.Single(entity => entity.Id == TaskId);
        var entityOfOtherTask = entities.Single(entity => entity.Id == otherTaskId);
        Assert.Equal(0, entityOfTask.TimerMinutes);
        Assert.Equal(45, entityOfOtherTask.TimerMinutes);
    }

    [Fact]
    public void CreateTaskEntities_NoTasks_ReturnsEmptySequence()
    {
        var payload = CreateValidPayload();
        payload.Tasks = [];

        var entities = payload.CreateTaskEntities().ToList();

        Assert.Empty(entities);
    }

    [Fact]
    public void SettingDataTransferModel_RoundTrip_PreservesAllValues()
    {
        var entity = new SettingEntity { Id = SettingId, Theme = 2, Language = 1, RingtoneId = RingtoneId };

        var model = SettingDataTransferModel.FromEntity(entity);
        var restored = model.ToEntity();

        Assert.Equal(SettingId, restored.Id);
        Assert.Equal(2, restored.Theme);
        Assert.Equal(1, restored.Language);
        Assert.Equal(RingtoneId, restored.RingtoneId);
    }

    [Fact]
    public void RingtoneDataTransferModel_RoundTrip_PreservesAllValues()
    {
        var entity = new RingtonesEntity { Id = RingtoneId, RingtoneFilePath = "Raw/Eins.mp3" };

        var model = RingtoneDataTransferModel.FromEntity(entity);
        var restored = model.ToEntity();

        Assert.Equal(RingtoneId, restored.Id);
        Assert.Equal("Raw/Eins.mp3", restored.RingtoneFilePath);
    }

    [Fact]
    public void TaskDataTransferModel_RoundTripOfNormalTask_PreservesAllValues()
    {
        var entity = new TaskEntity
        {
            Id = TaskId,
            IsDone = true,
            Note = "Notiz",
            IsRegularTask = false,
            IsWorkTask = false,
            IsPositive = true,
            CreationDate = new DateTime(2026, 3, 1, 14, 30, 0),
            TaskDate = new DateTime(2026, 3, 10),
            Priority = TaskPriority.Hoch,
            BreakMinutes = 7,
            TimerMinutes = 25
        };

        var model = TaskDataTransferModel.FromEntity(entity);
        var restored = model.ToEntity();

        Assert.Equal(TaskId, restored.Id);
        Assert.True(restored.IsDone);
        Assert.Equal("Notiz", restored.Note);
        Assert.False(restored.IsRegularTask);
        Assert.False(restored.IsWorkTask);
        Assert.True(restored.IsPositive);
        Assert.Equal(entity.CreationDate, restored.CreationDate);
        Assert.Equal(entity.TaskDate, restored.TaskDate);
        Assert.Equal(TaskPriority.Hoch, restored.Priority);
        Assert.Equal(7, restored.BreakMinutes);
        Assert.Equal(25, restored.TimerMinutes);
    }

    [Fact]
    public void TaskDataTransferModel_ToEntityOfRegularTask_ClearsTaskDate()
    {
        var model = CreateTaskModel(TaskId) with { IsRegularTask = true, TaskDate = new DateTime(2026, 3, 10) };

        var entity = model.ToEntity();

        Assert.Null(entity.TaskDate);
    }

    [Fact]
    public void TaskDataTransferModel_ToEntityOfNormalTask_TruncatesTaskDateToDay()
    {
        var model = CreateTaskModel(TaskId) with { TaskDate = new DateTime(2026, 3, 10, 16, 45, 0) };

        var entity = model.ToEntity();

        Assert.Equal(new DateTime(2026, 3, 10), entity.TaskDate);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(0, 25, 0, 25)]
    [InlineData(2, 0, 0, 120)]
    [InlineData(0, 45, 20, 45)]
    [InlineData(0, 45, 40, 46)]
    public void FocusTimerDataTransferModel_TimerMinutes_IsTimeOfDayRoundedToMinutes(
        int hours,
        int minutes,
        int seconds,
        int expectedMinutes)
    {
        var timeSet = new DateTime(2000, 1, 1, hours, minutes, seconds);
        var model = new FocusTimerDataTransferModel(TimerId, timeSet, null);

        var result = model.TimerMinutes;

        Assert.Equal(expectedMinutes, result);
    }

    [Fact]
    public void Serialize_PayloadWithSettings_UsesSettingsAndOmitsLegacyUsers()
    {
        var payload = CreateValidPayload();

        var json = JsonSerializer.Serialize(payload);

        Assert.Contains("\"Settings\"", json);
        Assert.DoesNotContain("\"Users\"", json);
    }

    [Fact]
    public void Deserialize_LegacyJsonWithUsers_FillsLegacySettingsAndStaysValid()
    {
        var legacyPayload = CreateValidPayload();
        legacyPayload.LegacySettings = legacyPayload.Settings;
        legacyPayload.Settings = null;
        var json = JsonSerializer.Serialize(legacyPayload);

        var restored = JsonSerializer.Deserialize<DataTransferPayload>(json);

        Assert.NotNull(restored);
        Assert.Contains("\"Users\"", json);
        Assert.Null(restored.Settings);
        Assert.NotNull(restored.LegacySettings);
        Assert.True(restored.IsValid());
    }

    [Fact]
    public void Deserialize_SerializedValidPayload_StaysValid()
    {
        var payload = CreateValidPayload();
        var json = JsonSerializer.Serialize(payload);

        var restored = JsonSerializer.Deserialize<DataTransferPayload>(json);

        Assert.NotNull(restored);
        Assert.True(restored.IsValid());
        Assert.Single(restored.Tasks!);
        Assert.Single(restored.FocusTimers!);
        Assert.Single(restored.Ringtones!);
    }

    private static DataTransferPayload CreateValidPayload()
    {
        var ringtone = new RingtoneDataTransferModel(RingtoneId, "Raw/Eins.mp3");
        var setting = new SettingDataTransferModel(SettingId, 0, (int)AppLanguage.German, RingtoneId);
        var task = CreateTaskModel(TaskId);
        var timer = CreateTimerModel(TimerId, 25, TaskId);

        return new DataTransferPayload
        {
            ExportedAtUtc = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
            Settings = [setting],
            Tasks = [task],
            FocusTimers = [timer],
            Ringtones = [ringtone]
        };
    }

    private static TaskDataTransferModel CreateTaskModel(Guid id)
    {
        return new TaskDataTransferModel(
            id,
            false,
            "Aufgabe",
            false,
            true,
            false,
            new DateTime(2026, 3, 1, 14, 30, 0),
            new DateTime(2026, 3, 10),
            TaskPriority.Mittel,
            5,
            25);
    }

    private static FocusTimerDataTransferModel CreateTimerModel(Guid id, int minutes, Guid? taskId)
    {
        var timeSet = new DateTime(2000, 1, 1).AddMinutes(minutes);

        return new FocusTimerDataTransferModel(id, timeSet, taskId);
    }
}
