using System.ComponentModel;
using FokusApp.Core.Enums;

namespace FokusApp.Components.WeeklyReviewComponent;

public sealed record WeeklyReviewDay(
    DateTime Date,
    int DoneForYou,
    int DoneToDo,
    double ForYouHeightPercent,
    double ToDoHeightPercent);

public sealed record WeeklyReviewPeak(int Count, IReadOnlyList<DateTime> Days);

public interface IWeeklyReviewViewModel : INotifyPropertyChanged
{

    bool IsReviewEnabled { get; }

    bool IsLoaded { get; }

    DateTime WeekStart { get; }

    DateTime WeekEnd { get; }

    bool IsCurrentWeek { get; }

    bool CanShowNextWeek { get; }

    bool IsEmpty { get; }

    int GoalForYou { get; }

    int GoalToDo { get; }

    int CreatedForYou { get; }

    int CreatedToDo { get; }

    GoalProgress? CreatedProgress { get; }

    int DoneForYou { get; }

    int DoneToDo { get; }

    GoalProgress? DoneProgress { get; }

    IReadOnlyList<WeeklyReviewDay> Days { get; }

    WeeklyReviewPeak? MostForYouDays { get; }

    WeeklyReviewPeak? MostToDoDays { get; }

    Task InitializeAsync();

    Task ShowPreviousWeekAsync();

    Task ShowNextWeekAsync();

    Task ShowCurrentWeekAsync();
}
