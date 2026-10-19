using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;

namespace FocusAppTests.Services;

public class TaskFilterServiceTests
{
    private static readonly DateTime BaseDate = new(2026, 3, 10);

    [Fact]
    public void Apply_NoOptions_ReturnsAllTasksSortedByCreationDateDescending()
    {
        var oldest = CreateTask(creationDayOffset: 0);
        var middle = CreateTask(creationDayOffset: 1);
        var newest = CreateTask(creationDayOffset: 2);

        var result = TaskFilterService.Apply(null, [middle, oldest, newest]);

        Assert.Equal([newest, middle, oldest], result);
    }

    [Fact]
    public void Apply_MultipleSourcesIncludingNull_MergesNonNullSources()
    {
        var first = CreateTask(creationDayOffset: 0);
        var second = CreateTask(creationDayOffset: 1);

        var options = new TaskFilterOptions();

        var result = TaskFilterService.Apply(options, [first], null, [second]);

        Assert.Equal(2, result.Count);
        Assert.Contains(first, result);
        Assert.Contains(second, result);
    }

    [Fact]
    public void Apply_NoSources_ReturnsEmptyList()
    {
        var options = new TaskFilterOptions();

        var result = TaskFilterService.Apply(options);

        Assert.Empty(result);
    }

    [Fact]
    public void Apply_AscendingSort_ReturnsOldestFirst()
    {
        var oldest = CreateTask(creationDayOffset: 0);
        var newest = CreateTask(creationDayOffset: 2);
        var options = new TaskFilterOptions { IsSortDescending = false };

        var result = TaskFilterService.Apply(options, [newest, oldest]);

        Assert.Equal([oldest, newest], result);
    }

