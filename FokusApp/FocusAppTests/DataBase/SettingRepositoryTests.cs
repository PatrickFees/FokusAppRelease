using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;

namespace FocusAppTests.DataBase;

public class SettingRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly SettingRepository _repository;
    private readonly RingtonesRepository _ringtonesRepository;

    public SettingRepositoryTests()
    {
        _database.MigrateApp();
        _repository = new SettingRepository(_database.AppFactory);
        _ringtonesRepository = new RingtonesRepository(_database.AppFactory);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task AnySettingExistsAsync_EmptyDatabase_ReturnsFalse()
    {
        var result = await _repository.AnySettingExistsAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task AnySettingExistsAsync_AfterAdd_ReturnsTrue()
    {
        var ringtone = await AddRingtoneAsync();
        var setting = TestEntities.CreateSetting(ringtone.Id);
        await _repository.AddAsync(setting);

        var result = await _repository.AnySettingExistsAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task AddAsync_SettingWithNavigationProperty_PersistsWithoutDuplicatingRingtone()
    {
        var ringtone = await AddRingtoneAsync();
        var setting = TestEntities.CreateSetting(ringtone.Id, theme: 2, language: 1);
        setting.Ringtone = ringtone;

        await _repository.AddAsync(setting);

        var settings = await _repository.GetAllAsync();
        var ringtones = await _ringtonesRepository.GetAllAsync();
        var stored = Assert.Single(settings);
        Assert.Equal(setting.Id, stored.Id);
        Assert.Equal(2, stored.Theme);
        Assert.Equal(1, stored.Language);
        Assert.Single(ringtones);
    }

    [Fact]
    public async Task GetAllAsync_StoredSetting_IncludesRingtone()
    {
        var ringtone = await AddRingtoneAsync("Raw/Eigener.mp3");
        var setting = TestEntities.CreateSetting(ringtone.Id);
        await _repository.AddAsync(setting);

        var settings = await _repository.GetAllAsync();

        var stored = Assert.Single(settings);
        Assert.NotNull(stored.Ringtone);
        Assert.Equal("Raw/Eigener.mp3", stored.Ringtone.RingtoneFilePath);
    }

    [Fact]
    public async Task AddAsync_UnknownRingtone_ThrowsDbUpdateException()
    {
        var unknownRingtoneId = Guid.NewGuid();
        var setting = TestEntities.CreateSetting(unknownRingtoneId);

        await Assert.ThrowsAsync<DbUpdateException>(() => _repository.AddAsync(setting));
    }

    [Fact]
    public async Task AddRangeAsync_MultipleSettings_PersistsAll()
    {
        var ringtone = await AddRingtoneAsync();
        var first = TestEntities.CreateSetting(ringtone.Id);
        var second = TestEntities.CreateSetting(ringtone.Id);
        var settings = new[] { first, second };

        await _repository.AddRangeAsync(settings);

        var stored = await _repository.GetAllAsync();
        Assert.Equal(2, stored.Count);
    }

    [Fact]
    public async Task UpdateAsync_ExistingSetting_UpdatesThemeLanguageAndRingtone()
    {
        var firstRingtone = await AddRingtoneAsync("Raw/Eins.mp3");
        var secondRingtone = await AddRingtoneAsync("Raw/Zwei.mp3");
        var setting = TestEntities.CreateSetting(firstRingtone.Id, theme: 0, language: 0);
        await _repository.AddAsync(setting);
        var changed = TestEntities.CreateSetting(secondRingtone.Id, theme: 2, language: 1);
        changed.Id = setting.Id;

        await _repository.UpdateAsync(changed);

        var settings = await _repository.GetAllAsync();
        var stored = Assert.Single(settings);
        Assert.Equal(2, stored.Theme);
        Assert.Equal(1, stored.Language);
        Assert.Equal(secondRingtone.Id, stored.RingtoneId);
        Assert.Equal("Raw/Zwei.mp3", stored.Ringtone.RingtoneFilePath);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var ringtone = await AddRingtoneAsync();
        var unknown = TestEntities.CreateSetting(ringtone.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.UpdateAsync(unknown));

        Assert.Contains(unknown.Id.ToString(), exception.Message);
    }

    [Fact]
    public async Task DeleteAllAsync_ExistingSettings_RemovesSettingsButKeepsRingtones()
    {
        var ringtone = await AddRingtoneAsync();
        var setting = TestEntities.CreateSetting(ringtone.Id);
        await _repository.AddAsync(setting);

        await _repository.DeleteAllAsync();

        var settings = await _repository.GetAllAsync();
        var ringtones = await _ringtonesRepository.GetAllAsync();
        Assert.Empty(settings);
        Assert.Single(ringtones);
    }

    private async Task<RingtonesEntity> AddRingtoneAsync(string filePath = "Raw/Klingelton.mp3")
    {
        var ringtone = TestEntities.CreateRingtone(filePath);
        await _ringtonesRepository.AddAsync(ringtone);

        return ringtone;
    }
}
