using FokusApp.Resources.Localization;
using FokusApp.Services;
using Microsoft.Extensions.Localization;
using NSubstitute;

namespace FocusAppTests.Services;

public class LocalizedTextProviderTests
{
    private readonly IStringLocalizer<AppStrings> _localizer = Substitute.For<IStringLocalizer<AppStrings>>();

    [Fact]
    public void GetText_UiTextProvider_ReturnsLocalizedTextWithArguments()
    {
        var provider = new LocalizedUiTextProvider(_localizer);
        _localizer["Greeting", "Anna", 3].Returns(new LocalizedString("Greeting", "Hallo Anna, 3"));

        var text = provider.GetText("Greeting", "Anna", 3);

        Assert.Equal("Hallo Anna, 3", text);
    }

    [Fact]
    public void GetText_UiTextProviderWithoutArguments_ReturnsLocalizedText()
    {
        var provider = new LocalizedUiTextProvider(_localizer);
        _localizer["Title", Arg.Any<object[]>()].Returns(new LocalizedString("Title", "Titel"));

        var text = provider.GetText("Title");

        Assert.Equal("Titel", text);
    }

    [Fact]
    public void GetText_UiTextProviderUnknownKey_ReturnsTheKey()
    {
        var provider = new LocalizedUiTextProvider(_localizer);
        _localizer["Unbekannt", Arg.Any<object[]>()].Returns(new LocalizedString("Unbekannt", "Unbekannt", resourceNotFound: true));

        var text = provider.GetText("Unbekannt");

        Assert.Equal("Unbekannt", text);
    }

    [Fact]
    public void GetText_DataTransferTextProvider_ReturnsLocalizedTextWithArguments()
    {
        var provider = new LocalizedDataTransferTextProvider(_localizer);
        _localizer["ImportedCount", 5].Returns(new LocalizedString("ImportedCount", "5 Aufgaben importiert"));

        var text = provider.GetText("ImportedCount", 5);

        Assert.Equal("5 Aufgaben importiert", text);
    }

    [Fact]
    public void GetText_DataTransferTextProviderWithoutArguments_ReturnsLocalizedText()
    {
        var provider = new LocalizedDataTransferTextProvider(_localizer);
        _localizer["ExportTitle", Arg.Any<object[]>()].Returns(new LocalizedString("ExportTitle", "Export"));

        var text = provider.GetText("ExportTitle");

        Assert.Equal("Export", text);
    }
}
