using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class AppThemeServiceTests
{
    private readonly IUserSettingsService _userSettingsService = Substitute.For<IUserSettingsService>();
    private readonly ISystemThemeProvider _systemThemeProvider = Substitute.For<ISystemThemeProvider>();
    private readonly AppThemeService _service;
    private int _changedCount;

    public AppThemeServiceTests()
    {
        _service = new AppThemeService(_userSettingsService, _systemThemeProvider);
        _service.ThemeChanged += (_, _) => _changedCount++;
    }

    [Fact]
    public async Task InitializeAsync_StoredTheme_BecomesSelectedTheme()
    {
        _userSettingsService.CurrentTheme.Returns(AppColorTheme.Blue);

        await _service.InitializeAsync();

        Assert.Equal(AppColorTheme.Blue, _service.SelectedTheme);
        Assert.Equal(AppColorTheme.Blue, _service.EffectiveTheme);
        Assert.Equal(1, _changedCount);
    }

    [Theory]
    [InlineData(true, AppColorTheme.Dark)]
    [InlineData(false, AppColorTheme.White)]
    public async Task EffectiveTheme_SystemTheme_FollowsSystemDarkMode(bool isSystemDark, AppColorTheme expected)
    {
        _userSettingsService.CurrentTheme.Returns(AppColorTheme.System);
        _systemThemeProvider.IsSystemDark.Returns(isSystemDark);
        await _service.InitializeAsync();

        Assert.Equal(AppColorTheme.System, _service.SelectedTheme);
        Assert.Equal(expected, _service.EffectiveTheme);
    }

    [Fact]
    public async Task SetThemeAsync_NewTheme_PersistsAndRaisesChanged()
    {
        _userSettingsService.SetThemeAsync(AppColorTheme.Red).Returns(call =>
        {
            _userSettingsService.CurrentTheme.Returns(AppColorTheme.Red);
            return Task.CompletedTask;
        });

        await _service.SetThemeAsync(AppColorTheme.Red);

        await _userSettingsService.Received(1).SetThemeAsync(AppColorTheme.Red);
        Assert.Equal(AppColorTheme.Red, _service.SelectedTheme);
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task SetThemeAsync_ThemeAlreadySelected_RaisesNoEvent()
    {
        _userSettingsService.CurrentTheme.Returns(AppColorTheme.System);

        await _service.SetThemeAsync(AppColorTheme.System);

        Assert.Equal(0, _changedCount);
    }

    [Fact]
    public async Task SystemThemeChanged_SystemThemeSelected_RaisesChanged()
    {
        _userSettingsService.CurrentTheme.Returns(AppColorTheme.System);
        await _service.InitializeAsync();
        _changedCount = 0;

        _systemThemeProvider.SystemThemeChanged += Raise.Event();

        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task SystemThemeChanged_FixedThemeSelected_RaisesNoEvent()
    {
        _userSettingsService.CurrentTheme.Returns(AppColorTheme.Pink);
        await _service.InitializeAsync();
        _changedCount = 0;

        _systemThemeProvider.SystemThemeChanged += Raise.Event();

        Assert.Equal(0, _changedCount);
    }
}
