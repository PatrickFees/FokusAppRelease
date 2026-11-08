using FokusApp.Components.SharedUI.TaskDayButton;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Services;

namespace FokusApp.Components.Onboarding;

public sealed class OnboardingDemoViewModel : IOnboardingDemoViewModel
{
    private const int FocusMinutes = 30;

    private readonly IUiTextProvider _textProvider;
    private readonly TimeProvider _timeProvider;
    private readonly List<TaskDto> _workForYou = [];
    private readonly List<TaskDto> _workToDo = [];
    private readonly List<TaskDto> _privateForYou = [];
    private readonly List<TaskDto> _privateToDo = [];

    public OnboardingDemoViewModel(IUiTextProvider textProvider, TimeProvider timeProvider)
    {
        _textProvider = textProvider;
        _timeProvider = timeProvider;
        DayButton = new TaskDayButtonModel { SelectedDate = Today };
    }

    public TaskDayButtonModel DayButton { get; }

    private DateTime Today => _timeProvider.GetLocalNow().Date;

    public IReadOnlyList<TaskDto> WorkForYou => _workForYou;

    public IReadOnlyList<TaskDto> WorkToDo => _workToDo;

    public IReadOnlyList<TaskDto> PrivateForYou => _privateForYou;

    public IReadOnlyList<TaskDto> PrivateToDo => _privateToDo;

    public void ResetData(int breakMinutes)
    {
        _workForYou.Clear();
        _workToDo.Clear();
        _privateForYou.Clear();
        _privateToDo.Clear();
        _privateForYou.Add(NewTask(_textProvider.GetText("OnboardingDemoDateNight"), isForYou: true, breakMinutes));
        _privateForYou.Add(NewTask(_textProvider.GetText("OnboardingDemoMeditate"), isForYou: true, breakMinutes));
        _privateToDo.Add(NewTask(_textProvider.GetText("OnboardingDemoCleanFlat"), isForYou: false, breakMinutes));
        foreach (var task in _privateForYou.Concat(_privateToDo))
        {
            task.IsWorkTask = false;
        }
    }

    public void AddWorkTask(bool isForYou, string note, int breakMinutes)
    {
        (isForYou ? _workForYou : _workToDo).Add(NewTask(note, isForYou, breakMinutes));
    }

    public Guid? MarkFirstWorkToDoDone()
    {
        if (_workToDo.Count == 0)
        {
            return null;
        }

        _workToDo[0].IsDone = true;
        return _workToDo[0].Id;
    }

    private TaskDto NewTask(string note, bool isForYou, int breakMinutes)
    {
        var today = Today;
        return new TaskDto
        {
            Id = Guid.NewGuid(),
            Note = note,
            IsPositive = isForYou,
            IsWorkTask = true,
            Priority = TaskPriority.Keine,
            TaskDate = today,
            CreationDate = today,
            TimerMinutes = FocusMinutes,
            BreakMinutes = breakMinutes
        };
    }
}
