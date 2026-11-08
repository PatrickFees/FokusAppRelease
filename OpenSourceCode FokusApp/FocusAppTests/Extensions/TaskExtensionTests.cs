using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;

namespace FocusAppTests.Extensions;

public class TaskExtensionTests
{
    private static readonly DateTime SomeDay = new(2026, 3, 10);

    [Fact]
    public void CreateDefaultTask_WithDateAndTime_UsesDatePartOnly()
    {
        var dayWithTime = SomeDay.AddHours(13);

        var task = TaskExtension.CreateDefaultTask(dayWithTime);

        Assert.Equal(SomeDay, task.TaskDate);
    }

    [Theory]
    [InlineData(2026, 3, 9, 2026, 3, 14)]
    [InlineData(2026, 3, 13, 2026, 3, 14)]
    [InlineData(2026, 3, 14, 2026, 3, 21)]
    [InlineData(2026, 3, 15, 2026, 3, 21)]
    [InlineData(2026, 2, 27, 2026, 2, 28)]
    [InlineData(2026, 2, 28, 2026, 3, 7)]
    [InlineData(2026, 12, 28, 2027, 1, 2)]
    public void GetNextWeekendDay_Weekday_ReturnsNextSaturdayAfterToday(
        int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay)
    {
        var today = new DateTime(year, month, day, 14, 30, 0);

        var weekend = TaskExtension.GetNextWeekendDay(today);

        Assert.Equal(new DateTime(expectedYear, expectedMonth, expectedDay), weekend);
    }

    [Fact]
    public void IsPastOpenTask_OpenTaskYesterday_IsTrue()
    {
        var task = new TaskDto { TaskDate = SomeDay.AddDays(-1) };

        Assert.True(TaskExtension.IsPastOpenTask(task, SomeDay));
    }

    [Fact]
    public void IsPastOpenTask_TaskToday_IsFalse()
    {
        var task = new TaskDto { TaskDate = SomeDay.AddHours(8) };

        Assert.False(TaskExtension.IsPastOpenTask(task, SomeDay));
    }

