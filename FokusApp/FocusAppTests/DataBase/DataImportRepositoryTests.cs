using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Implementations;
using FokusApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FocusAppTests.DataBase;

public class DataImportRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly DataImportRepository _repository;
    private readonly TaskRepository _taskRepository;
    private readonly SettingRepository _settingRepository;
    private readonly RingtonesRepository _ringtonesRepository;

    public DataImportRepositoryTests()
    {
        _database.MigrateApp();
        _repository = new DataImportRepository(_database.AppFactory);
        _taskRepository = new TaskRepository(_database.AppFactory);
        _settingRepository = new SettingRepository(_database.AppFactory);
        _ringtonesRepository = new RingtonesRepository(_database.AppFactory);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task ReplaceAllAsync_EmptyDatabase_StoresAllEntities()
    {
        var ringtone = TestEntities.CreateRingtone();
        var setting = TestEntities.CreateSetting(ringtone.Id);
        var task = TestEntities.CreateTask();

        await _repository.ReplaceAllAsync([ringtone], [setting], [task]);

        var ringtones = await _ringtonesRepository.GetAllAsync();
        var settings = await _settingRepository.GetAllAsync();
        var tasks = await _taskRepository.GetAllAsync();
        var storedRingtone = Assert.Single(ringtones);
        Assert.Equal(ringtone.Id, storedRingtone.Id);
        var storedSetting = Assert.Single(settings);
        Assert.Equal(setting.Id, storedSetting.Id);
        var storedTask = Assert.Single(tasks);
        Assert.Equal(task.Id, storedTask.Id);
    }

    [Fact]
    public async Task ReplaceAllAsync_ExistingData_ReplacesInsteadOfMerging()
    {
        await SeedAsync();
        var newRingtone = TestEntities.CreateRingtone("Raw/Importiert.mp3");
        var newSetting = TestEntities.CreateSetting(newRingtone.Id, theme: 2);
        var newTask = TestEntities.CreateTask(isDone: true);

        await _repository.ReplaceAllAsync([newRingtone], [newSetting], [newTask]);

        var ringtones = await _ringtonesRepository.GetAllAsync();
        var settings = await _settingRepository.GetAllAsync();
        var tasks = await _taskRepository.GetAllAsync();
        var storedRingtone = Assert.Single(ringtones);
        Assert.Equal(newRingtone.Id, storedRingtone.Id);
        var storedSetting = Assert.Single(settings);
        Assert.Equal(newSetting.Id, storedSetting.Id);
        var storedTask = Assert.Single(tasks);
        Assert.Equal(newTask.Id, storedTask.Id);
    }

    [Fact]
    public async Task ReplaceAllAsync_EmptyCollections_ClearsAllTables()
    {
        await SeedAsync();

        await _repository.ReplaceAllAsync([], [], []);

        var ringtones = await _ringtonesRepository.GetAllAsync();
        var settings = await _settingRepository.GetAllAsync();
        var tasks = await _taskRepository.GetAllAsync();
        Assert.Empty(ringtones);
        Assert.Empty(settings);
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task ReplaceAllAsync_TaskViolatesConstraint_RollsBackAndKeepsOldData()
    {
        var seeded = await SeedAsync();
        var newRingtone = TestEntities.CreateRingtone("Raw/Importiert.mp3");
        var newSetting = TestEntities.CreateSetting(newRingtone.Id);
        var invalidTask = TestEntities.CreateTask(isRegular: true);
        invalidTask.TaskDate = TestEntities.BaseDate.Date;

        await Assert.ThrowsAsync<DbUpdateException>(
            () => _repository.ReplaceAllAsync([newRingtone], [newSetting], [invalidTask]));

        var ringtones = await _ringtonesRepository.GetAllAsync();
        var settings = await _settingRepository.GetAllAsync();
        var tasks = await _taskRepository.GetAllAsync();
        var storedRingtone = Assert.Single(ringtones);
        Assert.Equal(seeded.Ringtone.Id, storedRingtone.Id);
        var storedSetting = Assert.Single(settings);
        Assert.Equal(seeded.Setting.Id, storedSetting.Id);
        var storedTask = Assert.Single(tasks);
        Assert.Equal(seeded.Task.Id, storedTask.Id);
    }

    [Fact]
    public async Task ReplaceAllAsync_SettingReferencesUnknownRingtone_RollsBackAndKeepsOldData()
    {
        var seeded = await SeedAsync();
        var unknownRingtoneId = Guid.NewGuid();
        var orphanSetting = TestEntities.CreateSetting(unknownRingtoneId);

        await Assert.ThrowsAsync<DbUpdateException>(() => _repository.ReplaceAllAsync([], [orphanSetting], []));

        var settings = await _settingRepository.GetAllAsync();
        var tasks = await _taskRepository.GetAllAsync();
        var storedSetting = Assert.Single(settings);
        Assert.Equal(seeded.Setting.Id, storedSetting.Id);
        var storedTask = Assert.Single(tasks);
        Assert.Equal(seeded.Task.Id, storedTask.Id);
    }

    [Fact]
    public async Task ReplaceAllAsync_NullArgument_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.ReplaceAllAsync(null!, [], []));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.ReplaceAllAsync([], null!, []));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.ReplaceAllAsync([], [], null!));
    }

    [Fact]
    public async Task ReplaceAllAsync_CancelledToken_ThrowsAndKeepsOldData()
    {
        var seeded = await SeedAsync();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _repository.ReplaceAllAsync([], [], [], cancellation.Token));

        var tasks = await _taskRepository.GetAllAsync();
        var storedTask = Assert.Single(tasks);
        Assert.Equal(seeded.Task.Id, storedTask.Id);
    }

    [Fact]
    public async Task ExportAndImport_RoundTripThroughJson_RestoresIdenticalData()
    {
        var seeded = await SeedAsync();
        var regularTask = TestEntities.CreateTask(isRegular: true, isWork: false, creationOffsetMinutes: 15);
        regularTask.TimerMinutes = 0;
        await _taskRepository.AddAsync(regularTask);
        var json = await ExportToJsonAsync();
        using var target = new SqliteTestDatabase();
        target.MigrateApp();
        var targetImport = new DataImportRepository(target.AppFactory);

        await ImportFromJsonAsync(json, targetImport);

        var targetTaskRepository = new TaskRepository(target.AppFactory);
        var targetTasks = await targetTaskRepository.GetAllAsync();
        var targetSettingRepository = new SettingRepository(target.AppFactory);
        var targetSettings = await targetSettingRepository.GetAllAsync();
        var targetRingtoneRepository = new RingtonesRepository(target.AppFactory);
        var targetRingtones = await targetRingtoneRepository.GetAllAsync();
        var sourceTasks = await _taskRepository.GetAllAsync();
        Assert.Equal(sourceTasks.Count, targetTasks.Count);
        foreach (var sourceTask in sourceTasks)
        {
            var targetTask = targetTasks.Single(task => task.Id == sourceTask.Id);
            AssertTasksEqual(sourceTask, targetTask);
        }

        var targetSetting = Assert.Single(targetSettings);
        Assert.Equal(seeded.Setting.Id, targetSetting.Id);
        Assert.Equal(seeded.Setting.Theme, targetSetting.Theme);
        Assert.Equal(seeded.Setting.Language, targetSetting.Language);
        Assert.Equal(seeded.Ringtone.RingtoneFilePath, targetSetting.Ringtone.RingtoneFilePath);
        var storedTargetRingtone = Assert.Single(targetRingtones);
        Assert.Equal(seeded.Ringtone.Id, storedTargetRingtone.Id);
    }

    [Fact]
    public async Task Import_PayloadWithoutSettingsAndLegacyUsers_StoresLegacySettings()
    {
        var seeded = await SeedAsync();
        var json = await ExportToJsonAsync();
        var legacyJson = json.Replace("\"Settings\"", "\"Users\"", StringComparison.Ordinal);
        await _repository.ReplaceAllAsync([], [], []);

        await ImportFromJsonAsync(legacyJson, _repository);

        var settings = await _settingRepository.GetAllAsync();
        var setting = Assert.Single(settings);
        Assert.Equal(seeded.Setting.Id, setting.Id);
    }

    private async Task<SeededData> SeedAsync()
    {
        var ringtone = TestEntities.CreateRingtone();
        var setting = TestEntities.CreateSetting(ringtone.Id, theme: 2, language: 1);
        var task = TestEntities.CreateTask();
        await _ringtonesRepository.AddAsync(ringtone);
        await _settingRepository.AddAsync(setting);
        await _taskRepository.AddAsync(task);

        return new SeededData(ringtone, setting, task);
    }

    private async Task<string> ExportToJsonAsync()
    {
        var settings = await _settingRepository.GetAllAsync();
        var tasks = await _taskRepository.GetAllAsync();
        var ringtones = await _ringtonesRepository.GetAllAsync();
        var payload = new DataTransferPayload
        {
            ExportedAtUtc = TestEntities.BaseDate,
            Settings = settings.Select(SettingDataTransferModel.FromEntity).ToList(),
            Tasks = tasks.Select(TaskDataTransferModel.FromEntity).ToList(),
            Ringtones = ringtones.Select(RingtoneDataTransferModel.FromEntity).ToList(),
            FocusTimers = []
        };
        var options = new JsonSerializerOptions { WriteIndented = true };

        return JsonSerializer.Serialize(payload, options);
    }

    private static async Task ImportFromJsonAsync(string json, DataImportRepository target)
    {
        var payload = JsonSerializer.Deserialize<DataTransferPayload>(json);
        Assert.NotNull(payload);
        Assert.True(payload.IsValid());
        var ringtones = payload.Ringtones!.Select(item => item.ToEntity()).ToArray();
        var settings = payload.GetSettingsForImport().Select(item => item.ToEntity()).ToArray();
        var tasks = payload.CreateTaskEntities().ToArray();

        await target.ReplaceAllAsync(ringtones, settings, tasks);
    }

    private static void AssertTasksEqual(TaskEntity expected, TaskEntity actual)
    {
        Assert.Equal(expected.IsDone, actual.IsDone);
        Assert.Equal(expected.Note, actual.Note);
        Assert.Equal(expected.IsRegularTask, actual.IsRegularTask);
        Assert.Equal(expected.IsWorkTask, actual.IsWorkTask);
        Assert.Equal(expected.IsPositive, actual.IsPositive);
        Assert.Equal(expected.CreationDate, actual.CreationDate);
        Assert.Equal(expected.TaskDate, actual.TaskDate);
        Assert.Equal(expected.Priority, actual.Priority);
        Assert.Equal(expected.BreakMinutes, actual.BreakMinutes);
        Assert.Equal(expected.TimerMinutes, actual.TimerMinutes);
    }

    private sealed record SeededData(RingtonesEntity Ringtone, SettingEntity Setting, TaskEntity Task);
}
