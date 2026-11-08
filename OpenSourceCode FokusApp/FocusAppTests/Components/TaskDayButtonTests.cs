using System.Globalization;
using Bunit;
using FokusApp.Components.SharedUI.TaskDayButton;
using Microsoft.AspNetCore.Components;

namespace FocusAppTests.Components;

public class TaskDayButtonTests : ComponentTestContext
{
    private int _clickCount;

    [Fact]
    public void Render_DateOnly_ShowsTheLongDate()
    {
        var model = new TaskDayButtonModel { SelectedDate = new DateTime(2026, 3, 10) };
        var expected = model.SelectedDate.ToString("D", CultureInfo.CurrentCulture);

        var cut = RenderButton(model);

        Assert.Equal(expected, cut.Find("button").TextContent.Trim());
    }

    [Fact]
    public void Render_CustomText_ShowsThatTextInsteadOfTheDate()
    {
        var model = new TaskDayButtonModel { SelectedDate = new DateTime(2026, 3, 10), CustomText = "Alle Tage" };

        var cut = RenderButton(model);

        Assert.Equal("Alle Tage", cut.Find("button").TextContent.Trim());
    }

    [Fact]
    public void Click_Enabled_ReportsTheClick()
    {
        var cut = RenderButton(new TaskDayButtonModel { IsEnabled = true });

        cut.Find("button").Click();

        Assert.Equal(1, _clickCount);
    }

    [Fact]
    public void Render_Disabled_DisablesTheButton()
    {
        var cut = RenderButton(new TaskDayButtonModel { IsEnabled = false });

        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void Click_DisabledButtonClicked_ReportsNothing()
    {
        var cut = RenderButton(new TaskDayButtonModel { IsEnabled = false });

        cut.Find("button").TriggerEvent("onclick", new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.Equal(0, _clickCount);
    }

    private IRenderedComponent<TaskDayButton> RenderButton(TaskDayButtonModel model)
    {
        return Render<TaskDayButton>(parameters => parameters
            .Add(button => button.Model, model)
            .Add(button => button.OnClick, EventCallback.Factory.Create(this, () => _clickCount++)));
    }
}
