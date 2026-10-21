using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using Microsoft.Extensions.Logging;

namespace FokusApp;

public partial class MainPage : ContentPage
{
    // Native page background behind the WebView; keep in sync with --color-bg in wwwroot/css/themes.css.
    private static readonly Dictionary<AppColorTheme, Color> PageBackgrounds = new()
    {
        [AppColorTheme.White] = Color.FromArgb("#FFFFFF"),
        [AppColorTheme.Dark] = Color.FromArgb("#0B1020"),
        [AppColorTheme.Pink] = Color.FromArgb("#F9D3E3"),
        [AppColorTheme.Blue] = Color.FromArgb("#CFE2FA"),
        [AppColorTheme.Red] = Color.FromArgb("#F8D6D1")
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
        if (_uiBackPressService.TryHandleBackPress())
        {
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private void ApplyPageBackground()
    {
        BackgroundColor = PageBackgrounds.GetValueOrDefault(_themeService.EffectiveTheme, Colors.White);
    }
}
