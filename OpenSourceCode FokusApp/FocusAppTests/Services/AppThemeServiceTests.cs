using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class AppThemeServiceTests
{
    private readonly IAppearanceSettingsService _appearanceSettingsService = Substitute.For<IAppearanceSettingsService>();
    private readonly AppThemeService _service;
    private int _changedCount;

    public AppThemeServiceTests()
    {
        _service = new AppThemeService(_appearanceSettingsService);
        _service.ThemeChanged += (_, _) => _changedCount++;
    }

    [Fact]
    public async Task InitializeAsync_StoredTheme_BecomesSelectedTheme()
    {
        _appearanceSettingsService.CurrentTheme.Returns(AppColorTheme.Green);

        await _service.InitializeAsync();

        Assert.Equal(AppColorTheme.Green, _service.SelectedTheme);
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task SetThemeAsync_NewTheme_PersistsAndRaisesChanged()
    {
        _appearanceSettingsService.SetThemeAsync(AppColorTheme.Red).Returns(call =>
        {
            _appearanceSettingsService.CurrentTheme.Returns(AppColorTheme.Red);
            return Task.CompletedTask;
        });

        await _service.SetThemeAsync(AppColorTheme.Red);

        await _appearanceSettingsService.Received(1).SetThemeAsync(AppColorTheme.Red);
        Assert.Equal(AppColorTheme.Red, _service.SelectedTheme);
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task SetThemeAsync_ThemeAlreadySelected_RaisesNoEvent()
    {
        _appearanceSettingsService.CurrentTheme.Returns(AppColorTheme.Blue);

        await _service.SetThemeAsync(AppColorTheme.Blue);

        Assert.Equal(0, _changedCount);
    }

    [Fact]
    public async Task ReloadAsync_StoredThemeChanged_UpdatesSelectedThemeAndRaisesChanged()
    {
        _appearanceSettingsService.CurrentTheme.Returns(AppColorTheme.Yellow);

        await _service.ReloadAsync();

        Assert.Equal(AppColorTheme.Yellow, _service.SelectedTheme);
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public void SelectedTheme_BeforeInitialization_IsBlueAndLight()
    {
        Assert.Equal(AppColorTheme.Blue, _service.SelectedTheme);
        Assert.Equal(AppColorMode.Light, _service.SelectedMode);
    }

    [Fact]
    public async Task SetThemeAsync_WithoutSubscribers_UpdatesSelectedThemeWithoutError()
    {
        var service = new AppThemeService(_appearanceSettingsService);
        _appearanceSettingsService.CurrentTheme.Returns(AppColorTheme.Green);

        await service.SetThemeAsync(AppColorTheme.Green);

        Assert.Equal(AppColorTheme.Green, service.SelectedTheme);
    }

    [Fact]
    public async Task InitializeAsync_WithoutSubscribers_UpdatesSelectedThemeWithoutError()
    {
        var service = new AppThemeService(_appearanceSettingsService);
        _appearanceSettingsService.CurrentTheme.Returns(AppColorTheme.Pink);

        await service.InitializeAsync();

        Assert.Equal(AppColorTheme.Pink, service.SelectedTheme);
    }

    [Fact]
    public async Task InitializeAsync_StoredColorMode_BecomesSelectedMode()
    {
        _appearanceSettingsService.CurrentColorMode.Returns(AppColorMode.Dark);

        await _service.InitializeAsync();

        Assert.Equal(AppColorMode.Dark, _service.SelectedMode);
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task SetColorModeAsync_NewMode_PersistsAndRaisesChanged()
    {
        _appearanceSettingsService.SetColorModeAsync(AppColorMode.Dark).Returns(call =>
        {
            _appearanceSettingsService.CurrentColorMode.Returns(AppColorMode.Dark);
            return Task.CompletedTask;
        });

        await _service.SetColorModeAsync(AppColorMode.Dark);

        await _appearanceSettingsService.Received(1).SetColorModeAsync(AppColorMode.Dark);
        Assert.Equal(AppColorMode.Dark, _service.SelectedMode);
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task SetColorModeAsync_ModeAlreadySelected_RaisesNoEvent()
    {
        _appearanceSettingsService.CurrentColorMode.Returns(AppColorMode.Light);

        await _service.SetColorModeAsync(AppColorMode.Light);

        Assert.Equal(0, _changedCount);
    }

    [Fact]
    public async Task SetColorModeAsync_DoesNotChangeTheSelectedTheme()
    {
        _appearanceSettingsService.CurrentTheme.Returns(AppColorTheme.Pink);
        await _service.InitializeAsync();
        _appearanceSettingsService.CurrentColorMode.Returns(AppColorMode.Dark);

        await _service.SetColorModeAsync(AppColorMode.Dark);

        Assert.Equal(AppColorTheme.Pink, _service.SelectedTheme);
        Assert.Equal(AppColorMode.Dark, _service.SelectedMode);
    }
}
