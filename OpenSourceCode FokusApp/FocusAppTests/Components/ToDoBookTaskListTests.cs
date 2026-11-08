using Bunit;
using FokusApp.Components.ToDoBook;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using Microsoft.AspNetCore.Components;

namespace FocusAppTests.Components;

public class ToDoBookTaskListTests : ComponentTestContext
{
    private readonly List<Guid> _clickedTasks = [];
    private int _addCount;

    [Fact]
    public void Render_NoTasks_ShowsTitleAddButtonAndEmptyHint()
    {
        var cut = RenderList([]);

        Assert.Equal("Für mich", cut.Find("h2").TextContent);
        Assert.Equal("Hinzufügen", cut.Find(".todo-book-add-button").GetAttribute("aria-label"));
        Assert.Equal(Text("NoTasksForDay"), cut.Find(".todo-book-empty").TextContent);
        Assert.Empty(cut.FindAll(".todo-book-task-item"));
    }

    [Fact]
    public void Render_Tasks_ListsEachWithNotePriorityAndNoEmptyHint()
    {
        var first = CreateTask("Lesen", TaskPriority.Hoch);
        var second = CreateTask("Malen", TaskPriority.Keine);

        var cut = RenderList([first, second]);

        var items = cut.FindAll(".todo-book-task-item");
        Assert.Equal(2, items.Count);
        Assert.Equal("Lesen", items[0].QuerySelector(".todo-book-task-note")!.TextContent);
        Assert.Equal(Text("Priority", TaskPriority.Hoch.ToString()), items[0].QuerySelector(".task-priority")!.TextContent);
        Assert.Empty(cut.FindAll(".todo-book-empty"));
    }

    [Fact]
    public void Render_DoneTask_IsMarkedAsDoneWithStateLabel()
    {
        var done = CreateTask("Fertig", TaskPriority.Keine);
        done.IsDone = true;
        var open = CreateTask("Offen", TaskPriority.Keine);

        var cut = RenderList([done, open]);

        var items = cut.FindAll(".todo-book-task-item");
        Assert.True(items[0].ClassList.Contains("is-done"));
        Assert.Equal(Text("Done"), items[0].QuerySelector(".todo-book-task-state")!.TextContent);
        Assert.False(items[1].ClassList.Contains("is-done"));
        Assert.Null(items[1].QuerySelector(".todo-book-task-state"));
    }

    [Fact]
    public void Render_JustCompletedDoneTask_GetsTheHighlightClass()
    {
        var done = CreateTask("Fertig", TaskPriority.Keine);
        done.IsDone = true;

        var cut = RenderList([done], justCompleted: done.Id);

        Assert.True(cut.Find(".todo-book-task-item").ClassList.Contains("just-done"));
    }

    [Fact]
    public void Render_JustCompletedIdOfOpenTask_GetsNoHighlight()
    {
        var open = CreateTask("Offen", TaskPriority.Keine);

        var cut = RenderList([open], justCompleted: open.Id);

        Assert.False(cut.Find(".todo-book-task-item").ClassList.Contains("just-done"));
    }

    [Fact]
    public void ClickTask_TaskWithId_ReportsTheId()
    {
        var task = CreateTask("Lesen", TaskPriority.Keine);
        var cut = RenderList([task]);

        cut.Find(".todo-book-task-item").Click();

        var clicked = Assert.Single(_clickedTasks);
        Assert.Equal(task.Id, clicked);
    }

    [Fact]
    public void ClickTask_TaskWithoutId_ReportsNothing()
    {
        var task = CreateTask("Lesen", TaskPriority.Keine);
        task.Id = null;
        var cut = RenderList([task]);

        cut.Find(".todo-book-task-item").Click();

        Assert.Empty(_clickedTasks);
    }

    [Fact]
    public void ClickAdd_Always_ReportsTheAddRequest()
    {
        var cut = RenderList([]);

        cut.Find(".todo-book-add-button").Click();

        Assert.Equal(1, _addCount);
    }

    [Fact]
    public void Render_TaskWithTimeAndRepetition_ShowsTimeAndRepeatIconInTheMeta()
    {
        var task = CreateTask("Lesen", TaskPriority.Keine);
        task.TaskTime = new TimeSpan(7, 0, 0);
        task.RepeatUnit = TaskRepeatUnit.Day;

        var cut = RenderList([task]);

        var meta = cut.Find(".todo-book-task-meta");
        Assert.Equal("07:00", meta.QuerySelector(".task-schedule-time")!.TextContent.Trim());
        Assert.NotNull(meta.QuerySelector(".task-schedule-repeat"));
    }

    private IRenderedComponent<ToDoBookTaskList> RenderList(IReadOnlyList<TaskDto> tasks, Guid? justCompleted = null)
    {
        return Render<ToDoBookTaskList>(parameters => parameters
            .Add(list => list.Title, "Für mich")
            .Add(list => list.AddButtonAriaLabel, "Hinzufügen")
            .Add(list => list.Tasks, tasks)
            .Add(list => list.JustCompletedTaskId, justCompleted)
            .Add(list => list.OnAdd, EventCallback.Factory.Create(this, () => _addCount++))
            .Add(list => list.OnTaskClick, EventCallback.Factory.Create<Guid>(this, id => _clickedTasks.Add(id))));
    }

    private static TaskDto CreateTask(string note, TaskPriority priority)
    {
        return new TaskDto { Id = Guid.NewGuid(), Note = note, Priority = priority };
    }
}