    [Theory]
    [InlineData("einkauf")]
    [InlineData("EINKAUF")]
    [InlineData("  einkauf  ")]
    [InlineData("kauf")]
    public void Apply_SearchText_MatchesNoteCaseInsensitiveAndTrimmed(string searchText)
    {
        var match = CreateTask(note: "Einkaufen gehen");
        var other = CreateTask(note: "Sport");
        var options = new TaskFilterOptions { SearchText = searchText };

        var result = TaskFilterService.Apply(options, [match, other]);

        Assert.Equal([match], result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Apply_BlankSearchText_DoesNotFilter(string? searchText)
    {
        var tasks = new[] { CreateTask(note: "A"), CreateTask(note: null) };
        var options = new TaskFilterOptions { SearchText = searchText };

        var result = TaskFilterService.Apply(options, tasks);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Apply_SearchText_TaskWithoutNoteDoesNotMatch()
    {
        var withoutNote = CreateTask(note: null);
        var options = new TaskFilterOptions { SearchText = "a" };

        var result = TaskFilterService.Apply(options, [withoutNote]);

        Assert.Empty(result);
    }

    [Fact]
    public void Apply_SelectedPriorities_ReturnsOnlyMatchingPriorities()
    {
        var urgent = CreateTask(priority: TaskPriority.Dringend);
        var high = CreateTask(priority: TaskPriority.Hoch);
        var low = CreateTask(priority: TaskPriority.Wenig);
        var options = new TaskFilterOptions { SelectedPriorities = [TaskPriority.Dringend, TaskPriority.Wenig] };

        var result = TaskFilterService.Apply(options, [urgent, high, low]);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(high, result);
    }

    [Fact]
    public void Apply_UndefinedPriorityValue_IsTreatedAsNone()
    {
        var undefinedPriority = CreateTask(priority: (TaskPriority)99);
        var urgent = CreateTask(priority: TaskPriority.Dringend);
        var options = new TaskFilterOptions { SelectedPriorities = [TaskPriority.Keine] };

        var result = TaskFilterService.Apply(options, [undefinedPriority, urgent]);

        Assert.Equal([undefinedPriority], result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Apply_IsPositiveFilter_ReturnsOnlyMatchingTasks(bool isPositive)
    {
        var positive = CreateTask(isPositive: true);
        var negative = CreateTask(isPositive: false);
        var options = new TaskFilterOptions { IsPositive = isPositive };

        var result = TaskFilterService.Apply(options, [positive, negative]);

        Assert.Single(result);
        Assert.Equal(isPositive, result[0].IsPositive);
    }

    [Fact]
    public void Apply_IsRegularTemplateTrue_ReturnsOnlyOpenRegularTasks()
    {
        var template = CreateTask(isRegular: true);
        var doneRegular = CreateTask(isRegular: true, isDone: true);
        var normal = CreateTask();
        var options = new TaskFilterOptions { IsRegularTemplate = true };

        var result = TaskFilterService.Apply(options, [template, doneRegular, normal]);

        Assert.Equal([template], result);
    }

    [Fact]
    public void Apply_IsRegularTemplateFalse_ExcludesOpenRegularTasks()
    {
        var template = CreateTask(isRegular: true);
        var doneRegular = CreateTask(isRegular: true, isDone: true);
        var normal = CreateTask();
        var options = new TaskFilterOptions { IsRegularTemplate = false };

        var result = TaskFilterService.Apply(options, [template, doneRegular, normal]);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(template, result);
    }

    [Fact]
    public void Apply_SelectedDate_ReturnsTasksOfThatDayIgnoringTime()
    {
        var afternoon = BaseDate.AddHours(15);
        var nextDay = BaseDate.AddDays(1);
        var onDay = CreateTask(taskDate: afternoon);
        var otherDay = CreateTask(taskDate: nextDay);
        var options = new TaskFilterOptions { SelectedDate = BaseDate.AddHours(8) };

        var result = TaskFilterService.Apply(options, [onDay, otherDay]);

        Assert.Equal([onDay], result);
    }

    [Fact]
    public void Apply_SelectedDate_TaskWithoutTaskDateFallsBackToCreationDate()
    {
        var createdOnDay = CreateTask(creationDayOffset: 0, taskDate: null);
        var createdOtherDay = CreateTask(creationDayOffset: 3, taskDate: null);
        var options = new TaskFilterOptions { SelectedDate = BaseDate };

        var result = TaskFilterService.Apply(options, [createdOnDay, createdOtherDay]);

        Assert.Equal([createdOnDay], result);
    }

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public void Apply_SelectedDateWithRegularTemplates_KeepsTemplatesOnlyWhenRequested(bool keepTemplates, int expectedCount)
    {
        var onDay = CreateTask(taskDate: BaseDate);
        var laterDay = BaseDate.AddDays(5);
        var template = CreateTask(isRegular: true, taskDate: laterDay);
        var options = new TaskFilterOptions
        {
            SelectedDate = BaseDate,
            KeepRegularTemplatesVisibleWhenDayFiltered = keepTemplates
        };

        var result = TaskFilterService.Apply(options, [onDay, template]);

        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void Apply_SortDoneTasksLastDescending_PutsDoneTasksAtEndNewestFirstWithinGroups()
    {
        var openOld = CreateTask(creationDayOffset: 0);
        var openNew = CreateTask(creationDayOffset: 1);
        var doneOld = CreateTask(creationDayOffset: 2, isDone: true);
        var doneNew = CreateTask(creationDayOffset: 3, isDone: true);
        var options = new TaskFilterOptions { SortDoneTasksLast = true, IsSortDescending = true };

        var result = TaskFilterService.Apply(options, [doneOld, openOld, doneNew, openNew]);

        Assert.Equal([openNew, openOld, doneNew, doneOld], result);
    }

    [Fact]
    public void Apply_SortDoneTasksLastAscending_PutsDoneTasksAtEndOldestFirstWithinGroups()
    {
        var openOld = CreateTask(creationDayOffset: 0);
        var openNew = CreateTask(creationDayOffset: 1);
        var doneOld = CreateTask(creationDayOffset: 2, isDone: true);
        var doneNew = CreateTask(creationDayOffset: 3, isDone: true);
        var options = new TaskFilterOptions { SortDoneTasksLast = true, IsSortDescending = false };

        var result = TaskFilterService.Apply(options, [doneNew, openNew, doneOld, openOld]);

        Assert.Equal([openOld, openNew, doneOld, doneNew], result);
    }

    [Fact]
    public void Apply_CombinedFilters_AppliesAllConditions()
    {
        var match = CreateTask(note: "Bericht", priority: TaskPriority.Hoch, isPositive: true, taskDate: BaseDate);
        var wrongPriority = CreateTask(note: "Bericht", priority: TaskPriority.Wenig, isPositive: true, taskDate: BaseDate);
        var nextDay = BaseDate.AddDays(1);
        var wrongDay = CreateTask(note: "Bericht", priority: TaskPriority.Hoch, isPositive: true, taskDate: nextDay);
        var options = new TaskFilterOptions
        {
            SearchText = "bericht",
            SelectedPriorities = [TaskPriority.Hoch],
            IsPositive = true,
            SelectedDate = BaseDate
        };

        var result = TaskFilterService.Apply(options, [match, wrongPriority, wrongDay]);

        Assert.Equal([match], result);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    public void IsRegularTemplate_RegularAndDoneFlags_ReturnsExpectedResult(bool isRegular, bool isDone, bool expected)
    {
        var task = CreateTask(isRegular: isRegular, isDone: isDone);

        var result = TaskFilterService.IsRegularTemplate(task);

        Assert.Equal(expected, result);
    }

    private static TaskDto CreateTask(
        string? note = "Aufgabe",
        int creationDayOffset = 0,
        DateTime? taskDate = null,
        TaskPriority priority = TaskPriority.Keine,
        bool isDone = false,
        bool isRegular = false,
        bool isPositive = false)
    {
        return new TaskDto
        {
            Id = Guid.NewGuid(),
            Note = note,
            CreationDate = BaseDate.AddDays(creationDayOffset),
            TaskDate = taskDate,
            Priority = priority,
            IsDone = isDone,
            IsRegularTask = isRegular,
            IsPositive = isPositive
        };
    }
}
