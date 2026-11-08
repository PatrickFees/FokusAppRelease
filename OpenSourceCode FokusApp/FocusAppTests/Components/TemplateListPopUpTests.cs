using Bunit;
using FokusApp.Components.PopUps.TemplateList;
using FokusApp.Components.PopUps.TaskEdit;
using Microsoft.AspNetCore.Components;

namespace FocusAppTests.Components;

public class TemplateListPopUpTests : ComponentTestContext
{
    private readonly List<Guid> _copied = [];
    private readonly List<Guid> _edited = [];
    private int _cancelCount;

    [Fact]
    public void Render_Closed_ShowsNothing()
    {
        var cut = RenderPopup(isOpen: false, options: [Option("Sport", isPositive: true)]);

        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void Render_OpenWithoutOptions_ShowsTheEmptyHintOnly()
    {
        var cut = RenderPopup(isOpen: true, options: []);

        var hint = cut.Find(".template-empty");
        Assert.Equal(Text("NoTemplates"), hint.TextContent);
        Assert.Empty(cut.FindAll(".template-list-item"));
        Assert.Equal(Text("Templates"), cut.Find("h2").TextContent);
    }

    [Fact]
    public void Render_OpenWithOptions_ListsEveryTemplateWithItsKind()
    {
        var forYou = Option("Sport", isPositive: true);
        var toDo = Option("Steuern", isPositive: false);

        var cut = RenderPopup(isOpen: true, options: [forYou, toDo]);

        var items = cut.FindAll(".template-list-item");
        Assert.Equal(2, items.Count);
        Assert.Equal("Sport", items[0].QuerySelector(".template-list-text")!.TextContent);
        Assert.Equal(Text("TaskTypeForYou"), items[0].QuerySelector(".template-list-kind")!.TextContent);
        Assert.Equal("Steuern", items[1].QuerySelector(".template-list-text")!.TextContent);
        Assert.Equal(Text("TaskTypeToDo"), items[1].QuerySelector(".template-list-kind")!.TextContent);
        Assert.Empty(cut.FindAll(".template-empty"));
    }

    [Fact]
    public void Render_Open_IsAnAccessibleModalDialog()
    {
        var cut = RenderPopup(isOpen: true, options: []);

        var dialog = cut.Find(".template-list-popup");
        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.Equal(Text("Templates"), dialog.GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_Open_ShowsApplyAndEditButtonsWithIconAndLabel()
    {
        var cut = RenderPopup(isOpen: true, options: [Option("Sport", isPositive: true)]);

        var copy = cut.Find(".template-icon-button.copy");
        var edit = cut.Find(".template-icon-button.edit");
        Assert.NotNull(copy.QuerySelector(".template-action-icon"));
        Assert.NotNull(edit.QuerySelector(".template-action-icon"));
        Assert.Equal(Text("Apply"), copy.QuerySelector(".template-action-label")!.TextContent);
        Assert.Equal(Text("Edit"), edit.QuerySelector(".template-action-label")!.TextContent);
    }

    [Fact]
    public void ClickCopy_Template_ReportsItsId()
    {
        var first = Option("Sport", isPositive: true);
        var second = Option("Steuern", isPositive: false);
        var cut = RenderPopup(isOpen: true, options: [first, second]);

        cut.FindAll(".template-icon-button.copy")[1].Click();

        var copied = Assert.Single(_copied);
        Assert.Equal(second.TaskId, copied);
        Assert.Empty(_edited);
    }

    [Fact]
    public void ClickEdit_Template_ReportsItsId()
    {
        var first = Option("Sport", isPositive: true);
        var cut = RenderPopup(isOpen: true, options: [first]);

        cut.Find(".template-icon-button.edit").Click();

        var edited = Assert.Single(_edited);
        Assert.Equal(first.TaskId, edited);
        Assert.Empty(_copied);
    }

    [Fact]
    public void ClickCloseButton_Open_Cancels()
    {
        var cut = RenderPopup(isOpen: true, options: []);

        cut.Find(".template-list-close").Click();

        Assert.Equal(1, _cancelCount);
    }

    [Fact]
    public void ClickBackdrop_Open_Cancels()
    {
        var cut = RenderPopup(isOpen: true, options: []);

        cut.Find(".template-list-backdrop").Click();

        Assert.Equal(1, _cancelCount);
    }

    [Fact]
    public void ClickInsidePopup_DoesNotCancel()
    {
        var cut = RenderPopup(isOpen: true, options: []);

        var popup = cut.Find(".template-list-popup");

        Assert.Throws<MissingEventHandlerException>(() => popup.Click());
        Assert.Equal(0, _cancelCount);
    }

    private IRenderedComponent<TemplateListPopUp> RenderPopup(
        bool isOpen,
        IReadOnlyList<TaskEditTemplateOptionModel> options)
    {
        return Render<TemplateListPopUp>(parameters => parameters
            .Add(popup => popup.IsOpen, isOpen)
            .Add(popup => popup.Options, options)
            .Add(popup => popup.OnCopy, EventCallback.Factory.Create<Guid>(this, id => _copied.Add(id)))
            .Add(popup => popup.OnEdit, EventCallback.Factory.Create<Guid>(this, id => _edited.Add(id)))
            .Add(popup => popup.OnCancel, EventCallback.Factory.Create(this, () => _cancelCount++)));
    }

    private static TaskEditTemplateOptionModel Option(string text, bool isPositive)
    {
        return new TaskEditTemplateOptionModel
        {
            TaskId = Guid.NewGuid(),
            Text = text,
            IsPositive = isPositive
        };
    }
}
