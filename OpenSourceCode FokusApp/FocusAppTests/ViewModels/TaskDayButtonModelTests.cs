using System.Globalization;
using FokusApp.Components.SharedUI.TaskDayButton;

namespace FocusAppTests.ViewModels;

public class TaskDayButtonModelTests
{
    [Fact]
    public void Constructor_NoValues_IsEnabledWithoutCustomText()
    {
        var model = new TaskDayButtonModel();

        Assert.True(model.IsEnabled);
        Assert.Null(model.CustomText);
    }

    [Fact]
    public void Text_NoCustomText_ShowsTheLongDateInTheCurrentCulture()
    {
        var germanCulture = new CultureInfo("de-DE");
        var model = new TaskDayButtonModel { SelectedDate = TestTasks.Day };
        var expected = TestTasks.Day.ToString("D", germanCulture);

        using var scope = new CultureScope(germanCulture);

        Assert.Equal(expected, model.Text);
        Assert.Contains("2026", model.Text);
    }

    [Fact]
    public void Text_CustomText_WinsOverTheDate()
    {
        var model = new TaskDayButtonModel { SelectedDate = TestTasks.Day, CustomText = "Heute" };

        Assert.Equal("Heute", model.Text);
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        public CultureScope(CultureInfo culture)
        {
            CultureInfo.CurrentCulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _previous;
        }
    }
}
