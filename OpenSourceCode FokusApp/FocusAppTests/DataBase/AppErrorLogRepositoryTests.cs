using FokusApp.Core.DataBase.Repositories.Implementations;

namespace FocusAppTests.DataBase;

public class AppErrorLogRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly AppErrorLogRepository _repository;

    public AppErrorLogRepositoryTests()
    {
        _database.MigrateErrors();
        _repository = new AppErrorLogRepository(_database.ErrorFactory);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task GetLatestAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _repository.GetLatestAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_FullyPopulatedLog_PersistsAllFields()
    {
        var log = TestEntities.CreateErrorLog();

        await _repository.AddAsync(log);

        var stored = await _repository.GetLatestAsync();
        var item = Assert.Single(stored);
        Assert.Equal(log.Id, item.Id);
        Assert.Equal(log.OccurredAtUtc, item.OccurredAtUtc);
        Assert.Equal("Quelle", item.Source);
        Assert.Equal("System.InvalidOperationException", item.ExceptionType);
        Assert.Equal("Fehlertext", item.Message);
        Assert.Equal("Stack", item.StackTrace);
        Assert.Equal("Inner", item.InnerException);
        Assert.Equal("/route", item.Route);
        Assert.Equal("Windows", item.Platform);
        Assert.Equal("11", item.OsVersion);
        Assert.Equal("1.0", item.AppVersion);
        Assert.True(item.IsFatal);
        Assert.True(item.IsUiFailure);
        Assert.Equal(4, item.ThreadId);
        Assert.Equal("{}", item.PayloadJson);
    }

    [Fact]
    public async Task AddAsync_OptionalFieldsNull_PersistsNulls()
    {
        var log = TestEntities.CreateErrorLog();
        log.StackTrace = null;
        log.InnerException = null;
        log.Route = null;
        log.PayloadJson = null;

        await _repository.AddAsync(log);

        var stored = await _repository.GetLatestAsync();
        var item = Assert.Single(stored);
        Assert.Null(item.StackTrace);
        Assert.Null(item.InnerException);
        Assert.Null(item.Route);
        Assert.Null(item.PayloadJson);
    }

    [Fact]
    public async Task GetLatestAsync_LogsWithDifferentTimes_ReturnsNewestFirst()
    {
        var oldest = TestEntities.CreateErrorLog(occurredOffsetMinutes: 0);
        var newest = TestEntities.CreateErrorLog(occurredOffsetMinutes: 30);
        var middle = TestEntities.CreateErrorLog(occurredOffsetMinutes: 10);
        await _repository.AddAsync(oldest);
        await _repository.AddAsync(newest);
        await _repository.AddAsync(middle);

        var result = await _repository.GetLatestAsync();

        var ids = result.Select(entry => entry.Id).ToList();
        Assert.Equal([newest.Id, middle.Id, oldest.Id], ids);
    }

    [Fact]
    public async Task GetLatestAsync_SameTime_OrdersByIdDescendingForStableResult()
    {
        var first = TestEntities.CreateErrorLog();
        var second = TestEntities.CreateErrorLog();
        await _repository.AddAsync(first);
        await _repository.AddAsync(second);

        var result = await _repository.GetLatestAsync();

        var ids = result.Select(entry => entry.Id).ToList();
        var expected = new[] { first.Id, second.Id }.OrderByDescending(id => id).ToList();
        Assert.Equal(expected, ids);
    }
}
