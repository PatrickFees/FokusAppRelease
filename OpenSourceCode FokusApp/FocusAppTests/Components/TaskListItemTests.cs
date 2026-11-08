using Bunit;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Components.TaskOverViewComponent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FocusAppTests.Components;

public class TaskListItemTests : ComponentTestContext
{
    private readonly List<Guid> _edited = [];
    private readonly List<Guid> _deleted = [];
    private readonly List<Guid> _rescheduled = [];
    private readonly List<(Guid TaskId, PastTaskTarget Target)> _assigned = [];
    private readonly List<Guid> _discarded = [];

    [Fact]
    public void Render_ActiveTask_ShowsNoteCreationDayPriorityAndOpenStatus()
    {
        var task = CreateTask("Bericht", TaskPriority.Hoch);

        var cut = RenderItem(task, TaskListItem.TaskListItemType.Active);

        Assert.Equal("Bericht", cut.Find(".task-description").TextContent);
        Assert.Equal(Text("CreatedOn", task.CreationDate), cut.Find(".task-due-date").TextContent);
        Assert.Equal(Text("Priority", TaskPriority.Hoch.ToString()), cut.Find(".task-priority").TextContent);
        Assert.Equal(Text("Status", Text("Open")), cut.Find(".task-estimate").TextContent);
    }

    [Fact]
    public void Render_TaskWithTimeAndRepetition_ShowsTimeAndRepeatIcon()
    {
        var task = CreateTask("Sport");
        task.TaskTime = new TimeSpan(18, 30, 0);
        task.RepeatUnit = TaskRepeatUnit.Week;

        var cut = RenderItem(task, TaskListItem.TaskListItemType.Active);

        Assert.Equal("18:30", cut.Find(".task-schedule-time").TextContent.Trim());
        Assert.NotEmpty(cut.FindAll(".task-schedule-repeat"));
    }

    [Fact]
    public void Render_TaskWithoutSchedule_ShowsNoTimeAndNoRepeatIcon()
    {
        var task = CreateTask("Bericht");

        var cut = RenderItem(task, TaskListItem.TaskListItemType.Active);

        Assert.Empty(cut.FindAll(".task-schedule-time"));
        Assert.Empty(cut.FindAll(".task-schedule-repeat"));
    }

    [Fact]
    public void Render_RunningActiveTask_ShowsActiveStatusAndOnlyTheRunningButton()
    {
        var task = CreateTask("Bericht");

        var cut = RenderItem(task, TaskListItem.TaskListItemType.Active, isRunning: true);

        Assert.Equal(Text("Status", Text("Active")), cut.Find(".task-estimate").TextContent);
        var running = cut.Find(".task-action.running");
        Assert.True(running.HasAttribute("disabled"));
        Assert.Equal(Text("TaskCurrentlyRunning"), running.TextContent.Trim());
        Assert.Empty(cut.FindAll(".task-action.edit"));
        Assert.Empty(cut.FindAll(".task-action.delete"));
    }

    [Fact]
    public void Render_Template_ShowsTemplateLabelsWithoutCreationDay()
    {
        var task = CreateTask("Vorlage");

        var cut = RenderItem(task, TaskListItem.TaskListItemType.Template);

        Assert.Equal(Text("TemplateWithoutDate"), cut.Find(".task-due-date").TextContent);
        Assert.Equal(Text("Status", Text("Template")), cut.Find(".task-estimate").TextContent);
    }

    [Fact]
    public void Render_CompletedTask_ShowsCompletionDayAndNoStatus()
    {
        var task = CreateTask("Fertig");

        var cut = RenderItem(task, TaskListItem.TaskListItemType.Completed);

        Assert.Equal(Text("CompletedOn", task.CreationDate), cut.Find(".task-due-date").TextContent);
        Assert.Empty(cut.FindAll(".task-estimate"));
    }

    [Fact]
    public void Render_OverdueTask_ShowsPastStyleOpenStatusAndAssignChips()
    {
        var task = CreateTask("Gestern");

        var cut = RenderItem(task, TaskListItem.TaskListItemType.Overdue);

        Assert.True(cut.Find(".task-item").ClassList.Contains("is-past"));
        Assert.Equal(Text("Status", Text("Open")), cut.Find(".task-estimate").TextContent);
        Assert.Equal(4, cut.FindAll(".task-assign-chip").Count);
    }

    [Fact]
    public void Render_RunningOverdueTask_ShowsNoAssignChips()
    {
        var task = CreateTask("Gestern");

        var cut = RenderItem(task, TaskListItem.TaskListItemType.Overdue, isRunning: true);

        Assert.Empty(cut.FindAll(".task-assign-chip"));
    }

    [Theory]
    [InlineData(TaskListItem.TaskListItemType.Active)]
    [InlineData(TaskListItem.TaskListItemType.Template)]
    [InlineData(TaskListItem.TaskListItemType.Completed)]
    public void Render_NotOverdue_ShowsNoAssignChipsAndNoPastStyle(TaskListItem.TaskListItemType type)
    {
        var cut = RenderItem(CreateTask("Aufgabe"), type);

        Assert.Empty(cut.FindAll(".task-assign-chip"));
        Assert.False(cut.Find(".task-item").ClassList.Contains("is-past"));
    }

