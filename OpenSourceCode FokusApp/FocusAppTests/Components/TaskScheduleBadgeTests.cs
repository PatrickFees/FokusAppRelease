using Bunit;
using FokusApp.Components.SharedUI.TaskScheduleBadge;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;

namespace FocusAppTests.Components;

public class TaskScheduleBadgeTests : ComponentTestContext
{
    [Fact]
    public void Render_TaskWithoutTimeAndRepetition_ShowsNothing()
    {
        var cut = RenderBadge(new TaskDto());

        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void Render_TaskWithTime_ShowsTheTimeWithLeadingZeros()
    {
        var cut = RenderBadge(new TaskDto { TaskTime = new TimeSpan(7, 5, 0) });

        var time = cut.Find(".task-schedule-time");
        Assert.Equal("07:05", time.TextContent.Trim());
        Assert.Equal(Text("TaskTimeAria", "07:05"), time.GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll(".task-schedule-repeat"));
    }

    [Fact]
    public void Render_RepeatingTask_ShowsTheRepeatIcon()
    {
        var cut = RenderBadge(new TaskDto { RepeatUnit = TaskRepeatUnit.Week });

        var repeat = cut.Find(".task-schedule-repeat");
        Assert.Equal(Text("RepeatingTask"), repeat.GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll(".task-schedule-time"));
    }

    [Fact]
    public void Render_TaskOfASeries_ShowsTheRepeatIconEvenWithoutOwnRule()
    {
        var cut = RenderBadge(new TaskDto { SeriesId = Guid.NewGuid() });

        Assert.NotEmpty(cut.FindAll(".task-schedule-repeat"));
    }

    private IRenderedComponent<TaskScheduleBadge> RenderBadge(TaskDto task)
    {
        return Render<TaskScheduleBadge>(parameters => parameters.Add(badge => badge.Task, task));
    }
}
