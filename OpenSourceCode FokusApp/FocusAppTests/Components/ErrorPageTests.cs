using System.Collections.ObjectModel;
using Bunit;
using FocusAppTests.MauiShims;
using FokusApp.Components.ErrorComponent;
using FokusApp.Core;
using FokusApp.Core.Interfaces;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

[Collection(SerialMauiShimsCollection.Name)]
public class ErrorPageTests : ComponentTestContext
{
    private readonly IErrorPageViewModel _viewModel = Substitute.For<IErrorPageViewModel>();
    private readonly IDeviceInfo _deviceInfo = Substitute.For<IDeviceInfo>();
    private readonly IClipboard _clipboard = Substitute.For<IClipboard>();
    private readonly IExternalLinkOpener _externalLinkOpener = Substitute.For<IExternalLinkOpener>();
    private readonly ObservableCollection<ErrorPageItem> _items = [];

    public ErrorPageTests()
    {
        _viewModel.Items.Returns(_items);
        _viewModel.LoadErrorMessage.Returns(string.Empty);
        _deviceInfo.Platform.Returns(DevicePlatform.Android);
        DeviceInfo.Current = _deviceInfo;
        Clipboard.Default = _clipboard;
        Services.AddSingleton(_viewModel);
        Services.AddSingleton(_externalLinkOpener);
    }

    [Fact]
    public void Render_Page_SetsThePageTitle()
    {
        var head = Render<HeadOutlet>();

        Render<ErrorPage>();

        head.WaitForAssertion(() => Assert.Contains(Text("Errors"), head.Markup));
    }

    [Fact]
    public void Render_Page_InitializesTheViewModelAndShowsTheHeader()
    {
        var cut = Render<ErrorPage>();

        _viewModel.Received(1).InitializeAsync();
        Assert.Equal(Text("Errors"), cut.Find("h1").TextContent);
        Assert.Equal(Text("Refresh"), cut.Find(".errors-refresh-button").TextContent);
    }

    [Fact]
    public void Render_NoErrors_ShowsTheFriendlyEmptyHint()
    {
        var cut = Render<ErrorPage>();

        var status = Assert.Single(cut.FindAll(".errors-status"));
        Assert.Equal(Text("NoSavedErrors"), status.TextContent);
        Assert.Empty(cut.FindAll(".error-card"));
    }

    [Fact]
    public void Render_LoadError_ShowsTheMessageAndTheEmptyHint()
    {
        _viewModel.LoadErrorMessage.Returns("Fehler beim Laden");

        var cut = Render<ErrorPage>();

        var statuses = cut.FindAll(".errors-status").Select(status => status.TextContent).ToList();
        Assert.Equal(["Fehler beim Laden", Text("NoSavedErrors")], statuses);
    }

    [Fact]
    public void Render_Errors_ShowsACardPerErrorWithTitleAndReadOnlyText()
    {
        _items.Add(CreateItem("Erster Fehler", "Text eins"));
        _items.Add(CreateItem("Zweiter Fehler", "Text zwei"));

        var cut = Render<ErrorPage>();

        var cards = cut.FindAll(".error-card");
        Assert.Equal(2, cards.Count);
        Assert.Equal("Erster Fehler", cards[0].QuerySelector("h2")!.TextContent);
        var field = cards[1].QuerySelector(".error-text-field")!;
        Assert.Equal("Text zwei", field.TextContent);
        Assert.True(field.HasAttribute("readonly"));
        Assert.Empty(cut.FindAll(".errors-status"));
    }

    [Theory]
    [InlineData(true, false, true, false)]
    [InlineData(false, true, false, true)]
    [InlineData(true, true, true, true)]
    [InlineData(false, false, false, false)]
    public void Render_Error_ShowsFlagsForFatalAndUiFailures(bool isFatal, bool isUi, bool expectFatal, bool expectUi)
    {
        _items.Add(CreateItem("Fehler", "Text", isFatal, isUi));

        var cut = Render<ErrorPage>();

        Assert.Equal(expectFatal, cut.FindAll(".flag.fatal").Count == 1);
        Assert.Equal(expectUi, cut.FindAll(".flag.ui").Count == 1);
    }