    [Fact]
    public void ClickEdit_TaskWithId_ReportsTheId()
    {
        var task = CreateTask("Bericht");
        var cut = RenderItem(task, TaskListItem.TaskListItemType.Active);

        cut.Find(".task-action.edit").Click();

        var edited = Assert.Single(_edited);
        Assert.Equal(task.Id, edited);
    }

    [Fact]
    public void ClickEdit_TaskWithoutId_ReportsNothing()
    {
        var task = CreateTask("Bericht");
        task.Id = null;
        var cut = RenderItem(task, TaskListItem.TaskListItemType.Active);

        cut.Find(".task-action.edit").Click();

        Assert.Empty(_edited);
    }

    [Fact]
    public void ClickDelete_TaskWithId_ReportsTheId()
    {
        var task = CreateTask("Bericht");
        var cut = RenderItem(task, TaskListItem.TaskListItemType.Active);

        cut.Find(".task-action.delete").Click();

        var deleted = Assert.Single(_deleted);
        Assert.Equal(task.Id, deleted);
    }

    [Fact]
    public void ClickDelete_TaskWithoutId_ReportsNothing()
    {
        var task = CreateTask("Bericht");
        task.Id = null;
        var cut = RenderItem(task, TaskListItem.TaskListItemType.Active);

        cut.Find(".task-action.delete").Click();

        Assert.Empty(_deleted);
    }

    [Theory]
    [InlineData(0, PastTaskTarget.Today)]
    [InlineData(1, PastTaskTarget.Tomorrow)]
    public void ClickAssignChip_Target_ReportsTheTaskWithThatTargetAfterTheLeaveAnimation(int chipIndex, PastTaskTarget expected)
    {
        var task = CreateTask("Gestern");
        var cut = RenderItem(task, TaskListItem.TaskListItemType.Overdue);

        cut.FindAll(".task-assign-chip")[chipIndex].Click();

        cut.WaitForAssertion(() => Assert.Single(_assigned));
        Assert.Equal((task.Id!.Value, expected), _assigned[0]);
    }

    [Fact]
    public void ClickAssignChip_Task_MarksTheItemAsLeaving()
    {
        var task = CreateTask("Gestern");
        var cut = RenderItem(task, TaskListItem.TaskListItemType.Overdue);

        cut.FindAll(".task-assign-chip")[0].Click();

        cut.WaitForAssertion(() => Assert.True(cut.Find(".task-item").ClassList.Contains("is-leaving")));
    }

    [Fact]
    public void ClickAssignChip_TaskWithoutId_ReportsNothing()
    {
        var task = CreateTask("Gestern");
        task.Id = null;
        var cut = RenderItem(task, TaskListItem.TaskListItemType.Overdue);

        cut.FindAll(".task-assign-chip")[0].Click();

        Assert.Empty(_assigned);
        Assert.False(cut.Find(".task-item").ClassList.Contains("is-leaving"));
    }

    [Fact]
    public void ClickRescheduleChip_TaskWithId_ReportsTheId()
    {
        var task = CreateTask("Gestern");
        var cut = RenderItem(task, TaskListItem.TaskListItemType.Overdue);

        cut.FindAll(".task-assign-chip")[2].Click();

        var rescheduled = Assert.Single(_rescheduled);
        Assert.Equal(task.Id, rescheduled);
    }

    [Fact]
    public void ClickRescheduleChip_TaskWithoutId_ReportsNothing()
    {
        var task = CreateTask("Gestern");
        task.Id = null;
        var cut = RenderItem(task, TaskListItem.TaskListItemType.Overdue);

        cut.FindAll(".task-assign-chip")[2].Click();

        Assert.Empty(_rescheduled);
    }

    [Fact]
    public void ClickDiscardChip_TaskWithId_ReportsTheIdAfterTheLeaveAnimation()
    {
        var task = CreateTask("Gestern");
        var cut = RenderItem(task, TaskListItem.TaskListItemType.Overdue);

        cut.Find(".task-assign-chip.discard").Click();

        cut.WaitForAssertion(() => Assert.Single(_discarded));
        Assert.Equal(task.Id, _discarded[0]);
    }

    [Fact]
    public void ClickDiscardChip_TaskWithoutId_ReportsNothing()
    {
        var task = CreateTask("Gestern");
        task.Id = null;
        var cut = RenderItem(task, TaskListItem.TaskListItemType.Overdue);

        cut.Find(".task-assign-chip.discard").Click();

        Assert.Empty(_discarded);
    }

