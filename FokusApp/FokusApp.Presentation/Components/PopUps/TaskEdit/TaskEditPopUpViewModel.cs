using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Components.SharedUI.TaskDayButton;
using FokusApp.Core.Extensions;
using FokusApp.Core;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Enums;

namespace FokusApp.Components.PopUps.TaskEdit; 
public sealed partial class TaskEditPopUpViewModel : ObservableObject, ITaskEditPopUpViewModel
{
    private static readonly TaskPriority[] PriorityOrder =
    [
        TaskPriority.Keine,
        TaskPriority.Wenig,
        TaskPriority.Mittel,
        TaskPriority.Hoch,
        TaskPriority.Dringend
    ];

    [ObservableProperty]
    private bool showCalendarPopup;

    private readonly TimeProvider _timeProvider;
    private DateTime _contextualTaskDate;
    private DateTime? _hiddenTaskDate;
    private bool _isWorkScope = true;
    private bool _isTaskDateLocked;

    private const int StepMinutes = 5;
    public const int MinimumTimerMinutes = AppConstants.MinimumMinutes;
    public const int MaximumTimerMinutes = AppConstants.MaximumMinutes;

    public TaskEditPopUpViewModel(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _contextualTaskDate = GetToday();
        Model = TaskExtension.CreateDefaultTask(_contextualTaskDate);
    }

    public int TimerStepMinutes => StepMinutes;

    public bool IsUpdateMode { get; private set; }
    public bool IsRegularTaskLocked { get; private set; }
    public TaskDto? TaskBeingEdited { get; private set; }
    public TaskDto Model { get; private set; }
    public bool HasSelectedTaskPositivity { get; private set; }
    public string FocusTimerDisplay => GetTimerDisplay(TimerType.FocusTimer);
    public string BreakTimerDisplay => GetTimerDisplay(TimerType.BreakTimer);
    public DateTime CalendarDate => Model.TaskDate ?? _hiddenTaskDate ?? _contextualTaskDate;
    public bool IsTaskDateAllowed => !Model.IsRegularTask;
    public bool HasRegularTemplateOptions => RegularTemplateOptions.Count > 0;
    public IReadOnlyList<TaskEditRegularTemplateOptionModel> RegularTemplateOptions { get; private set; } = [];
    public int SelectedPriorityIndex => Math.Max(0, Array.IndexOf(PriorityOrder, Model.Priority));
    public int MaximumPriorityIndex => PriorityOrder.Length - 1;

    public bool CanSave =>
        Model.TimerMinutes > 0 &&
        HasSelectedTaskPositivity &&
        !string.IsNullOrWhiteSpace(Model.Note);

    public TaskDayButtonModel TaskDayButtonModel => new()
    {
        SelectedDate = CalendarDate,
        IsEnabled = IsTaskDateAllowed && !_isTaskDateLocked
    };

    public void Open(TaskEditPopUpInputModel? inputModel)
    {
        inputModel ??= new TaskEditPopUpInputModel
        {
            ContextualTaskDate = GetToday()
        };

        _contextualTaskDate = inputModel.ContextualTaskDate.Date;
        _isWorkScope = inputModel.IsWorkScope;
        _isTaskDateLocked = inputModel.IsTaskDateLocked;
        IsUpdateMode = inputModel.Task?.Id is not null;
        TaskBeingEdited = IsUpdateMode && inputModel.Task is not null
            ? TaskExtension.Clone(inputModel.Task)
            : null;

        Model = inputModel.Task is null
            ? TaskExtension.CreateDefaultTask(_contextualTaskDate, _isWorkScope)
            : CreateEditableCopy(inputModel.Task);

        if (inputModel.Task is null && inputModel.InitialIsPositive.HasValue)
        {
            Model.IsPositive = inputModel.InitialIsPositive.Value;
        }

        HasSelectedTaskPositivity = inputModel.Task is not null || inputModel.InitialIsPositive.HasValue;
        IsRegularTaskLocked = TaskBeingEdited is not null &&
            TaskFilterService.IsRegularTemplate(TaskBeingEdited);
        RegularTemplateOptions = inputModel.RegularTemplateOptions ?? [];
        _hiddenTaskDate = Model.TaskDate?.Date ?? _contextualTaskDate;
        ShowCalendarPopup = false;
    }

    public void SetPriorityIndex(int index)
    {
        var normalizedIndex = Math.Clamp(index, 0, MaximumPriorityIndex);
        Model.Priority = PriorityOrder[normalizedIndex];
        OnPropertyChanged(nameof(SelectedPriorityIndex));
    }

    public void SelectTaskPositivity(bool isPositive)
    {
        Model.IsPositive = isPositive;
        HasSelectedTaskPositivity = true;
        NotifyProperties(nameof(CanSave));
    }

    public void SetDate(DateTime? selectedDate)
    {
        if (Model.IsRegularTask || _isTaskDateLocked)
        {
            return;
        }

        Model.TaskDate = selectedDate?.Date;
        _hiddenTaskDate = Model.TaskDate;
        CloseCalendarPopup();
        NotifyProperties(nameof(CalendarDate), nameof(TaskDayButtonModel));
    }

