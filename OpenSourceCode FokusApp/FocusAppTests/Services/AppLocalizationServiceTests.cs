using System.Globalization;
using FocusAppTests.MauiShims;
using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using NSubstitute;

namespace FocusAppTests.Services;

[Collection(SerialMauiShimsCollection.Name)]
public class AppLocalizationServiceTests : IDisposable
{
    private readonly CultureInfo? _previousDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
    private readonly CultureInfo? _previousDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _previousUiCulture = CultureInfo.CurrentUICulture;
    private readonly IAppearanceSettingsService _appearanceSettings = Substitute.For<IAppearanceSettingsService>();
    private readonly AppLocalizationService _service;
    private int _changedCount;

    public AppLocalizationServiceTests()
    {
        _appearanceSettings.CurrentLanguage.Returns(AppLanguage.German);
        _service = new AppLocalizationService(_appearanceSettings);
        _service.LanguageChanged += (_, _) => _changedCount++;
    }

    public void Dispose()
    {
        MainThread.InvokeHandler = action =>
        {
            action();
            return Task.CompletedTask;
        };
        CultureInfo.DefaultThreadCurrentCulture = _previousDefaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _previousDefaultUiCulture;
        CultureInfo.CurrentCulture = _previousCulture;
        CultureInfo.CurrentUICulture = _previousUiCulture;
    }

    [Fact]
    public void CurrentLanguage_BeforeInitialization_IsGerman()
    {
        Assert.Equal(AppLanguage.German, _service.CurrentLanguage);
    }

    [Theory]
    [InlineData(AppLanguage.German, "de-DE")]
    [InlineData(AppLanguage.English, "en-US")]
    public async Task InitializeAsync_StoredLanguage_AppliesTheMatchingCultureAndRaisesChanged(AppLanguage language, string expectedCulture)
    {
        _appearanceSettings.CurrentLanguage.Returns(language);

        await _service.InitializeAsync();

        Assert.Equal(language, _service.CurrentLanguage);
        Assert.Equal(expectedCulture, CultureInfo.CurrentCulture.Name);
        Assert.Equal(expectedCulture, CultureInfo.CurrentUICulture.Name);
        Assert.Equal(expectedCulture, CultureInfo.DefaultThreadCurrentCulture!.Name);
        Assert.Equal(expectedCulture, CultureInfo.DefaultThreadCurrentUICulture!.Name);
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_AppliesTheLanguageOnlyOnce()
    {
        await _service.InitializeAsync();
        await _service.InitializeAsync();

        Assert.Equal(1, _changedCount);
        _ = _appearanceSettings.Received(1).CurrentLanguage;
    }

    [Fact]
    public async Task InitializeAsync_ConcurrentCalls_AppliesTheLanguageOnlyOnce()
    {
        var first = _service.InitializeAsync();
        var second = _service.InitializeAsync();

        await Task.WhenAll(first, second);

        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task InitializeAsync_WithoutSubscribers_CompletesWithoutError()
    {
        var service = new AppLocalizationService(_appearanceSettings);

        var exception = await Record.ExceptionAsync(() => service.InitializeAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task SetLanguageAsync_OtherLanguage_StoresItSwitchesCultureAndRaisesChanged()
    {
        await _service.InitializeAsync();
        _changedCount = 0;

        await _service.SetLanguageAsync(AppLanguage.English);

        await _appearanceSettings.Received(1).SetLanguageAsync(AppLanguage.English);
        Assert.Equal(AppLanguage.English, _service.CurrentLanguage);
        Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task SetLanguageAsync_SameLanguage_StoresItButRaisesNoEvent()
    {
        await _service.InitializeAsync();
        _changedCount = 0;

        await _service.SetLanguageAsync(AppLanguage.German);

        await _appearanceSettings.Received(1).SetLanguageAsync(AppLanguage.German);
        Assert.Equal(0, _changedCount);
    }

    [Fact]
    public async Task SetLanguageAsync_UnknownLanguage_FallsBackToGerman()
    {
        var unknown = (AppLanguage)42;

        await _service.SetLanguageAsync(unknown);

        await _appearanceSettings.Received(1).SetLanguageAsync(AppLanguage.German);
        Assert.Equal(AppLanguage.German, _service.CurrentLanguage);
    }

    [Fact]
    public async Task SetLanguageAsync_ChangeWithoutSubscribers_CompletesWithoutError()
    {
        var service = new AppLocalizationService(_appearanceSettings);

        var exception = await Record.ExceptionAsync(() => service.SetLanguageAsync(AppLanguage.English));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ReloadAsync_ChangedStoredLanguage_AppliesItAndRaisesChanged()
    {
        await _service.InitializeAsync();
        _changedCount = 0;
        _appearanceSettings.CurrentLanguage.Returns(AppLanguage.English);

        await _service.ReloadAsync();

        Assert.Equal(AppLanguage.English, _service.CurrentLanguage);
        Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task ReloadAsync_WithoutPriorInitialization_PreventsALaterInitialLoad()
    {
        await _service.ReloadAsync();
        _appearanceSettings.ClearReceivedCalls();

        await _service.InitializeAsync();

        _ = _appearanceSettings.DidNotReceive().CurrentLanguage;
    }

    [Fact]
    public async Task ReloadAsync_WithoutSubscribers_CompletesWithoutError()
    {
        var service = new AppLocalizationService(_appearanceSettings);

        var exception = await Record.ExceptionAsync(() => service.ReloadAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task InitializeAsync_SecondCallWaitsForTheFirstOne_AppliesTheLanguageOnlyOnce()
    {
        var gate = new TaskCompletionSource();
        MainThread.InvokeHandler = action =>
        {
            action();
            return gate.Task;
        };
        _appearanceSettings.CurrentLanguage.Returns(AppLanguage.English);

        var first = _service.InitializeAsync();
        var second = _service.InitializeAsync();
        gate.SetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(1, _changedCount);
        _ = _appearanceSettings.Received(1).CurrentLanguage;
    }
}
