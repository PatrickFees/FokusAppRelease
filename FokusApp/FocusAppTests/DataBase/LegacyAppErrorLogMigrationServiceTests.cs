using FokusApp.Core.DataBase.ErrorLogging;
using FokusApp.Core.DataBase.Repositories.Implementations;
using Microsoft.Data.Sqlite;

namespace FocusAppTests.DataBase;

public class LegacyAppErrorLogMigrationServiceTests : IDisposable
{
    private const string MigrationWithLegacyTable = "20260701000100_MoveFocusTimerMinutesToTasks";

    private readonly SqliteTestDatabase _database = new();
    private readonly AppErrorLogRepository _errorRepository;
    private readonly LegacyAppErrorLogMigrationService _service;

    public LegacyAppErrorLogMigrationServiceTests()
    {
        _errorRepository = new AppErrorLogRepository(_database.ErrorFactory);
        _service = new LegacyAppErrorLogMigrationService(_database.AppFactory, _database.ErrorFactory);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task CopyExistingLogsToErrorDatabase_CurrentProductiveSchemaWithoutLegacyTable_CopiesNothing()
    {
        _database.MigrateApp();
        _database.MigrateErrors();

        _service.CopyExistingLogsToErrorDatabase();

        var logs = await _errorRepository.GetLatestAsync();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task CopyExistingLogsToErrorDatabase_EmptyLegacyTable_CopiesNothing()
    {
        _database.MigrateAppTo(MigrationWithLegacyTable);
        _database.MigrateErrors();

        _service.CopyExistingLogsToErrorDatabase();

        var logs = await _errorRepository.GetLatestAsync();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task CopyExistingLogsToErrorDatabase_LegacyLogs_CopiesAllFields()
    {
        _database.MigrateAppTo(MigrationWithLegacyTable);
        _database.MigrateErrors();
        var legacy = new LegacyRow();
        InsertLegacyLog(legacy);

        _service.CopyExistingLogsToErrorDatabase();

        var logs = await _errorRepository.GetLatestAsync();
        var log = Assert.Single(logs);
        var expectedTime = new DateTime(2026, 3, 10, 8, 0, 0);
        Assert.Equal(legacy.Id, log.Id);
        Assert.Equal(expectedTime, log.OccurredAtUtc);
        Assert.Equal("Quelle", log.Source);
        Assert.Equal("System.Exception", log.ExceptionType);
        Assert.Equal("Fehlertext", log.Message);
        Assert.Equal("Stack", log.StackTrace);
        Assert.Equal("Inner", log.InnerException);
        Assert.Equal("/route", log.Route);
        Assert.Equal("Android", log.Platform);
        Assert.Equal("14", log.OsVersion);
        Assert.Equal("1.2.3", log.AppVersion);
        Assert.True(log.IsFatal);
        Assert.True(log.IsUiFailure);
        Assert.Equal(9, log.ThreadId);
        Assert.Equal("{\"a\":1}", log.PayloadJson);
    }

    [Fact]
    public async Task CopyExistingLogsToErrorDatabase_MultipleLogs_CopiesAll()
    {
        _database.MigrateAppTo(MigrationWithLegacyTable);
        _database.MigrateErrors();
        InsertLegacyLog(new LegacyRow());
        InsertLegacyLog(new LegacyRow());
        InsertLegacyLog(new LegacyRow());

        _service.CopyExistingLogsToErrorDatabase();

        var logs = await _errorRepository.GetLatestAsync();
        Assert.Equal(3, logs.Count);
    }

    [Fact]
    public async Task CopyExistingLogsToErrorDatabase_CalledTwice_DoesNotCreateDuplicates()
    {
        _database.MigrateAppTo(MigrationWithLegacyTable);
        _database.MigrateErrors();
        InsertLegacyLog(new LegacyRow());
        _service.CopyExistingLogsToErrorDatabase();

        _service.CopyExistingLogsToErrorDatabase();

        var logs = await _errorRepository.GetLatestAsync();
        Assert.Single(logs);
    }

    [Fact]
    public async Task CopyExistingLogsToErrorDatabase_IdAlreadyInErrorDatabase_KeepsExistingEntry()
    {
        _database.MigrateAppTo(MigrationWithLegacyTable);
        _database.MigrateErrors();
        var existing = TestEntities.CreateErrorLog();
        existing.Message = "Bereits vorhanden";
        await _errorRepository.AddAsync(existing);
        var legacy = new LegacyRow { Id = existing.Id, Message = "Alt" };
        InsertLegacyLog(legacy);

        _service.CopyExistingLogsToErrorDatabase();

        var logs = await _errorRepository.GetLatestAsync();
        var log = Assert.Single(logs);
        Assert.Equal("Bereits vorhanden", log.Message);
    }

    [Fact]
    public async Task CopyExistingLogsToErrorDatabase_NullableColumnsNull_CopiesNullsAndZeroThreadId()
    {
        _database.MigrateAppTo(MigrationWithLegacyTable);
        _database.MigrateErrors();
        var legacy = new LegacyRow
        {
            StackTrace = null,
            InnerException = null,
            Route = null,
            Platform = null,
            OsVersion = null,
            AppVersion = null,
            PayloadJson = null,
            IsFatal = 0,
            IsUiFailure = 0,
            ThreadId = 0
        };
        InsertLegacyLog(legacy);

        _service.CopyExistingLogsToErrorDatabase();

        var logs = await _errorRepository.GetLatestAsync();
        var log = Assert.Single(logs);
        Assert.Null(log.StackTrace);
        Assert.Null(log.InnerException);
        Assert.Null(log.Route);
        Assert.Null(log.Platform);
        Assert.Null(log.OsVersion);
        Assert.Null(log.AppVersion);
        Assert.Null(log.PayloadJson);
        Assert.False(log.IsFatal);
        Assert.False(log.IsUiFailure);
        Assert.Equal(0, log.ThreadId);
    }

    [Fact]
    public async Task CopyExistingLogsToErrorDatabase_BlankRequiredStrings_UsesFallbackTexts()
    {
        _database.MigrateAppTo(MigrationWithLegacyTable);
        _database.MigrateErrors();
        var legacy = new LegacyRow { Source = "", ExceptionType = "   ", Message = "" };
        InsertLegacyLog(legacy);

        _service.CopyExistingLogsToErrorDatabase();

        var logs = await _errorRepository.GetLatestAsync();
        var log = Assert.Single(logs);
        Assert.Equal("Unknown", log.Source);
        Assert.Equal("Unknown", log.ExceptionType);
        Assert.Equal("Unknown error", log.Message);
    }

    [Fact]
    public async Task CopyExistingLogsToErrorDatabase_InvalidGuid_GeneratesNewId()
    {
        _database.MigrateAppTo(MigrationWithLegacyTable);
        _database.MigrateErrors();
        var legacy = new LegacyRow { IdText = "keine-guid" };
        InsertLegacyLog(legacy);

        _service.CopyExistingLogsToErrorDatabase();

        var logs = await _errorRepository.GetLatestAsync();
        var log = Assert.Single(logs);
        Assert.NotEqual(Guid.Empty, log.Id);
    }

    [Fact]
    public async Task CopyExistingLogsToErrorDatabase_TimestampWithOffset_ConvertsToUtc()
    {
        _database.MigrateAppTo(MigrationWithLegacyTable);
        _database.MigrateErrors();
        var legacy = new LegacyRow { OccurredAtText = "2026-03-10T10:00:00+02:00" };
        InsertLegacyLog(legacy);

        _service.CopyExistingLogsToErrorDatabase();

        var logs = await _errorRepository.GetLatestAsync();
        var log = Assert.Single(logs);
        var expected = new DateTime(2026, 3, 10, 8, 0, 0);
        Assert.Equal(expected, log.OccurredAtUtc);
    }

    [Fact]
    public async Task CopyExistingLogsToErrorDatabase_TimestampWithoutZone_IsTreatedAsUtc()
    {
        _database.MigrateAppTo(MigrationWithLegacyTable);
        _database.MigrateErrors();
        var legacy = new LegacyRow { OccurredAtText = "2026-03-10 08:15:30" };
        InsertLegacyLog(legacy);

        _service.CopyExistingLogsToErrorDatabase();

        var logs = await _errorRepository.GetLatestAsync();
        var log = Assert.Single(logs);
        var expected = new DateTime(2026, 3, 10, 8, 15, 30);
        Assert.Equal(expected, log.OccurredAtUtc);
    }

    [Fact]
    public async Task CopyExistingLogsToErrorDatabase_UnparsableTimestamp_FallsBackToCurrentUtcTime()
    {
        _database.MigrateAppTo(MigrationWithLegacyTable);
        _database.MigrateErrors();
        var legacy = new LegacyRow { OccurredAtText = "kein Datum" };
        InsertLegacyLog(legacy);
        var before = DateTime.UtcNow;

        _service.CopyExistingLogsToErrorDatabase();

        var after = DateTime.UtcNow;
        var logs = await _errorRepository.GetLatestAsync();
        var log = Assert.Single(logs);
        Assert.InRange(log.OccurredAtUtc, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void CopyExistingLogsToErrorDatabase_ErrorDatabaseNotMigrated_ThrowsAndLeavesLegacyLogsInPlace()
    {
        _database.MigrateAppTo(MigrationWithLegacyTable);
        InsertLegacyLog(new LegacyRow());

        Assert.Throws<SqliteException>(() => _service.CopyExistingLogsToErrorDatabase());

        var remaining = CountLegacyLogs();
        Assert.Equal(1, remaining);
    }

    private void InsertLegacyLog(LegacyRow row)
    {
        using var connection = new SqliteConnection($"Data Source={_database.AppDatabasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO "AppErrorLogs"
                ("Id", "OccurredAtUtc", "Source", "ExceptionType", "Message", "StackTrace", "InnerException",
                 "Route", "Platform", "OsVersion", "AppVersion", "IsFatal", "IsUiFailure", "ThreadId", "PayloadJson")
            VALUES
                ($Id, $OccurredAtUtc, $Source, $ExceptionType, $Message, $StackTrace, $InnerException,
                 $Route, $Platform, $OsVersion, $AppVersion, $IsFatal, $IsUiFailure, $ThreadId, $PayloadJson);
            """;
        AddParameter(command, "$Id", row.IdText ?? row.Id.ToString());
        AddParameter(command, "$OccurredAtUtc", row.OccurredAtText);
        AddParameter(command, "$Source", row.Source);
        AddParameter(command, "$ExceptionType", row.ExceptionType);
        AddParameter(command, "$Message", row.Message);
        AddParameter(command, "$StackTrace", row.StackTrace);
        AddParameter(command, "$InnerException", row.InnerException);
        AddParameter(command, "$Route", row.Route);
        AddParameter(command, "$Platform", row.Platform);
        AddParameter(command, "$OsVersion", row.OsVersion);
        AddParameter(command, "$AppVersion", row.AppVersion);
        AddParameter(command, "$IsFatal", row.IsFatal);
        AddParameter(command, "$IsUiFailure", row.IsUiFailure);
        AddParameter(command, "$ThreadId", row.ThreadId);
        AddParameter(command, "$PayloadJson", row.PayloadJson);
        command.ExecuteNonQuery();
    }

    private long CountLegacyLogs()
    {
        using var connection = new SqliteConnection($"Data Source={_database.AppDatabasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM \"AppErrorLogs\";";
        var result = command.ExecuteScalar();

        return Convert.ToInt64(result);
    }

    private static void AddParameter(SqliteCommand command, string name, object? value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    private sealed class LegacyRow
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string? IdText { get; init; }
        public string OccurredAtText { get; init; } = "2026-03-10 08:00:00";
        public string Source { get; init; } = "Quelle";
        public string ExceptionType { get; init; } = "System.Exception";
        public string Message { get; init; } = "Fehlertext";
        public string? StackTrace { get; init; } = "Stack";
        public string? InnerException { get; init; } = "Inner";
        public string? Route { get; init; } = "/route";
        public string? Platform { get; init; } = "Android";
        public string? OsVersion { get; init; } = "14";
        public string? AppVersion { get; init; } = "1.2.3";
        public int IsFatal { get; init; } = 1;
        public int IsUiFailure { get; init; } = 1;
        public int ThreadId { get; init; } = 9;
        public string? PayloadJson { get; init; } = "{\"a\":1}";
    }
}
