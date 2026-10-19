using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
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

    public IReadOnlyList<SettingDataTransferModel> GetSettingsForImport()
        => Settings ?? LegacySettings ?? [];

    public bool IsValid()
    {
        if (Tasks is null || FocusTimers is null || Ringtones is null ||
            Settings is not null && LegacySettings is not null)
        {
            return false;
        }

        var settings = GetSettingsForImport();
        if (settings.Count == 0 || Ringtones.Count == 0 ||
            Tasks.Any(task => task is null) ||
            FocusTimers.Any(timer => timer is null) ||
            Ringtones.Any(ringtone => ringtone is null) ||
            settings.Any(setting => setting is null))
        {
            return false;
        }

        if (HasInvalidOrDuplicateIds(Tasks.Select(task => task.Id)) ||
            HasInvalidOrDuplicateIds(FocusTimers.Select(timer => timer.Id)) ||
            HasInvalidOrDuplicateIds(Ringtones.Select(ringtone => ringtone.Id)) ||
            HasInvalidOrDuplicateIds(settings.Select(setting => setting.Id)))
        {
            return false;
        }

        var taskIds = Tasks.Select(task => task.Id).ToHashSet();
        var ringtoneIds = Ringtones.Select(ringtone => ringtone.Id).ToHashSet();

        return settings.All(setting =>
                   ringtoneIds.Contains(setting.RingtoneId) &&
                   IsSupportedTheme(setting.Theme) &&
                   Enum.IsDefined((AppLanguage)setting.Language)) &&
               Ringtones.All(ringtone => !string.IsNullOrWhiteSpace(ringtone.RingtoneFilePath)) &&
               Tasks.All(task =>
                   Enum.IsDefined(task.Priority) &&
                   IsValidDuration(task.BreakMinutes) &&
                   IsValidDuration(task.TimerMinutes)) &&
               FocusTimers.All(timer =>
                   (!timer.TaskId.HasValue || taskIds.Contains(timer.TaskId.Value)) &&
                   IsValidDuration(timer.TimerMinutes));
    }

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

    private static bool IsValidDuration(int minutes) => minutes is >= 0 and <= MaximumTimerMinutes;

    // Persisted MAUI AppTheme values: Unspecified, Light and Dark.
    private static bool IsSupportedTheme(int theme) => theme is >= 0 and <= 2;
}

public sealed record SettingDataTransferModel(Guid Id, int Theme, int Language, Guid RingtoneId)
{
    public static SettingDataTransferModel FromEntity(SettingEntity setting)
        => new(setting.Id, setting.Theme, setting.Language, setting.RingtoneId);

    public SettingEntity ToEntity() => new()
    {
        Id = Id,
        Theme = Theme,
        Language = Language,
        RingtoneId = RingtoneId
    };
}

public sealed record TaskDataTransferModel(
    Guid Id,
    bool IsDone,
    string? Note,
    bool IsRegularTask,
    bool IsWorkTask,
    bool IsPositive,
    DateTime CreationDate,
    DateTime? TaskDate,
    TaskPriority Priority,
    int BreakMinutes,
    int TimerMinutes)
{
    public static TaskDataTransferModel FromEntity(TaskEntity task)
        => new(
            task.Id,
            task.IsDone,
            task.Note,
            task.IsRegularTask,
            task.IsWorkTask,
            task.IsPositive,
            task.CreationDate,
            task.TaskDate,
            task.Priority,
            task.BreakMinutes,
            task.TimerMinutes);

    public TaskEntity ToEntity() => new()
    {
        Id = Id,
        IsDone = IsDone,
        Note = Note,
        IsRegularTask = IsRegularTask,
        IsWorkTask = IsWorkTask,
        IsPositive = IsPositive,
        CreationDate = CreationDate,
        TaskDate = IsRegularTask ? null : TaskDate?.Date,
        Priority = Priority,
        BreakMinutes = BreakMinutes,
        TimerMinutes = TimerMinutes
    };
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
