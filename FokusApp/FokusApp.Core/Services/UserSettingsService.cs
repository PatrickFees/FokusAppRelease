using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class UserSettingsService : IUserSettingsService
{
    private readonly ISettingRepository _settingRepository;
    private readonly IRingtoneCatalogService _ringtoneCatalogService;
    private readonly ReaderWriterLockSlim _cacheLock = new();
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private SettingEntity? _currentSetting;
    private volatile bool _initialized;

    public UserSettingsService(ISettingRepository settingRepository, IRingtoneCatalogService ringtoneCatalogService)
    {
        _settingRepository = settingRepository;
        _ringtoneCatalogService = ringtoneCatalogService;
    }

    public AppLanguage CurrentLanguage
    {
        get
        {
            _cacheLock.EnterReadLock();
            try
            {
                return NormalizeLanguage(_currentSetting?.Language);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }
    }

    public AppColorTheme CurrentTheme
    {
        get
        {
            _cacheLock.EnterReadLock();
            try
            {
                return NormalizeTheme(_currentSetting?.Theme);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }
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

    public Task<RingtonesEntity?> GetSelectedRingtoneAsync()
    {
        _cacheLock.EnterReadLock();
        try
        {
            var ringtone = _currentSetting?.Ringtone
                ?? (_currentSetting is null ? null : _ringtoneCatalogService.FindById(_currentSetting.RingtoneId));
            return Task.FromResult(ringtone is null ? null : CloneRingtone(ringtone));
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    public async Task SetSelectedRingtoneAsync(Guid ringtoneId)
    {
        SettingEntity? updatedSetting;
        _cacheLock.EnterReadLock();
        try
        {
            var ringtone = _ringtoneCatalogService.FindById(ringtoneId);
            if (ringtone is null || _currentSetting is null)
            {
                return;
            }

            updatedSetting = new SettingEntity
            {
                Id = _currentSetting.Id,
                Theme = _currentSetting.Theme,
                Language = _currentSetting.Language,
                RingtoneId = ringtone.Id,
                Ringtone = ringtone
            };
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
    }

    public async Task SetLanguageAsync(AppLanguage language)
    {
        var normalizedLanguage = NormalizeLanguage((int)language);
        SettingEntity? updatedSetting;

        _cacheLock.EnterReadLock();
        try
        {
            if (_currentSetting is null || _currentSetting.Language == (int)normalizedLanguage)
            {
                return;
            }

            updatedSetting = new SettingEntity
            {
                Id = _currentSetting.Id,
                Theme = _currentSetting.Theme,
                Language = (int)normalizedLanguage,
                RingtoneId = _currentSetting.RingtoneId,
                Ringtone = _currentSetting.Ringtone
            };
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
    }

    public async Task SetThemeAsync(AppColorTheme theme)
    {
        var normalizedTheme = NormalizeTheme((int)theme);
        SettingEntity? updatedSetting;

        _cacheLock.EnterReadLock();
        try
        {
            if (_currentSetting is null || _currentSetting.Theme == (int)normalizedTheme)
            {
                return;
            }

            updatedSetting = new SettingEntity
            {
                Id = _currentSetting.Id,
                Theme = (int)normalizedTheme,
                Language = _currentSetting.Language,
                RingtoneId = _currentSetting.RingtoneId,
                Ringtone = _currentSetting.Ringtone
            };
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
    }

    private SettingEntity? CloneSetting(SettingEntity? setting)
    {
        if (setting is null)
        {
            return null;
        }

        var ringtone = _ringtoneCatalogService.FindById(setting.RingtoneId) ?? setting.Ringtone;

        return new SettingEntity
        {
            Id = setting.Id,
            Theme = setting.Theme,
            Language = setting.Language,
            RingtoneId = setting.RingtoneId,
            Ringtone = ringtone is null
                ? new RingtonesEntity { Id = setting.RingtoneId }
                : CloneRingtone(ringtone)
        };
    }

    private static RingtonesEntity CloneRingtone(RingtonesEntity ringtone)
    {
        return new RingtonesEntity
        {
            Id = ringtone.Id,
            RingtoneFilePath = ringtone.RingtoneFilePath
        };
    }

    private static AppColorTheme NormalizeTheme(int? theme) => theme is { } value && Enum.IsDefined((AppColorTheme)value)
        ? (AppColorTheme)value
        : AppColorTheme.System;

    private static AppLanguage NormalizeLanguage(int? language) => language switch
    {
        (int)AppLanguage.German => AppLanguage.German,
        _ => AppLanguage.English
    };
}
