using FokusApp.Core.Enums;
using FokusApp.Services;

namespace FocusAppTests.ViewModels;

public class TaskPriorityFormatterTests
{
    private readonly TaskPriorityFormatter _formatter;

    public TaskPriorityFormatterTests()
    {
        var textProvider = TestTexts.Create();
        _formatter = new TaskPriorityFormatter(textProvider);
    }

    [Theory]
    [InlineData(TaskPriority.Keine, "PriorityNone")]
    [InlineData(TaskPriority.Wenig, "PriorityLow")]
    [InlineData(TaskPriority.Mittel, "PriorityMedium")]
    [InlineData(TaskPriority.Hoch, "PriorityHigh")]
    [InlineData(TaskPriority.Dringend, "PriorityUrgent")]
    public void Format_Priority_UsesMatchingTextKey(TaskPriority priority, string expectedKey)
    {
        var text = _formatter.Format(priority);

        Assert.Equal(expectedKey, text);
    }

    [Fact]
    public void Format_UndefinedPriority_UsesUnknownText()
    {
        var undefined = (TaskPriority)99;

        var text = _formatter.Format(undefined);

        Assert.Equal("Unknown", text);
    }

    [Theory]
    [InlineData(TaskPriority.Keine, "priority-keine")]
    [InlineData(TaskPriority.Wenig, "priority-wenig")]
    [InlineData(TaskPriority.Mittel, "priority-mittel")]
    [InlineData(TaskPriority.Hoch, "priority-hoch")]
    [InlineData(TaskPriority.Dringend, "priority-dringend")]
    public void GetCssClass_Priority_ReturnsMatchingClass(TaskPriority priority, string expectedClass)
    {
        var cssClass = _formatter.GetCssClass(priority);

        Assert.Equal(expectedClass, cssClass);
    }

    [Fact]
    public void GetCssClass_NoPriority_ReturnsEmptyString()
    {
        var cssClass = _formatter.GetCssClass(null);

        Assert.Equal(string.Empty, cssClass);
    }

    [Fact]
    public void GetCssClass_UndefinedPriority_ReturnsEmptyString()
    {
        var undefined = (TaskPriority)99;

        var cssClass = _formatter.GetCssClass(undefined);

        Assert.Equal(string.Empty, cssClass);
    }
}
