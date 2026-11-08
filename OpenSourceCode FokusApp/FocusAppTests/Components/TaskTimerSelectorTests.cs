using Bunit;
using FokusApp.Components.PopUps.TaskEdit;
using Microsoft.AspNetCore.Components;

namespace FocusAppTests.Components;

public class TaskTimerSelectorTests : ComponentTestContext
{
    private int _decreaseCount;
    private int _increaseCount;
    private int _manualCount;

    [Fact]
    public void Render_Values_ShowsLabelAndDisplayWithAccessibleButtons()
    {
        var cut = RenderSelector();

        var label = cut.Find("label");
        var display = cut.Find(".time-display");
        var buttons = cut.FindAll(".timer-step-button");
        Assert.Equal("Fokuszeit", label.TextContent);
        Assert.Equal("00:25", display.TextContent.Trim());
        Assert.Equal("Weniger", buttons[0].GetAttribute("aria-label"));
        Assert.Equal("Mehr", buttons[1].GetAttribute("aria-label"));
        Assert.Equal("Eingeben", display.GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_NoParameters_ShowsEmptyTexts()
    {
        var cut = Render<TaskTimerSelector>();

        Assert.Equal(string.Empty, cut.Find("label").TextContent);
        Assert.Equal(string.Empty, cut.Find(".time-display").TextContent.Trim());
    }

    [Fact]
    public void ClickDecrease_Always_ReportsDecrease()
    {
        var cut = RenderSelector();

        cut.FindAll(".timer-step-button")[0].Click();

        Assert.Equal(1, _decreaseCount);
        Assert.Equal(0, _increaseCount);
        Assert.Equal(0, _manualCount);
    }

    [Fact]
    public void ClickIncrease_Always_ReportsIncrease()
    {
        var cut = RenderSelector();

        cut.FindAll(".timer-step-button")[1].Click();

        Assert.Equal(0, _decreaseCount);
        Assert.Equal(1, _increaseCount);
        Assert.Equal(0, _manualCount);
    }

    [Fact]
    public void ClickDisplay_Always_RequestsManualInput()
    {
        var cut = RenderSelector();

        cut.Find(".time-display").Click();

        Assert.Equal(0, _decreaseCount);
        Assert.Equal(0, _increaseCount);
        Assert.Equal(1, _manualCount);
    }

    private IRenderedComponent<TaskTimerSelector> RenderSelector()
    {
        return Render<TaskTimerSelector>(parameters => parameters
            .Add(selector => selector.Label, "Fokuszeit")
            .Add(selector => selector.Display, "00:25")
            .Add(selector => selector.DecreaseAriaLabel, "Weniger")
            .Add(selector => selector.IncreaseAriaLabel, "Mehr")
            .Add(selector => selector.ManualInputAriaLabel, "Eingeben")
            .Add(selector => selector.OnDecrease, EventCallback.Factory.Create(this, () => _decreaseCount++))
            .Add(selector => selector.OnIncrease, EventCallback.Factory.Create(this, () => _increaseCount++))
            .Add(selector => selector.OnManualInput, EventCallback.Factory.Create(this, () => _manualCount++)));
    }
}
