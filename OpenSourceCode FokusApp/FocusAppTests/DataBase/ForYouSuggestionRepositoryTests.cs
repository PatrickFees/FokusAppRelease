using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Implementations;

namespace FocusAppTests.DataBase;

public class ForYouSuggestionRepositoryTests : IDisposable
{
    private static readonly DateTime Morning = new(2026, 3, 15, 9, 0, 0);

    private readonly SqliteTestDatabase _database = new();
    private readonly ForYouSuggestionRepository _repository;

    public ForYouSuggestionRepositoryTests()
    {
        _database.MigrateApp();
        _repository = new ForYouSuggestionRepository(_database.AppFactory);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsNothing()
    {
        var suggestions = await _repository.GetAllAsync();

        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task AddAsync_Suggestion_IsStored()
    {
        var suggestion = Create("Spazieren gehen", Morning);

        await _repository.AddAsync(suggestion);

        var stored = Assert.Single(await _repository.GetAllAsync());
        Assert.Equal(suggestion.Id, stored.Id);
        Assert.Equal("Spazieren gehen", stored.Text);
        Assert.Equal(Morning, stored.CreatedAt);
    }

    [Fact]
    public async Task GetAllAsync_SeveralSuggestions_AreOrderedOldestFirst()
    {
        await _repository.AddAsync(Create("Zweite", Morning.AddHours(1)));
        await _repository.AddAsync(Create("Erste", Morning));
        await _repository.AddAsync(Create("Dritte", Morning.AddHours(2)));

        var suggestions = await _repository.GetAllAsync();

        Assert.Equal(["Erste", "Zweite", "Dritte"], suggestions.Select(item => item.Text));
    }

    [Fact]
    public async Task UpdateAsync_ExistingSuggestion_ChangesOnlyTheText()
    {
        var suggestion = Create("Alt", Morning);
        await _repository.AddAsync(suggestion);

        await _repository.UpdateAsync(new ForYouSuggestionEntity { Id = suggestion.Id, Text = "Neu" });

        var stored = Assert.Single(await _repository.GetAllAsync());
        Assert.Equal("Neu", stored.Text);
        Assert.Equal(Morning, stored.CreatedAt);
    }

    [Fact]
    public async Task UpdateAsync_UnknownSuggestion_DoesNothing()
    {
        await _repository.AddAsync(Create("Eins", Morning));

        await _repository.UpdateAsync(new ForYouSuggestionEntity { Id = Guid.NewGuid(), Text = "Zwei" });

        var stored = Assert.Single(await _repository.GetAllAsync());
        Assert.Equal("Eins", stored.Text);
    }

    [Fact]
    public async Task DeleteAsync_ExistingSuggestion_RemovesOnlyThatOne()
    {
        var first = Create("Eins", Morning);
        var second = Create("Zwei", Morning.AddHours(1));
        await _repository.AddAsync(first);
        await _repository.AddAsync(second);

        await _repository.DeleteAsync(first.Id);

        var stored = Assert.Single(await _repository.GetAllAsync());
        Assert.Equal(second.Id, stored.Id);
    }

    [Fact]
    public async Task DeleteAsync_UnknownSuggestion_DoesNothing()
    {
        await _repository.AddAsync(Create("Eins", Morning));

        await _repository.DeleteAsync(Guid.NewGuid());

        Assert.Single(await _repository.GetAllAsync());
    }

    private static ForYouSuggestionEntity Create(string text, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        Text = text,
        CreatedAt = createdAt
    };
}
