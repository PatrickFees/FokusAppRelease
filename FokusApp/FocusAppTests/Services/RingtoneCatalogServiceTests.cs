using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Services;
using NSubstitute;

namespace FocusAppTests.Services;

public class RingtoneCatalogServiceTests
{
    private readonly IRingtonesRepository _repository = Substitute.For<IRingtonesRepository>();
    private readonly RingtoneCatalogService _service;

    public RingtoneCatalogServiceTests()
    {
        SetStoredRingtones();
        _service = new RingtoneCatalogService(_repository);
    }

    [Fact]
    public void DefaultRingtoneFilePath_Always_ReturnsHappyWithClap()
    {
        Assert.Equal("Raw/Happy with Clap.mp3", _service.DefaultRingtoneFilePath);
    }

    [Fact]
    public void GetAllRingtones_BeforeInitialization_ReturnsEmptyList()
    {
        var result = _service.GetAllRingtones();

        Assert.Empty(result);
    }

    [Fact]
    public void FindById_BeforeInitialization_ReturnsNull()
    {
        var result = _service.FindById(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task InitializeAsync_StoredRingtones_LoadsThemIntoCatalog()
    {
        var first = CreateRingtone("Raw/Eins.mp3");
        var second = CreateRingtone("Raw/Zwei.mp3");
        SetStoredRingtones(first, second);

        await _service.InitializeAsync();

        var result = _service.GetAllRingtones();
        var ids = result.Select(ringtone => ringtone.Id);
        Assert.Equal([first.Id, second.Id], ids);
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_LoadsOnlyOnce()
    {
        await _service.InitializeAsync();
        await _service.InitializeAsync();

        await _repository.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task ReloadAsync_ChangedRepository_ReplacesCatalog()
    {
        var oldRingtone = CreateRingtone("Raw/Alt.mp3");
        var newRingtone = CreateRingtone("Raw/Neu.mp3");
        SetStoredRingtones(oldRingtone);
        await _service.InitializeAsync();
        SetStoredRingtones(newRingtone);

        await _service.ReloadAsync();

        var result = _service.GetAllRingtones();
        var ringtone = Assert.Single(result);
        Assert.Equal(newRingtone.Id, ringtone.Id);
    }

    [Fact]
    public async Task ReloadAsync_WithoutPriorInitialization_LoadsAndPreventsSecondInitialLoad()
    {
        await _service.ReloadAsync();
        await _service.InitializeAsync();

        await _repository.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task FindById_ExistingRingtone_ReturnsIdAndPath()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        SetStoredRingtones(ringtone);
        await _service.InitializeAsync();

        var result = _service.FindById(ringtone.Id);

        Assert.NotNull(result);
        Assert.Equal(ringtone.Id, result.Id);
        Assert.Equal("Raw/Eins.mp3", result.RingtoneFilePath);
    }

    [Fact]
    public async Task FindById_UnknownId_ReturnsNull()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        var unknownId = Guid.NewGuid();
        SetStoredRingtones(ringtone);
        await _service.InitializeAsync();

        var result = _service.FindById(unknownId);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindById_ModifyingReturnedRingtone_DoesNotChangeCatalog()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        SetStoredRingtones(ringtone);
        await _service.InitializeAsync();

        var firstResult = _service.FindById(ringtone.Id);
        firstResult!.RingtoneFilePath = "Raw/Geändert.mp3";

        var secondResult = _service.FindById(ringtone.Id);
        Assert.Equal("Raw/Eins.mp3", secondResult!.RingtoneFilePath);
    }

    [Fact]
    public async Task GetAllRingtones_ModifyingReturnedRingtone_DoesNotChangeCatalog()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        SetStoredRingtones(ringtone);
        await _service.InitializeAsync();

        var firstResult = _service.GetAllRingtones();
        firstResult[0].RingtoneFilePath = "Raw/Geändert.mp3";

        var secondResult = _service.GetAllRingtones();
        Assert.Equal("Raw/Eins.mp3", secondResult[0].RingtoneFilePath);
    }

    [Fact]
    public async Task InitializeAsync_ModifyingRepositoryEntityAfterLoad_DoesNotChangeCatalog()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        SetStoredRingtones(ringtone);
        await _service.InitializeAsync();

        ringtone.RingtoneFilePath = "Raw/Geändert.mp3";

        var result = _service.FindById(ringtone.Id);
        Assert.Equal("Raw/Eins.mp3", result!.RingtoneFilePath);
    }

    private static RingtonesEntity CreateRingtone(string filePath)
    {
        return new RingtonesEntity
        {
            Id = Guid.NewGuid(),
            RingtoneFilePath = filePath
        };
    }

    private void SetStoredRingtones(params RingtonesEntity[] ringtones)
    {
        var storedRingtones = ringtones.ToList();
        _repository.GetAllAsync().Returns(storedRingtones);
    }
}
