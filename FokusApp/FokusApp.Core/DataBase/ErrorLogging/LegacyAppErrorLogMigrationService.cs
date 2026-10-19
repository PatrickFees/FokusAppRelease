using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Globalization;

namespace FokusApp.Core.DataBase.ErrorLogging;

public interface ILegacyAppErrorLogMigrationService
{
    void CopyExistingLogsToErrorDatabase();
}

public sealed class LegacyAppErrorLogMigrationService : ILegacyAppErrorLogMigrationService
{
    private readonly IDbContextFactory<FokusAppDbContext> _productiveDbContextFactory;
    private readonly IDbContextFactory<AppErrorDbContext> _errorDbContextFactory;

    public LegacyAppErrorLogMigrationService(
        IDbContextFactory<FokusAppDbContext> productiveDbContextFactory,
        IDbContextFactory<AppErrorDbContext> errorDbContextFactory)
    {
        _productiveDbContextFactory = productiveDbContextFactory;
        _errorDbContextFactory = errorDbContextFactory;
    }

    public void CopyExistingLogsToErrorDatabase()
    {
        using var productiveDbContext = _productiveDbContextFactory.CreateDbContext();
        var productiveConnection = productiveDbContext.Database.GetDbConnection();
        var shouldCloseProductiveConnection = productiveConnection.State != ConnectionState.Open;

        try
        {
            if (shouldCloseProductiveConnection)
            {
                productiveConnection.Open();
            }

            if (!TableExists(productiveConnection, "AppErrorLogs"))
            {
                return;
            }

            var legacyLogs = ReadLegacyLogs(productiveConnection);
            if (legacyLogs.Count == 0)
            {
                return;
            }

            CopyLogsToErrorDatabase(legacyLogs);
        }
        finally
        {
            if (shouldCloseProductiveConnection)
            {
                productiveConnection.Close();
            }
        }
    }

    private static bool TableExists(DbConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(1)
            FROM sqlite_master
            WHERE type = 'table' AND name = $TableName;
            """;
        AddParameter(command, "$TableName", tableName);

        var result = command.ExecuteScalar();
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    private static List<LegacyAppErrorLog> ReadLegacyLogs(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT "Id",
                   "OccurredAtUtc",
                   "Source",
                   "ExceptionType",
                   "Message",
                   "StackTrace",
                   "InnerException",
                   "Route",
                   "Platform",
                   "OsVersion",
                   "AppVersion",
                   "IsFatal",
                   "IsUiFailure",
                   "ThreadId",
                   "PayloadJson"
            FROM "AppErrorLogs";
            """;

        using var reader = command.ExecuteReader();
        var logs = new List<LegacyAppErrorLog>();

        while (reader.Read())
        {
            logs.Add(new LegacyAppErrorLog(
                ReadGuid(reader, 0),
                ReadDateTime(reader, 1),
                ReadRequiredString(reader, 2, "Unknown"),
                ReadRequiredString(reader, 3, "Unknown"),
                ReadRequiredString(reader, 4, "Unknown error"),
                ReadOptionalString(reader, 5),
                ReadOptionalString(reader, 6),
                ReadOptionalString(reader, 7),
                ReadOptionalString(reader, 8),
                ReadOptionalString(reader, 9),
                ReadOptionalString(reader, 10),
                ReadBoolean(reader, 11),
                ReadBoolean(reader, 12),
                ReadInt32(reader, 13),
                ReadOptionalString(reader, 14)));
        }

        return logs;
    }

    private void CopyLogsToErrorDatabase(IReadOnlyList<LegacyAppErrorLog> logs)
    {
        using var errorDbContext = _errorDbContextFactory.CreateDbContext();
        var errorConnection = errorDbContext.Database.GetDbConnection();
        var shouldCloseErrorConnection = errorConnection.State != ConnectionState.Open;

        try
        {
            if (shouldCloseErrorConnection)
            {
                errorConnection.Open();
            }

            using var transaction = errorConnection.BeginTransaction();
            foreach (var log in logs)
            {
                InsertLog(errorConnection, transaction, log);
            }

            transaction.Commit();
        }
        finally
        {
            if (shouldCloseErrorConnection)
            {
                errorConnection.Close();
            }
        }
    }

