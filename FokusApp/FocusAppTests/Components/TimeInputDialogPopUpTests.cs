using Bunit;
using FokusApp.Components.PopUps.TimeInputDialog;
using Microsoft.AspNetCore.Components;

namespace FocusAppTests.Components;

public class TimeInputDialogPopUpTests : ComponentTestContext
{
    private readonly List<int> _confirmedMinutes = [];
    private int _cancelCount;

    [Fact]
    public void Render_Closed_ShowsNothing()
    {
        var cut = RenderDialog(isOpen: false);

        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void Render_Open_ShowsInitialMinutesAndDefaultTitle()
    {
        var cut = RenderDialog(isOpen: true, initialMinutes: 25);

        var input = cut.Find("input");
        var title = cut.Find(".timer-input-title");
        Assert.Equal("25", input.GetAttribute("value"));
        Assert.Equal(Text("SetTimer"), title.TextContent);
        Assert.Empty(cut.FindAll(".timer-input-error"));
    }

    [Fact]
    public void Render_OpenWithTitle_ShowsGivenTitle()
    {
        var cut = RenderDialog(isOpen: true, title: "Pause einstellen");

        var title = cut.Find(".timer-input-title");
        Assert.Equal("Pause einstellen", title.TextContent);
    }

    [Fact]
    public void Render_Open_LabelShowsMaximumMinutes()
    {
        var cut = RenderDialog(isOpen: true);

        var label = cut.Find(".timer-input-label");
        var input = cut.Find("input");
        Assert.Equal(Text("MinutesMaximum", 120), label.TextContent);
        Assert.Equal("120", input.GetAttribute("max"));
        Assert.Equal("1", input.GetAttribute("min"));
    }

    [Theory]
    [InlineData(300, "120")]
    [InlineData(-5, "0")]
    public void Render_InitialMinutesOutsideRange_ShowsClampedValue(int initialMinutes, string expectedInput)
    {
        var cut = RenderDialog(isOpen: true, initialMinutes: initialMinutes);

        var input = cut.Find("input");
        Assert.Equal(expectedInput, input.GetAttribute("value"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("2,5")]
    public void Submit_InvalidInput_ShowsErrorAndDoesNotConfirm(string input)
    {
        var cut = RenderDialog(isOpen: true, initialMinutes: 25);

        EnterAndSubmit(cut, input);

        var error = cut.Find(".timer-input-error");
        Assert.Equal(Text("EnterMinutesError"), error.TextContent);
        Assert.Empty(_confirmedMinutes);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("5", 5)]
    [InlineData("119", 119)]
    [InlineData("120", 120)]
    public void Submit_ValidInput_ConfirmsMinutes(string input, int expectedMinutes)
    {
        var cut = RenderDialog(isOpen: true, initialMinutes: 25);

        EnterAndSubmit(cut, input);

        var confirmed = Assert.Single(_confirmedMinutes);
        Assert.Equal(expectedMinutes, confirmed);
        Assert.Empty(cut.FindAll(".timer-input-error"));
    }

    [Theory]
    [InlineData("121")]
    [InlineData("500")]
    [InlineData("99999")]
    public void Submit_MoreThanMaximum_ConfirmsMaximum(string input)
    {
        var cut = RenderDialog(isOpen: true, initialMinutes: 25);

        EnterAndSubmit(cut, input);

        var confirmed = Assert.Single(_confirmedMinutes);
        Assert.Equal(120, confirmed);
        Assert.Empty(cut.FindAll(".timer-input-error"));
    }

    [Fact]
    public void Submit_UnchangedInitialMinutes_ConfirmsInitialMinutes()
    {
        var cut = RenderDialog(isOpen: true, initialMinutes: 25);

        cut.Find("form").Submit();

        var confirmed = Assert.Single(_confirmedMinutes);
        Assert.Equal(25, confirmed);
    }

    [Fact]
    public void Submit_AfterInvalidInputWasCorrected_ConfirmsAndHidesError()
    {
        var cut = RenderDialog(isOpen: true, initialMinutes: 25);
        EnterAndSubmit(cut, "0");

        EnterAndSubmit(cut, "30");

        var confirmed = Assert.Single(_confirmedMinutes);
        Assert.Equal(30, confirmed);
        Assert.Empty(cut.FindAll(".timer-input-error"));
    }

    [Fact]
    public void ChangeInput_AfterError_HidesError()
    {
        var cut = RenderDialog(isOpen: true, initialMinutes: 25);
        EnterAndSubmit(cut, "0");
        Assert.Single(cut.FindAll(".timer-input-error"));

        cut.Find("input").Input("3");

        Assert.Empty(cut.FindAll(".timer-input-error"));
    }

    [Fact]
    public void Reopen_AfterError_ResetsErrorAndInput()
    {
        var cut = RenderDialog(isOpen: true, initialMinutes: 25);
        EnterAndSubmit(cut, "0");
        cut.Render(parameters => parameters.Add(x => x.IsOpen, false));

        cut.Render(parameters => parameters.Add(x => x.IsOpen, true));

        var input = cut.Find("input");
        Assert.Equal("25", input.GetAttribute("value"));
        Assert.Empty(cut.FindAll(".timer-input-error"));
    }

    [Fact]
    public void ClickCancelButton_CallsOnCancel()
    {
        var cut = RenderDialog(isOpen: true);
        var cancelButton = cut.Find("button.btn-secondary");

        cancelButton.Click();

        Assert.Equal(1, _cancelCount);
        Assert.Empty(_confirmedMinutes);
    }

    [Fact]
    public void ClickOverlay_CallsOnCancel()
    {
        var cut = RenderDialog(isOpen: true);
        var overlay = cut.Find(".timer-input-overlay");

        overlay.Click();

        Assert.Equal(1, _cancelCount);
    }

    [Fact]
    public void ClickInsideDialog_DoesNotReachOverlay()
    {
        var cut = RenderDialog(isOpen: true);
        var title = cut.Find(".timer-input-title");

        // bUnit wirft, wenn kein Handler erreicht wird: stopPropagation am Dialog hält den Klick vom Overlay fern.
        Assert.Throws<MissingEventHandlerException>(() => title.Click());

        Assert.Equal(0, _cancelCount);
    }

    private IRenderedComponent<TimeInputDialogPopUp> RenderDialog(
        bool isOpen,
        int initialMinutes = 25,
        string? title = null)
    {
        return Render<TimeInputDialogPopUp>(parameters => parameters
            .Add(x => x.IsOpen, isOpen)
            .Add(x => x.InitialMinutes, initialMinutes)
            .Add(x => x.Title, title)
            .Add(x => x.OnConfirm, EventCallback.Factory.Create<int>(this, ConfirmMinutes))
            .Add(x => x.OnCancel, EventCallback.Factory.Create(this, CountCancel)));
    }

    private void ConfirmMinutes(int minutes)
    {
        _confirmedMinutes.Add(minutes);
    }

    private void CountCancel()
    {
        _cancelCount++;
    }

    private static void EnterAndSubmit(IRenderedComponent<TimeInputDialogPopUp> cut, string input)
    {
        cut.Find("input").Input(input);
        cut.Find("form").Submit();
    }
}
