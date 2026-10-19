using Bunit;
using FokusApp.Components.PopUps.ConfirmDialog;
using Microsoft.AspNetCore.Components;

namespace FocusAppTests.Components;

public class ConfirmDialogPopUpTests : ComponentTestContext
{
    [Fact]
    public void Render_Closed_ShowsNothing()
    {
        var cut = RenderDialog(isOpen: false);

        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void Render_Open_ShowsTitleMessageAndConfirmText()
    {
        var cut = RenderDialog(isOpen: true);

        var title = cut.Find(".confirm-dialog-title");
        var message = cut.Find(".confirm-dialog-message");
        var confirmButton = cut.Find("button.btn-primary");
        Assert.Equal("Titel", title.TextContent);
        Assert.Equal("Nachricht", message.TextContent);
        Assert.Equal("Bestätigen", confirmButton.TextContent.Trim());
    }

    [Fact]
    public void Render_OpenWithCancelText_ShowsCancelButton()
    {
        var cut = RenderDialog(isOpen: true, cancelText: "Abbrechen");

        var cancelButton = cut.Find("button.btn-secondary");
        Assert.Equal("Abbrechen", cancelButton.TextContent.Trim());
        Assert.Equal(2, cut.FindAll("button").Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Render_OpenWithoutCancelText_ShowsOnlyConfirmButton(string? cancelText)
    {
        var cut = RenderDialog(isOpen: true, cancelText: cancelText);

        var buttons = cut.FindAll("button");
        Assert.Single(buttons);
        Assert.Empty(cut.FindAll("button.btn-secondary"));
    }

    [Fact]
    public void Render_Destructive_UsesDangerButton()
    {
        var cut = RenderDialog(isOpen: true, isDestructive: true);

        var confirmButton = cut.Find("button.btn-danger");
        Assert.Equal("Bestätigen", confirmButton.TextContent.Trim());
        Assert.Empty(cut.FindAll("button.btn-primary"));
    }

    [Fact]
    public void Render_NotDestructive_UsesPrimaryButton()
    {
        var cut = RenderDialog(isOpen: true, isDestructive: false);

        Assert.Single(cut.FindAll("button.btn-primary"));
        Assert.Empty(cut.FindAll("button.btn-danger"));
    }

    [Fact]
    public void ClickConfirm_CallsOnConfirmOnly()
    {
        var calls = new CallCounter();
        var cut = RenderDialog(isOpen: true, cancelText: "Abbrechen", calls: calls);
        var confirmButton = cut.Find("button.btn-primary");

        confirmButton.Click();

        Assert.Equal(1, calls.Confirm);
        Assert.Equal(0, calls.Cancel);
    }

    [Fact]
    public void ClickCancelButton_CallsOnCancelOnly()
    {
        var calls = new CallCounter();
        var cut = RenderDialog(isOpen: true, cancelText: "Abbrechen", calls: calls);
        var cancelButton = cut.Find("button.btn-secondary");

        cancelButton.Click();

        Assert.Equal(0, calls.Confirm);
        Assert.Equal(1, calls.Cancel);
    }

    [Fact]
    public void ClickOverlay_CallsOnCancel()
    {
        var calls = new CallCounter();
        var cut = RenderDialog(isOpen: true, cancelText: "Abbrechen", calls: calls);
        var overlay = cut.Find(".confirm-dialog-overlay");

        overlay.Click();

        Assert.Equal(0, calls.Confirm);
        Assert.Equal(1, calls.Cancel);
    }

    [Fact]
    public void ClickInsideDialog_DoesNotReachOverlay()
    {
        var calls = new CallCounter();
        var cut = RenderDialog(isOpen: true, cancelText: "Abbrechen", calls: calls);
        var message = cut.Find(".confirm-dialog-message");

        // bUnit wirft, wenn kein Handler erreicht wird: stopPropagation am Dialog hält den Klick vom Overlay fern.
        Assert.Throws<MissingEventHandlerException>(() => message.Click());

        Assert.Equal(0, calls.Confirm);
        Assert.Equal(0, calls.Cancel);
    }

    [Fact]
    public void ClickOverlay_WithoutCancelHandler_CallsOnConfirm()
    {
        var calls = new CallCounter();
        var cut = RenderDialog(isOpen: true, calls: calls, withCancelHandler: false);
        var overlay = cut.Find(".confirm-dialog-overlay");

        overlay.Click();

        Assert.Equal(1, calls.Confirm);
        Assert.Equal(0, calls.Cancel);
    }

    [Fact]
    public void Render_OpenDialog_HasAccessibleDialogRole()
    {
        var cut = RenderDialog(isOpen: true);

        var dialog = cut.Find("[role=dialog]");
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.Equal("Titel", dialog.GetAttribute("aria-label"));
    }

    private IRenderedComponent<ConfirmDialogPopUp> RenderDialog(
        bool isOpen,
        string? cancelText = null,
        bool isDestructive = false,
        CallCounter? calls = null,
        bool withCancelHandler = true)
    {
        var counter = calls ?? new CallCounter();

        return Render<ConfirmDialogPopUp>(parameters =>
        {
            parameters
                .Add(x => x.IsOpen, isOpen)
                .Add(x => x.Title, "Titel")
                .Add(x => x.Message, "Nachricht")
                .Add(x => x.ConfirmText, "Bestätigen")
                .Add(x => x.CancelText, cancelText)
                .Add(x => x.IsDestructive, isDestructive)
                .Add(x => x.OnConfirm, EventCallback.Factory.Create(this, counter.CountConfirm));

            if (withCancelHandler)
            {
                parameters.Add(x => x.OnCancel, EventCallback.Factory.Create(this, counter.CountCancel));
            }
        });
    }

    private sealed class CallCounter
    {
        public int Confirm { get; private set; }

        public int Cancel { get; private set; }

        public void CountConfirm()
        {
            Confirm++;
        }

        public void CountCancel()
        {
            Cancel++;
        }
    }
}
