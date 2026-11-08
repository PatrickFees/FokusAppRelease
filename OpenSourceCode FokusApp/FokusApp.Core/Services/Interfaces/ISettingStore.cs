using FokusApp.Core.DataBase.Entities;

namespace FokusApp.Core.Services.Interfaces;

public interface ISettingStore
{

    event EventHandler? SettingsLoaded;

    Task InitializeAsync();
    Task ReloadAsync();

    TResult Read<TResult>(Func<SettingEntity?, TResult> project);

    Task<bool> UpdateAsync(Func<SettingEntity, SettingEntity?> tryMutate);
}