    [Fact]
    public void ClickRefresh_Always_ReloadsAtTheViewModel()
    {
        var cut = Render<ErrorPage>();

        cut.Find(".errors-refresh-button").Click();

        _viewModel.Received(1).ReloadAsync();
    }

    [Fact]
    public void ClickCopy_Error_CopiesTheTrimmedText()
    {
        _items.Add(CreateItem("Fehler", "  Fehlertext\n"));
        var cut = Render<ErrorPage>();

        cut.FindAll(".error-action-button")[0].Click();

        _clipboard.Received(1).SetTextAsync("Fehlertext");
    }

    [Fact]
    public void Render_Android_OffersCopyAndShare()
    {
        _items.Add(CreateItem("Fehler", "Text"));

        var cut = Render<ErrorPage>();

        var buttons = cut.FindAll(".error-action-button").Select(button => button.TextContent).ToList();
        Assert.Equal([Text("Copy"), Text("Share")], buttons);
    }

    [Fact]
    public void Render_Windows_OffersOnlyCopy()
    {
        _deviceInfo.Platform.Returns(DevicePlatform.WinUI);
        _items.Add(CreateItem("Fehler", "Text"));

        var cut = Render<ErrorPage>();

        var button = Assert.Single(cut.FindAll(".error-action-button"));
        Assert.Equal(Text("Copy"), button.TextContent);
    }

    [Fact]
    public void ClickShare_Error_OpensAMailtoLinkToTheDeveloperWithSubjectAndBody()
    {
        _items.Add(CreateItem("Fehler", "Fehlertext"));
        var cut = Render<ErrorPage>();

        cut.FindAll(".error-action-button")[1].Click();

        var expectedSubject = Uri.EscapeDataString(Text("ShareErrorEmailSubject"));
        _externalLinkOpener.Received(1).OpenAsync(
            $"mailto:{AppConstants.DeveloperEmailAddress}?subject={expectedSubject}&body=Fehlertext");
    }

    [Fact]
    public void PropertyChanged_ViewModel_RendersNewErrors()
    {
        var cut = Render<ErrorPage>();
        _items.Add(CreateItem("Neu", "Neuer Text"));

        var args = new System.ComponentModel.PropertyChangedEventArgs(nameof(IErrorPageViewModel.Items));
        _viewModel.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(_viewModel, args);

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".error-card")));
    }

    [Fact]
    public async Task Dispose_AfterDispose_IgnoresViewModelChanges()
    {
        Render<ErrorPage>();

        await DisposeComponentsAsync();

        var args = new System.ComponentModel.PropertyChangedEventArgs(nameof(IErrorPageViewModel.Items));
        var exception = Record.Exception(() =>
            _viewModel.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(_viewModel, args));
        Assert.Null(exception);
    }

    [Fact]
    public void ClickCopy_ErrorWithoutText_CopiesAnEmptyText()
    {
        _items.Add(CreateItem("Fehler", null!));
        var cut = Render<ErrorPage>();

        cut.FindAll(".error-action-button")[0].Click();

        _clipboard.Received(1).SetTextAsync(string.Empty);
    }

    [Fact]
    public void ClickShare_ErrorWithoutText_OpensAMailtoLinkWithAnEmptyBody()
    {
        _items.Add(CreateItem("Fehler", null!));
        var cut = Render<ErrorPage>();

        cut.FindAll(".error-action-button")[1].Click();

        var expectedSubject = Uri.EscapeDataString(Text("ShareErrorEmailSubject"));
        _externalLinkOpener.Received(1).OpenAsync(
            $"mailto:{AppConstants.DeveloperEmailAddress}?subject={expectedSubject}&body=");
    }

    private static ErrorPageItem CreateItem(string title, string text, bool isFatal = false, bool isUi = false)
    {
        return new ErrorPageItem(Guid.NewGuid(), title, new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc), isFatal, isUi, text);
    }
}
