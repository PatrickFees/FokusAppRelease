using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Services;
using System.Text.Json.Serialization;

namespace FokusApp.Core.Models;

public sealed class DataTransferPayload
{
    public const int MaximumTimerMinutes = 120;

    public DateTime ExportedAtUtc { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SettingDataTransferModel>? Settings { get; set; }

    [JsonPropertyName("Users")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SettingDataTransferModel>? LegacySettings { get; set; }

    public List<TaskDataTransferModel>? Tasks { get; set; }
    public List<FocusTimerDataTransferModel>? FocusTimers { get; set; }
    public List<RingtoneDataTransferModel>? Ringtones { get; set; }

    public List<ForYouSuggestionDataTransferModel>? ForYouSuggestions { get; set; }

    public IReadOnlyList<SettingDataTransferModel> GetSettingsForImport()
        => Settings ?? LegacySettings ?? [];

    public bool IsValid()
    {
        if (Tasks is null || FocusTimers is null || Ringtones is null ||
            Settings is not null && LegacySettings is not null)
        {
            return false;
        }

        return AreTasksValid() &&
               AreRingtonesValid(out var ringtoneIds) &&
               AreFocusTimersValid() &&
               AreSettingsValid(ringtoneIds);
    }

    private bool AreTasksValid()
    {
        var tasks = Tasks!;
        if (tasks.Any(task => task is null) || HasInvalidOrDuplicateIds(tasks.Select(task => task.Id)))
        {
            return false;
        }

        return tasks.All(task =>
            Enum.IsDefined(task.Priority) &&
            IsValidDuration(task.BreakMinutes) &&
            IsValidDuration(task.TimerMinutes) &&
            task.HasValidSchedule());
    }

    private bool AreFocusTimersValid()
    {
        var focusTimers = FocusTimers!;
        if (focusTimers.Any(timer => timer is null) || HasInvalidOrDuplicateIds(focusTimers.Select(timer => timer.Id)))
        {
            return false;
        }

        var taskIds = Tasks!.Select(task => task.Id).ToHashSet();
        return focusTimers.All(timer =>
            (!timer.TaskId.HasValue || taskIds.Contains(timer.TaskId.Value)) &&
            IsValidDuration(timer.TimerMinutes));
    }

    private bool AreRingtonesValid(out HashSet<Guid> ringtoneIds)
    {
        var ringtones = Ringtones!;
        if (ringtones.Count == 0 || ringtones.Any(ringtone => ringtone is null) ||
            HasInvalidOrDuplicateIds(ringtones.Select(ringtone => ringtone.Id)))
        {
            ringtoneIds = [];
            return false;
        }

        ringtoneIds = ringtones.Select(ringtone => ringtone.Id).ToHashSet();
        return ringtones.All(ringtone => !string.IsNullOrWhiteSpace(ringtone.RingtoneFilePath));
    }

    private bool AreSettingsValid(HashSet<Guid> ringtoneIds)
    {
        var settings = GetSettingsForImport();
        if (settings.Count == 0 || settings.Any(setting => setting is null) ||
            HasInvalidOrDuplicateIds(settings.Select(setting => setting.Id)))
        {
            return false;
        }

        if (!AreForYouSuggestionsValid())
        {
            return false;
        }

        return settings.All(setting =>
            ringtoneIds.Contains(setting.RingtoneId) &&
            IsSupportedTheme(setting.Theme) &&
            Enum.IsDefined((AppColorMode)setting.ColorMode) &&
            Enum.IsDefined((AppLanguage)setting.Language) &&
            AppRewardingSettings.IsKnownStoredGoal(setting.BalanceGoal) &&
            IsValidCustomPart(setting.CustomForYou) &&
            IsValidCustomPart(setting.CustomToDo) &&
            Enum.IsDefined((OnboardingState)setting.OnboardingState) &&
            Enum.IsDefined((DefaultScope)setting.DefaultScope));
    }

    private bool AreForYouSuggestionsValid() =>
        ForYouSuggestions is null ||
        (ForYouSuggestions.All(suggestion => suggestion is not null && suggestion.IsValid()) &&
         !HasInvalidOrDuplicateIds(ForYouSuggestions.Select(suggestion => suggestion.Id)));

    public IEnumerable<TaskEntity> CreateTaskEntities()
    {
        var legacyTimerMinutesByTaskId = FocusTimers!
            .Where(timer => timer.TaskId.HasValue)
            .GroupBy(timer => timer.TaskId!.Value)
            .ToDictionary(group => group.Key, group => group.First().TimerMinutes);

        foreach (var taskModel in Tasks!)
        {
            var task = taskModel.ToEntity();
            if (task.TimerMinutes == 0 && legacyTimerMinutesByTaskId.TryGetValue(task.Id, out var legacyTimerMinutes))
            {
                task.TimerMinutes = legacyTimerMinutes;
            }

            yield return task;
        }
    }

    private static bool HasInvalidOrDuplicateIds(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        return idList.Any(id => id == Guid.Empty) || idList.Distinct().Count() != idList.Count;
    }

    private static bool IsValidCustomPart(int part) =>
        part is >= AppRewardingSettings.DefaultCustomPart and <= AppRewardingSettings.MaximumCustomPart;

    private static bool IsValidDuration(int minutes) => minutes is >= 0 and <= MaximumTimerMinutes;

    private static bool IsSupportedTheme(int theme) =>
        Enum.IsDefined((AppColorTheme)theme) || theme == SettingDataTransferModel.LegacyDarkTheme || theme is 0 or 4;
}

public sealed record ForYouSuggestionDataTransferModel(
    Guid Id,
    string Text,
    DateTime CreatedAt,
    string? PoolKey = null,
    bool IsDeleted = false)
{
    public static ForYouSuggestionDataTransferModel FromEntity(ForYouSuggestionEntity suggestion)
        => new(suggestion.Id, suggestion.Text, suggestion.CreatedAt, suggestion.PoolKey, suggestion.IsDeleted);

    public bool IsValid()
    {
        var isHiddenBuiltIn = IsDeleted && PoolKey is not null;
        return Text.Length <= ForYouSuggestionEntity.MaximumTextLength &&
               (PoolKey is null || PoolKey.Length <= ForYouSuggestionEntity.MaximumPoolKeyLength) &&
               (isHiddenBuiltIn || !string.IsNullOrWhiteSpace(Text));
    }

    public ForYouSuggestionEntity ToEntity() => new()
    {
        Id = Id,
        Text = Text,
        CreatedAt = CreatedAt,
        PoolKey = PoolKey,
        IsDeleted = IsDeleted && PoolKey is not null
    };
}

public sealed record SettingDataTransferModel(
    Guid Id,
    int Theme,
    int Language,
    Guid RingtoneId,
    bool MicroFeedbackEnabled = true,
    bool ReviewsEnabled = true,
    bool CompanionEnabled = true,
    int BalanceGoal = 0,
    int CustomForYou = 1,
    int CustomToDo = 1,
    int OnboardingState = (int)Enums.OnboardingState.Completed,
    int DefaultScope = (int)Enums.DefaultScope.Both,
    int ColorMode = (int)AppColorMode.Light)
{

    internal const int LegacyDarkTheme = 2;

    public static SettingDataTransferModel FromEntity(SettingEntity setting)
        => new(
            setting.Id,
            setting.Theme,
            setting.Language,
            setting.RingtoneId,
            setting.MicroFeedbackEnabled,
            setting.ReviewsEnabled,
            setting.CompanionEnabled,
            setting.BalanceGoal,
            setting.CustomForYou,
            setting.CustomToDo,
            setting.OnboardingState,
            setting.DefaultScope,
            setting.ColorMode);

    public SettingEntity ToEntity()
    {
        var (goal, forYou, toDo) = AppRewardingSettings.ReadStoredGoal(BalanceGoal, CustomForYou, CustomToDo);

        return new SettingEntity
        {
            Id = Id,
            Theme = Enum.IsDefined((AppColorTheme)Theme) ? Theme : (int)AppColorTheme.Blue,
            ColorMode = Theme == LegacyDarkTheme ? (int)AppColorMode.Dark : ColorMode,
            Language = Language,
            RingtoneId = RingtoneId,
            MicroFeedbackEnabled = MicroFeedbackEnabled,
            ReviewsEnabled = ReviewsEnabled,
            CompanionEnabled = CompanionEnabled,
            BalanceGoal = (int)goal,
            CustomForYou = forYou,
            CustomToDo = toDo,
            OnboardingState = OnboardingState,
            DefaultScope = DefaultScope
        };
    }
}

public sealed record TaskDataTransferModel(
    Guid Id,
    bool IsDone,
    string? Note,
    bool IsTemplate,
    bool IsWorkTask,
    bool IsPositive,
    DateTime CreationDate,
    DateTime? TaskDate,
    TaskPriority Priority,
    int BreakMinutes,
    int TimerMinutes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? CompletedAt = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TimeSpan? TaskTime = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? ReminderMinutesBefore = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] int RepeatUnit = 0,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] int RepeatInterval = 1,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] int RepeatWeekdays = 0,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? SeriesId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? RepeatGeneratedUntil = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? RepeatWeekdayOrdinal = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? RepeatMonth = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? RepeatWeekStart = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? RepeatEndDate = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SourceCalendarEventId = null)
{

    [JsonPropertyName("IsRegularTask")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LegacyIsRegularTask { get; init; }

    private bool IsTemplateTask => IsTemplate || LegacyIsRegularTask == true;

    public static TaskDataTransferModel FromEntity(TaskEntity task)
        => new(
            task.Id,
            task.IsDone,
            task.Note,
            task.IsTemplate,
            task.IsWorkTask,
            task.IsPositive,
            task.CreationDate,
            task.TaskDate,
            task.Priority,
            task.BreakMinutes,
            task.TimerMinutes,
            task.IsDone ? task.CompletedAt : null,
            task.TaskTime,
            task.ReminderMinutesBefore,
            (int)task.RepeatUnit,
            task.RepeatInterval,
            (int)task.RepeatWeekdays,
            task.SeriesId,
            task.RepeatGeneratedUntil,
            task.RepeatWeekdayOrdinal,
            task.RepeatMonth,
            (int?)task.RepeatWeekStart,
            task.RepeatEndDate,
            task.SourceCalendarEventId);

    public bool HasValidSchedule()
    {
        return (TaskTime is null || TaskTime.Value >= TimeSpan.Zero && TaskTime.Value < TimeSpan.FromDays(1)) &&
            (ReminderMinutesBefore is null || ReminderMinutesBefore is >= 0 and <= AppConstants.MaximumReminderMinutes) &&
            Enum.IsDefined((TaskRepeatUnit)RepeatUnit) &&
            RepeatInterval is >= TaskRepeatCalculator.MinimumInterval and <= TaskRepeatCalculator.MaximumInterval &&
            ((TaskWeekdays)RepeatWeekdays & ~TaskWeekdays.All) == TaskWeekdays.None &&
            HasValidOrdinalRepeat() &&
            HasValidWeekStart() &&
            HasValidEndDate();
    }

    private bool HasValidEndDate() =>
        RepeatEndDate is null || TaskDate is not null && RepeatEndDate.Value.Date >= TaskDate.Value.Date;

    private bool HasValidWeekStart()
    {
        if (RepeatWeekStart is not { } value)
        {
            return true;
        }

        var flag = (TaskWeekdays)value;
        if ((flag & ~TaskWeekdays.All) != TaskWeekdays.None)
        {
            return false;
        }

        var bits = (int)flag;
        return bits != 0 && (bits & (bits - 1)) == 0;
    }

    private bool HasValidOrdinalRepeat()
    {
        var unit = (TaskRepeatUnit)RepeatUnit;
        if (unit is not (TaskRepeatUnit.MonthWeekday or TaskRepeatUnit.YearWeekday))
        {
            return RepeatWeekdayOrdinal is null && RepeatMonth is null;
        }

        var hasValidOrdinal = RepeatWeekdayOrdinal.HasValue && TaskRepeatCalculator.IsValidWeekdayOrdinal(RepeatWeekdayOrdinal.Value);
        var hasValidMonth = unit == TaskRepeatUnit.YearWeekday
            ? RepeatMonth is >= 1 and <= 12
            : RepeatMonth is null;

        return hasValidOrdinal && hasValidMonth;
    }

    public TaskEntity ToEntity() => TaskExtension.MapToEntity(new TaskDto
    {
        Id = Id,
        IsDone = IsDone,
        Note = Note,
        IsTemplate = IsTemplateTask,
        IsWorkTask = IsWorkTask,
        IsPositive = IsPositive,
        CreationDate = CreationDate,
        TaskDate = IsTemplateTask ? null : TaskDate?.Date,
        Priority = Priority,
        BreakMinutes = BreakMinutes,
        TimerMinutes = TimerMinutes,
        CompletedAt = IsDone ? CompletedAt : null,
        TaskTime = TaskTime,
        ReminderMinutesBefore = ReminderMinutesBefore,
        RepeatUnit = (TaskRepeatUnit)RepeatUnit,
        RepeatInterval = RepeatInterval,
        RepeatWeekdays = (TaskWeekdays)RepeatWeekdays,
        SeriesId = SeriesId,
        RepeatGeneratedUntil = RepeatGeneratedUntil,
        RepeatWeekdayOrdinal = RepeatWeekdayOrdinal,
        RepeatMonth = RepeatMonth,
        RepeatWeekStart = (TaskWeekdays?)RepeatWeekStart,
        RepeatEndDate = RepeatEndDate,
        SourceCalendarEventId = SourceCalendarEventId
    });
}

public sealed record FocusTimerDataTransferModel(Guid Id, DateTime TimeSet, Guid? TaskId)
{
    public int TimerMinutes => (int)Math.Round(TimeSet.TimeOfDay.TotalMinutes);
}

public sealed record RingtoneDataTransferModel(Guid Id, string? RingtoneFilePath)
{
    public static RingtoneDataTransferModel FromEntity(RingtonesEntity ringtone)
        => new(ringtone.Id, ringtone.RingtoneFilePath);

    public RingtonesEntity ToEntity() => new()
    {
        Id = Id,
        RingtoneFilePath = RingtoneFilePath
    };
}
