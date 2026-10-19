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
        Assert.False(task.IsRegularTask);
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
        Assert.Equal(original.IsRegularTask, clone.IsRegularTask);
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
    public void NormalizeTaskForPersistence_RegularTask_ClearsTaskDate()
    {
        var task = CreateFullDto();
        task.IsRegularTask = true;

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
        task.IsRegularTask = true;

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
            IsRegularTask = true,
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
        Assert.Equal(entity.IsRegularTask, dto.IsRegularTask);
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
    public void MapToEntity_RegularTask_ClearsTaskDate()
    {
        var dto = CreateFullDto();
        dto.IsRegularTask = true;

        var entity = TaskExtension.MapToEntity(dto);

        Assert.True(entity.IsRegularTask);
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
    [InlineData(false, true, TaskCategory.Regular)]
    [InlineData(true, false, TaskCategory.Done)]
    [InlineData(true, true, TaskCategory.Done)]
    public void GetCategory_DoneAndRegularFlags_ReturnsExpectedCategory(bool isDone, bool isRegular, TaskCategory expected)
    {
        var task = new TaskDto { IsDone = isDone, IsRegularTask = isRegular };

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
            IsRegularTask = true,
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
        Assert.True(target.IsRegularTask);
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
            IsRegularTask = false,
            IsWorkTask = false,
            IsPositive = true,
            CreationDate = new DateTime(2026, 3, 1, 14, 30, 0),
            TaskDate = new DateTime(2026, 3, 10, 16, 45, 0),
            Priority = TaskPriority.Hoch,
            BreakMinutes = 7,
            TimerMinutes = 25
        };
    }
}