    private static void InsertLog(
        DbConnection connection,
        DbTransaction transaction,
        LegacyAppErrorLog log)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT OR IGNORE INTO "AppErrorLogs"
                ("Id",
                 "OccurredAtUtc",
                 "Source",
                 "ExceptionType",
                 "Message",
                 "StackTrace",
                 "InnerException",
                 "Route",
                 "Platform",
                 "OsVersion",
                 "AppVersion",
                 "IsFatal",
                 "IsUiFailure",
                 "ThreadId",
                 "PayloadJson")
            VALUES
                ($Id,
                 $OccurredAtUtc,
                 $Source,
                 $ExceptionType,
                 $Message,
                 $StackTrace,
                 $InnerException,
                 $Route,
                 $Platform,
                 $OsVersion,
                 $AppVersion,
                 $IsFatal,
                 $IsUiFailure,
                 $ThreadId,
                 $PayloadJson);
            """;

        AddParameter(command, "$Id", log.Id);
        AddParameter(command, "$OccurredAtUtc", log.OccurredAtUtc);
        AddParameter(command, "$Source", log.Source);
        AddParameter(command, "$ExceptionType", log.ExceptionType);
        AddParameter(command, "$Message", log.Message);
        AddParameter(command, "$StackTrace", log.StackTrace);
        AddParameter(command, "$InnerException", log.InnerException);
        AddParameter(command, "$Route", log.Route);
        AddParameter(command, "$Platform", log.Platform);
        AddParameter(command, "$OsVersion", log.OsVersion);
        AddParameter(command, "$AppVersion", log.AppVersion);
        AddParameter(command, "$IsFatal", log.IsFatal);
        AddParameter(command, "$IsUiFailure", log.IsUiFailure);
        AddParameter(command, "$ThreadId", log.ThreadId);
        AddParameter(command, "$PayloadJson", log.PayloadJson);
        command.ExecuteNonQuery();
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static Guid ReadGuid(DbDataReader reader, int ordinal)
    {
        var rawValue = reader.GetValue(ordinal);
        return Guid.TryParse(Convert.ToString(rawValue, CultureInfo.InvariantCulture), out var parsed)
            ? parsed
            : Guid.NewGuid();
    }

    private static DateTime ReadDateTime(DbDataReader reader, int ordinal)
    {
        var rawValue = reader.GetValue(ordinal);
        if (rawValue is DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                : dateTime.ToUniversalTime();
        }

        var rawText = Convert.ToString(rawValue, CultureInfo.InvariantCulture);
        if (DateTime.TryParse(
            rawText,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsedDateTime))
        {
            return parsedDateTime;
        }

        return DateTime.UtcNow;
    }

    private static string ReadRequiredString(DbDataReader reader, int ordinal, string fallback)
    {
        var rawText = ReadOptionalString(reader, ordinal);
        return string.IsNullOrWhiteSpace(rawText) ? fallback : rawText;
    }

    private static string? ReadOptionalString(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static bool ReadBoolean(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return false;
        }

        var rawValue = reader.GetValue(ordinal);
        return rawValue switch
        {
            bool value => value,
            long value => value != 0,
            int value => value != 0,
            _ => Convert.ToBoolean(rawValue, CultureInfo.InvariantCulture)
        };
    }

    private static int ReadInt32(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private sealed record LegacyAppErrorLog(
        Guid Id,
        DateTime OccurredAtUtc,
        string Source,
        string ExceptionType,
        string Message,
        string? StackTrace,
        string? InnerException,
        string? Route,
        string? Platform,
        string? OsVersion,
        string? AppVersion,
        bool IsFatal,
        bool IsUiFailure,
        int ThreadId,
        string? PayloadJson);
}
