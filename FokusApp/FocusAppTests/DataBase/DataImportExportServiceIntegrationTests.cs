using FokusApp.Core.DataBase.Repositories.Implementations;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using System.Text;

namespace FocusAppTests.DataBase;

public class DataImportExportServiceIntegrationTests : IDisposable
{
    private readonly SqliteTestDatabase _source = new();
    private readonly SqliteTestDatabase _target = new();
    private readonly FakeTimeProvider _timeProvider = new();

    public DataImportExportServiceIntegrationTests()
    {
        _source.MigrateApp();
        _target.MigrateApp();
    }

    public void Dispose()
    {
        _source.Dispose();
        _target.Dispose();
    }

    [Fact]
    public async Task ExportAndImport_ThroughService_RestoresAllDataInOtherDatabase()
    {
        var ringtone = TestEntities.CreateRingtone("Raw/Eigener.mp3");
        var setting = TestEntities.CreateSetting(ringtone.Id, theme: 2, language: 1);
        var normalTask = TestEntities.CreateTask(creationOffsetMinutes: 5);
        var regularTask = TestEntities.CreateTask(isRegular: true, isWork: false);
        var doneTask = TestEntities.CreateTask(isDone: true);
        await new RingtonesRepository(_source.AppFactory).AddAsync(ringtone);
        await new SettingRepository(_source.AppFactory).AddAsync(setting);
        await new TaskRepository(_source.AppFactory).AddRangeAsync([normalTask, regularTask, doneTask]);
        var fileService = Substitute.For<IDataTransferFileService>();
        string? exportedJson = null;
        await fileService.ShareExportFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Do<string>(json => exportedJson = json));
        var exporter = CreateService(_source, fileService);
        await exporter.ExportAsync();
        var importStream = new MemoryStream(Encoding.UTF8.GetBytes(exportedJson!));
        fileService.OpenImportFileAsync(Arg.Any<string>()).Returns(importStream);
        var importer = CreateService(_target, fileService);

        var result = await importer.ImportAsync();

        var targetTasks = await new TaskRepository(_target.AppFactory).GetAllAsync();
        var targetSettings = await new SettingRepository(_target.AppFactory).GetAllAsync();
        var targetRingtones = await new RingtonesRepository(_target.AppFactory).GetAllAsync();
        var targetTaskIds = targetTasks.Select(task => task.Id).Order().ToList();
        var sourceTaskIds = new[] { normalTask.Id, regularTask.Id, doneTask.Id }.Order().ToList();
        var targetSetting = Assert.Single(targetSettings);
        Assert.True(result.IsSuccess);
        Assert.Equal(sourceTaskIds, targetTaskIds);
        Assert.Equal(setting.Id, targetSetting.Id);
        Assert.Equal("Raw/Eigener.mp3", targetSetting.Ringtone.RingtoneFilePath);
        Assert.Single(targetRingtones);
    }

    [Fact]
    public async Task ResetToStartDataAsync_ExistingData_ReplacesItWithSameDataAsFirstStart()
    {
        var ringtone = TestEntities.CreateRingtone("Raw/Eigener.mp3");
        var setting = TestEntities.CreateSetting(ringtone.Id, theme: 2, language: 1);
        var task = TestEntities.CreateTask();
        await new RingtonesRepository(_source.AppFactory).AddAsync(ringtone);
        await new SettingRepository(_source.AppFactory).AddAsync(setting);
        await new TaskRepository(_source.AppFactory).AddRangeAsync([task]);
        var service = CreateService(_source, Substitute.For<IDataTransferFileService>());
        var firstStart = await CreateFirstStartSnapshotAsync();

        var result = await service.ResetToStartDataAsync();

        var resetSnapshot = await ReadSnapshotAsync(_source);
        Assert.True(result.IsSuccess);
        Assert.Equal(firstStart.RingtonePaths, resetSnapshot.RingtonePaths);
        Assert.Equal(firstStart.TaskNotes, resetSnapshot.TaskNotes);
        Assert.Equal(firstStart.DefaultRingtonePath, resetSnapshot.DefaultRingtonePath);
        Assert.Equal(firstStart.Language, resetSnapshot.Language);
        Assert.Equal(firstStart.Theme, resetSnapshot.Theme);
        Assert.DoesNotContain(task.Id, resetSnapshot.TaskIds);
        Assert.DoesNotContain(ringtone.Id, resetSnapshot.RingtoneIds);
    }

    private async Task<StartDataSnapshot> CreateFirstStartSnapshotAsync()
    {
        using var freshDatabase = new SqliteTestDatabase();
        freshDatabase.MigrateApp();
        var checker = new StartupDataChecker(
            new TaskRepository(freshDatabase.AppFactory),
            new SettingRepository(freshDatabase.AppFactory),
            new RingtonesRepository(freshDatabase.AppFactory),
            _timeProvider);
        await checker.EnsureFirstStartDataAsync();
        return await ReadSnapshotAsync(freshDatabase);
    }

    private static async Task<StartDataSnapshot> ReadSnapshotAsync(SqliteTestDatabase database)
    {
        var tasks = await new TaskRepository(database.AppFactory).GetAllAsync();
        var settings = await new SettingRepository(database.AppFactory).GetAllAsync();
        var ringtones = await new RingtonesRepository(database.AppFactory).GetAllAsync();
        var setting = Assert.Single(settings);

        return new StartDataSnapshot(
            ringtones.Select(ringtone => ringtone.RingtoneFilePath!).Order().ToList(),
            ringtones.Select(ringtone => ringtone.Id).ToList(),
            tasks.Select(task => task.Note!).Order().ToList(),
            tasks.Select(task => task.Id).ToList(),
            setting.Ringtone.RingtoneFilePath!,
            setting.Language,
            setting.Theme);
    }

    private sealed record StartDataSnapshot(
        List<string> RingtonePaths,
        List<Guid> RingtoneIds,
        List<string> TaskNotes,
        List<Guid> TaskIds,
        string DefaultRingtonePath,
        int Language,
        int Theme);

    private DataImportExportService CreateService(SqliteTestDatabase database, IDataTransferFileService fileService)
    {
        var textProvider = Substitute.For<IDataTransferTextProvider>();
        textProvider.GetText(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgAt<string>(0));

        return new DataImportExportService(
            new TaskRepository(database.AppFactory),
            new SettingRepository(database.AppFactory),
            new RingtonesRepository(database.AppFactory),
            new DataImportRepository(database.AppFactory),
            fileService,
            textProvider,
            Substitute.For<IAppDataCoordinator>(),
            Substitute.For<IAppLocalizationService>(),
            _timeProvider);
    }
}