    [Fact]
    public void Swipe_LeftThenRight_ShowsAndHidesTheActions()
    {
        var cut = RenderItem(CreateTask("Bericht"), TaskListItem.TaskListItemType.Active);

        Item(cut).PointerDown(new PointerEventArgs { ClientX = 200 });
        Item(cut).PointerUp(new PointerEventArgs { ClientX = 100 });
        Assert.True(Item(cut).ClassList.Contains("show-actions"));

        Item(cut).PointerDown(new PointerEventArgs { ClientX = 100 });
        Item(cut).PointerUp(new PointerEventArgs { ClientX = 200 });
        Assert.False(Item(cut).ClassList.Contains("show-actions"));
    }

    [Fact]
    public void Swipe_ShortMoveWhileActionsAreShown_KeepsThem()
    {
        var cut = RenderItem(CreateTask("Bericht"), TaskListItem.TaskListItemType.Active);
        Item(cut).PointerDown(new PointerEventArgs { ClientX = 200 });
        Item(cut).PointerUp(new PointerEventArgs { ClientX = 100 });

        Item(cut).PointerDown(new PointerEventArgs { ClientX = 100 });
        Item(cut).PointerUp(new PointerEventArgs { ClientX = 110 });

        Assert.True(Item(cut).ClassList.Contains("show-actions"));
    }

    [Fact]
    public void Swipe_ShortMoveWhileActionsAreHidden_KeepsThemHidden()
    {
        var cut = RenderItem(CreateTask("Bericht"), TaskListItem.TaskListItemType.Active);

        Item(cut).PointerDown(new PointerEventArgs { ClientX = 200 });
        Item(cut).PointerUp(new PointerEventArgs { ClientX = 190 });

        Assert.False(Item(cut).ClassList.Contains("show-actions"));
    }

    [Fact]
    public void PointerUp_WithoutPointerDown_ChangesNothing()
    {
        var cut = RenderItem(CreateTask("Bericht"), TaskListItem.TaskListItemType.Active);

        Item(cut).PointerUp(new PointerEventArgs { ClientX = 50 });

        Assert.False(Item(cut).ClassList.Contains("show-actions"));
    }

    [Theory]
    [InlineData("onpointerleave")]
    [InlineData("onpointercancel")]
    public void Swipe_PointerLeavesOrIsCancelled_ForgetsTheGesture(string resetEvent)
    {
        var cut = RenderItem(CreateTask("Bericht"), TaskListItem.TaskListItemType.Active);
        Item(cut).PointerDown(new PointerEventArgs { ClientX = 200 });

        Item(cut).TriggerEvent(resetEvent, new PointerEventArgs());
        Item(cut).PointerUp(new PointerEventArgs { ClientX = 50 });

        Assert.False(Item(cut).ClassList.Contains("show-actions"));
    }

    [Fact]
    public void Click_ContentWhileActionsAreShown_HidesTheActions()
    {
        var cut = RenderItem(CreateTask("Bericht"), TaskListItem.TaskListItemType.Active);
        Item(cut).PointerDown(new PointerEventArgs { ClientX = 200 });
        Item(cut).PointerUp(new PointerEventArgs { ClientX = 100 });

        cut.Find(".task-item-content").Click();

        Assert.False(Item(cut).ClassList.Contains("show-actions"));
    }

    [Fact]
    public void Render_UnknownListType_ShowsNoDateLabelAndNoStatus()
    {
        var unknownType = (TaskListItem.TaskListItemType)99;

        var cut = RenderItem(CreateTask("Aufgabe"), unknownType);

        Assert.Equal(string.Empty, cut.Find(".task-due-date").TextContent);
        Assert.Empty(cut.FindAll(".task-estimate"));
    }

    private IRenderedComponent<TaskListItem> RenderItem(
        TaskDto task,
        TaskListItem.TaskListItemType type,
        bool isRunning = false)
    {
        return Render<TaskListItem>(parameters => parameters
            .Add(item => item.Task, task)
            .Add(item => item.ListType, type)
            .Add(item => item.IsRunning, isRunning)
            .Add(item => item.OnEdit, EventCallback.Factory.Create<Guid>(this, id => _edited.Add(id)))
            .Add(item => item.OnDelete, EventCallback.Factory.Create<Guid>(this, id => _deleted.Add(id)))
            .Add(item => item.OnReschedule, EventCallback.Factory.Create<Guid>(this, id => _rescheduled.Add(id)))
            .Add(item => item.OnAssign, EventCallback.Factory.Create<(Guid TaskId, PastTaskTarget Target)>(this, args => _assigned.Add(args)))
            .Add(item => item.OnDiscard, EventCallback.Factory.Create<Guid>(this, id => _discarded.Add(id))));
    }

    private static AngleSharp.Dom.IElement Item(IRenderedComponent<TaskListItem> cut)
    {
        return cut.Find(".task-item");
    }

    private static TaskDto CreateTask(string note, TaskPriority priority = TaskPriority.Keine)
    {
        return new TaskDto
        {
            Id = Guid.NewGuid(),
            Note = note,
            Priority = priority,
            CreationDate = new DateTime(2026, 3, 10, 8, 0, 0)
        };
    }
}
