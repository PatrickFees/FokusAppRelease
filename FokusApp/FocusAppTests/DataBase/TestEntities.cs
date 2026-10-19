using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.ErrorLogging.Entities;
using FokusApp.Core.Enums;

namespace FocusAppTests.DataBase;

public static class TestEntities
{
    public static readonly DateTime BaseDate = new(2026, 3, 10, 8, 0, 0);

    public static TaskEntity CreateTask(
        bool isDone = false,
        bool isRegular = false,
        bool isWork = true,
        int creationOffsetMinutes = 0)
    {
        var creationDate = BaseDate.AddMinutes(creationOffsetMinutes);
        DateTime? taskDate = isRegular ? null : BaseDate.Date;

        return new TaskEntity
        {
            Id = Guid.NewGuid(),
            IsDone = isDone,
            Note = "Notiz",
            IsRegularTask = isRegular,
            IsWorkTask = isWork,
            IsPositive = true,
            CreationDate = creationDate,
            TaskDate = taskDate,
            Priority = TaskPriority.Dringend,
            BreakMinutes = 7,
            TimerMinutes = 25
        };
    }

    public static RingtonesEntity CreateRingtone(string? filePath = "Raw/Klingelton.mp3")
    {
        return new RingtonesEntity
        {
            Id = Guid.NewGuid(),
            RingtoneFilePath = filePath
        };
    }

    public static SettingEntity CreateSetting(Guid ringtoneId, int theme = 1, int language = 1)
    {
        return new SettingEntity
        {
            Id = Guid.NewGuid(),
            Theme = theme,
            Language = language,
            RingtoneId = ringtoneId,
            Ringtone = null!
        };
    }

    public static AppErrorLogEntity CreateErrorLog(int occurredOffsetMinutes = 0)
    {
        var occurredAt = DateTime.SpecifyKind(BaseDate.AddMinutes(occurredOffsetMinutes), DateTimeKind.Utc);

        return new AppErrorLogEntity
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            Source = "Quelle",
            ExceptionType = "System.InvalidOperationException",
            Message = "Fehlertext",
            StackTrace = "Stack",
            InnerException = "Inner",
            Route = "/route",
            Platform = "Windows",
            OsVersion = "11",
            AppVersion = "1.0",
            IsFatal = true,
            IsUiFailure = true,
            ThreadId = 4,
            PayloadJson = "{}"
        };
    }
}
