using FokusApp.Components.ForYouSuggestions;
using FokusApp.Core.Models;

namespace FocusAppTests.ViewModels;

public class ForYouHistoryTests
{
    private static readonly DateTime Today = new(2026, 3, 15);

    [Fact]
    public void Build_NoTasks_ReturnsNothing()
    {
        var texts = ForYouHistory.Build([], Today);

        Assert.Empty(texts);
    }

    [Fact]
    public void Build_DoneAndPastOpenForYouTasks_AreListed()
    {
        var done = Task("Yoga", isDone: true, date: Today.AddDays(-1));
        var pastOpen = Task("Lesen", date: Today.AddDays(-2));

        var texts = ForYouHistory.Build([done, pastOpen], Today);

        Assert.Equal(["Yoga", "Lesen"], texts);
    }

    [Fact]
    public void Build_ToDoTasks_AreNotListed()
    {
        var toDo = Task("Steuer", isPositive: false, isDone: true, date: Today.AddDays(-1));

        var texts = ForYouHistory.Build([toDo], Today);

        Assert.Empty(texts);
    }

    [Fact]
    public void Build_TemplatesAndCurrentOpenTasks_AreNotListed()
    {
        var template = Task("Vorlage", isTemplate: true, date: null);
        var today = Task("Heute", date: Today);
        var future = Task("Morgen", date: Today.AddDays(1));

        var texts = ForYouHistory.Build([template, today, future], Today);

        Assert.Empty(texts);
    }

    [Fact]
    public void Build_SameTextInOtherCase_IsListedOnce()
    {
        var older = Task("yoga", isDone: true, date: Today.AddDays(-5));
        var newer = Task("YOGA", isDone: true, date: Today.AddDays(-1));

        var texts = ForYouHistory.Build([older, newer], Today);

        Assert.Equal(["YOGA"], texts);
    }

    [Fact]
    public void Build_SameTextWithOtherWhitespace_IsListedOnce()
    {
        var first = Task("Ein  Spaziergang", isDone: true, date: Today.AddDays(-3));
        var second = Task(" einSpaziergang ", isDone: true, date: Today.AddDays(-2));
        var third = Task("EIN\tSPAZIER GANG", isDone: true, date: Today.AddDays(-1));

        var texts = ForYouHistory.Build([first, second, third], Today);

        Assert.Equal(["EIN\tSPAZIER GANG"], texts);
    }

    [Fact]
    public void Build_Texts_AreTrimmedAndBlankOnesSkipped()
    {
        var padded = Task("  Malen  ", isDone: true, date: Today.AddDays(-1));
        var blank = Task("   ", isDone: true, date: Today.AddDays(-1));
        var missing = Task(null, isDone: true, date: Today.AddDays(-1));

        var texts = ForYouHistory.Build([padded, blank, missing], Today);

        Assert.Equal(["Malen"], texts);
    }

    [Fact]
    public void Build_Tasks_AreListedNewestFirst()
    {
        var oldest = Task("Alt", isDone: true, date: Today.AddDays(-9));
        var newest = Task("Neu", isDone: true, date: Today.AddDays(-1));
        var middle = Task("Mitte", isDone: true, date: Today.AddDays(-4));

        var texts = ForYouHistory.Build([oldest, newest, middle], Today);

        Assert.Equal(["Neu", "Mitte", "Alt"], texts);
    }

    [Fact]
    public void Build_DoneTask_IsSortedByItsCompletionTime()
    {
        var plannedLongAgo = Task("Spät erledigt", isDone: true, date: Today.AddDays(-20));
        plannedLongAgo.CompletedAt = Today.AddDays(-1);
        var plannedRecently = Task("Früh erledigt", isDone: true, date: Today.AddDays(-5));
        plannedRecently.CompletedAt = Today.AddDays(-6);

        var texts = ForYouHistory.Build([plannedRecently, plannedLongAgo], Today);

        Assert.Equal(["Spät erledigt", "Früh erledigt"], texts);
    }

    [Theory]
    [InlineData("Ab C", "abc")]
    [InlineData("  a\tb\nc ", "abc")]
    [InlineData("ÄÖ", "äö")]
    public void NormalizeKey_Text_RemovesWhitespaceAndLowersCase(string text, string expected)
    {
        var key = ForYouHistory.NormalizeKey(text);

        Assert.Equal(expected, key);
    }

    private static TaskDto Task(
        string? note,
        bool isPositive = true,
        bool isDone = false,
        bool isTemplate = false,
        DateTime? date = null)
    {
        return new TaskDto
        {
            Id = Guid.NewGuid(),
            Note = note!,
            IsPositive = isPositive,
            IsDone = isDone,
            IsTemplate = isTemplate,
            TaskDate = date,
            CreationDate = (date ?? Today).AddDays(-1)
        };
    }
}
