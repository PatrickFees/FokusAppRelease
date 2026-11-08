using FokusApp.Core.DataBase.Repositories.Implementations;

namespace FocusAppTests.DataBase;

public class CalendarAutoSyncCalendarRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly CalendarAutoSyncCalendarRepository _repository;

    public CalendarAutoSyncCalendarRepositoryTests()
    {
        _database.MigrateApp();
        _repository = new CalendarAutoSyncCalendarRepository(_database.AppFactory);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsNothing()
    {
        var calendars = await _repository.GetAllAsync();

        Assert.Empty(calendars);
    }

    [Fact]
    public async Task ReplaceAllAsync_EmptyDatabase_StoresAllGivenIds()
    {
        await _repository.ReplaceAllAsync(["calendar-1", "calendar-2"]);

        var calendars = await _repository.GetAllAsync();

        Assert.Equal(["calendar-1", "calendar-2"], calendars.Select(calendar => calendar.CalendarId).Order());
    }

    [Fact]
    public async Task ReplaceAllAsync_ExistingSelection_ReplacesItCompletely()
    {
        await _repository.ReplaceAllAsync(["calendar-1", "calendar-2"]);

        await _repository.ReplaceAllAsync(["calendar-3"]);

        var calendars = await _repository.GetAllAsync();
        var stored = Assert.Single(calendars);
        Assert.Equal("calendar-3", stored.CalendarId);
    }

    [Fact]
    public async Task ReplaceAllAsync_EmptyList_ClearsExistingSelection()
    {
        await _repository.ReplaceAllAsync(["calendar-1"]);

        await _repository.ReplaceAllAsync([]);

        Assert.Empty(await _repository.GetAllAsync());
    }
}
