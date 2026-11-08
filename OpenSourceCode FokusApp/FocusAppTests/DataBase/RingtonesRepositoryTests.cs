using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Implementations;
using Microsoft.Data.Sqlite;

namespace FocusAppTests.DataBase;

public class RingtonesRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly RingtonesRepository _repository;

    public RingtonesRepositoryTests()
    {
        _database.MigrateApp();
        _repository = new RingtonesRepository(_database.AppFactory);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _repository.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_NewRingtone_PersistsIdAndPath()
    {
        var ringtone = TestEntities.CreateRingtone("Raw/Neu.mp3");

        await _repository.AddAsync(ringtone);

        var stored = await _repository.GetAllAsync();
        var item = Assert.Single(stored);
        Assert.Equal(ringtone.Id, item.Id);
        Assert.Equal("Raw/Neu.mp3", item.RingtoneFilePath);
    }

    [Fact]
    public async Task AddAsync_PathAlreadyStored_IgnoresDuplicate()
    {
        var first = TestEntities.CreateRingtone("Raw/Gleich.mp3");
        var duplicate = TestEntities.CreateRingtone("Raw/Gleich.mp3");
        await _repository.AddAsync(first);

        await _repository.AddAsync(duplicate);

        var stored = await _repository.GetAllAsync();
        var item = Assert.Single(stored);
        Assert.Equal(first.Id, item.Id);
    }

    [Fact]
    public async Task AddRangeAsync_EmptyCollection_DoesNothing()
    {
        var ringtones = new List<RingtonesEntity>();

        await _repository.AddRangeAsync(ringtones);

        var stored = await _repository.GetAllAsync();
        Assert.Empty(stored);
    }

    [Fact]
    public async Task AddRangeAsync_MixOfNewAndExistingPaths_AddsOnlyNewOnes()
    {
        var existing = TestEntities.CreateRingtone("Raw/Alt.mp3");
        await _repository.AddAsync(existing);
        var duplicate = TestEntities.CreateRingtone("Raw/Alt.mp3");
        var added = TestEntities.CreateRingtone("Raw/Neu.mp3");
        var ringtones = new[] { duplicate, added };

        await _repository.AddRangeAsync(ringtones);

        var stored = await _repository.GetAllAsync();
        var paths = stored.Select(item => item.RingtoneFilePath).OrderBy(path => path).ToList();
        Assert.Equal(["Raw/Alt.mp3", "Raw/Neu.mp3"], paths);
        Assert.DoesNotContain(stored, item => item.Id == duplicate.Id);
    }

    [Fact]
    public async Task AddRangeAsync_OnlyExistingPaths_DoesNotAddAnything()
    {
        var existing = TestEntities.CreateRingtone("Raw/Alt.mp3");
        await _repository.AddAsync(existing);
        var duplicate = TestEntities.CreateRingtone("Raw/Alt.mp3");
        var ringtones = new[] { duplicate };

        await _repository.AddRangeAsync(ringtones);

        var stored = await _repository.GetAllAsync();
        Assert.Single(stored);
    }

    [Fact]
    public async Task DeleteAllAsync_ExistingRingtones_RemovesAll()
    {
        var first = TestEntities.CreateRingtone("Raw/A.mp3");
        var second = TestEntities.CreateRingtone("Raw/B.mp3");
        var ringtones = new[] { first, second };
        await _repository.AddRangeAsync(ringtones);

        await _repository.DeleteAllAsync();

        var stored = await _repository.GetAllAsync();
        Assert.Empty(stored);
    }

    [Fact]
    public async Task DeleteAllAsync_RingtoneReferencedBySetting_ThrowsAndKeepsRingtone()
    {
        var ringtone = TestEntities.CreateRingtone();
        await _repository.AddAsync(ringtone);
        var settingRepository = new SettingRepository(_database.AppFactory);
        var setting = TestEntities.CreateSetting(ringtone.Id);
        await settingRepository.AddAsync(setting);

        await Assert.ThrowsAsync<SqliteException>(() => _repository.DeleteAllAsync());

        var stored = await _repository.GetAllAsync();
        Assert.Single(stored);
    }
}
