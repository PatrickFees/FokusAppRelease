using Bunit;
using FokusApp.Components.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FocusAppTests.Components;

public class SuggestionSwipeItemTests : ComponentTestContext
{
    private const int ShortLongPress = 30;

    private readonly List<string> _edited = [];
    private readonly List<string> _deleted = [];

    [Fact]
    public void PointerDown_HeldLongEnough_SlidesTheCardOpenWithoutSwiping()
    {
        var cut = RenderItem();

        cut.Find(".suggestion-card").PointerDown(new PointerEventArgs { ClientX = 200 });

        cut.WaitForAssertion(() => Assert.Contains("show-actions", cut.Find(".suggestion-card").ClassName));
    }

    [Fact]
    public async Task PointerUp_BeforeTheLongPressTime_KeepsTheActionsHidden()
    {
        var cut = RenderItem(longPressMilliseconds: 200);

        cut.Find(".suggestion-card").PointerDown(new PointerEventArgs { ClientX = 200 });
        cut.Find(".suggestion-card").PointerUp(new PointerEventArgs { ClientX = 200 });
        await Task.Delay(300);

        cut.WaitForAssertion(() => Assert.DoesNotContain("show-actions", cut.Find(".suggestion-card").ClassName));
    }

    [Fact]
    public async Task PointerCancel_BeforeTheLongPressTime_KeepsTheActionsHidden()
    {
        var cut = RenderItem(longPressMilliseconds: 200);

        cut.Find(".suggestion-card").PointerDown(new PointerEventArgs { ClientX = 200 });
        cut.Find(".suggestion-card").TriggerEvent("onpointercancel", new PointerEventArgs());
        await Task.Delay(300);

        cut.WaitForAssertion(() => Assert.DoesNotContain("show-actions", cut.Find(".suggestion-card").ClassName));
    }

    [Fact]
    public async Task PointerLeave_BeforeTheLongPressTime_KeepsTheActionsHidden()
    {
        var cut = RenderItem(longPressMilliseconds: 200);

        cut.Find(".suggestion-card").PointerDown(new PointerEventArgs { ClientX = 200 });
        cut.Find(".suggestion-card").TriggerEvent("onpointerleave", new PointerEventArgs());
        await Task.Delay(300);

        cut.WaitForAssertion(() => Assert.DoesNotContain("show-actions", cut.Find(".suggestion-card").ClassName));
    }

    [Fact]
    public void LetGoAfterLongPress_KeepsTheActionsOpenEvenWithTheFollowingClick()
    {
        var cut = RenderItem();
        cut.Find(".suggestion-card").PointerDown(new PointerEventArgs { ClientX = 200 });
        cut.WaitForAssertion(() => Assert.Contains("show-actions", cut.Find(".suggestion-card").ClassName));

        cut.Find(".suggestion-card").PointerUp(new PointerEventArgs { ClientX = 200 });
        cut.Find(".suggestion-content").Click();

        Assert.Contains("show-actions", cut.Find(".suggestion-card").ClassName);
    }

    [Fact]
    public void ClickOnTheContent_AfterTheLongPressClick_SlidesTheCardShut()
    {
        var cut = RenderItem();
        cut.Find(".suggestion-card").PointerDown(new PointerEventArgs { ClientX = 200 });
        cut.WaitForAssertion(() => Assert.Contains("show-actions", cut.Find(".suggestion-card").ClassName));
        cut.Find(".suggestion-card").PointerUp(new PointerEventArgs { ClientX = 200 });
        cut.Find(".suggestion-content").Click();

        cut.Find(".suggestion-content").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("show-actions", cut.Find(".suggestion-card").ClassName));
    }

    [Fact]
    public void PointerDown_AfterAnUnclickedLongPress_DoesNotSwallowTheNextRealClick()
    {
        var cut = RenderItem();
        cut.Find(".suggestion-card").PointerDown(new PointerEventArgs { ClientX = 200 });
        cut.WaitForAssertion(() => Assert.Contains("show-actions", cut.Find(".suggestion-card").ClassName));
        cut.Find(".suggestion-card").TriggerEvent("onpointerleave", new PointerEventArgs());

        cut.Find(".suggestion-card").PointerDown(new PointerEventArgs { ClientX = 200 });
        cut.Find(".suggestion-card").PointerUp(new PointerEventArgs { ClientX = 200 });
        cut.Find(".suggestion-content").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("show-actions", cut.Find(".suggestion-card").ClassName));
    }

    [Fact]
    public void LongPress_ThenClickEdit_ReportsTheKeyAndSlidesShut()
    {
        var cut = RenderItem();
        cut.Find(".suggestion-card").PointerDown(new PointerEventArgs { ClientX = 200 });
        cut.WaitForAssertion(() => Assert.Contains("show-actions", cut.Find(".suggestion-card").ClassName));
        cut.Find(".suggestion-card").PointerUp(new PointerEventArgs { ClientX = 200 });

        cut.Find(".suggestion-action.edit").Click();

        Assert.Equal(["schluessel"], _edited);
        cut.WaitForAssertion(() => Assert.DoesNotContain("show-actions", cut.Find(".suggestion-card").ClassName));
    }

    [Fact]
    public void LongPress_ThenClickDelete_ReportsTheKey()
    {
        var cut = RenderItem();
        cut.Find(".suggestion-card").PointerDown(new PointerEventArgs { ClientX = 200 });
        cut.WaitForAssertion(() => Assert.Contains("show-actions", cut.Find(".suggestion-card").ClassName));
        cut.Find(".suggestion-card").PointerUp(new PointerEventArgs { ClientX = 200 });

        cut.Find(".suggestion-action.delete").Click();

        Assert.Equal(["schluessel"], _deleted);
    }

    [Fact]
    public void LongPress_ThenSwipeRight_SlidesTheCardShut()
    {
        var cut = RenderItem();
        cut.Find(".suggestion-card").PointerDown(new PointerEventArgs { ClientX = 100 });
        cut.WaitForAssertion(() => Assert.Contains("show-actions", cut.Find(".suggestion-card").ClassName));

        cut.Find(".suggestion-card").PointerUp(new PointerEventArgs { ClientX = 220 });

        cut.WaitForAssertion(() => Assert.DoesNotContain("show-actions", cut.Find(".suggestion-card").ClassName));
    }

    [Fact]
    public async Task Dispose_WhilePressed_CancelsTheLongPressWithoutError()
    {
        var cut = RenderItem(longPressMilliseconds: 100);
        cut.Find(".suggestion-card").PointerDown(new PointerEventArgs { ClientX = 200 });

        await DisposeComponentsAsync();
        await Task.Delay(200);

        Assert.Empty(_edited);
    }

    private IRenderedComponent<SuggestionSwipeItem> RenderItem(int longPressMilliseconds = ShortLongPress)
    {
        return Render<SuggestionSwipeItem>(parameters => parameters
            .Add(item => item.Key, "schluessel")
            .Add(item => item.Text, "Spazieren gehen")
            .Add(item => item.LongPressMilliseconds, longPressMilliseconds)
            .Add(item => item.OnEdit, EventCallback.Factory.Create<string>(this, key => _edited.Add(key)))
            .Add(item => item.OnDelete, EventCallback.Factory.Create<string>(this, key => _deleted.Add(key))));
    }
}
