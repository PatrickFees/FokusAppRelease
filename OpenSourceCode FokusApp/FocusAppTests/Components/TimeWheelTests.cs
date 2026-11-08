using Bunit;
using FokusApp.Components.SharedUI.TimeWheel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FocusAppTests.Components;

public class TimeWheelTests : ComponentTestContext
{
    private readonly List<int> _changedValues = [];

    [Fact]
    public void Render_Range_ShowsOneItemPerNumberWithLeadingZero()
    {
        var cut = RenderWheel(minimum: 0, maximum: 23, value: 9);

        var items = cut.FindAll(".time-wheel-item");
        Assert.Equal(24, items.Count);
        Assert.Equal("00", items[0].TextContent.Trim());
        Assert.Equal("09", items[9].TextContent.Trim());
        Assert.Equal("23", items[23].TextContent.Trim());
    }

    [Fact]
    public void Render_Value_MarksOnlyThatItemAsSelected()
    {
        var cut = RenderWheel(minimum: 0, maximum: 59, value: 30);

        var selected = cut.FindAll(".time-wheel-item.selected");
        Assert.Single(selected);
        Assert.Equal("30", selected[0].TextContent.Trim());
    }

    [Fact]
    public void Render_WithoutValue_MarksNoItemAndTheWheelAsUnset()
    {
        var cut = RenderWheel(minimum: 0, maximum: 59, value: 30, hasValue: false);

        Assert.Empty(cut.FindAll(".time-wheel-item.selected"));
        Assert.True(cut.Find(".time-wheel").ClassList.Contains("is-unset"));
    }

    [Fact]
    public void Render_WithoutValueAndPlaceholder_ShowsThePlaceholderOnTheCenterPosition()
    {
        var cut = RenderWheel(minimum: 0, maximum: 23, value: 9, hasValue: false, placeholderText: "Keine");

        Assert.Equal("Keine", cut.Find(".time-wheel-highlight .time-wheel-placeholder").TextContent.Trim());
    }

    [Fact]
    public void Render_WithValueAndPlaceholder_ShowsNoPlaceholder()
    {
        var cut = RenderWheel(minimum: 0, maximum: 23, value: 9, placeholderText: "Keine");

        Assert.Empty(cut.FindAll(".time-wheel-placeholder"));
    }

    [Fact]
    public void Render_WithoutValueAndWithoutPlaceholder_ShowsNoPlaceholder()
    {
        var cut = RenderWheel(minimum: 0, maximum: 23, value: 9, hasValue: false);

        Assert.Empty(cut.FindAll(".time-wheel-placeholder"));
    }

    [Fact]
    public void Render_Wheel_IsAnAccessibleSpinButton()
    {
        var cut = RenderWheel(minimum: 0, maximum: 23, value: 9);

        var wheel = cut.Find(".time-wheel");
        Assert.Equal("spinbutton", wheel.GetAttribute("role"));
        Assert.Equal("Stunden", wheel.GetAttribute("aria-label"));
        Assert.Equal("0", wheel.GetAttribute("aria-valuemin"));
        Assert.Equal("23", wheel.GetAttribute("aria-valuemax"));
        Assert.Equal("9", wheel.GetAttribute("aria-valuenow"));
    }

    [Fact]
    public void Render_FirstRender_LoadsTheWheelModule()
    {
        RenderWheel(minimum: 0, maximum: 23, value: 9);

        JSInterop.VerifyInvoke("import");
    }

    [Fact]
    public void Click_Item_ReportsThatNumber()
    {
        var cut = RenderWheel(minimum: 0, maximum: 23, value: 9);

        cut.FindAll(".time-wheel-item")[14].Click();

        Assert.Equal([14], _changedValues);
    }

    [Fact]
    public void Click_ItemWithMinimumAboveZero_ReportsTheNumberNotTheIndex()
    {
        var cut = RenderWheel(minimum: 5, maximum: 10, value: 5);

        cut.FindAll(".time-wheel-item")[2].Click();

        Assert.Equal([7], _changedValues);
    }

    [Theory]
    [InlineData("ArrowDown", 10, 11)]
    [InlineData("ArrowUp", 10, 9)]
    [InlineData("PageDown", 10, 15)]
    [InlineData("PageUp", 10, 5)]
    [InlineData("ArrowUp", 0, 0)]
    [InlineData("ArrowDown", 23, 23)]
    public void KeyDown_ArrowAndPageKeys_StepTheValueWithinTheRange(string key, int value, int expected)
    {
        var cut = RenderWheel(minimum: 0, maximum: 23, value: value);

        cut.Find(".time-wheel").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.Equal([expected], _changedValues);
    }

    [Fact]
    public void KeyDown_OtherKey_ReportsNothing()
    {
        var cut = RenderWheel(minimum: 0, maximum: 23, value: 9);

        cut.Find(".time-wheel").KeyDown(new KeyboardEventArgs { Key = "a" });

        Assert.Empty(_changedValues);
    }

    [Fact]
    public async Task OnWheelSettled_OtherIndex_ReportsTheNumberOfThatIndex()
    {
        var cut = RenderWheel(minimum: 0, maximum: 59, value: 10);

        await cut.InvokeAsync(() => cut.Instance.OnWheelSettled(42));

        Assert.Equal([42], _changedValues);
    }

    [Fact]
    public async Task OnWheelSettled_IndexBeyondTheRange_IsClamped()
    {
        var cut = RenderWheel(minimum: 0, maximum: 23, value: 10);

        await cut.InvokeAsync(() => cut.Instance.OnWheelSettled(99));

        Assert.Equal([23], _changedValues);
    }

    [Fact]
    public async Task OnWheelSettled_SameIndexAsValue_ReportsNothing()
    {
        var cut = RenderWheel(minimum: 0, maximum: 23, value: 10);

        await cut.InvokeAsync(() => cut.Instance.OnWheelSettled(10));

        Assert.Empty(_changedValues);
    }

    [Fact]
    public async Task OnWheelSettled_SameIndexWhileNoValueYet_ReportsItSoTheValueGetsPicked()
    {
        var cut = RenderWheel(minimum: 0, maximum: 23, value: 9, hasValue: false);

        await cut.InvokeAsync(() => cut.Instance.OnWheelSettled(9));

        Assert.Equal([9], _changedValues);
    }

    [Fact]
    public async Task OnWheelMoved_WhileNoValueYet_ReportsTheShownValueAtOnce()
    {
        var cut = RenderWheel(minimum: 0, maximum: 23, value: 9, hasValue: false);

        await cut.InvokeAsync(() => cut.Instance.OnWheelMoved());

        Assert.Equal([9], _changedValues);
    }

    [Fact]
    public async Task OnWheelMoved_WithValue_ReportsNothing()
    {
        var cut = RenderWheel(minimum: 0, maximum: 23, value: 9);

        await cut.InvokeAsync(() => cut.Instance.OnWheelMoved());

        Assert.Empty(_changedValues);
    }

    private IRenderedComponent<TimeWheel> RenderWheel(int minimum, int maximum, int value, bool hasValue = true, string placeholderText = "")
    {
        return Render<TimeWheel>(parameters => parameters
            .Add(wheel => wheel.Minimum, minimum)
            .Add(wheel => wheel.Maximum, maximum)
            .Add(wheel => wheel.Value, value)
            .Add(wheel => wheel.HasValue, hasValue)
            .Add(wheel => wheel.PlaceholderText, placeholderText)
            .Add(wheel => wheel.AriaLabel, "Stunden")
            .Add(wheel => wheel.ValueChanged, EventCallback.Factory.Create<int>(this, number => _changedValues.Add(number))));
    }
}