    public void SetRegularity(bool isRegular)
    {
        if (IsRegularTaskLocked && !isRegular)
        {
            return;
        }

        if (isRegular)
        {
            _hiddenTaskDate = Model.TaskDate?.Date ?? _hiddenTaskDate ?? _contextualTaskDate;
        }
        else
        {
            Model.TaskDate = Model.TaskDate?.Date ?? _hiddenTaskDate?.Date ?? _contextualTaskDate;
        }

        Model.IsRegularTask = isRegular;
        NotifyProperties(
            nameof(IsTaskDateAllowed),
            nameof(CalendarDate),
            nameof(TaskDayButtonModel));
    }

    public int GetTimerMinutes(TimerType type) => type switch
    {
        TimerType.FocusTimer => Model.TimerMinutes,
        TimerType.BreakTimer => Model.BreakMinutes,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public string GetTimerDisplay(TimerType type)
        => FormatTimerDisplay(GetTimerMinutes(type));

    public void AdjustTimer(TimerType type, int deltaMinutes)
    {
        var targetMinutes = Math.Max(MinimumTimerMinutes, GetTimerMinutes(type) + deltaMinutes);
        SetTimerMinutes(type, targetMinutes);
    }

    public void SetTimerMinutes(TimerType type, int minutes)
    {
        if (minutes < MinimumTimerMinutes)
        {
            return;
        }

        var normalizedMinutes = NormalizeDuration(minutes);

        switch (type)
        {
            case TimerType.FocusTimer:
                Model.TimerMinutes = normalizedMinutes;
                NotifyProperties(nameof(FocusTimerDisplay), nameof(CanSave));
                break;
            case TimerType.BreakTimer:
                Model.BreakMinutes = normalizedMinutes;
                NotifyProperties(nameof(BreakTimerDisplay));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public TaskEditPopUpClosePayload CreateClosePayload(
        TaskEditPopUpCloseAction action,
        Guid? regularTemplateTaskId)
    {
        var resultTask = CreateResultTask();
        var hasChanges = !IsUpdateMode ||
            TaskBeingEdited is null ||
            HasEditableChanges(TaskBeingEdited, resultTask);

        return new TaskEditPopUpClosePayload
        {
            Action = action,
            Task = resultTask,
            IsUpdateMode = IsUpdateMode,
            HasChanges = hasChanges,
            RegularTemplateTaskId = regularTemplateTaskId
        };
    }

    public void RequestOpeningCalendarPopUp()
    {
        if (IsTaskDateAllowed)
        {
            ShowCalendarPopup = true;
        }
    }

    public void CloseCalendarPopup()
    {
        ShowCalendarPopup = false;
    }

    public void Close()
    {
        ShowCalendarPopup = false;
    }

    private DateTime GetToday() => _timeProvider.GetLocalNow().Date;

    private TaskDto CreateResultTask()
    {
        var resultTask = CreateEditableCopy(Model);
        resultTask.Note = resultTask.Note?.Trim() ?? string.Empty;

        if (IsUpdateMode && TaskBeingEdited is not null)
        {
            resultTask.Id = TaskBeingEdited.Id;
            resultTask.IsDone = TaskBeingEdited.IsDone;
            resultTask.IsWorkTask = TaskBeingEdited.IsWorkTask;
            resultTask.CreationDate = TaskBeingEdited.CreationDate;
        }
        else
        {
            resultTask.Id = Guid.NewGuid();
            resultTask.IsDone = false;
            resultTask.IsWorkTask = _isWorkScope;
            resultTask.CreationDate = _timeProvider.GetLocalNow().DateTime;
        }

        if (!resultTask.IsRegularTask && !resultTask.TaskDate.HasValue)
        {
            resultTask.TaskDate = _hiddenTaskDate?.Date ?? _contextualTaskDate;
        }

        return TaskExtension.NormalizeTaskForPersistence(resultTask);
    }

    private bool HasEditableChanges(TaskDto originalTask, TaskDto resultTask)
    {
        var original = NormalizeForComparison(originalTask);
        var result = NormalizeForComparison(resultTask);

        return !string.Equals(original.Note, result.Note, StringComparison.Ordinal) ||
            original.Priority != result.Priority ||
            original.TimerMinutes != result.TimerMinutes ||
            original.BreakMinutes != result.BreakMinutes ||
            original.IsPositive != result.IsPositive ||
            original.IsRegularTask != result.IsRegularTask ||
            original.TaskDate != result.TaskDate;
    }

    private static TaskDto CreateEditableCopy(TaskDto task)
    {
        var copy = TaskExtension.Clone(task);
        copy.Note ??= string.Empty;
        copy.TimerMinutes = NormalizeDuration(copy.TimerMinutes);
        copy.BreakMinutes = NormalizeDuration(copy.BreakMinutes);
        return copy;
    }

    private static TaskDto NormalizeForComparison(TaskDto task)
    {
        var normalizedTask = TaskExtension.NormalizeTaskForPersistence(task);
        normalizedTask.Note = normalizedTask.Note?.Trim() ?? string.Empty;
        normalizedTask.TimerMinutes = NormalizeDuration(normalizedTask.TimerMinutes);
        normalizedTask.BreakMinutes = NormalizeDuration(normalizedTask.BreakMinutes);
        return normalizedTask;
    }

    private static int NormalizeDuration(int minutes)
        => Math.Clamp(minutes, 0, MaximumTimerMinutes);

    private static string FormatTimerDisplay(int minutes)
    {
        var time = TimeSpan.FromMinutes(Math.Max(0, minutes));
        return time.ToString(@"hh\:mm");
    }

    private void NotifyProperties(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
