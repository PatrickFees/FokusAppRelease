using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class RingtoneCatalogService : IRingtoneCatalogService
{
    public string DefaultRingtoneFilePath => "Raw/Happy with Clap.mp3";

    private readonly IRingtonesRepository _ringtonesRepository;
    private readonly ReaderWriterLockSlim _cacheLock = new();
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private List<RingtonesEntity> _ringtoneList = [];
    private volatile bool _initialized;

    public RingtoneCatalogService(IRingtonesRepository ringtonesRepository)
    {
        _ringtonesRepository = ringtonesRepository;
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

            await LoadRingtonesAsync().ConfigureAwait(false);
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
            await LoadRingtonesAsync().ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public IReadOnlyList<RingtonesEntity> GetAllRingtones()
    {
        _cacheLock.EnterReadLock();
        try
        {
            return _ringtoneList.Select(CloneRingtone).ToArray();
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    public RingtonesEntity? FindById(Guid id)
    {
        _cacheLock.EnterReadLock();
        try
        {
            var ringtone = _ringtoneList.FirstOrDefault(item => item.Id == id);
            return ringtone is null ? null : CloneRingtone(ringtone);
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    private async Task LoadRingtonesAsync()
    {
        var ringtones = await _ringtonesRepository.GetAllAsync().ConfigureAwait(false);

        _cacheLock.EnterWriteLock();
        try
        {
            _ringtoneList = ringtones.Select(CloneRingtone).ToList();
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    private static RingtonesEntity CloneRingtone(RingtonesEntity ringtone)
    {
        return new RingtonesEntity
        {
            Id = ringtone.Id,
            RingtoneFilePath = ringtone.RingtoneFilePath
        };
    }
}
