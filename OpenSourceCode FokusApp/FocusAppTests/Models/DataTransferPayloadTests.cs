using System.Text.Json;
using FokusApp.Core;
using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
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
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    [InlineData(6, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    public void IsValid_SettingTheme_MustBeKnownAppColorTheme(int theme, bool expected)
    {
        var payload = CreateValidPayload();
        payload.Settings = [new SettingDataTransferModel(SettingId, theme, 0, RingtoneId)];

        var result = payload.IsValid();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, (int)AppColorTheme.Blue)]
    [InlineData(4, (int)AppColorTheme.Blue)]
    [InlineData(5, (int)AppColorTheme.Red)]
    [InlineData(7, (int)AppColorTheme.Yellow)]
    public void SettingToEntity_ThemeFromExport_RemovedThemesBecomeBlue(int theme, int expected)
    {
        var model = new SettingDataTransferModel(SettingId, theme, 0, RingtoneId);

        var entity = model.ToEntity();

        Assert.Equal(expected, entity.Theme);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(-1, false)]
    public void IsValid_SettingColorMode_MustBeKnownAppColorMode(int colorMode, bool expected)
    {
        var payload = CreateValidPayload();
        payload.Settings = [new SettingDataTransferModel(SettingId, (int)AppColorTheme.Blue, 0, RingtoneId, ColorMode: colorMode)];

        var result = payload.IsValid();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SettingToEntity_LegacyDarkTheme_BecomesBlueInDarkMode()
    {
        var model = new SettingDataTransferModel(SettingId, 2, 0, RingtoneId);

        var entity = model.ToEntity();

        Assert.Equal((int)AppColorTheme.Blue, entity.Theme);
        Assert.Equal((int)AppColorMode.Dark, entity.ColorMode);
    }

    [Theory]
    [InlineData(AppColorTheme.Pink, AppColorMode.Dark)]
    [InlineData(AppColorTheme.Yellow, AppColorMode.Light)]
    public void SettingModel_FromEntityAndBack_KeepsThemeAndColorMode(AppColorTheme theme, AppColorMode mode)
    {
        var entity = new SettingEntity { Id = SettingId, Theme = (int)theme, ColorMode = (int)mode, Language = 0, RingtoneId = RingtoneId };

        var restored = SettingDataTransferModel.FromEntity(entity).ToEntity();

        Assert.Equal((int)theme, restored.Theme);
        Assert.Equal((int)mode, restored.ColorMode);
    }

    [Fact]
    public void Deserialize_ExportWithoutColorMode_ImportsAsLight()
    {
        var payload = CreateValidPayload();
        payload.Settings = [new SettingDataTransferModel(SettingId, (int)AppColorTheme.Green, 0, RingtoneId)];
        var json = JsonSerializer.Serialize(payload);
        json = json.Replace(",\"ColorMode\":0", string.Empty);

        var restored = JsonSerializer.Deserialize<DataTransferPayload>(json)!;

        Assert.DoesNotContain("ColorMode", json);
        Assert.True(restored.IsValid());
        Assert.Equal((int)AppColorMode.Light, restored.GetSettingsForImport()[0].ToEntity().ColorMode);
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
        var entity = new SettingEntity { Id = SettingId, Theme = 3, Language = 1, RingtoneId = RingtoneId };

        var model = SettingDataTransferModel.FromEntity(entity);
        var restored = model.ToEntity();

        Assert.Equal(SettingId, restored.Id);
        Assert.Equal(3, restored.Theme);
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
            IsTemplate = false,
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
        Assert.False(restored.IsTemplate);
        Assert.False(restored.IsWorkTask);
        Assert.True(restored.IsPositive);
        Assert.Equal(entity.CreationDate, restored.CreationDate);
        Assert.Equal(entity.TaskDate, restored.TaskDate);
        Assert.Equal(TaskPriority.Hoch, restored.Priority);
        Assert.Equal(7, restored.BreakMinutes);
        Assert.Equal(25, restored.TimerMinutes);
    }

    [Fact]
    public void TaskDataTransferModel_RoundTrip_PreservesRepeatWeekStart()
    {
        var entity = TaskExtension.MapToEntity(new TaskDto
        {
            Id = TaskId,
            Note = "Notiz",
            TaskDate = new DateTime(2026, 3, 10),
            RepeatUnit = TaskRepeatUnit.Week,
            RepeatInterval = 2,
            RepeatWeekdays = TaskWeekdays.Monday,
            RepeatWeekStart = TaskWeekdays.Sunday
        });

        var model = TaskDataTransferModel.FromEntity(entity);
        var restored = model.ToEntity();

        Assert.Equal((int)TaskWeekdays.Sunday, model.RepeatWeekStart);
        Assert.Equal(TaskWeekdays.Sunday, restored.RepeatWeekStart);
    }

    [Theory]
    [InlineData((int)TaskWeekdays.Monday, true)]
    [InlineData((int)TaskWeekdays.Sunday, true)]
    [InlineData((int)(TaskWeekdays.Monday | TaskWeekdays.Tuesday), false)]
    [InlineData(0, false)]
    [InlineData((int)TaskWeekdays.All + 1, false)]
    public void HasValidSchedule_RepeatWeekStart_AcceptsOnlyExactlyOneWeekdayFlagOrNull(int? weekStart, bool expectedValid)
    {
        var model = CreateTaskModel(TaskId) with
        {
            RepeatUnit = (int)TaskRepeatUnit.Week,
            RepeatInterval = 2,
            RepeatWeekStart = weekStart
        };

        Assert.Equal(expectedValid, model.HasValidSchedule());
    }

    [Fact]
    public void HasValidSchedule_RepeatWeekStartNull_IsValid()
    {
        var model = CreateTaskModel(TaskId) with
        {
            RepeatUnit = (int)TaskRepeatUnit.Week,
            RepeatInterval = 2,
            RepeatWeekStart = null
        };

        Assert.True(model.HasValidSchedule());
    }

    [Fact]
    public void TaskDataTransferModel_RoundTrip_PreservesRepeatEndDate()
    {
        var entity = TaskExtension.MapToEntity(new TaskDto
        {
            Id = TaskId,
            Note = "Notiz",
            TaskDate = new DateTime(2026, 3, 10),
            RepeatUnit = TaskRepeatUnit.Day,
            RepeatEndDate = new DateTime(2026, 6, 1)
        });

        var model = TaskDataTransferModel.FromEntity(entity);
        var restored = model.ToEntity();

        Assert.Equal(new DateTime(2026, 6, 1), model.RepeatEndDate);
        Assert.Equal(new DateTime(2026, 6, 1), restored.RepeatEndDate);
    }

    [Fact]
    public void HasValidSchedule_RepeatEndDateNull_IsValid()
    {
        var model = CreateTaskModel(TaskId) with
        {
            RepeatUnit = (int)TaskRepeatUnit.Day,
            RepeatEndDate = null
        };

        Assert.True(model.HasValidSchedule());
    }

    [Fact]
    public void HasValidSchedule_RepeatEndDateOnOrAfterTaskDate_IsValid()
    {
        var model = CreateTaskModel(TaskId) with
        {
            RepeatUnit = (int)TaskRepeatUnit.Day,
            RepeatEndDate = new DateTime(2026, 3, 10)
        };

        Assert.True(model.HasValidSchedule());
    }

    [Fact]
    public void HasValidSchedule_RepeatEndDateBeforeTaskDate_IsInvalid()
    {
        var model = CreateTaskModel(TaskId) with
        {
            RepeatUnit = (int)TaskRepeatUnit.Day,
            RepeatEndDate = new DateTime(2026, 3, 9)
        };

        Assert.False(model.HasValidSchedule());
    }

    [Fact]
    public void TaskDataTransferModel_ToEntityOfTemplate_ClearsTaskDate()
    {
        var model = CreateTaskModel(TaskId) with { IsTemplate = true, TaskDate = new DateTime(2026, 3, 10) };

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

    [Fact]
    public void FromEntity_DoneTaskWithCompletedAt_ExportsCompletedAt()
    {
        var completedAt = new DateTime(2026, 3, 11, 17, 45, 0);
        var entity = new TaskEntity { Id = TaskId, IsDone = true, TaskDate = new DateTime(2026, 3, 10), CompletedAt = completedAt };

        var model = TaskDataTransferModel.FromEntity(entity);

        Assert.Equal(completedAt, model.CompletedAt);
    }

    [Fact]
    public void Serialize_TaskWithoutCompletedAt_WritesNoCompletedAtField()
    {
        var payload = CreateValidPayload();

        var json = JsonSerializer.Serialize(payload);

        Assert.DoesNotContain("CompletedAt", json);
    }

    [Fact]
    public void Deserialize_ExportWithCompletedAt_RoundTripsTimestampIntoEntity()
    {
        var completedAt = new DateTime(2026, 3, 11, 17, 45, 0);
        var payload = CreateValidPayload();
        payload.Tasks = [CreateTaskModel(TaskId) with { IsDone = true, CompletedAt = completedAt }];

        var json = JsonSerializer.Serialize(payload);
        var restored = JsonSerializer.Deserialize<DataTransferPayload>(json);

        Assert.Contains("CompletedAt", json);
        Assert.True(restored!.IsValid());
        var entity = Assert.Single(restored.CreateTaskEntities());
        Assert.Equal(completedAt, entity.CompletedAt);
    }

    [Fact]
    public void Deserialize_ExportFromOlderAppVersionWithoutCompletedAt_ImportsAsLegacyDoneTask()
    {
        const string oldTaskJson = """
            {"Id":"3f2504e0-4f89-11d3-9a0c-0305e82c3301","IsDone":true,"Note":"Alt","IsRegularTask":false,
             "IsWorkTask":true,"IsPositive":false,"CreationDate":"2026-03-01T14:30:00","TaskDate":"2026-03-10T00:00:00",
             "Priority":2,"BreakMinutes":5,"TimerMinutes":25}
            """;
        var model = JsonSerializer.Deserialize<TaskDataTransferModel>(oldTaskJson);

        var entity = model!.ToEntity();

        Assert.Null(entity.CompletedAt);
        Assert.True(entity.IsDone);
        Assert.Equal(new DateTime(2026, 3, 10), TaskExtension.GetEffectiveCompletedDate(TaskExtension.MapToDto(entity)));
    }

    [Fact]
    public void Deserialize_ExportFromOlderAppVersionWithRegularTaskFlag_ImportsAsTemplateWithoutDate()
    {
        const string oldTaskJson = """
            {"Id":"3f2504e0-4f89-11d3-9a0c-0305e82c3301","IsDone":false,"Note":"Alt","IsRegularTask":true,
             "IsWorkTask":true,"IsPositive":false,"CreationDate":"2026-03-01T14:30:00","TaskDate":"2026-03-10T00:00:00",
             "Priority":2,"BreakMinutes":5,"TimerMinutes":25}
            """;
        var model = JsonSerializer.Deserialize<TaskDataTransferModel>(oldTaskJson);

        var entity = model!.ToEntity();

        Assert.True(entity.IsTemplate);
        Assert.Null(entity.TaskDate);
    }

    [Fact]
    public void ToEntity_OpenTaskWithCompletedAtInFile_DropsCompletedAt()
    {
        var model = CreateTaskModel(TaskId) with { IsDone = false, CompletedAt = new DateTime(2026, 3, 11) };

        var entity = model.ToEntity();

        Assert.Null(entity.CompletedAt);
    }

    [Fact]
    public void SettingModel_FromEntityAndBack_KeepsRewarding()
    {
        var entity = new SettingEntity
        {
            Id = SettingId,
            Theme = 1,
            Language = 0,
            RingtoneId = RingtoneId,
            MicroFeedbackEnabled = false,
            ReviewsEnabled = false,
            CompanionEnabled = true,
            BalanceGoal = (int)BalanceGoal.ThreeToTwo
        };

        var restored = SettingDataTransferModel.FromEntity(entity).ToEntity();

        Assert.False(restored.MicroFeedbackEnabled);
        Assert.False(restored.ReviewsEnabled);
        Assert.True(restored.CompanionEnabled);
        Assert.Equal((int)BalanceGoal.ThreeToTwo, restored.BalanceGoal);
    }

    [Fact]
    public void Deserialize_SwitchedOffRewarding_StayOffAfterJsonRoundTrip()
    {
        var payload = CreateValidPayload();
        payload.Settings = [new SettingDataTransferModel(SettingId, 0, 0, RingtoneId, false, false, false, 0)];

        var json = JsonSerializer.Serialize(payload);
        var restored = JsonSerializer.Deserialize<DataTransferPayload>(json);

        var setting = Assert.Single(restored!.GetSettingsForImport());
        Assert.False(setting.MicroFeedbackEnabled);
        Assert.False(setting.ReviewsEnabled);
        Assert.False(setting.CompanionEnabled);
    }

    [Fact]
    public void Deserialize_SettingFromOlderAppVersionWithoutRewarding_GetsDefaults()
    {
        var oldJson = "{\"Id\":\"" + SettingId + "\",\"Theme\":2,\"Language\":1,\"RingtoneId\":\"" + RingtoneId + "\"}";

        var model = JsonSerializer.Deserialize<SettingDataTransferModel>(oldJson);

        var entity = model!.ToEntity();
        Assert.True(entity.MicroFeedbackEnabled);
        Assert.True(entity.ReviewsEnabled);
        Assert.True(entity.CompanionEnabled);
        Assert.Equal((int)BalanceGoal.OneToOne, entity.BalanceGoal);
        Assert.Equal((int)AppColorTheme.Blue, entity.Theme);
        Assert.Equal((int)AppColorMode.Dark, entity.ColorMode);
    }

    [Theory]
    [InlineData(1, (int)BalanceGoal.Custom, 1, 2)]
    [InlineData(2, (int)BalanceGoal.Custom, 1, 3)]
    [InlineData(3, (int)BalanceGoal.OneToOne, 1, 1)]
    public void ToEntity_ImportedRemovedBalanceGoal_BecomesTheNearestRemainingOne(int stored, int goal, int forYou, int toDo)
    {
        var entity = new SettingDataTransferModel(SettingId, 0, 0, RingtoneId, BalanceGoal: stored).ToEntity();

        Assert.Equal(goal, entity.BalanceGoal);
        Assert.Equal(forYou, entity.CustomForYou);
        Assert.Equal(toDo, entity.CustomToDo);
    }

    [Fact]
    public void IsValid_UnknownOnboardingState_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.Settings = [new SettingDataTransferModel(SettingId, 0, 0, RingtoneId, OnboardingState: 9)];

        Assert.False(payload.IsValid());
    }

    [Fact]
    public void Setting_OldExportWithoutOnboardingState_CountsAsCompletedSoImportBringsNoIntroduction()
    {
        var restored = new SettingDataTransferModel(SettingId, 0, 0, RingtoneId).ToEntity();

        Assert.Equal(1, restored.OnboardingState);
    }

    [Fact]
    public void Setting_OnboardingState_SurvivesExportAndImport()
    {
        var entity = new SettingEntity { Id = SettingId, RingtoneId = RingtoneId, OnboardingState = 2 };

        var restored = SettingDataTransferModel.FromEntity(entity).ToEntity();

        Assert.Equal(2, restored.OnboardingState);
    }

    [Fact]
    public void Deserialize_OldExportWithRemovedReminderFields_IgnoresThemAndKeepsTheRest()
    {
        var oldJson = "{\"Id\":\"" + SettingId + "\",\"Theme\":2,\"Language\":1,\"RingtoneId\":\"" + RingtoneId + "\",\"RemindersEnabled\":true,\"ReminderMode\":1,\"ReminderMinutes\":450}";

        var model = JsonSerializer.Deserialize<SettingDataTransferModel>(oldJson);

        var entity = model!.ToEntity();
        Assert.Equal((int)AppColorTheme.Blue, entity.Theme);
        Assert.Equal((int)AppColorMode.Dark, entity.ColorMode);
        Assert.Equal(1, entity.Language);
        Assert.Equal(RingtoneId, entity.RingtoneId);
    }

    [Fact]
    public void IsValid_WithoutSuggestions_ReturnsTrue()
    {
        var payload = CreateValidPayload();
        payload.ForYouSuggestions = null;

        Assert.True(payload.IsValid());
    }

    [Fact]
    public void IsValid_ValidSuggestions_ReturnsTrue()
    {
        var payload = CreateValidPayload();
        payload.ForYouSuggestions = [new ForYouSuggestionDataTransferModel(Guid.NewGuid(), "Lesen", DateTime.UtcNow)];

        Assert.True(payload.IsValid());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_SuggestionWithoutText_ReturnsFalse(string text)
    {
        var payload = CreateValidPayload();
        payload.ForYouSuggestions = [new ForYouSuggestionDataTransferModel(Guid.NewGuid(), text, DateTime.UtcNow)];

        Assert.False(payload.IsValid());
    }

    [Fact]
    public void IsValid_HiddenBuiltInSuggestionWithoutText_ReturnsTrue()
    {
        var payload = CreateValidPayload();
        payload.ForYouSuggestions = [new ForYouSuggestionDataTransferModel(Guid.NewGuid(), string.Empty, DateTime.UtcNow, "ForYouPoolMovement1", IsDeleted: true)];

        Assert.True(payload.IsValid());
    }

    [Fact]
    public void IsValid_DeletedSuggestionWithoutPoolKeyOrTooLongPoolKey_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.ForYouSuggestions = [new ForYouSuggestionDataTransferModel(Guid.NewGuid(), string.Empty, DateTime.UtcNow, IsDeleted: true)];
        Assert.False(payload.IsValid());

        var poolKey = new string('k', ForYouSuggestionEntity.MaximumPoolKeyLength + 1);
        payload.ForYouSuggestions = [new ForYouSuggestionDataTransferModel(Guid.NewGuid(), "Lesen", DateTime.UtcNow, poolKey)];
        Assert.False(payload.IsValid());
    }

    [Fact]
    public void ToEntity_BuiltInChange_KeepsPoolKeyAndDeletedFlag()
    {
        var model = new ForYouSuggestionDataTransferModel(Guid.NewGuid(), "Neu", DateTime.UtcNow, "ForYouPoolCalm2", IsDeleted: true);

        var entity = ForYouSuggestionDataTransferModel.FromEntity(model.ToEntity()).ToEntity();

        Assert.Equal("ForYouPoolCalm2", entity.PoolKey);
        Assert.True(entity.IsDeleted);
    }

    [Fact]
    public void IsValid_SuggestionTooLong_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        var text = new string('a', ForYouSuggestionEntity.MaximumTextLength + 1);
        payload.ForYouSuggestions = [new ForYouSuggestionDataTransferModel(Guid.NewGuid(), text, DateTime.UtcNow)];

        Assert.False(payload.IsValid());
    }

    [Fact]
    public void IsValid_DuplicateOrEmptySuggestionIds_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var payload = CreateValidPayload();
        payload.ForYouSuggestions =
        [
            new ForYouSuggestionDataTransferModel(id, "Lesen", DateTime.UtcNow),
            new ForYouSuggestionDataTransferModel(id, "Malen", DateTime.UtcNow)
        ];
        Assert.False(payload.IsValid());

        payload.ForYouSuggestions = [new ForYouSuggestionDataTransferModel(Guid.Empty, "Lesen", DateTime.UtcNow)];
        Assert.False(payload.IsValid());
    }

    [Fact]
    public void IsValid_UnknownBalanceGoal_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.Settings = [new SettingDataTransferModel(SettingId, 0, 0, RingtoneId, BalanceGoal: 99)];

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void IsValid_KnownBalanceGoal_ReturnsTrue(int goal)
    {
        var payload = CreateValidPayload();
        payload.Settings = [new SettingDataTransferModel(SettingId, 0, 0, RingtoneId, BalanceGoal: goal)];

        var result = payload.IsValid();

        Assert.True(result);
    }

    [Fact]
    public void IsValid_NullSuggestionEntry_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        payload.ForYouSuggestions = [null!];

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Theory]
    [InlineData(AppRewardingSettings.DefaultCustomPart - 1, AppRewardingSettings.DefaultCustomPart, false)]
    [InlineData(AppRewardingSettings.MaximumCustomPart + 1, AppRewardingSettings.DefaultCustomPart, false)]
    [InlineData(AppRewardingSettings.DefaultCustomPart, AppRewardingSettings.DefaultCustomPart - 1, false)]
    [InlineData(AppRewardingSettings.DefaultCustomPart, AppRewardingSettings.MaximumCustomPart + 1, false)]
    [InlineData(AppRewardingSettings.DefaultCustomPart, AppRewardingSettings.MaximumCustomPart, true)]
    [InlineData(AppRewardingSettings.MaximumCustomPart, AppRewardingSettings.DefaultCustomPart, true)]
    public void IsValid_CustomRatioParts_MustBeWithinOneAndMaximum(int customForYou, int customToDo, bool expected)
    {
        var payload = CreateValidPayload();
        payload.Settings = [new SettingDataTransferModel(SettingId, 0, 0, RingtoneId, CustomForYou: customForYou, CustomToDo: customToDo)];

        var result = payload.IsValid();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromEntity_OpenTaskWithCompletedAt_DoesNotExportCompletedAt()
    {
        var completedAt = new DateTime(2026, 3, 11, 17, 45, 0);
        var entity = new TaskEntity { Id = TaskId, IsDone = false, TaskDate = new DateTime(2026, 3, 10), CompletedAt = completedAt };

        var model = TaskDataTransferModel.FromEntity(entity);

        Assert.Null(model.CompletedAt);
    }

    [Fact]
    public void TaskDataTransferModel_ToEntityOfNormalTaskWithoutTaskDate_KeepsTaskDateEmpty()
    {
        var model = CreateTaskModel(TaskId) with { TaskDate = null };

        var entity = model.ToEntity();

        Assert.Null(entity.TaskDate);
    }

    [Fact]
    public void IsValid_TaskWithTimeReminderAndRepetition_ReturnsTrue()
    {
        var payload = CreateValidPayload();
        var task = CreateTaskModel(TaskId) with
        {
            TaskTime = new TimeSpan(9, 30, 0),
            ReminderMinutesBefore = 15,
            RepeatUnit = (int)TaskRepeatUnit.Week,
            RepeatInterval = 2,
            RepeatWeekdays = (int)(TaskWeekdays.Monday | TaskWeekdays.Friday)
        };
        payload.Tasks = [task];

        var result = payload.IsValid();

        Assert.True(result);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(23 * 60 + 59, true)]
    [InlineData(24 * 60, false)]
    public void IsValid_TaskTime_MustBeWithinTheDay(int minutes, bool expected)
    {
        var payload = CreateValidPayload();
        var task = CreateTaskModel(TaskId) with { TaskTime = TimeSpan.FromMinutes(minutes) };
        payload.Tasks = [task];

        var result = payload.IsValid();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(AppConstants.MaximumReminderMinutes, true)]
    [InlineData(AppConstants.MaximumReminderMinutes + 1, false)]
    public void IsValid_ReminderMinutes_MustBeWithinTheMaximum(int minutes, bool expected)
    {
        var payload = CreateValidPayload();
        var task = CreateTaskModel(TaskId) with { ReminderMinutesBefore = minutes };
        payload.Tasks = [task];

        var result = payload.IsValid();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(-1, false)]
    public void IsValid_RepeatUnit_MustBeKnown(int unit, bool expected)
    {
        var payload = CreateValidPayload();
        var task = CreateTaskModel(TaskId) with { RepeatUnit = unit };
        payload.Tasks = [task];

        var result = payload.IsValid();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(99, true)]
    [InlineData(100, false)]
    public void IsValid_RepeatInterval_MustBeWithinTheRange(int interval, bool expected)
    {
        var payload = CreateValidPayload();
        var task = CreateTaskModel(TaskId) with { RepeatInterval = interval };
        payload.Tasks = [task];

        var result = payload.IsValid();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValid_RepeatWeekdaysWithUnknownBit_ReturnsFalse()
    {
        var payload = CreateValidPayload();
        var task = CreateTaskModel(TaskId) with { RepeatWeekdays = 128 };
        payload.Tasks = [task];

        var result = payload.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void TaskDataTransferModel_ExportOfAnOldFileWithoutScheduleFields_ImportsWithDefaults()
    {
        var model = CreateTaskModel(TaskId);

        var entity = model.ToEntity();

        Assert.Null(entity.TaskTime);
        Assert.Null(entity.ReminderMinutesBefore);
        Assert.Equal(TaskRepeatUnit.None, entity.RepeatUnit);
        Assert.Equal(1, entity.RepeatInterval);
        Assert.Null(entity.SeriesId);
    }

    [Fact]
    public void TaskDataTransferModel_ScheduleFields_SurviveExportAndImport()
    {
        var seriesId = Guid.NewGuid();
        var entity = new TaskEntity
        {
            Id = TaskId,
            TaskDate = new DateTime(2026, 3, 10),
            TaskTime = new TimeSpan(18, 15, 0),
            ReminderMinutesBefore = 30,
            RepeatUnit = TaskRepeatUnit.Week,
            RepeatInterval = 2,
            RepeatWeekdays = TaskWeekdays.Tuesday | TaskWeekdays.Saturday,
            SeriesId = seriesId
        };

        var model = TaskDataTransferModel.FromEntity(entity);
        var json = JsonSerializer.Serialize(model);
        var restoredModel = JsonSerializer.Deserialize<TaskDataTransferModel>(json)!;
        var restored = restoredModel.ToEntity();

        Assert.Equal(new TimeSpan(18, 15, 0), restored.TaskTime);
        Assert.Equal(30, restored.ReminderMinutesBefore);
        Assert.Equal(TaskRepeatUnit.Week, restored.RepeatUnit);
        Assert.Equal(2, restored.RepeatInterval);
        Assert.Equal(TaskWeekdays.Tuesday | TaskWeekdays.Saturday, restored.RepeatWeekdays);
        Assert.Equal(seriesId, restored.SeriesId);
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
