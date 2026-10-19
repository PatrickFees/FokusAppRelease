using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;
using System.Globalization;

namespace FokusApp.Services;

public sealed class AppLocalizationService(IUserSettingsService userSettingsService) : IAppLocalizationService
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _isInitialized;

    public event EventHandler? LanguageChanged;

    public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.German;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            CurrentLanguage = userSettingsService.CurrentLanguage;
            await ApplyCultureAsync(CurrentLanguage).ConfigureAwait(false);
            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SetLanguageAsync(AppLanguage language)
    {
        var normalizedLanguage = Normalize(language);
        await userSettingsService.SetLanguageAsync(normalizedLanguage).ConfigureAwait(false);

        if (CurrentLanguage == normalizedLanguage)
        {
            return;
        }

        CurrentLanguage = normalizedLanguage;
        await ApplyCultureAsync(normalizedLanguage).ConfigureAwait(false);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ReloadAsync()
    {
        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            CurrentLanguage = userSettingsService.CurrentLanguage;
            await ApplyCultureAsync(CurrentLanguage).ConfigureAwait(false);
            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static Task ApplyCultureAsync(AppLanguage language)
    {
        var culture = CultureInfo.GetCultureInfo(language switch
        {
            AppLanguage.English => "en-US",
            _ => "de-DE"
        });

        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        });
    }

    private static AppLanguage Normalize(AppLanguage language) =>
        Enum.IsDefined(language) ? language : AppLanguage.German;
}
