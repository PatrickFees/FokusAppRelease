using FokusApp.Core.DataBase;
using FokusApp.Core.DataBase.ErrorLogging;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FocusAppTests.DataBase;

/// <summary>
/// Eigene SQLite-Dateien (App- und Fehlerdatenbank) in einem Temp-Verzeichnis pro Testinstanz,
/// mit je einer eigenen <see cref="IDbContextFactory{TContext}"/>. Die Konfiguration entspricht <c>MauiProgram.cs</c>.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private const string MigrationsAssembly = "FokusApp.Migrations";

    private readonly string _directory;

    public SqliteTestDatabase()
    {
        var uniqueName = Guid.NewGuid().ToString("N");
        _directory = Path.Combine(Path.GetTempPath(), "FokusAppTests", uniqueName);
        Directory.CreateDirectory(_directory);

        AppDatabasePath = Path.Combine(_directory, "FokusApp.db");
        ErrorDatabasePath = Path.Combine(_directory, "FokusApp.Errors.db");
        AppFactory = new TestDbContextFactory<FokusAppDbContext>(CreateAppContext);
        ErrorFactory = new TestDbContextFactory<AppErrorDbContext>(CreateErrorContext);
    }

    public string AppDatabasePath { get; }
    public string ErrorDatabasePath { get; }
    public IDbContextFactory<FokusAppDbContext> AppFactory { get; }
    public IDbContextFactory<AppErrorDbContext> ErrorFactory { get; }

    public SqliteTestDatabase MigrateApp()
    {
        using var context = AppFactory.CreateDbContext();
        context.Database.Migrate();

        return this;
    }

    public SqliteTestDatabase MigrateAppTo(string targetMigration)
    {
        using var context = AppFactory.CreateDbContext();
        var migrator = context.GetService<IMigrator>();
        migrator.Migrate(targetMigration);

        return this;
    }

    public SqliteTestDatabase MigrateErrors()
    {
        using var context = ErrorFactory.CreateDbContext();
        context.Database.Migrate();

        return this;
    }

    public void ExecuteAppSql(string sql) => ExecuteNonQuery(AppDatabasePath, sql);

    public long CountTables(string databasePath, string tableName)
    {
        var sql = $"SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = '{tableName}';";

        return ExecuteScalar(databasePath, sql);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    private FokusAppDbContext CreateAppContext()
    {
        var options = new DbContextOptionsBuilder<FokusAppDbContext>()
            .UseSqlite(
                CreateConnectionString(AppDatabasePath),
                sqlite => sqlite.MigrationsAssembly(MigrationsAssembly))
            .Options;

        return new FokusAppDbContext(options);
    }

    private AppErrorDbContext CreateErrorContext()
    {
        var options = new DbContextOptionsBuilder<AppErrorDbContext>()
            .UseSqlite(
                CreateConnectionString(ErrorDatabasePath),
                sqlite => sqlite.MigrationsAssembly(MigrationsAssembly))
            .Options;

        return new AppErrorDbContext(options);
    }

    private static void ExecuteNonQuery(string databasePath, string sql)
    {
        using var connection = new SqliteConnection(CreateConnectionString(databasePath));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static long ExecuteScalar(string databasePath, string sql)
    {
        using var connection = new SqliteConnection(CreateConnectionString(databasePath));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = command.ExecuteScalar();

        return Convert.ToInt64(result);
    }

    private static string CreateConnectionString(string path) => $"Data Source={path};Pooling=False";

    private sealed class TestDbContextFactory<TContext>(Func<TContext> create) : IDbContextFactory<TContext>
        where TContext : DbContext
    {
        public TContext CreateDbContext() => create();
    }
}
