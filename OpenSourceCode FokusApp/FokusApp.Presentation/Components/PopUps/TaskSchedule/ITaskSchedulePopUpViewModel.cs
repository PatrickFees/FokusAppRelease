using FokusApp.Core.Enums;
using System.ComponentModel;

namespace FokusApp.Components.PopUps.TaskSchedule;

public interface ITaskSchedulePopUpViewModel : INotifyPropertyChanged
{

    bool HasTime { get; }
    int Hour { get; }
    int Minute { get; }
    string TimeDisplay { get; }

    int? ReminderMinutesBefore { get; }
    bool CanSetReminder { get; }
    IReadOnlyList<int> ReminderOptions { get; }

    bool CanRepeat { get; }
    TaskRepeatUnit RepeatUnit { get; }
    int RepeatInterval { get; }
    TaskWeekdays RepeatWeekdays { get; }
    bool IsRepeating { get; }
    bool IsWeeklyRepeat { get; }

    bool IsMultiWeekRepeat { get; }

    bool IsOrdinalWeekdayRepeat { get; }

    bool IsYearlyOrdinalWeekdayRepeat { get; }

    int RepeatWeekdayOrdinal { get; }

    int RepeatMonth { get; }

    TaskWeekdays RepeatWeekStart { get; }

    bool HasRepeatEndDate { get; }

    DateTime RepeatEndDate { get; }

    void Open(TaskScheduleModel initial, DateTime taskDate, bool canRepeat);
    void SetHour(int hour);
    void SetMinute(int minute);
    void ClearTime();
    void SetReminder(int? minutesBefore);
    void SetRepeatUnit(TaskRepeatUnit unit);
    void SetRepeatInterval(int interval);
    void StepRepeatInterval(int delta);
    void ToggleWeekday(TaskWeekdays weekday);
    bool IsWeekdaySelected(TaskWeekdays weekday);

    void SetOrdinalWeekday(TaskWeekdays weekday);
    void SetRepeatWeekdayOrdinal(int ordinal);
    void SetRepeatMonth(int month);

    void SetRepeatWeekStart(TaskWeekdays weekday);

    void SetRepeatEndDate(DateTime date);

    void ClearRepeatEndDate();
    TaskScheduleModel CreateResult();
}
