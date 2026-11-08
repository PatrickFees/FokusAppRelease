using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class SettingStore : ISettingStore
{
    public event EventHandler? SettingsLoaded;

    private readonly ISettingRepository _settingRepository;
    private readonly IRingtoneCatalogService _ringtoneCatalogService;
    private readonly ReaderWriterLockSlim _cacheLock = new();
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private SettingEntity? _currentSetting;
    private volatile bool _initialized;

    public SettingStore(ISettingRepository settingRepository, IRingtoneCatalogService ringtoneCatalogService)
    {
        _settingRepository = settingRepository;
        _ringtoneCatalogService = ringtoneCatalogService;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            await LoadSettingsAsync().ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task ReloadAsync()
    {
        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await LoadSettingsAsync().ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public TResult Read<TResult>(Func<SettingEntity?, TResult> project)
    {
        _cacheLock.EnterReadLock();
        try
        {
            return project(_currentSetting);
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    public async Task<bool> UpdateAsync(Func<SettingEntity, SettingEntity?> tryMutate)
    {
        SettingEntity? updatedSetting;

        _cacheLock.EnterReadLock();
        try
        {
            if (_currentSetting is null)
            {
                return false;
            }

            updatedSetting = tryMutate(_currentSetting);
            if (updatedSetting is null)
            {
                return false;
            }
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }

        await _settingRepository.UpdateAsync(updatedSetting).ConfigureAwait(false);

        _cacheLock.EnterWriteLock();
        try
        {
            _currentSetting = updatedSetting;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }

        return true;
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingRepository.GetAllAsync().ConfigureAwait(false);
        var settingEntity = settings.FirstOrDefault();
        var cloned = CloneSetting(settingEntity);

        _cacheLock.EnterWriteLock();
        try
        {
            _currentSetting = cloned;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }

        SettingsLoaded?.Invoke(this, EventArgs.Empty);
    }

    private SettingEntity? CloneSetting(SettingEntity? setting)
    {
        if (setting is null)
        {
            return null;
        }

        var ringtone = _ringtoneCatalogService.FindById(setting.RingtoneId) ?? setting.Ringtone;

        var cloned = SettingSnapshot.Copy(setting);
        cloned.Ringtone = ringtone is null
            ? new RingtonesEntity { Id = setting.RingtoneId }
            : SettingSnapshot.CopyRingtone(ringtone);
        return cloned;
    }
}
