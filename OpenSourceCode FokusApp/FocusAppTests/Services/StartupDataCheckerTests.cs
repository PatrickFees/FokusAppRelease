using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Services;
using NSubstitute;

namespace FocusAppTests.Services;

public class StartupDataCheckerTests
{
    private const string DefaultRingtone = "Raw/Happy with Clap.mp3";

    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtonesRepository _ringtonesRepository = Substitute.For<IRingtonesRepository>();
    private readonly StartupDataChecker _checker;
    private List<RingtonesEntity> _storedRingtones = [];
    private List<RingtonesEntity> _addedRingtones = [];
    private SettingEntity? _addedSetting;

    public StartupDataCheckerTests()
    {
        _settingRepository.AnySettingExistsAsync().Returns(false);
        _ringtonesRepository.AddRangeAsync(Arg.Do<IEnumerable<RingtonesEntity>>(CaptureRingtones));
        _ringtonesRepository.GetAllAsync().Returns(_ => _storedRingtones);
        _settingRepository.AddAsync(Arg.Do<SettingEntity>(setting => _addedSetting = setting));
        _checker = new StartupDataChecker(_settingRepository, _ringtonesRepository);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_SettingsAlreadyExist_DoesNotCreateAnyData()
    {
        _settingRepository.AnySettingExistsAsync().Returns(true);

        await _checker.EnsureFirstStartDataAsync();

        await _ringtonesRepository.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<RingtonesEntity>>());
        await _settingRepository.DidNotReceive().AddAsync(Arg.Any<SettingEntity>());
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_FirstStart_CreatesFourRingtonesWithUniqueIds()
    {
        await _checker.EnsureFirstStartDataAsync();

        var ringtoneIds = _addedRingtones.Select(ringtone => ringtone.Id);
        var uniqueIdCount = ringtoneIds.Distinct().Count();
        var filePaths = _addedRingtones.Select(ringtone => ringtone.RingtoneFilePath);
        Assert.Equal(4, _addedRingtones.Count);
        Assert.Equal(4, uniqueIdCount);
        Assert.All(filePaths, filePath => Assert.NotEmpty(filePath ?? string.Empty));
        Assert.Contains(DefaultRingtone, filePaths);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_FirstStart_CreatesSettingsWithDefaultRingtone()
    {
        await _checker.EnsureFirstStartDataAsync();

        var defaultRingtone = _addedRingtones.Single(ringtone => ringtone.RingtoneFilePath == DefaultRingtone);
        Assert.NotNull(_addedSetting);
        Assert.NotEqual(Guid.Empty, _addedSetting.Id);
        Assert.Equal(0, _addedSetting.Theme);
        Assert.Equal(0, _addedSetting.Language);
        Assert.Equal(defaultRingtone.Id, _addedSetting.RingtoneId);
        Assert.Same(defaultRingtone, _addedSetting.Ringtone);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_DefaultRingtoneNotStored_FallsBackToFirstStoredRingtone()
    {
        var other = CreateRingtone("Raw/Anderer.mp3");
        _ringtonesRepository.GetAllAsync().Returns(_ => [other]);

        await _checker.EnsureFirstStartDataAsync();

        Assert.Equal(other.Id, _addedSetting?.RingtoneId);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_DefaultRingtonePathDiffersInCase_StillFindsDefault()
    {
        var upperCaseDefaultPath = DefaultRingtone.ToUpperInvariant();
        var other = CreateRingtone("Raw/Anderer.mp3");
        var defaultWithOtherCase = CreateRingtone(upperCaseDefaultPath);
        _ringtonesRepository.GetAllAsync().Returns(_ => [other, defaultWithOtherCase]);

        await _checker.EnsureFirstStartDataAsync();

        Assert.Equal(defaultWithOtherCase.Id, _addedSetting?.RingtoneId);
    }

    [Fact]
    public async Task EnsureFirstStartDataAsync_NoRingtoneStored_ThrowsAndCreatesNoSettings()
    {
        var noRingtones = new List<RingtonesEntity>();
        _ringtonesRepository.GetAllAsync().Returns(_ => noRingtones);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _checker.EnsureFirstStartDataAsync());

        await _settingRepository.DidNotReceive().AddAsync(Arg.Any<SettingEntity>());
    }

    private static RingtonesEntity CreateRingtone(string filePath)
    {
        return new RingtonesEntity
        {
            Id = Guid.NewGuid(),
            RingtoneFilePath = filePath
        };
    }

    private void CaptureRingtones(IEnumerable<RingtonesEntity> ringtones)
    {
        _addedRingtones = ringtones.ToList();
        _storedRingtones = _addedRingtones;
    }
}
