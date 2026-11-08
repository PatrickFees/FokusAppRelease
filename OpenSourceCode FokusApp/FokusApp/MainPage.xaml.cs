using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using Microsoft.Extensions.Logging;

namespace FokusApp;

public partial class MainPage : ContentPage
{

    private static readonly Dictionary<(AppColorTheme Theme, AppColorMode Mode), Color> PageBackgrounds = new()
    {
        [(AppColorTheme.Blue, AppColorMode.Light)] = Color.FromArgb("#FFFFFF"),
        [(AppColorTheme.Blue, AppColorMode.Dark)] = Color.FromArgb("#0B1020"),
        [(AppColorTheme.Pink, AppColorMode.Light)] = Color.FromArgb("#F9D3E3"),
        [(AppColorTheme.Pink, AppColorMode.Dark)] = Color.FromArgb("#1F0B16"),
        [(AppColorTheme.Red, AppColorMode.Light)] = Color.FromArgb("#F8D6D1"),
        [(AppColorTheme.Red, AppColorMode.Dark)] = Color.FromArgb("#1F0C0B"),
        [(AppColorTheme.Green, AppColorMode.Light)] = Color.FromArgb("#D3EBD5"),
        [(AppColorTheme.Green, AppColorMode.Dark)] = Color.FromArgb("#0A1A0D"),
        [(AppColorTheme.Yellow, AppColorMode.Light)] = Color.FromArgb("#F8E9A1"),
        [(AppColorTheme.Yellow, AppColorMode.Dark)] = Color.FromArgb("#1C1505")
    };

    private readonly IUiBackPressService _uiBackPressService;
    private readonly IAppThemeService _themeService;

    public MainPage(ILogger<MainPage> logger, IUiBackPressService _uiBackPressService, IAppThemeService themeService)
    {
        InitializeComponent();
        this._uiBackPressService = _uiBackPressService;
        _themeService = themeService;
        _themeService.ThemeChanged += (_, _) => MainThread.BeginInvokeOnMainThread(ApplyPageBackground);
        ApplyPageBackground();
    }

    protected override bool OnBackButtonPressed()
    {
        _uiBackPressService.TryHandleBackPress();
        return true;
    }

    private void ApplyPageBackground()
    {
        BackgroundColor = PageBackgrounds.GetValueOrDefault((_themeService.SelectedTheme, _themeService.SelectedMode), Colors.White);
    }
}
