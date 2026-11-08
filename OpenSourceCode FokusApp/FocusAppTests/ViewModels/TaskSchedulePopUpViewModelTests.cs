using FokusApp.Components.PopUps.TaskSchedule;
using FokusApp.Core;
using FokusApp.Core.Enums;

namespace FocusAppTests.ViewModels;

public class TaskSchedulePopUpViewModelTests
{

    private static readonly DateTime TaskDate = new(2026, 3, 10);

    private readonly TaskSchedulePopUpViewModel _viewModel = new();

    [Fact]
    public void Open_EmptySchedule_HasNoTimeNoReminderAndNoRepetition()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        Assert.False(_viewModel.HasTime);
        Assert.Null(_viewModel.ReminderMinutesBefore);
        Assert.False(_viewModel.CanSetReminder);
        Assert.Equal(TaskRepeatUnit.None, _viewModel.RepeatUnit);
        Assert.False(_viewModel.IsRepeating);
        Assert.Equal(1, _viewModel.RepeatInterval);
    }

    [Fact]
    public void Open_EmptySchedule_ProposesNineOClockForTheWheels()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        Assert.Equal(9, _viewModel.Hour);
        Assert.Equal(0, _viewModel.Minute);
        Assert.Equal("09:00", _viewModel.TimeDisplay);
    }

    [Fact]
    public void Open_ScheduleWithTimeAndReminder_ShowsThem()
    {
        var initial = new TaskScheduleModel { Time = new TimeSpan(14, 35, 0), ReminderMinutesBefore = 30 };

        _viewModel.Open(initial, TaskDate, canRepeat: true);

        Assert.True(_viewModel.HasTime);
        Assert.Equal(14, _viewModel.Hour);
        Assert.Equal(35, _viewModel.Minute);
        Assert.Equal(30, _viewModel.ReminderMinutesBefore);
        Assert.True(_viewModel.CanSetReminder);
    }

    [Fact]
    public void Open_ReminderWithoutTime_DropsTheReminder()
    {
        var initial = new TaskScheduleModel { ReminderMinutesBefore = 30 };

        _viewModel.Open(initial, TaskDate, canRepeat: true);

        Assert.Null(_viewModel.ReminderMinutesBefore);
    }

    [Fact]
    public void Open_ReminderOutsideTheStandardOptions_StaysSelectableInOrder()
    {
        var initial = new TaskScheduleModel { Time = new TimeSpan(8, 0, 0), ReminderMinutesBefore = 45 };

        _viewModel.Open(initial, TaskDate, canRepeat: true);

        Assert.Contains(45, _viewModel.ReminderOptions);
        Assert.Equal(_viewModel.ReminderOptions.Order().ToList(), _viewModel.ReminderOptions.ToList());
    }

    [Fact]
    public void Open_StandardReminder_UsesTheStandardOptions()
    {
        var initial = new TaskScheduleModel { Time = new TimeSpan(8, 0, 0), ReminderMinutesBefore = 15 };

        _viewModel.Open(initial, TaskDate, canRepeat: true);

        Assert.Equal(AppConstants.ReminderMinuteOptions, _viewModel.ReminderOptions);
    }

    [Fact]
    public void Open_WeeklyWithoutWeekdays_SelectsTheWeekdayOfTheTaskDate()
    {
        var initial = new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Week };

        _viewModel.Open(initial, TaskDate, canRepeat: true);

        Assert.Equal(TaskWeekdays.Tuesday, _viewModel.RepeatWeekdays);
    }

    [Fact]
    public void Open_CannotRepeat_ForcesNoRepetition()
    {
        var initial = new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day };

        _viewModel.Open(initial, TaskDate, canRepeat: false);

        Assert.False(_viewModel.CanRepeat);
        Assert.Equal(TaskRepeatUnit.None, _viewModel.RepeatUnit);
    }

    [Fact]
    public void Open_IntervalOutsideTheRange_IsClamped()
    {
        var initial = new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day, RepeatInterval = 500 };

        _viewModel.Open(initial, TaskDate, canRepeat: true);

        Assert.Equal(99, _viewModel.RepeatInterval);
    }

    [Fact]
    public void SetHour_NoTimeYet_SetsTheTimeWithTheProposedMinute()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        _viewModel.SetHour(17);

        Assert.True(_viewModel.HasTime);
        Assert.Equal("17:00", _viewModel.TimeDisplay);
    }

    [Fact]
    public void SetHour_NoTimeYet_DefaultsTheReminderToAtTheTime()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        _viewModel.SetHour(17);

        Assert.Equal(0, _viewModel.ReminderMinutesBefore);
    }

    [Fact]
    public void SetMinute_NoTimeYet_SetsTheTimeWithTheProposedHour()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        _viewModel.SetMinute(45);

        Assert.True(_viewModel.HasTime);
        Assert.Equal("09:45", _viewModel.TimeDisplay);
    }

    [Fact]
    public void SetMinute_NoTimeYet_DefaultsTheReminderToAtTheTime()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        _viewModel.SetMinute(45);

        Assert.Equal(0, _viewModel.ReminderMinutesBefore);
    }

    [Fact]
    public void SetHour_TimeAlreadySet_LeavesAnAlreadyChosenReminderUntouched()
    {
        var initial = new TaskScheduleModel { Time = new TimeSpan(10, 0, 0), ReminderMinutesBefore = 30 };
        _viewModel.Open(initial, TaskDate, canRepeat: true);

        _viewModel.SetHour(11);

        Assert.Equal(30, _viewModel.ReminderMinutesBefore);
    }

    [Fact]
    public void SetHour_TimeAlreadySetWithoutReminder_StaysWithoutReminder()
    {
        var initial = new TaskScheduleModel { Time = new TimeSpan(10, 0, 0) };
        _viewModel.Open(initial, TaskDate, canRepeat: true);

        _viewModel.SetHour(11);

        Assert.Null(_viewModel.ReminderMinutesBefore);
    }

    [Fact]
    public void SetReminder_AfterTheDefaultWasApplied_CanStillSwitchToAnEarlierOrNoReminder()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetHour(17);

        _viewModel.SetReminder(30);
        Assert.Equal(30, _viewModel.ReminderMinutesBefore);

        _viewModel.SetReminder(null);
        Assert.Null(_viewModel.ReminderMinutesBefore);
    }

    [Theory]
    [InlineData(-3, 0)]
    [InlineData(30, 23)]
    public void SetHour_OutsideTheDay_IsClamped(int hour, int expected)
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        _viewModel.SetHour(hour);

        Assert.Equal(expected, _viewModel.Hour);
    }

    [Theory]
    [InlineData(-3, 0)]
    [InlineData(75, 59)]
    public void SetMinute_OutsideTheHour_IsClamped(int minute, int expected)
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        _viewModel.SetMinute(minute);

        Assert.Equal(expected, _viewModel.Minute);
    }

    [Fact]
    public void SetHour_SameValueAsBefore_DoesNotNotifyAgain()
    {
        _viewModel.Open(new TaskScheduleModel { Time = new TimeSpan(10, 0, 0) }, TaskDate, canRepeat: true);
        var notifications = 0;
        _viewModel.PropertyChanged += (_, _) => notifications++;

        _viewModel.SetHour(10);

        Assert.Equal(0, notifications);
    }

    [Fact]
    public void ClearTime_TimeAndReminderSet_RemovesBoth()
    {
        var initial = new TaskScheduleModel { Time = new TimeSpan(14, 0, 0), ReminderMinutesBefore = 10 };
        _viewModel.Open(initial, TaskDate, canRepeat: true);

        _viewModel.ClearTime();

        Assert.False(_viewModel.HasTime);
        Assert.Null(_viewModel.ReminderMinutesBefore);
        Assert.False(_viewModel.CanSetReminder);
    }

    [Fact]
    public void SetReminder_WithTime_StoresIt()
    {
        _viewModel.Open(new TaskScheduleModel { Time = new TimeSpan(14, 0, 0) }, TaskDate, canRepeat: true);

        _viewModel.SetReminder(60);

        Assert.Equal(60, _viewModel.ReminderMinutesBefore);
    }

    [Fact]
    public void SetReminder_WithoutTime_IsIgnored()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        _viewModel.SetReminder(60);

        Assert.Null(_viewModel.ReminderMinutesBefore);
    }

    [Fact]
    public void SetReminder_Null_RemovesTheReminder()
    {
        var initial = new TaskScheduleModel { Time = new TimeSpan(14, 0, 0), ReminderMinutesBefore = 10 };
        _viewModel.Open(initial, TaskDate, canRepeat: true);

        _viewModel.SetReminder(null);

        Assert.Null(_viewModel.ReminderMinutesBefore);
    }

    [Fact]
    public void SetRepeatUnit_Week_IsWeeklyAndKeepsTheTaskWeekday()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        _viewModel.SetRepeatUnit(TaskRepeatUnit.Week);

        Assert.True(_viewModel.IsRepeating);
        Assert.True(_viewModel.IsWeeklyRepeat);
        Assert.True(_viewModel.IsWeekdaySelected(TaskWeekdays.Tuesday));
    }

    [Fact]
    public void SetRepeatUnit_CannotRepeat_IsIgnored()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: false);

        _viewModel.SetRepeatUnit(TaskRepeatUnit.Day);

        Assert.Equal(TaskRepeatUnit.None, _viewModel.RepeatUnit);
    }

    [Fact]
    public void SetRepeatUnit_UnknownUnit_IsIgnored()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        _viewModel.SetRepeatUnit((TaskRepeatUnit)42);

        Assert.Equal(TaskRepeatUnit.None, _viewModel.RepeatUnit);
    }

    [Fact]
    public void StepRepeatInterval_StaysWithinTheAllowedRange()
    {
        _viewModel.Open(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day }, TaskDate, canRepeat: true);

        _viewModel.StepRepeatInterval(-1);
        Assert.Equal(1, _viewModel.RepeatInterval);

        _viewModel.StepRepeatInterval(1);
        Assert.Equal(2, _viewModel.RepeatInterval);

        _viewModel.SetRepeatInterval(99);
        _viewModel.StepRepeatInterval(1);
        Assert.Equal(99, _viewModel.RepeatInterval);
    }

    [Fact]
    public void ToggleWeekday_AddsAndRemovesDays()
    {
        _viewModel.Open(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Week }, TaskDate, canRepeat: true);

        _viewModel.ToggleWeekday(TaskWeekdays.Friday);
        Assert.True(_viewModel.IsWeekdaySelected(TaskWeekdays.Friday));

        _viewModel.ToggleWeekday(TaskWeekdays.Friday);
        Assert.False(_viewModel.IsWeekdaySelected(TaskWeekdays.Friday));
    }

    [Fact]
    public void ToggleWeekday_LastSelectedDay_StaysSelected()
    {
        _viewModel.Open(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Week }, TaskDate, canRepeat: true);

        _viewModel.ToggleWeekday(TaskWeekdays.Tuesday);

        Assert.True(_viewModel.IsWeekdaySelected(TaskWeekdays.Tuesday));
    }

    [Theory]
    [InlineData(TaskWeekdays.None)]
    [InlineData((TaskWeekdays)128)]
    public void ToggleWeekday_NoRealWeekday_IsIgnored(TaskWeekdays weekday)
    {
        _viewModel.Open(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Week }, TaskDate, canRepeat: true);

        _viewModel.ToggleWeekday(weekday);

        Assert.Equal(TaskWeekdays.Tuesday, _viewModel.RepeatWeekdays);
    }

    [Fact]
    public void IsWeekdaySelected_None_IsFalse()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        Assert.False(_viewModel.IsWeekdaySelected(TaskWeekdays.None));
    }

    [Fact]
    public void CreateResult_TimeReminderAndWeeklyRepetition_ReturnsAllOfThem()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetHour(7);
        _viewModel.SetMinute(15);
        _viewModel.SetReminder(10);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Week);
        _viewModel.SetRepeatInterval(2);
        _viewModel.ToggleWeekday(TaskWeekdays.Thursday);

        var result = _viewModel.CreateResult();

        Assert.Equal(new TimeSpan(7, 15, 0), result.Time);
        Assert.Equal(10, result.ReminderMinutesBefore);
        Assert.Equal(TaskRepeatUnit.Week, result.RepeatUnit);
        Assert.Equal(2, result.RepeatInterval);
        Assert.Equal(TaskWeekdays.Tuesday | TaskWeekdays.Thursday, result.RepeatWeekdays);
    }

    [Fact]
    public void CreateResult_NothingChosen_ReturnsAnEmptySchedule()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        var result = _viewModel.CreateResult();

        Assert.Null(result.Time);
        Assert.Null(result.ReminderMinutesBefore);
        Assert.Equal(TaskRepeatUnit.None, result.RepeatUnit);
        Assert.Equal(1, result.RepeatInterval);
        Assert.Equal(TaskWeekdays.None, result.RepeatWeekdays);
    }

    [Fact]
    public void CreateResult_RepetitionSwitchedBackOff_ResetsIntervalAndWeekdays()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Week);
        _viewModel.SetRepeatInterval(4);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.None);

        var result = _viewModel.CreateResult();

        Assert.Equal(1, result.RepeatInterval);
        Assert.Equal(TaskWeekdays.None, result.RepeatWeekdays);
    }

    [Fact]
    public void CreateResult_MonthlyRepetition_HasNoWeekdays()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Week);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Month);

        var result = _viewModel.CreateResult();

        Assert.Equal(TaskRepeatUnit.Month, result.RepeatUnit);
        Assert.Equal(TaskWeekdays.None, result.RepeatWeekdays);
    }

    [Fact]
    public void IsMultiWeekRepeat_WeeklyWithIntervalOne_IsFalse()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Week);

        Assert.False(_viewModel.IsMultiWeekRepeat);
    }

    [Fact]
    public void IsMultiWeekRepeat_WeeklyWithIntervalAboveOne_IsTrue()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Week);
        _viewModel.SetRepeatInterval(2);

        Assert.True(_viewModel.IsMultiWeekRepeat);
    }

    [Fact]
    public void IsMultiWeekRepeat_MonthlyWithIntervalAboveOne_IsFalse()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Month);
        _viewModel.SetRepeatInterval(2);

        Assert.False(_viewModel.IsMultiWeekRepeat);
    }

    [Fact]
    public void Open_WeeklyWithWeekStart_SelectsIt()
    {
        var initial = new TaskScheduleModel
        {
            RepeatUnit = TaskRepeatUnit.Week, RepeatInterval = 2, RepeatWeekStart = TaskWeekdays.Sunday
        };

        _viewModel.Open(initial, TaskDate, canRepeat: true);

        Assert.Equal(TaskWeekdays.Sunday, _viewModel.RepeatWeekStart);
    }

    [Fact]
    public void Open_WeeklyWithoutWeekStart_DefaultsToMonday()
    {
        _viewModel.Open(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Week }, TaskDate, canRepeat: true);

        Assert.Equal(TaskWeekdays.Monday, _viewModel.RepeatWeekStart);
    }

    [Fact]
    public void SetRepeatWeekStart_WeeklyRepeat_ChangesIt()
    {
        _viewModel.Open(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Week }, TaskDate, canRepeat: true);

        _viewModel.SetRepeatWeekStart(TaskWeekdays.Sunday);

        Assert.Equal(TaskWeekdays.Sunday, _viewModel.RepeatWeekStart);
    }

    [Fact]
    public void SetRepeatWeekStart_NotWeeklyRepeat_IsIgnored()
    {
        _viewModel.Open(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Month }, TaskDate, canRepeat: true);

        _viewModel.SetRepeatWeekStart(TaskWeekdays.Sunday);

        Assert.Equal(TaskWeekdays.Monday, _viewModel.RepeatWeekStart);
    }

    [Theory]
    [InlineData(TaskWeekdays.None)]
    [InlineData((TaskWeekdays)128)]
    public void SetRepeatWeekStart_NoRealWeekday_IsIgnored(TaskWeekdays weekday)
    {
        _viewModel.Open(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Week }, TaskDate, canRepeat: true);

        _viewModel.SetRepeatWeekStart(weekday);

        Assert.Equal(TaskWeekdays.Monday, _viewModel.RepeatWeekStart);
    }

    [Fact]
    public void SetRepeatWeekStart_MultipleFlags_KeepsOnlyTheLowestBit()
    {
        _viewModel.Open(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Week }, TaskDate, canRepeat: true);

        _viewModel.SetRepeatWeekStart(TaskWeekdays.Wednesday | TaskWeekdays.Friday);

        Assert.Equal(TaskWeekdays.Wednesday, _viewModel.RepeatWeekStart);
    }

    [Fact]
    public void CreateResult_MultiWeekRepeatWithNonMondayWeekStart_IncludesIt()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Week);
        _viewModel.SetRepeatInterval(2);
        _viewModel.SetRepeatWeekStart(TaskWeekdays.Sunday);

        var result = _viewModel.CreateResult();

        Assert.Equal(TaskWeekdays.Sunday, result.RepeatWeekStart);
    }

    [Fact]
    public void CreateResult_WeeklyWithIntervalOne_OmitsWeekStartEvenIfSet()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Week);
        _viewModel.SetRepeatInterval(2);
        _viewModel.SetRepeatWeekStart(TaskWeekdays.Sunday);
        _viewModel.SetRepeatInterval(1);

        var result = _viewModel.CreateResult();

        Assert.Null(result.RepeatWeekStart);
    }

    [Fact]
    public void CreateResult_MultiWeekRepeatWithDefaultMondayWeekStart_OmitsIt()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Week);
        _viewModel.SetRepeatInterval(2);

        var result = _viewModel.CreateResult();

        Assert.Null(result.RepeatWeekStart);
    }

    [Fact]
    public void CreateResult_MonthlyRepetition_HasNoWeekStart()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Week);
        _viewModel.SetRepeatInterval(2);
        _viewModel.SetRepeatWeekStart(TaskWeekdays.Sunday);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Month);

        var result = _viewModel.CreateResult();

        Assert.Null(result.RepeatWeekStart);
    }

    [Fact]
    public void Open_RepeatingWithoutEndDate_HasRepeatEndDateIsFalse()
    {
        _viewModel.Open(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day }, TaskDate, canRepeat: true);

        Assert.False(_viewModel.HasRepeatEndDate);
    }

    [Fact]
    public void Open_RepeatingWithEndDateOnOrAfterTaskDate_SelectsIt()
    {
        var initial = new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day, RepeatEndDate = TaskDate.AddDays(10) };

        _viewModel.Open(initial, TaskDate, canRepeat: true);

        Assert.True(_viewModel.HasRepeatEndDate);
        Assert.Equal(TaskDate.AddDays(10), _viewModel.RepeatEndDate);
    }

    [Fact]
    public void Open_RepeatingWithEndDateBeforeTaskDate_IsTreatedAsUnset()
    {
        var initial = new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day, RepeatEndDate = TaskDate.AddDays(-1) };

        _viewModel.Open(initial, TaskDate, canRepeat: true);

        Assert.False(_viewModel.HasRepeatEndDate);
    }

    [Fact]
    public void SetRepeatEndDate_Always_TurnsHasRepeatEndDateOn()
    {
        _viewModel.Open(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day }, TaskDate, canRepeat: true);

        _viewModel.SetRepeatEndDate(TaskDate.AddDays(14));

        Assert.True(_viewModel.HasRepeatEndDate);
        Assert.Equal(TaskDate.AddDays(14), _viewModel.RepeatEndDate);
    }

    [Fact]
    public void SetRepeatEndDate_BeforeTaskDate_ClampsToTaskDate()
    {
        _viewModel.Open(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day }, TaskDate, canRepeat: true);

        _viewModel.SetRepeatEndDate(TaskDate.AddDays(-5));

        Assert.Equal(TaskDate, _viewModel.RepeatEndDate);
    }

    [Fact]
    public void ClearRepeatEndDate_Always_TurnsHasRepeatEndDateOff()
    {
        var initial = new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day, RepeatEndDate = TaskDate.AddDays(10) };
        _viewModel.Open(initial, TaskDate, canRepeat: true);

        _viewModel.ClearRepeatEndDate();

        Assert.False(_viewModel.HasRepeatEndDate);
    }

    [Fact]
    public void CreateResult_RepeatingWithEndDateSet_IncludesIt()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Day);
        _viewModel.SetRepeatEndDate(TaskDate.AddDays(30));

        var result = _viewModel.CreateResult();

        Assert.Equal(TaskDate.AddDays(30), result.RepeatEndDate);
    }

    [Fact]
    public void CreateResult_RepeatingWithoutEndDateSet_OmitsIt()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Day);

        var result = _viewModel.CreateResult();

        Assert.Null(result.RepeatEndDate);
    }

    [Fact]
    public void CreateResult_EndDateSetThenRepeatTurnedOff_OmitsIt()
    {
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);
        _viewModel.SetRepeatUnit(TaskRepeatUnit.Day);
        _viewModel.SetRepeatEndDate(TaskDate.AddDays(30));
        _viewModel.SetRepeatUnit(TaskRepeatUnit.None);

        var result = _viewModel.CreateResult();

        Assert.Null(result.RepeatEndDate);
    }

    [Fact]
    public void Open_Twice_StartsFreshEachTime()
    {
        _viewModel.Open(new TaskScheduleModel { Time = new TimeSpan(20, 0, 0) }, TaskDate, canRepeat: true);
        _viewModel.Open(new TaskScheduleModel(), TaskDate, canRepeat: true);

        Assert.False(_viewModel.HasTime);
    }
}