    [Fact]
    public void IsPastOpenTask_DoneOrTemplate_IsFalse()
    {
        var done = new TaskDto { TaskDate = SomeDay.AddDays(-1), IsDone = true };
        var template = new TaskDto { TaskDate = SomeDay.AddDays(-1), IsTemplate = true };

        Assert.False(TaskExtension.IsPastOpenTask(done, SomeDay));
        Assert.False(TaskExtension.IsPastOpenTask(template, SomeDay));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateDefaultTask_Scope_IsAppliedToIsWorkTask(bool isWorkScope)
    {
        var task = TaskExtension.CreateDefaultTask(SomeDay, isWorkScope);

        Assert.Equal(isWorkScope, task.IsWorkTask);
    }

    [Fact]
    public void CreateDefaultTask_Defaults_AreSet()
    {
        var task = TaskExtension.CreateDefaultTask(SomeDay);

        Assert.Equal(string.Empty, task.Note);
        Assert.Equal(TaskPriority.Keine, task.Priority);
        Assert.Equal(0, task.TimerMinutes);
        Assert.Equal(5, task.BreakMinutes);
        Assert.False(task.IsTemplate);
        Assert.False(task.IsDone);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateDefaultActiveTask_ScopeAndDay_HasNoIdAndGivenDayWithoutTime(bool isWorkScope)
    {
        var dayWithTime = SomeDay.AddHours(13);

        var task = TaskExtension.CreateDefaultActiveTask(isWorkScope, dayWithTime);

        Assert.Null(task.Id);
        Assert.Equal(string.Empty, task.Note);
        Assert.Equal(isWorkScope, task.IsWorkTask);
        Assert.Equal(SomeDay, task.TaskDate);
    }

    [Fact]
    public void Clone_Task_CopiesAllPropertiesAndTruncatesTaskDateToDate()
    {
        var original = CreateFullDto();

        var clone = TaskExtension.Clone(original);

        Assert.NotSame(original, clone);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.IsDone, clone.IsDone);
        Assert.Equal(original.Note, clone.Note);
        Assert.Equal(original.IsTemplate, clone.IsTemplate);
        Assert.Equal(original.IsWorkTask, clone.IsWorkTask);
        Assert.Equal(original.IsPositive, clone.IsPositive);
        Assert.Equal(original.CreationDate, clone.CreationDate);
        Assert.Equal(original.TaskDate!.Value.Date, clone.TaskDate);
        Assert.Equal(original.Priority, clone.Priority);
        Assert.Equal(original.BreakMinutes, clone.BreakMinutes);
        Assert.Equal(original.TimerMinutes, clone.TimerMinutes);
    }

    [Fact]
    public void Clone_ChangingClone_DoesNotAffectOriginal()
    {
        var original = CreateFullDto();

        var clone = TaskExtension.Clone(original);
        clone.Note = "Geändert";

        Assert.Equal("Notiz", original.Note);
    }

    [Fact]
    public void Clone_NullTask_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TaskExtension.Clone(null!));
    }

    [Fact]
    public void NormalizeTaskForPersistence_Template_ClearsTaskDate()
    {
        var task = CreateFullDto();
        task.IsTemplate = true;

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Null(normalized.TaskDate);
    }

    [Fact]
    public void NormalizeTaskForPersistence_NormalTask_KeepsDatePartOnly()
    {
        var task = CreateFullDto();

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Equal(task.TaskDate!.Value.Date, normalized.TaskDate);
    }

    [Fact]
    public void NormalizeTaskForPersistence_Task_DoesNotModifyOriginal()
    {
        var task = CreateFullDto();
        task.IsTemplate = true;

        TaskExtension.NormalizeTaskForPersistence(task);

        Assert.NotNull(task.TaskDate);
    }

    [Fact]
    public void MapToDto_Entity_CopiesAllProperties()
    {
        var entity = new TaskEntity
        {
            Id = Guid.NewGuid(),
            IsDone = true,
            Note = "Notiz",
            IsTemplate = true,
            IsWorkTask = false,
            IsPositive = true,
            CreationDate = new DateTime(2026, 3, 1, 14, 30, 0),
            TaskDate = SomeDay,
            Priority = TaskPriority.Dringend,
            BreakMinutes = 3,
            TimerMinutes = 50
        };

        var dto = TaskExtension.MapToDto(entity);

        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.IsDone, dto.IsDone);
        Assert.Equal(entity.Note, dto.Note);
        Assert.Equal(entity.IsTemplate, dto.IsTemplate);
        Assert.Equal(entity.IsWorkTask, dto.IsWorkTask);
        Assert.Equal(entity.IsPositive, dto.IsPositive);
        Assert.Equal(entity.CreationDate, dto.CreationDate);
        Assert.Equal(entity.TaskDate, dto.TaskDate);
        Assert.Equal(entity.Priority, dto.Priority);
        Assert.Equal(entity.BreakMinutes, dto.BreakMinutes);
        Assert.Equal(entity.TimerMinutes, dto.TimerMinutes);
    }

    [Fact]
    public void MapToEntity_NormalTask_CopiesAllPropertiesAndTruncatesTaskDate()
    {
        var dto = CreateFullDto();

        var entity = TaskExtension.MapToEntity(dto);

        Assert.Equal(dto.Id, entity.Id);
        Assert.Equal(dto.IsDone, entity.IsDone);
        Assert.Equal(dto.Note, entity.Note);
        Assert.Equal(dto.IsWorkTask, entity.IsWorkTask);
        Assert.Equal(dto.IsPositive, entity.IsPositive);
        Assert.Equal(dto.CreationDate, entity.CreationDate);
        Assert.Equal(dto.TaskDate!.Value.Date, entity.TaskDate);
        Assert.Equal(dto.Priority, entity.Priority);
        Assert.Equal(dto.BreakMinutes, entity.BreakMinutes);
        Assert.Equal(dto.TimerMinutes, entity.TimerMinutes);
    }

    [Fact]
    public void MapToEntity_Template_ClearsTaskDate()
    {
        var dto = CreateFullDto();
        dto.IsTemplate = true;

        var entity = TaskExtension.MapToEntity(dto);

        Assert.True(entity.IsTemplate);
        Assert.Null(entity.TaskDate);
    }

    [Theory]
    [MemberData(nameof(InvalidIds))]
    public void MapToEntity_MissingOrEmptyId_Throws(Guid? id)
    {
        var dto = CreateFullDto();
        dto.Id = id;

        Assert.Throws<InvalidOperationException>(() => TaskExtension.MapToEntity(dto));
    }

    [Theory]
    [InlineData(false, false, TaskCategory.Normal)]
    [InlineData(false, true, TaskCategory.Template)]
    [InlineData(true, false, TaskCategory.Done)]
    [InlineData(true, true, TaskCategory.Done)]
    public void GetCategory_DoneAndTemplateFlags_ReturnsExpectedCategory(bool isDone, bool isTemplate, TaskCategory expected)
    {
        var task = new TaskDto { IsDone = isDone, IsTemplate = isTemplate };

        var category = TaskExtension.GetCategory(task);

        Assert.Equal(expected, category);
    }

    [Fact]
    public void UpdateFrom_Source_CopiesAllPropertiesExceptId()
    {
        var targetId = Guid.NewGuid();
        var target = new TaskEntity { Id = targetId, Note = "alt" };
        var source = new TaskEntity
        {
            Id = Guid.NewGuid(),
            IsDone = true,
            Note = "neu",
            IsTemplate = true,
            IsWorkTask = false,
            IsPositive = true,
            CreationDate = new DateTime(2026, 3, 1),
            TaskDate = SomeDay,
            Priority = TaskPriority.Mittel,
            BreakMinutes = 9,
            TimerMinutes = 40
        };

        target.UpdateFrom(source);

        Assert.Equal(targetId, target.Id);
        Assert.True(target.IsDone);
        Assert.Equal("neu", target.Note);
        Assert.True(target.IsTemplate);
        Assert.False(target.IsWorkTask);
        Assert.True(target.IsPositive);
        Assert.Equal(source.CreationDate, target.CreationDate);
        Assert.Equal(SomeDay, target.TaskDate);
        Assert.Equal(TaskPriority.Mittel, target.Priority);
        Assert.Equal(9, target.BreakMinutes);
        Assert.Equal(40, target.TimerMinutes);
    }

    [Fact]
    public void UpdateFrom_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TaskEntity().UpdateFrom(null!));
    }

    public static TheoryData<Guid?> InvalidIds => new() { null, Guid.Empty };

    private static TaskDto CreateFullDto()
    {
        return new TaskDto
        {
            Id = Guid.NewGuid(),
            IsDone = true,
            Note = "Notiz",
            IsTemplate = false,
            IsWorkTask = false,
            IsPositive = true,
            CreationDate = new DateTime(2026, 3, 1, 14, 30, 0),
            TaskDate = new DateTime(2026, 3, 10, 16, 45, 0),
            Priority = TaskPriority.Hoch,
            BreakMinutes = 7,
            TimerMinutes = 25
        };
    }

    [Fact]
    public void GetEffectiveCompletedDate_DoneTaskWithCompletedAt_ReturnsDayOfCompletion()
    {
        var task = new TaskDto
        {
            IsDone = true,
            TaskDate = SomeDay,
            CompletedAt = SomeDay.AddDays(2).AddHours(23).AddMinutes(59)
        };

        var date = TaskExtension.GetEffectiveCompletedDate(task);

        Assert.Equal(SomeDay.AddDays(2), date);
    }

    [Fact]
    public void GetEffectiveCompletedDate_LegacyDoneTaskWithoutCompletedAt_ReturnsTaskDate()
    {
        var task = new TaskDto { IsDone = true, TaskDate = SomeDay.AddHours(9), CreationDate = SomeDay.AddDays(-5) };

        var date = TaskExtension.GetEffectiveCompletedDate(task);

        Assert.Equal(SomeDay, date);
    }

    [Fact]
    public void GetEffectiveCompletedDate_LegacyDoneTaskWithoutTaskDate_FallsBackToCreationDate()
    {
        var task = new TaskDto { IsDone = true, TaskDate = null, CreationDate = SomeDay.AddDays(-5).AddHours(8) };

        var date = TaskExtension.GetEffectiveCompletedDate(task);

        Assert.Equal(SomeDay.AddDays(-5), date);
    }

    [Fact]
    public void GetEffectiveCompletedDate_OpenTask_ReturnsNull()
    {
        var task = new TaskDto { IsDone = false, TaskDate = SomeDay, CompletedAt = SomeDay };

        var date = TaskExtension.GetEffectiveCompletedDate(task);

        Assert.Null(date);
    }

    [Fact]
    public void MapToEntityAndBack_DoneTaskWithCompletedAt_KeepsCompletedAt()
    {
        var completedAt = new DateTime(2026, 3, 11, 17, 45, 12);
        var dto = new TaskDto { Id = Guid.NewGuid(), IsDone = true, TaskDate = SomeDay, CompletedAt = completedAt };

        var restored = TaskExtension.MapToDto(TaskExtension.MapToEntity(dto));

        Assert.Equal(completedAt, restored.CompletedAt);
    }

    [Fact]
    public void MapToEntity_OpenTaskWithCompletedAt_DropsCompletedAt()
    {
        var dto = new TaskDto { Id = Guid.NewGuid(), IsDone = false, TaskDate = SomeDay, CompletedAt = SomeDay };

        var entity = TaskExtension.MapToEntity(dto);

        Assert.Null(entity.CompletedAt);
    }

    [Fact]
    public void NormalizeTaskForPersistence_OpenTaskWithCompletedAt_ClearsIt()
    {
        var dto = new TaskDto { Id = Guid.NewGuid(), IsDone = false, TaskDate = SomeDay, CompletedAt = SomeDay };

        var normalized = TaskExtension.NormalizeTaskForPersistence(dto);

        Assert.Null(normalized.CompletedAt);
    }

    [Fact]
    public void Clone_TaskWithCompletedAt_CopiesIt()
    {
        var dto = new TaskDto { Id = Guid.NewGuid(), IsDone = true, CompletedAt = SomeDay.AddHours(5) };

        var clone = TaskExtension.Clone(dto);

        Assert.Equal(dto.CompletedAt, clone.CompletedAt);
    }

    [Fact]
    public void UpdateFrom_SourceWithCompletedAt_CopiesIt()
    {
        var target = new TaskEntity { Id = Guid.NewGuid(), IsDone = false };
        var source = new TaskEntity { Id = target.Id, IsDone = true, CompletedAt = SomeDay.AddHours(7) };

        target.UpdateFrom(source);

        Assert.Equal(SomeDay.AddHours(7), target.CompletedAt);
    }

    [Fact]
    public void IsPastOpenTask_TaskWithoutDate_UsesCreationDate()
    {
        var past = new TaskDto { TaskDate = null, CreationDate = SomeDay.AddDays(-2) };
        var current = new TaskDto { TaskDate = null, CreationDate = SomeDay };

        Assert.True(TaskExtension.IsPastOpenTask(past, SomeDay));
        Assert.False(TaskExtension.IsPastOpenTask(current, SomeDay));
    }

    [Fact]
    public void MapToEntity_NormalTaskWithoutTaskDate_KeepsTaskDateEmpty()
    {
        var dto = CreateFullDto();
        dto.TaskDate = null;

        var entity = TaskExtension.MapToEntity(dto);

        Assert.Null(entity.TaskDate);
    }

    [Fact]
    public void MapToEntity_OpenTemplate_KeepsTemplateFlagAndNoCompletion()
    {
        var dto = CreateFullDto();
        dto.IsDone = false;
        dto.IsTemplate = true;
        dto.CompletedAt = new DateTime(2026, 3, 11, 10, 0, 0);

        var entity = TaskExtension.MapToEntity(dto);

        Assert.True(entity.IsTemplate);
        Assert.Null(entity.TaskDate);
        Assert.Null(entity.CompletedAt);
    }

    [Fact]
    public void NormalizeTaskForPersistence_TimeWithSeconds_IsCutToWholeMinutes()
    {
        var task = CreateScheduledTask();
        task.TaskTime = new TimeSpan(9, 30, 45);

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Equal(new TimeSpan(9, 30, 0), normalized.TaskTime);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24 * 60)]
    public void NormalizeTaskForPersistence_TimeOutsideTheDay_IsRemoved(int minutes)
    {
        var task = CreateScheduledTask();
        task.TaskTime = TimeSpan.FromMinutes(minutes);

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Null(normalized.TaskTime);
        Assert.Null(normalized.ReminderMinutesBefore);
    }

    [Fact]
    public void NormalizeTaskForPersistence_ReminderWithoutTime_IsRemoved()
    {
        var task = CreateScheduledTask();
        task.TaskTime = null;
        task.ReminderMinutesBefore = 10;

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Null(normalized.ReminderMinutesBefore);
    }

    [Fact]
    public void NormalizeTaskForPersistence_NegativeReminder_IsRemoved()
    {
        var task = CreateScheduledTask();
        task.ReminderMinutesBefore = -5;

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Null(normalized.ReminderMinutesBefore);
    }

    [Fact]
    public void NormalizeTaskForPersistence_ReminderAtTheTime_IsKept()
    {
        var task = CreateScheduledTask();
        task.ReminderMinutesBefore = 0;

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Equal(0, normalized.ReminderMinutesBefore);
    }

    [Fact]
    public void NormalizeTaskForPersistence_WeeklyWithoutWeekdays_UsesWeekdayOfTheTaskDate()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Week;
        task.RepeatWeekdays = TaskWeekdays.None;

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Equal(TaskWeekdays.Tuesday, normalized.RepeatWeekdays);
    }

    [Fact]
    public void NormalizeTaskForPersistence_NotWeekly_DropsWeekdays()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Month;
        task.RepeatWeekdays = TaskWeekdays.Monday | TaskWeekdays.Friday;

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Equal(TaskWeekdays.None, normalized.RepeatWeekdays);
    }

    [Fact]
    public void NormalizeTaskForPersistence_WeeklyWithMultipleWeekStartFlags_KeepsOnlyTheLowestBit()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Week;
        task.RepeatWeekStart = TaskWeekdays.Wednesday | TaskWeekdays.Friday;

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Equal(TaskWeekdays.Wednesday, normalized.RepeatWeekStart);
    }

    [Fact]
    public void NormalizeTaskForPersistence_WeeklyWithoutWeekStart_StaysNull()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Week;
        task.RepeatWeekStart = null;

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Null(normalized.RepeatWeekStart);
    }

    [Fact]
    public void NormalizeTaskForPersistence_WeeklyWithEmptyWeekStartFlag_BecomesNull()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Week;
        task.RepeatWeekStart = TaskWeekdays.None;

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Null(normalized.RepeatWeekStart);
    }

    [Fact]
    public void NormalizeTaskForPersistence_NotWeekly_DropsWeekStart()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Month;
        task.RepeatWeekStart = TaskWeekdays.Sunday;

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Null(normalized.RepeatWeekStart);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(250, 99)]
    public void NormalizeTaskForPersistence_IntervalOutsideTheRange_IsClamped(int interval, int expected)
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Day;
        task.RepeatInterval = interval;

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Equal(expected, normalized.RepeatInterval);
    }

    [Fact]
    public void NormalizeTaskForPersistence_NoRepeat_ResetsAllRuleFields()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.None;
        task.RepeatInterval = 4;
        task.RepeatWeekdays = TaskWeekdays.Friday;
        task.RepeatGeneratedUntil = SomeDay;
        task.RepeatEndDate = SomeDay.AddDays(10);

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Equal(1, normalized.RepeatInterval);
        Assert.Equal(TaskWeekdays.None, normalized.RepeatWeekdays);
        Assert.Null(normalized.RepeatGeneratedUntil);
        Assert.Null(normalized.RepeatEndDate);
    }

    [Fact]
    public void NormalizeTaskForPersistence_EndDateOnOrAfterTaskDate_IsKept()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Day;
        task.RepeatEndDate = SomeDay.AddDays(5);

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Equal(SomeDay.AddDays(5), normalized.RepeatEndDate);
    }

    [Fact]
    public void NormalizeTaskForPersistence_EndDateBeforeTaskDate_BecomesNull()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Day;
        task.RepeatEndDate = SomeDay.AddDays(-1);

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Null(normalized.RepeatEndDate);
    }

    [Fact]
    public void NormalizeTaskForPersistence_EndDateOnTaskDate_IsKept()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Day;
        task.RepeatEndDate = SomeDay;

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Equal(SomeDay, normalized.RepeatEndDate);
    }

    [Fact]
    public void NormalizeTaskForPersistence_Template_DoesNotRepeatButKeepsTime()
    {
        var task = CreateScheduledTask();
        task.IsTemplate = true;
        task.RepeatUnit = TaskRepeatUnit.Week;
        task.SeriesId = Guid.NewGuid();

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Equal(TaskRepeatUnit.None, normalized.RepeatUnit);
        Assert.Null(normalized.SeriesId);
        Assert.Equal(task.TaskTime, normalized.TaskTime);
    }

    [Fact]
    public void NormalizeTaskForPersistence_SeriesTask_HasNoGeneratedUntil()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Day;
        task.SeriesId = Guid.NewGuid();
        task.RepeatGeneratedUntil = SomeDay;

        var normalized = TaskExtension.NormalizeTaskForPersistence(task);

        Assert.Null(normalized.RepeatGeneratedUntil);
    }

    [Fact]
    public void IsSeriesStart_RepeatingTaskWithoutSeriesId_IsTrue()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Day;

        var isSeriesStart = TaskExtension.IsSeriesStart(task);

        Assert.True(isSeriesStart);
    }

    [Fact]
    public void IsSeriesStart_TaskOfASeries_IsFalse()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Day;
        task.SeriesId = Guid.NewGuid();

        var isSeriesStart = TaskExtension.IsSeriesStart(task);

        Assert.False(isSeriesStart);
    }

    [Fact]
    public void CreateSeriesTask_Always_CopiesTheSeriesStartAsAnOpenTaskOnTheDay()
    {
        var seriesStart = CreateScheduledTask();
        seriesStart.RepeatUnit = TaskRepeatUnit.Week;
        seriesStart.IsDone = true;
        seriesStart.CompletedAt = SomeDay;
        seriesStart.RepeatGeneratedUntil = SomeDay.AddDays(30);
        var day = SomeDay.AddDays(7).AddHours(5);
        var now = SomeDay.AddHours(8);

        var seriesTask = TaskExtension.CreateSeriesTask(seriesStart, day, now);

        Assert.NotEqual(seriesStart.Id, seriesTask.Id);
        Assert.Equal(seriesStart.Id, seriesTask.SeriesId);
        Assert.Equal(SomeDay.AddDays(7), seriesTask.TaskDate);
        Assert.False(seriesTask.IsDone);
        Assert.Null(seriesTask.CompletedAt);
        Assert.Null(seriesTask.RepeatGeneratedUntil);
        Assert.Equal(now, seriesTask.CreationDate);
        Assert.Equal(seriesStart.Note, seriesTask.Note);
        Assert.Equal(seriesStart.TaskTime, seriesTask.TaskTime);
        Assert.Equal(seriesStart.RepeatUnit, seriesTask.RepeatUnit);
    }

    [Fact]
    public void MapToEntityAndBack_ScheduleFields_SurviveTheRoundTrip()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Week;
        task.RepeatInterval = 2;
        task.RepeatWeekdays = TaskWeekdays.Monday | TaskWeekdays.Thursday;
        task.RepeatWeekStart = TaskWeekdays.Sunday;
        task.SeriesId = Guid.NewGuid();
        task.RepeatEndDate = SomeDay.AddDays(90);

        var entity = TaskExtension.MapToEntity(task);
        var restored = TaskExtension.MapToDto(entity);

        Assert.Equal(task.TaskTime, restored.TaskTime);
        Assert.Equal(task.ReminderMinutesBefore, restored.ReminderMinutesBefore);
        Assert.Equal(task.RepeatUnit, restored.RepeatUnit);
        Assert.Equal(2, restored.RepeatInterval);
        Assert.Equal(task.RepeatWeekdays, restored.RepeatWeekdays);
        Assert.Equal(task.RepeatWeekStart, restored.RepeatWeekStart);
        Assert.Equal(task.SeriesId, restored.SeriesId);
        Assert.Equal(task.RepeatEndDate, restored.RepeatEndDate);
    }

    [Fact]
    public void Clone_ScheduleFields_AreCopied()
    {
        var task = CreateScheduledTask();
        task.RepeatUnit = TaskRepeatUnit.Month;
        task.RepeatInterval = 3;
        task.SeriesId = Guid.NewGuid();
        task.RepeatGeneratedUntil = SomeDay.AddDays(60);
        task.RepeatEndDate = SomeDay.AddDays(120);

        var clone = TaskExtension.Clone(task);

        Assert.Equal(task.TaskTime, clone.TaskTime);
        Assert.Equal(task.ReminderMinutesBefore, clone.ReminderMinutesBefore);
        Assert.Equal(TaskRepeatUnit.Month, clone.RepeatUnit);
        Assert.Equal(3, clone.RepeatInterval);
        Assert.Equal(task.SeriesId, clone.SeriesId);
        Assert.Equal(task.RepeatGeneratedUntil, clone.RepeatGeneratedUntil);
        Assert.Equal(task.RepeatEndDate, clone.RepeatEndDate);
    }

    [Fact]
    public void UpdateFrom_ScheduleFields_AreCopiedToTheTarget()
    {
        var source = TaskExtension.MapToEntity(CreateScheduledTask());
        source.RepeatUnit = TaskRepeatUnit.Year;
        source.SeriesId = Guid.NewGuid();
        source.RepeatWeekStart = TaskWeekdays.Sunday;
        source.RepeatEndDate = SomeDay.AddDays(45);
        var target = new TaskEntity { Id = source.Id };

        target.UpdateFrom(source);

        Assert.Equal(source.TaskTime, target.TaskTime);
        Assert.Equal(source.ReminderMinutesBefore, target.ReminderMinutesBefore);
        Assert.Equal(TaskRepeatUnit.Year, target.RepeatUnit);
        Assert.Equal(source.SeriesId, target.SeriesId);
        Assert.Equal(source.RepeatWeekStart, target.RepeatWeekStart);
        Assert.Equal(source.RepeatEndDate, target.RepeatEndDate);
    }

    private static TaskDto CreateScheduledTask()
    {
        return new TaskDto
        {
            Id = Guid.NewGuid(),
            Note = "Aufgabe",
            TaskDate = SomeDay,
            TaskTime = new TimeSpan(9, 30, 0),
            ReminderMinutesBefore = 15,
            CreationDate = SomeDay.AddDays(-1)
        };
    }
}
