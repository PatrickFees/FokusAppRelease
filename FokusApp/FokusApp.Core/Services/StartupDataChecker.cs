using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class StartupDataChecker(
    ITaskRepository taskRepository,
    ISettingRepository settingRepository,
    IRingtonesRepository ringtonesRepository,
    TimeProvider timeProvider) : IStartupDataChecker
{
    public async Task EnsureFirstStartDataAsync()
    {
        if (await settingRepository.AnySettingExistsAsync().ConfigureAwait(false))
        {
            return;
        }

        await ringtonesRepository.AddRangeAsync(StartupDataFactory.CreateRingtones()).ConfigureAwait(false);
        var storedRingtones = await ringtonesRepository.GetAllAsync().ConfigureAwait(false);
        var defaultRingtone = StartupDataFactory.SelectDefaultRingtone(storedRingtones);
        var setting = StartupDataFactory.CreateSetting(defaultRingtone);
        await settingRepository.AddAsync(setting).ConfigureAwait(false);

        var startingTasks = StartupDataFactory.CreateStartingTasks(timeProvider.GetLocalNow());
        await taskRepository.AddRangeAsync(startingTasks).ConfigureAwait(false);
    }
}
