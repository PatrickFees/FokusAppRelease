using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;

namespace FokusApp.Core.Services.Interfaces;

public interface IUserSettingsService
{
    AppLanguage CurrentLanguage { get; }
    AppColorTheme CurrentTheme { get; }

    Task InitializeAsync();
    Task ReloadAsync();

    Task<RingtonesEntity?> GetSelectedRingtoneAsync();
    Task SetSelectedRingtoneAsync(Guid ringtoneId);
    Task SetLanguageAsync(AppLanguage language);
    Task SetThemeAsync(AppColorTheme theme);
}
