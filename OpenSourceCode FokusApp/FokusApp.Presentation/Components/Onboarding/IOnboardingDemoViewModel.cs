using FokusApp.Components.SharedUI.TaskDayButton;
using FokusApp.Core.Models;

namespace FokusApp.Components.Onboarding;

public interface IOnboardingDemoViewModel
{
    TaskDayButtonModel DayButton { get; }
    IReadOnlyList<TaskDto> WorkForYou { get; }
    IReadOnlyList<TaskDto> WorkToDo { get; }
    IReadOnlyList<TaskDto> PrivateForYou { get; }
    IReadOnlyList<TaskDto> PrivateToDo { get; }

    void ResetData(int breakMinutes);

    void AddWorkTask(bool isForYou, string note, int breakMinutes);

    Guid? MarkFirstWorkToDoDone();
}
