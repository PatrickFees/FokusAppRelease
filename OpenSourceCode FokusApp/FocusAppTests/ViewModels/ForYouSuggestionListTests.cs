using FokusApp.Components.ForYouSuggestions;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Services;

namespace FocusAppTests.ViewModels;

public class ForYouSuggestionListTests
{
    [Fact]
    public void Build_NoOwnSuggestions_ReturnsThePoolInItsOrder()
    {
        var items = ForYouSuggestionList.Build([], [], new KeyTextProvider());

        Assert.Equal(ForYouSuggestionPool.Entries.Count, items.Count);
        Assert.Equal(ForYouSuggestionPool.Entries.Select(entry => entry.TextKey), items.Select(item => item.Text));
        Assert.All(items, item => Assert.False(item.IsOwn));
        Assert.All(items, item => Assert.NotNull(item.Category));
    }

    [Fact]
    public void Build_OwnSuggestions_AreListedFirstAndMarkedAsOwn()
    {
        var own = new[] { Own("Lesen"), Own("Malen") };

        var items = ForYouSuggestionList.Build(own, [], new KeyTextProvider());

        Assert.Equal("Lesen", items[0].Text);
        Assert.Equal("Malen", items[1].Text);
        Assert.True(items[0].IsOwn);
        Assert.True(items[1].IsOwn);
        Assert.Null(items[0].Category);
        Assert.Equal(2 + ForYouSuggestionPool.Entries.Count, items.Count);
    }

    [Fact]
    public void Build_OwnSuggestionEqualToPoolText_HidesThePoolEntry()
    {
        var firstPoolKey = ForYouSuggestionPool.Entries[0].TextKey;
        var own = new[] { Own(firstPoolKey.ToUpperInvariant()) };

        var items = ForYouSuggestionList.Build(own, [], new KeyTextProvider());

        Assert.Single(items, item => string.Equals(item.Text, firstPoolKey, StringComparison.OrdinalIgnoreCase));
        Assert.True(items[0].IsOwn);
        Assert.Equal(ForYouSuggestionPool.Entries.Count, items.Count);
    }

    [Fact]
    public void Build_ForYouTemplates_AreListedFirstWithTheirId()
    {
        var template = Template("Spazieren", isPositive: true);

        var items = ForYouSuggestionList.Build([Own("Lesen")], [template], new KeyTextProvider());

        Assert.Equal("Spazieren", items[0].Text);
        Assert.Equal(template.Id, items[0].TemplateId);
        Assert.False(items[0].IsOwn);
        Assert.Null(items[0].Category);
        Assert.Equal("Lesen", items[1].Text);
        Assert.Null(items[1].TemplateId);
    }

    [Fact]
    public void Build_ToDoDoneOrNamelessTemplates_AreIgnored()
    {
        var toDo = Template("Steuer", isPositive: false);
        var done = Template("Erledigt", isPositive: true);
        done.IsDone = true;
        var nameless = Template("  ", isPositive: true);

        var items = ForYouSuggestionList.Build([], [toDo, done, nameless], new KeyTextProvider());

        Assert.Equal(ForYouSuggestionPool.Entries.Count, items.Count);
        Assert.All(items, item => Assert.Null(item.TemplateId));
    }

    [Fact]
    public void Build_TemplateWithTheTextOfAnOwnSuggestionOrPoolEntry_ShowsOnlyTheTemplate()
    {
        var poolKey = ForYouSuggestionPool.Entries[0].TextKey;
        var template = Template(poolKey.ToUpperInvariant(), isPositive: true);

        var items = ForYouSuggestionList.Build([Own(poolKey.ToLowerInvariant())], [template], new KeyTextProvider());

        Assert.Single(items, item => string.Equals(item.Text, poolKey, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(template.Id, items[0].TemplateId);
        Assert.Equal(ForYouSuggestionPool.Entries.Count, items.Count);
    }

    [Fact]
    public void Build_EditedBuiltIn_ShowsTheNewTextInsteadOfTheLocalizedOne()
    {
        var entry = ForYouSuggestionPool.Entries[0];
        var edited = new ForYouSuggestionDto { Id = Guid.NewGuid(), Text = "Langer Spaziergang", PoolKey = entry.TextKey };

        var items = ForYouSuggestionList.Build([edited], [], new KeyTextProvider());

        var item = Assert.Single(items, item => item.Text == "Langer Spaziergang");
        Assert.False(item.IsOwn);
        Assert.Equal(entry.Category, item.Category);
        Assert.DoesNotContain(items, item => item.Text == entry.TextKey);
        Assert.Equal(ForYouSuggestionPool.Entries.Count, items.Count);
    }

    [Fact]
    public void Build_DeletedBuiltIn_IsLeftOut()
    {
        var entry = ForYouSuggestionPool.Entries[0];
        var deleted = new ForYouSuggestionDto { Id = Guid.NewGuid(), Text = string.Empty, PoolKey = entry.TextKey, IsDeleted = true };

        var items = ForYouSuggestionList.Build([deleted], [], new KeyTextProvider());

        Assert.DoesNotContain(items, item => item.Text == entry.TextKey);
        Assert.Equal(ForYouSuggestionPool.Entries.Count - 1, items.Count);
        Assert.All(items, item => Assert.False(item.IsOwn));
    }

    [Fact]
    public void Pool_Always_HasFiveSuggestionsPerCategoryWithUniqueKeys()
    {
        var categories = Enum.GetValues<ForYouSuggestionCategory>();

        Assert.Equal(categories.Length * ForYouSuggestionPool.SuggestionsPerCategory, ForYouSuggestionPool.Entries.Count);
        Assert.Equal(ForYouSuggestionPool.Entries.Count, ForYouSuggestionPool.Entries.Select(entry => entry.TextKey).Distinct().Count());
        foreach (var category in categories)
        {
            Assert.Equal(
                ForYouSuggestionPool.SuggestionsPerCategory,
                ForYouSuggestionPool.Entries.Count(entry => entry.Category == category));
        }
    }

    [Fact]
    public void Build_TemplateWithoutNote_IsNotOfferedAsHabit()
    {
        var template = new TaskDto { Id = Guid.NewGuid(), Note = null, IsPositive = true, IsTemplate = true };

        var items = ForYouSuggestionList.Build([], [template], new KeyTextProvider());

        Assert.DoesNotContain(items, item => item.TemplateId is not null);
        Assert.Equal(ForYouSuggestionPool.Entries.Count, items.Count);
    }

    private static TaskDto Template(string note, bool isPositive) => new()
    {
        Id = Guid.NewGuid(),
        Note = note,
        IsPositive = isPositive,
        IsTemplate = true
    };

    private static ForYouSuggestionDto Own(string text) => new() { Id = Guid.NewGuid(), Text = text };

    private sealed class KeyTextProvider : IUiTextProvider
    {
        public string GetText(string key, params object[] arguments) => key;
    }
}
