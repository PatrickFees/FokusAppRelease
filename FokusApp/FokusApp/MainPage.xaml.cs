using FokusApp.Services;
using Microsoft.Extensions.Logging;

namespace FokusApp;

public partial class MainPage : ContentPage
{
    private readonly IUiBackPressService _uiBackPressService;

    public MainPage(ILogger<MainPage> logger, IUiBackPressService _uiBackPressService)
    {
        InitializeComponent();
        this._uiBackPressService = _uiBackPressService;
    }

    protected override bool OnBackButtonPressed()
    {
        if (_uiBackPressService.TryHandleBackPress())
        {
            return true;
        }

        return base.OnBackButtonPressed();
    }
}
