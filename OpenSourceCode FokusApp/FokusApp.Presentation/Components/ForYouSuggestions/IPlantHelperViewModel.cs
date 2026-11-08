using System.ComponentModel;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Core.Services;

namespace FokusApp.Components.ForYouSuggestions;

public enum PlantBubbleKind
{
    None,

    PlanDay,

    ForYou,

    GoalReached,

    GoalExceeded,

    AskForYou
}

public interface IPlantHelperViewModel : INotifyPropertyChanged
{

    bool IsPlantVisible { get; }

    bool IsBubbleVisible { get; }

    PlantBubbleKind BubbleKind { get; }

    CompanionMood Mood { get; }

    bool IsSuggestionListOpen { get; }

    IReadOnlyList<ForYouSuggestionItem> Suggestions { get; }

    TaskEditPopUpInputModel? TaskEditorInput { get; }
    bool IsTaskEditorOpen { get; }

    Task InitializeAsync();

    void TapPlant();

    void OpenSuggestions();

    void CloseSuggestions();

    void DismissBubble();

    void PickSuggestion(ForYouSuggestionItem suggestion);

    Task HandleTaskEditPopupCloseAsync(TaskEditPopUpClosePayload payload);
}
