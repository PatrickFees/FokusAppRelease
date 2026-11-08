using FokusApp.Core.Enums;

namespace FokusApp.Core.Services.Interfaces;

public interface IAppLocalizationService
{
    event EventHandler? LanguageChanged;

    AppLanguage CurrentLanguage { get; }

    Task InitializeAsync();

    Task ReloadAsync();

    Task SetLanguageAsync(AppLanguage language);
}
