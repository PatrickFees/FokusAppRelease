using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Services;
using FocusAppTests.ViewModels;
using NSubstitute;

namespace FocusAppTests.Services;

public class ForYouSuggestionServiceTests
{
    private readonly IForYouSuggestionRepository _repository = Substitute.For<IForYouSuggestionRepository>();
    private readonly ForYouSuggestionService _service;
    private readonly List<ForYouSuggestionEntity> _stored = [];
    private int _changedCount;

    public ForYouSuggestionServiceTests()
    {
        _repository.GetAllAsync().Returns(_ => _stored.ToList());
        _service = new ForYouSuggestionService(_repository, TestTasks.CreateClock());
        _service.SuggestionsChanged += (_, _) => _changedCount++;
    }

    [Fact]
    public async Task AddAsync_NewText_StoresTrimmedSuggestionWithTheCurrentTime()
    {
        var added = await _service.AddAsync("  Spazieren gehen  ");

        Assert.True(added);
        await _repository.Received(1).AddAsync(Arg.Is<ForYouSuggestionEntity>(item =>
            item.Text == "Spazieren gehen" &&
            item.Id != Guid.Empty &&
            item.CreatedAt == TestTasks.Today.AddHours(9).AddMinutes(30)));
        Assert.Equal(1, _changedCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddAsync_EmptyText_StoresNothing(string text)
    {
        var added = await _service.AddAsync(text);

        Assert.False(added);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!);
        Assert.Equal(0, _changedCount);
    }

    [Fact]
    public async Task AddAsync_SameTextIgnoringCase_IsRefused()
    {
        _stored.Add(new ForYouSuggestionEntity { Id = Guid.NewGuid(), Text = "Lesen" });

        var added = await _service.AddAsync(" lesen ");

        Assert.False(added);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!);
        Assert.Equal(0, _changedCount);
    }

    [Fact]
    public async Task AddAsync_TooLongText_IsShortenedToTheMaximumLength()
    {
        var added = await _service.AddAsync(new string('a', ForYouSuggestionEntity.MaximumTextLength + 30));

        Assert.True(added);
        await _repository.Received(1).AddAsync(Arg.Is<ForYouSuggestionEntity>(item =>
            item.Text.Length == ForYouSuggestionEntity.MaximumTextLength));
    }

