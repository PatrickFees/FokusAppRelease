using System.Globalization;

namespace FokusApp.Components.SharedUI.TaskDayButton;

public sealed class TaskDayButtonModel
{
    public DateTime SelectedDate { get; set; }
    public string? CustomText { get; set; }
    public string Text => CustomText ?? SelectedDate.ToString("D", CultureInfo.CurrentCulture);
    public bool IsEnabled { get; set; } = true;
}
