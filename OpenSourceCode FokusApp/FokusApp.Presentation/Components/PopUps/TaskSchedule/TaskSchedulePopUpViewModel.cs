using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Core;
using FokusApp.Core.Enums;
using FokusApp.Core.Services;

namespace FokusApp.Components.PopUps.TaskSchedule;

public sealed partial class TaskSchedulePopUpViewModel : ObservableObject, ITaskSchedulePopUpViewModel
{
    private const int DefaultHour = 9;

    private TaskWeekdays _taskDateWeekday = TaskWeekdays.Monday;
    private DateTime _taskDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReminderMinutesBefore))]
    [NotifyPropertyChangedFor(nameof(CanSetReminder))]
    private bool hasTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeDisplay))]
    private int hour = DefaultHour;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeDisplay))]
    private int minute;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReminderMinutesBefore))]
    private int? reminderMinutesBeforeRaw;

    [ObservableProperty]
    private bool canRepeat = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RepeatUnit))]
    [NotifyPropertyChangedFor(nameof(IsRepeating))]
    [NotifyPropertyChangedFor(nameof(IsWeeklyRepeat))]
    [NotifyPropertyChangedFor(nameof(IsMultiWeekRepeat))]
    [NotifyPropertyChangedFor(nameof(IsOrdinalWeekdayRepeat))]
    [NotifyPropertyChangedFor(nameof(IsYearlyOrdinalWeekdayRepeat))]
    private TaskRepeatUnit repeatUnitValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMultiWeekRepeat))]
    private int repeatInterval = 1;

    [ObservableProperty]
    private TaskWeekdays repeatWeekdays;

    [ObservableProperty]
    private int repeatWeekdayOrdinal = 1;

    [ObservableProperty]
    private int repeatMonth = 1;

    [ObservableProperty]
    private TaskWeekdays repeatWeekStart = TaskWeekdays.Monday;

    [ObservableProperty]
    private bool hasRepeatEndDate;

    [ObservableProperty]
    private DateTime repeatEndDate;

    [ObservableProperty]
    private IReadOnlyList<int> reminderOptions = AppConstants.ReminderMinuteOptions;

    public string TimeDisplay => $"{Hour:00}:{Minute:00}";
    public int? ReminderMinutesBefore => HasTime ? ReminderMinutesBeforeRaw : null;
    public bool CanSetReminder => HasTime;
    public TaskRepeatUnit RepeatUnit => CanRepeat ? RepeatUnitValue : TaskRepeatUnit.None;
    public bool IsRepeating => RepeatUnit != TaskRepeatUnit.None;
    public bool IsWeeklyRepeat => RepeatUnit == TaskRepeatUnit.Week;
    public bool IsMultiWeekRepeat => IsWeeklyRepeat && RepeatInterval > 1;
    public bool IsOrdinalWeekdayRepeat => RepeatUnit is TaskRepeatUnit.MonthWeekday or TaskRepeatUnit.YearWeekday;
    public bool IsYearlyOrdinalWeekdayRepeat => RepeatUnit == TaskRepeatUnit.YearWeekday;

    public void Open(TaskScheduleModel initial, DateTime taskDate, bool canRepeat)
    {
        ArgumentNullException.ThrowIfNull(initial);

        _taskDateWeekday = TaskRepeatCalculator.ToWeekdays(taskDate.DayOfWeek);
        _taskDate = taskDate.Date;
        CanRepeat = canRepeat;

        HasTime = initial.Time.HasValue;
        var time = initial.Time ?? new TimeSpan(DefaultHour, 0, 0);
        Hour = Math.Clamp(time.Hours, 0, 23);
        Minute = Math.Clamp(time.Minutes, 0, 59);
        ReminderMinutesBeforeRaw = HasTime ? initial.ReminderMinutesBefore : null;
        ReminderOptions = BuildReminderOptions(ReminderMinutesBeforeRaw);

        RepeatUnitValue = canRepeat && Enum.IsDefined(initial.RepeatUnit)
            ? initial.RepeatUnit
            : TaskRepeatUnit.None;
        RepeatInterval = TaskRepeatCalculator.NormalizeInterval(initial.RepeatInterval);
        RepeatWeekdays = initial.RepeatWeekdays & TaskWeekdays.All;
        if (RepeatWeekdays == TaskWeekdays.None)
        {
            RepeatWeekdays = _taskDateWeekday;
        }

        RepeatWeekdayOrdinal = initial.RepeatWeekdayOrdinal.HasValue && TaskRepeatCalculator.IsValidWeekdayOrdinal(initial.RepeatWeekdayOrdinal.Value)
            ? initial.RepeatWeekdayOrdinal.Value
            : 1;
        RepeatMonth = initial.RepeatMonth is >= 1 and <= 12 ? initial.RepeatMonth.Value : taskDate.Month;
        RepeatWeekStart = TaskRepeatCalculator.ToSingleWeekday(initial.RepeatWeekStart ?? TaskWeekdays.None) ?? TaskWeekdays.Monday;

        HasRepeatEndDate = initial.RepeatEndDate is { } end && end.Date >= taskDate.Date;
        RepeatEndDate = HasRepeatEndDate ? initial.RepeatEndDate!.Value.Date : taskDate.Date.AddMonths(1);

        OnPropertyChanged(string.Empty);
    }

    public void SetHour(int hour)
    {
        Hour = Math.Clamp(hour, 0, 23);
        ApplyHasTime();
    }

    public void SetMinute(int minute)
    {
        Minute = Math.Clamp(minute, 0, 59);
        ApplyHasTime();
    }

    private void ApplyHasTime()
    {
        if (HasTime)
        {
            return;
        }

        HasTime = true;
        ReminderMinutesBeforeRaw ??= 0;
    }

    public void ClearTime()
    {
        HasTime = false;
        ReminderMinutesBeforeRaw = null;
    }

    public void SetReminder(int? minutesBefore)
    {
        if (!HasTime)
        {
            return;
        }

        ReminderMinutesBeforeRaw = minutesBefore is >= 0 ? minutesBefore : null;
    }

    public void SetRepeatUnit(TaskRepeatUnit unit)
    {
        if (!CanRepeat || !Enum.IsDefined(unit))
        {
            return;
        }

        RepeatUnitValue = unit;
        if (unit == TaskRepeatUnit.Week && RepeatWeekdays == TaskWeekdays.None)
        {
            RepeatWeekdays = _taskDateWeekday;
        }
        else if (unit is TaskRepeatUnit.MonthWeekday or TaskRepeatUnit.YearWeekday)
        {
            RepeatWeekdays = TaskRepeatCalculator.ToSingleWeekday(RepeatWeekdays) ?? _taskDateWeekday;
        }
    }

    public void SetRepeatInterval(int interval)
    {
        RepeatInterval = TaskRepeatCalculator.NormalizeInterval(interval);
    }

    public void StepRepeatInterval(int delta)
    {
        SetRepeatInterval(RepeatInterval + delta);
    }

    public void ToggleWeekday(TaskWeekdays weekday)
    {
        if (weekday == TaskWeekdays.None || (weekday & ~TaskWeekdays.All) != TaskWeekdays.None)
        {
            return;
        }

        var toggled = RepeatWeekdays ^ weekday;

        if (toggled == TaskWeekdays.None)
        {
            return;
        }

        RepeatWeekdays = toggled;
    }

    public bool IsWeekdaySelected(TaskWeekdays weekday)
    {
        return weekday != TaskWeekdays.None && RepeatWeekdays.HasFlag(weekday);
    }

    public void SetOrdinalWeekday(TaskWeekdays weekday)
    {
        if (!IsOrdinalWeekdayRepeat || weekday == TaskWeekdays.None || (weekday & ~TaskWeekdays.All) != TaskWeekdays.None)
        {
            return;
        }

        RepeatWeekdays = TaskRepeatCalculator.ToSingleWeekday(weekday) ?? weekday;
    }

    public void SetRepeatWeekdayOrdinal(int ordinal)
    {
        RepeatWeekdayOrdinal = TaskRepeatCalculator.IsValidWeekdayOrdinal(ordinal) ? ordinal : RepeatWeekdayOrdinal;
    }

    public void SetRepeatMonth(int month)
    {
        RepeatMonth = month is >= 1 and <= 12 ? month : RepeatMonth;
    }

    public void SetRepeatWeekStart(TaskWeekdays weekday)
    {
        if (!IsWeeklyRepeat || weekday == TaskWeekdays.None || (weekday & ~TaskWeekdays.All) != TaskWeekdays.None)
        {
            return;
        }

        RepeatWeekStart = TaskRepeatCalculator.ToSingleWeekday(weekday) ?? weekday;
    }

    public void SetRepeatEndDate(DateTime date)
    {
        RepeatEndDate = date.Date < _taskDate ? _taskDate : date.Date;
        HasRepeatEndDate = true;
    }

    public void ClearRepeatEndDate()
    {
        HasRepeatEndDate = false;
    }

    public TaskScheduleModel CreateResult()
    {
        var unit = RepeatUnit;
        var isOrdinalWeekday = unit is TaskRepeatUnit.MonthWeekday or TaskRepeatUnit.YearWeekday;

        return new TaskScheduleModel
        {
            Time = HasTime ? new TimeSpan(Hour, Minute, 0) : null,
            ReminderMinutesBefore = ReminderMinutesBefore,
            RepeatUnit = unit,
            RepeatInterval = unit == TaskRepeatUnit.None ? 1 : RepeatInterval,
            RepeatWeekdays = unit == TaskRepeatUnit.Week || isOrdinalWeekday ? RepeatWeekdays : TaskWeekdays.None,
            RepeatWeekdayOrdinal = isOrdinalWeekday ? RepeatWeekdayOrdinal : null,
            RepeatMonth = unit == TaskRepeatUnit.YearWeekday ? RepeatMonth : null,
            RepeatWeekStart = unit == TaskRepeatUnit.Week && RepeatInterval > 1 && RepeatWeekStart != TaskWeekdays.Monday
                ? RepeatWeekStart
                : null,
            RepeatEndDate = unit != TaskRepeatUnit.None && HasRepeatEndDate ? RepeatEndDate : null
        };
    }

    private static IReadOnlyList<int> BuildReminderOptions(int? current)
    {
        if (current is not { } minutes || AppConstants.ReminderMinuteOptions.Contains(minutes))
        {
            return AppConstants.ReminderMinuteOptions;
        }

        return AppConstants.ReminderMinuteOptions.Append(minutes).Order().ToList();
    }
}