    [Fact]
    public async Task UpdateAsync_ExistingSuggestion_ChangesTheText()
    {
        var id = Guid.NewGuid();
        _stored.Add(new ForYouSuggestionEntity { Id = id, Text = "Alt" });

        var updated = await _service.UpdateAsync(id, " Neu ");

        Assert.True(updated);
        await _repository.Received(1).UpdateAsync(Arg.Is<ForYouSuggestionEntity>(item => item.Id == id && item.Text == "Neu"));
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task UpdateAsync_SameTextAsItself_IsAllowed()
    {
        var id = Guid.NewGuid();
        _stored.Add(new ForYouSuggestionEntity { Id = id, Text = "Lesen" });

        var updated = await _service.UpdateAsync(id, "LESEN");

        Assert.True(updated);
    }

    [Fact]
    public async Task UpdateAsync_TextOfAnotherSuggestion_IsRefused()
    {
        var id = Guid.NewGuid();
        _stored.Add(new ForYouSuggestionEntity { Id = id, Text = "Lesen" });
        _stored.Add(new ForYouSuggestionEntity { Id = Guid.NewGuid(), Text = "Malen" });

        var updated = await _service.UpdateAsync(id, "malen");

        Assert.False(updated);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
    }

    [Fact]
    public async Task UpdateAsync_UnknownOrEmpty_IsRefused()
    {
        var id = Guid.NewGuid();
        _stored.Add(new ForYouSuggestionEntity { Id = id, Text = "Lesen" });

        Assert.False(await _service.UpdateAsync(Guid.NewGuid(), "Neu"));
        Assert.False(await _service.UpdateAsync(id, "  "));
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
        Assert.Equal(0, _changedCount);
    }

    [Fact]
    public async Task DeleteAsync_Suggestion_DeletesAndNotifies()
    {
        var id = Guid.NewGuid();

        await _service.DeleteAsync(id);

        await _repository.Received(1).DeleteAsync(id);
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task GetStoredSuggestionsAsync_Stored_ReturnsThem()
    {
        _stored.Add(new ForYouSuggestionEntity { Id = Guid.NewGuid(), Text = "Lesen" });

        var suggestions = await _service.GetStoredSuggestionsAsync();

        Assert.Equal("Lesen", Assert.Single(suggestions).Text);
    }

    [Fact]
    public async Task UpdateBuiltInAsync_FirstChange_StoresARowWithThePoolKey()
    {
        var key = ForYouSuggestionPool.Entries[0].TextKey;

        var saved = await _service.UpdateBuiltInAsync(key, "  Langer Spaziergang ");

        Assert.True(saved);
        await _repository.Received(1).AddAsync(Arg.Is<ForYouSuggestionEntity>(item =>
            item.PoolKey == key && item.Text == "Langer Spaziergang" && !item.IsDeleted));
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task UpdateBuiltInAsync_AlreadyChanged_UpdatesTheExistingRow()
    {
        var key = ForYouSuggestionPool.Entries[0].TextKey;
        var id = Guid.NewGuid();
        _stored.Add(new ForYouSuggestionEntity { Id = id, Text = "Alt", PoolKey = key });

        var saved = await _service.UpdateBuiltInAsync(key, "Neu");

        Assert.True(saved);
        await _repository.Received(1).UpdateAsync(Arg.Is<ForYouSuggestionEntity>(item => item.Id == id && item.Text == "Neu" && !item.IsDeleted));
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!);
    }

    [Fact]
    public async Task UpdateBuiltInAsync_UnknownKeyEmptyOrDuplicateText_IsRefused()
    {
        var key = ForYouSuggestionPool.Entries[0].TextKey;
        _stored.Add(new ForYouSuggestionEntity { Id = Guid.NewGuid(), Text = "Lesen" });

        Assert.False(await _service.UpdateBuiltInAsync("Unbekannt", "Neu"));
        Assert.False(await _service.UpdateBuiltInAsync(key, "  "));
        Assert.False(await _service.UpdateBuiltInAsync(key, "lesen"));
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
        Assert.Equal(0, _changedCount);
    }

    [Fact]
    public async Task DeleteBuiltInAsync_FirstChange_StoresAHidingRow()
    {
        var key = ForYouSuggestionPool.Entries[1].TextKey;

        await _service.DeleteBuiltInAsync(key);

        await _repository.Received(1).AddAsync(Arg.Is<ForYouSuggestionEntity>(item => item.PoolKey == key && item.IsDeleted));
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public async Task DeleteBuiltInAsync_AlreadyChanged_MarksTheExistingRowAsDeleted()
    {
        var key = ForYouSuggestionPool.Entries[1].TextKey;
        var id = Guid.NewGuid();
        _stored.Add(new ForYouSuggestionEntity { Id = id, Text = "Eigene Fassung", PoolKey = key });

        await _service.DeleteBuiltInAsync(key);

        await _repository.Received(1).UpdateAsync(Arg.Is<ForYouSuggestionEntity>(item => item.Id == id && item.IsDeleted));
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!);
    }

    [Fact]
    public async Task DeleteBuiltInAsync_UnknownKey_DoesNothing()
    {
        await _service.DeleteBuiltInAsync("Unbekannt");

        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!);
        Assert.Equal(0, _changedCount);
    }

    [Fact]
    public async Task AddAsync_TextOfADeletedBuiltInRow_IsAccepted()
    {
        _stored.Add(new ForYouSuggestionEntity { Id = Guid.NewGuid(), Text = "Lesen", PoolKey = ForYouSuggestionPool.Entries[0].TextKey, IsDeleted = true });

        Assert.True(await _service.AddAsync("Lesen"));
    }

    [Fact]
    public async Task AddAsync_TextOverMaximumLength_IsCutToMaximumLength()
    {
        var text = new string('a', ForYouSuggestionEntity.MaximumTextLength + 20);
        var expected = new string('a', ForYouSuggestionEntity.MaximumTextLength);

        var added = await _service.AddAsync(text);

        Assert.True(added);
        await _repository.Received(1).AddAsync(Arg.Is<ForYouSuggestionEntity>(item => item.Text == expected));
    }

    [Fact]
    public async Task AddAsync_NullText_StoresNothing()
    {
        var added = await _service.AddAsync(null!);

        Assert.False(added);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!);
    }

    [Fact]
    public async Task DeleteBuiltInAsync_UnknownKey_StoresNothing()
    {
        await _service.DeleteBuiltInAsync("Unbekannt");

        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
        Assert.Equal(0, _changedCount);
    }

    [Fact]
    public async Task Changes_WithoutSubscribers_CompleteWithoutError()
    {
        var service = new ForYouSuggestionService(_repository, TestTasks.CreateClock());
        var id = Guid.NewGuid();
        var key = ForYouSuggestionPool.Entries[0].TextKey;
        var hiddenKey = ForYouSuggestionPool.Entries[1].TextKey;
        _stored.Add(new ForYouSuggestionEntity { Id = id, Text = "Lesen" });

        var added = await service.AddAsync("Malen");
        var updated = await service.UpdateAsync(id, "Zeichnen");
        var builtInChanged = await service.UpdateBuiltInAsync(key, "Neu");
        await service.DeleteBuiltInAsync(hiddenKey);
        await service.DeleteAsync(id);

        Assert.True(added);
        Assert.True(updated);
        Assert.True(builtInChanged);
    }
}
