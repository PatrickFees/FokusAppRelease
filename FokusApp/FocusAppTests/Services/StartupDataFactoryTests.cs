using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Services;

namespace FocusAppTests.Services;

public class StartupDataFactoryTests
{
    [Fact]
    public void CreateRingtones_Always_ReturnsFourRingtonesWithUniqueIdsAndDefaultAsset()
    {
        var ringtones = StartupDataFactory.CreateRingtones().ToList();

        var uniqueIds = ringtones.Select(ringtone => ringtone.Id).Distinct().ToList();
        Assert.Equal(4, ringtones.Count);
        Assert.Equal(4, uniqueIds.Count);
        Assert.Contains(ringtones, ringtone => ringtone.RingtoneFilePath == StartupDataFactory.DefaultRingtoneAsset);
    }

    [Fact]
    public void SelectDefaultRingtone_DefaultAssetPresent_IgnoresCaseOfPath()
    {
        var other = new RingtonesEntity { Id = Guid.NewGuid(), RingtoneFilePath = "Raw/Andere.mp3" };
        var defaultRingtone = new RingtonesEntity
        {
            Id = Guid.NewGuid(),
            RingtoneFilePath = StartupDataFactory.DefaultRingtoneAsset.ToUpperInvariant()
        };
        RingtonesEntity[] ringtones = [other, defaultRingtone];

        var selected = StartupDataFactory.SelectDefaultRingtone(ringtones);

        Assert.Same(defaultRingtone, selected);
    }

    [Fact]
    public void SelectDefaultRingtone_DefaultAssetMissing_UsesFirstRingtone()
    {
        var first = new RingtonesEntity { Id = Guid.NewGuid(), RingtoneFilePath = "Raw/Erste.mp3" };
        var second = new RingtonesEntity { Id = Guid.NewGuid(), RingtoneFilePath = "Raw/Zweite.mp3" };
        RingtonesEntity[] ringtones = [first, second];

        var selected = StartupDataFactory.SelectDefaultRingtone(ringtones);

        Assert.Same(first, selected);
    }

    [Fact]
    public void SelectDefaultRingtone_NoRingtones_Throws()
    {
        RingtonesEntity[] ringtones = [];

        Assert.Throws<InvalidOperationException>(() => StartupDataFactory.SelectDefaultRingtone(ringtones));
    }

    [Fact]
    public void CreateSetting_DefaultRingtone_UsesGermanAndFirstTheme()
    {
        var ringtone = new RingtonesEntity { Id = Guid.NewGuid(), RingtoneFilePath = "Raw/A.mp3" };

        var setting = StartupDataFactory.CreateSetting(ringtone);

        Assert.Equal(ringtone.Id, setting.RingtoneId);
        Assert.Equal(0, setting.Language);
        Assert.Equal(0, setting.Theme);
    }

    [Fact]
    public void CreateStartingTasks_Now_ReturnsOneOpenAndOneDoneTaskForTodayInWorkScope()
    {
        var now = new DateTimeOffset(2026, 3, 10, 14, 30, 0, TimeSpan.Zero);

        var tasks = StartupDataFactory.CreateStartingTasks(now).ToList();

        Assert.Equal(2, tasks.Count);
        Assert.Single(tasks, task => !task.IsDone);
        Assert.Single(tasks, task => task.IsDone);
        Assert.All(tasks, task => Assert.Equal(now.Date, task.TaskDate));
        Assert.All(tasks, task => Assert.True(task.IsWorkTask));
    }
}
