using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Components.PopUps.TaskSchedule;
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
    private IReadOnlyList<TaskEditTemplateOptionModel> _allTemplateOptions = [];

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
    public bool IsTemplateLocked { get; private set; }
    public TaskDto? TaskBeingEdited { get; private set; }
    public TaskDto Model { get; private set; }
    public bool HasSelectedTaskPositivity { get; private set; }
    public string FocusTimerDisplay => GetTimerDisplay(TimerType.FocusTimer);
    public string BreakTimerDisplay => GetTimerDisplay(TimerType.BreakTimer);
    public DateTime CalendarDate => Model.TaskDate ?? _hiddenTaskDate ?? _contextualTaskDate;
    public bool IsTaskDateAllowed => !Model.IsTemplate;
    public bool HasTemplateOptions => TemplateOptions.Count > 0;

    public IReadOnlyList<TaskEditTemplateOptionModel> TemplateOptions => HasSelectedTaskPositivity
        ? _allTemplateOptions.Where(option => option.IsPositive == Model.IsPositive).ToList()
        : [];
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

    public bool HasSchedule => Model.TaskTime.HasValue || IsRepeating;
    public bool IsRepeating => Model.RepeatUnit != TaskRepeatUnit.None;
    public bool CanRepeat => !Model.IsTemplate;

    public string? ScheduleTimeDisplay => Model.TaskTime is { } time
        ? $"{time.Hours:00}:{time.Minutes:00}"
        : null;

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
        IsTemplateLocked = TaskBeingEdited is not null &&
            TaskFilterService.IsOpenTemplate(TaskBeingEdited);
        _allTemplateOptions = inputModel.TemplateOptions ?? [];
        _hiddenTaskDate = Model.TaskDate?.Date ?? _contextualTaskDate;
        ShowCalendarPopup = false;
    }

    public TaskScheduleModel CreateScheduleModel()
    {
        return new TaskScheduleModel
        {
            Time = Model.TaskTime,
            ReminderMinutesBefore = Model.ReminderMinutesBefore,
            RepeatUnit = Model.RepeatUnit,
            RepeatInterval = Model.RepeatInterval,
            RepeatWeekdays = Model.RepeatWeekdays,
            RepeatWeekdayOrdinal = Model.RepeatWeekdayOrdinal,
            RepeatMonth = Model.RepeatMonth,
            RepeatWeekStart = Model.RepeatWeekStart,
            RepeatEndDate = Model.RepeatEndDate
        };
    }

    public void ApplySchedule(TaskScheduleModel schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        Model.TaskTime = schedule.Time;
        Model.ReminderMinutesBefore = schedule.Time.HasValue ? schedule.ReminderMinutesBefore : null;
        Model.RepeatUnit = CanRepeat ? schedule.RepeatUnit : TaskRepeatUnit.None;
        Model.RepeatInterval = schedule.RepeatInterval;
        Model.RepeatWeekdays = schedule.RepeatWeekdays;
        Model.RepeatWeekdayOrdinal = schedule.RepeatWeekdayOrdinal;
        Model.RepeatMonth = schedule.RepeatMonth;
        Model.RepeatWeekStart = schedule.RepeatWeekStart;
        Model.RepeatEndDate = schedule.RepeatEndDate;
        NotifyProperties(nameof(HasSchedule), nameof(IsRepeating), nameof(ScheduleTimeDisplay));
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
        NotifyProperties(nameof(CanSave), nameof(TemplateOptions), nameof(HasTemplateOptions));
    }

    public void SetDate(DateTime? selectedDate)
    {
        if (Model.IsTemplate || _isTaskDateLocked)
        {
            return;
        }

        Model.TaskDate = selectedDate?.Date;
        _hiddenTaskDate = Model.TaskDate;
        CloseCalendarPopup();
        NotifyProperties(nameof(CalendarDate), nameof(TaskDayButtonModel));
    }

    public void SetTemplateFlag(bool isTemplate)
    {
        if (IsTemplateLocked && !isTemplate)
        {
            return;
        }

        if (isTemplate)
        {
            _hiddenTaskDate = Model.TaskDate?.Date ?? _hiddenTaskDate ?? _contextualTaskDate;
        }
        else
        {
            Model.TaskDate = Model.TaskDate?.Date ?? _hiddenTaskDate?.Date ?? _contextualTaskDate;
        }

        Model.IsTemplate = isTemplate;
        if (isTemplate)
        {

            Model.RepeatUnit = TaskRepeatUnit.None;
            Model.RepeatInterval = 1;
            Model.RepeatWeekdays = TaskWeekdays.None;
        }

        NotifyProperties(
            nameof(IsTaskDateAllowed),
            nameof(CalendarDate),
            nameof(TaskDayButtonModel),
            nameof(CanRepeat),
            nameof(IsRepeating),
            nameof(HasSchedule));
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
        Guid? templateTaskId)
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
            TemplateTaskId = templateTaskId
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

        if (!resultTask.IsTemplate && !resultTask.TaskDate.HasValue)
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
            original.IsTemplate != result.IsTemplate ||
            original.TaskDate != result.TaskDate ||
            original.TaskTime != result.TaskTime ||
            original.ReminderMinutesBefore != result.ReminderMinutesBefore ||
            original.RepeatUnit != result.RepeatUnit ||
            original.RepeatInterval != result.RepeatInterval ||
            original.RepeatWeekdays != result.RepeatWeekdays;
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
