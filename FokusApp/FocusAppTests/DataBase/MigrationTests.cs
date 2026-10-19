using FokusApp.Core.DataBase.ErrorLogging;
using FokusApp.Core.DataBase.Repositories.Implementations;
using FokusApp.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace FocusAppTests.DataBase;

public class MigrationTests : IDisposable
{
    private const string InitialMigration = "20260419000100_InitialCreate";

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public void MigrateApp_EmptyDatabase_AppliesAllMigrations()
    {
        _database.MigrateApp();

        using var context = _database.AppFactory.CreateDbContext();
        var pending = context.Database.GetPendingMigrations().ToList();
        var applied = context.Database.GetAppliedMigrations().ToList();
        var all = context.Database.GetMigrations().ToList();
        Assert.Empty(pending);
        Assert.Equal(all, applied);
        Assert.NotEmpty(all);
    }

    [Fact]
    public void MigrateApp_EmptyDatabase_CreatesCurrentSchema()
    {
        _database.MigrateApp();

        var path = _database.AppDatabasePath;
        Assert.Equal(1, _database.CountTables(path, "Tasks"));
        Assert.Equal(1, _database.CountTables(path, "Settings"));
        Assert.Equal(1, _database.CountTables(path, "Ringtones"));
        Assert.Equal(0, _database.CountTables(path, "Users"));
        Assert.Equal(0, _database.CountTables(path, "FocusTimers"));
        Assert.Equal(0, _database.CountTables(path, "AppErrorLogs"));
    }

    [Fact]
    public void MigrateApp_EmptyDatabase_ModelMatchesMigrationSnapshot()
    {
        _database.MigrateApp();

        using var context = _database.AppFactory.CreateDbContext();
        var hasPendingModelChanges = context.Database.HasPendingModelChanges();
        Assert.False(hasPendingModelChanges);
    }

    [Fact]
    public void MigrateApp_CalledTwice_IsIdempotent()
    {
        _database.MigrateApp();

        var exception = Record.Exception(() => _database.MigrateApp());

        Assert.Null(exception);
    }

    [Fact]
    public void MigrateErrors_EmptyDatabase_AppliesAllMigrationsAndCreatesTable()
    {
        _database.MigrateErrors();

        using var context = _database.ErrorFactory.CreateDbContext();
        var pending = context.Database.GetPendingMigrations().ToList();
        Assert.Empty(pending);
        Assert.Equal(1, _database.CountTables(_database.ErrorDatabasePath, "AppErrorLogs"));
    }

    [Fact]
    public void MigrateErrors_EmptyDatabase_ModelMatchesMigrationSnapshot()
    {
        _database.MigrateErrors();

        using var context = _database.ErrorFactory.CreateDbContext();
        var hasPendingModelChanges = context.Database.HasPendingModelChanges();
        Assert.False(hasPendingModelChanges);
    }

    [Fact]
    public async Task MigrateApp_ExistingFirstVersionDatabase_KeepsAndTransformsData()
    {
        var ids = SeedFirstVersionDatabase();

        _database.MigrateApp();

        var taskRepository = new TaskRepository(_database.AppFactory);
        var settingRepository = new SettingRepository(_database.AppFactory);
        var tasks = await taskRepository.GetAllAsync();
        var settings = await settingRepository.GetAllAsync();
        var normalTask = tasks.Single(task => task.Id == ids.NormalTaskId);
        var regularTask = tasks.Single(task => task.Id == ids.RegularTaskId);
        var setting = Assert.Single(settings);
        Assert.Equal(2, tasks.Count);
        Assert.Equal(25, normalTask.TimerMinutes);
        Assert.Equal(new DateTime(2026, 3, 10), normalTask.TaskDate);
        Assert.Equal(TaskPriority.Hoch, normalTask.Priority);
        Assert.Equal(5, regularTask.TimerMinutes);
        Assert.Null(regularTask.TaskDate);
        Assert.Equal(ids.SettingId, setting.Id);
        Assert.Equal("Raw/Alt.mp3", setting.Ringtone.RingtoneFilePath);
        Assert.Equal(2, setting.Theme);
        Assert.Equal(0, _database.CountTables(_database.AppDatabasePath, "FocusTimers"));
    }

    [Fact]
    public async Task StartupSequence_FirstVersionDatabaseWithErrorLogs_MovesLogsToErrorDatabase()
    {
        SeedFirstVersionDatabase();
        var errorLogId = Guid.NewGuid();
        InsertFirstVersionErrorLog(errorLogId);
        var legacyService = new LegacyAppErrorLogMigrationService(_database.AppFactory, _database.ErrorFactory);

        _database.MigrateErrors();
        legacyService.CopyExistingLogsToErrorDatabase();
        _database.MigrateApp();

        var errorRepository = new AppErrorLogRepository(_database.ErrorFactory);
        var logs = await errorRepository.GetLatestAsync();
        var log = Assert.Single(logs);
        Assert.Equal(errorLogId, log.Id);
        Assert.Equal("Altes Problem", log.Message);
        Assert.Equal(0, _database.CountTables(_database.AppDatabasePath, "AppErrorLogs"));
    }

    private SeededIds SeedFirstVersionDatabase()
    {
        var ringtoneId = Guid.NewGuid();
        var settingId = Guid.NewGuid();
        var normalTaskId = Guid.NewGuid();
        var regularTaskId = Guid.NewGuid();
        _database.MigrateAppTo(InitialMigration);
        _database.ExecuteAppSql(
            $"""
            INSERT INTO "Ringtones" ("Id", "RingtoneFilePath") VALUES ('{ringtoneId}', 'Raw/Alt.mp3');
            INSERT INTO "Users" ("Id", "Theme", "Language", "RingtoneId") VALUES ('{settingId}', 2, 1, '{ringtoneId}');
            INSERT INTO "Tasks"
                ("Id", "IsDone", "Note", "IsRegularTask", "IsWorkTask", "IsPositive",
                 "CreationDate", "TaskDate", "Priority", "BreakMinutes")
            VALUES
                ('{normalTaskId}', 0, 'Normal', 0, 1, 0, '2026-03-10 08:00:00', '2026-03-10 00:00:00', 3, 5),
                ('{regularTaskId}', 0, 'Regelmäßig', 1, 1, 0, '2026-03-09 08:00:00', '2026-03-09 00:00:00', 1, 5);
            INSERT INTO "FocusTimers" ("Id", "TimeSet", "TaskId")
            VALUES ('{Guid.NewGuid()}', '0001-01-01 00:25:00', '{normalTaskId}');
            """);

        return new SeededIds(settingId, normalTaskId, regularTaskId);
    }

    private void InsertFirstVersionErrorLog(Guid id)
    {
        _database.ExecuteAppSql(
            $"""
            INSERT INTO "AppErrorLogs"
                ("Id", "OccurredAtUtc", "Source", "ExceptionType", "Message", "IsFatal", "IsUiFailure", "ThreadId")
            VALUES
                ('{id}', '2026-03-10 08:00:00', 'Quelle', 'System.Exception', 'Altes Problem', 0, 0, 1);
            """);
    }

    private sealed record SeededIds(Guid SettingId, Guid NormalTaskId, Guid RegularTaskId);
}
