using System.ComponentModel;
using FokusApp.Components.ForYouSuggestions;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.TaskOverViewComponent;
using FokusApp.Core.Enums;

namespace FokusApp.Components.DayPlanner;

public static class DayPlannerRoutes
{
    public const string Planner = "/tagesplaner";
}

public enum DayPlannerStep
{

    Overview,

    Assign,

    ForYou,

    Duties,

    Done
}

public sealed record DayPlannerTask(Guid Id, string Note, bool IsPositive);

public sealed record DayPlannerInsight(string Text);

public interface IDayPlannerViewModel : INotifyPropertyChanged
{
    bool IsLoaded { get; }

    IReadOnlyList<DayPlannerStep> Steps { get; }

    DayPlannerStep Step { get; }

    bool CanGoBack { get; }

    IReadOnlyList<DayPlannerTask> TodayTasks { get; }

    int PlannedForYou { get; }

    int PlannedToDo { get; }

    int PlannedTotal { get; }

    int DoneForYou { get; }

    int DoneToDo { get; }

    string OverviewText { get; }

    int GoalForYou { get; }

    int GoalToDo { get; }

    GoalProgress GoalProgress { get; }

    bool IsGoalReached { get; }

    int RequiredForYou { get; }

    int AllowedToDo { get; }

    double ForYouDonePercent { get; }

    double ForYouExistingPercent { get; }

    double ForYouPlannedStartPercent { get; }

    double ForYouPlannedPercent { get; }

    double ToDoDonePercent { get; }

    double ToDoExistingPercent { get; }

    double ToDoPlannedStartPercent { get; }

    double ToDoPlannedPercent { get; }

    double ForYouGoalPercent { get; }

    double ToDoGoalPercent { get; }

    IReadOnlyList<DayPlannerTask> PastTasks { get; }

    DayPlannerInsight? Insight { get; }

    IReadOnlyList<ForYouSuggestionItem> Suggestions { get; }

    IReadOnlyList<string> PastForYous { get; }

    string ForYouText { get; }

    IReadOnlyList<DayPlannerTask> DutyTemplates { get; }

    string DoneText { get; }

    TaskEditPopUpInputModel? TaskEditorInput { get; }
    bool IsTaskEditorOpen { get; }

    bool IsReschedulePopupOpen { get; }

    DateTime RescheduleInitialDate { get; }

    UndoNotice? CurrentUndo { get; }

    Task InitializeAsync();

    void Next();

    void Back();

    void Finish();

    Task AssignPastTaskAsync(Guid taskId, PastTaskTarget target);

    Task AssignAllPastTasksToTomorrowAsync();

    void OpenReschedulePopup(Guid taskId);

    void CloseReschedulePopup();

    Task ConfirmRescheduleAsync(DateTime newDate);

    Task DiscardPastTaskAsync(Guid taskId);

    Task UndoAsync();

    void DismissUndo();

    void PickSuggestion(ForYouSuggestionItem suggestion);

    void OpenTaskEditorForForYou();

    void OpenTaskEditorForToDo();

    void PickDutyTemplate(Guid templateId);

    Task HandleTaskEditPopupCloseAsync(TaskEditPopUpClosePayload payload);
}
