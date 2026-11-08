using FokusApp.Components.ForYouSuggestions;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;

namespace FokusApp.Components.PopUps.TaskEdit;

public sealed class TaskEditorCoordinator
{

    public event EventHandler? StateChanged;

    private readonly IScopeService _scopeService;
    private readonly ITaskCacheService _taskCacheService;
    private readonly IUiTextProvider _textProvider;
    private readonly ITaskPriorityFormatter _priorityFormatter;
    private readonly ITaskSeriesService _taskSeriesService;
    private readonly TimeProvider _timeProvider;
    private TaskSeriesScope _seriesScope = TaskSeriesScope.WholeSeries;

    public TaskEditorCoordinator(
        IScopeService scopeService,
        ITaskCacheService taskCacheService,
        IUiTextProvider textProvider,
        ITaskPriorityFormatter priorityFormatter,
        ITaskSeriesService taskSeriesService,
        TimeProvider? timeProvider = null)
    {
        _scopeService = scopeService;
        _taskCacheService = taskCacheService;
        _textProvider = textProvider;
        _priorityFormatter = priorityFormatter;
        _taskSeriesService = taskSeriesService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public TaskEditPopUpInputModel? Input { get; private set; }
    public bool IsOpen => Input is not null;

    public bool LockTaskDate { get; set; }

    public bool CreateTodayTaskForNewTemplate { get; set; }

    public void Open(
        TaskDto? task,
        DateTime contextualTaskDate,
        IReadOnlyList<TaskDto> templateSource,
        bool? initialIsPositive = null,
        TaskSeriesScope seriesScope = TaskSeriesScope.WholeSeries)
    {
        _seriesScope = seriesScope;
        Input = new TaskEditPopUpInputModel
        {
            Task = task,
            ContextualTaskDate = contextualTaskDate.Date,
            IsWorkScope = _scopeService.IsWorkScope,
            IsTaskDateLocked = LockTaskDate,
            InitialIsPositive = initialIsPositive,
            TemplateOptions = GetTemplateOptions(templateSource)
        };
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void OpenForYouSuggestion(
        ForYouSuggestionItem suggestion,
        DateTime day,
        IReadOnlyList<TaskDto> templateSource)
    {
        if (suggestion.TemplateId is not null && TryOpenTemplateCopy(suggestion.TemplateId, day, templateSource))
        {
            return;
        }

        var task = TaskExtension.CreateDefaultTask(day, _scopeService.IsWorkScope);
        task.Note = suggestion.Text;
        task.IsPositive = true;
        Open(task, day, templateSource);
    }

    public void Close()
    {
        Input = null;
        _seriesScope = TaskSeriesScope.WholeSeries;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool TryOpenTemplateCopy(
        Guid? templateTaskId,
        DateTime contextualTaskDate,
        IReadOnlyList<TaskDto> templateSource)
    {
        if (!TryGetTemplate(templateTaskId, templateSource, out var templateTask))
        {
            return false;
        }

        var taskCopy = TaskExtension.Clone(templateTask);
        taskCopy.Id = null;
        taskCopy.IsDone = false;
        taskCopy.IsTemplate = false;
        taskCopy.CreationDate = default;
        taskCopy.TaskDate = contextualTaskDate.Date;

        Open(taskCopy, contextualTaskDate, templateSource);
        return true;
    }

    public async Task<TaskEditorCloseResult> HandleCloseAsync(
        TaskEditPopUpClosePayload payload,
        DateTime contextualTaskDate,
        IReadOnlyList<TaskDto> templateSource)
    {
        if (!IsOpen)
        {
            return new TaskEditorCloseResult(TaskEditorCloseOutcome.Ignored);
        }

        switch (payload.Action)
        {
            case TaskEditPopUpCloseAction.CopyTemplate:
                var copyOpened = TryOpenTemplateCopy(
                    payload.TemplateTaskId,
                    contextualTaskDate,
                    templateSource);
                return new TaskEditorCloseResult(copyOpened
                    ? TaskEditorCloseOutcome.TemplateCopyOpened
                    : TaskEditorCloseOutcome.Ignored);
            case TaskEditPopUpCloseAction.EditTemplate:
                return new TaskEditorCloseResult(payload.TemplateTaskId.HasValue
                    ? TaskEditorCloseOutcome.TemplateEditRequested
                    : TaskEditorCloseOutcome.Ignored);
            case TaskEditPopUpCloseAction.Cancel:
                Close();
                return new TaskEditorCloseResult(TaskEditorCloseOutcome.Closed);
            case TaskEditPopUpCloseAction.Save:
                return await TrySaveAsync(payload).ConfigureAwait(false);
            default:
                return new TaskEditorCloseResult(TaskEditorCloseOutcome.Ignored);
        }
    }

    private async Task<TaskEditorCloseResult> TrySaveAsync(TaskEditPopUpClosePayload payload)
    {
        try
        {
            var saved = await SaveAsync(payload).ConfigureAwait(false);
            return new TaskEditorCloseResult(saved
                ? TaskEditorCloseOutcome.Closed
                : TaskEditorCloseOutcome.NotSaved);
        }
        catch (Exception ex)
        {
            return new TaskEditorCloseResult(TaskEditorCloseOutcome.SaveFailed, ex);
        }
    }

    public async Task<bool> SaveAsync(TaskEditPopUpClosePayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Task.Note))
        {
            return false;
        }

        if (payload.IsUpdateMode)
        {
            if (payload.HasChanges)
            {
                var previousTask = payload.Task.Id.HasValue
                    ? _taskCacheService.FindTaskById(payload.Task.Id.Value)
                    : null;

                if (payload.Task.IsTemplate && previousTask is { IsTemplate: false })
                {
                    await SaveTaskAndAddTemplateAsync(payload.Task, previousTask).ConfigureAwait(false);
                }
                else
                {
                    await _taskCacheService.UpdateTaskAsync(payload.Task).ConfigureAwait(false);
                    await _taskSeriesService.HandleSavedTaskAsync(payload.Task, previousTask, _seriesScope).ConfigureAwait(false);
                }
            }
        }
        else
        {
            await _taskCacheService.AddTaskAsync(payload.Task).ConfigureAwait(false);
            await _taskSeriesService.HandleSavedTaskAsync(payload.Task, null).ConfigureAwait(false);

            if (CreateTodayTaskForNewTemplate && payload.Task.IsTemplate)
            {
                var todayTask = TaskExtension.Clone(payload.Task);
                todayTask.Id = null;
                todayTask.IsDone = false;
                todayTask.IsTemplate = false;
                todayTask.CreationDate = default;
                todayTask.TaskDate = _timeProvider.GetLocalNow().Date;

                await _taskCacheService.AddTaskAsync(todayTask).ConfigureAwait(false);
            }
        }

        Close();
        return true;
    }

    private async Task SaveTaskAndAddTemplateAsync(TaskDto editedTask, TaskDto previousTask)
    {
        var task = TaskExtension.Clone(editedTask);
        task.IsTemplate = false;
        task.TaskDate = previousTask.TaskDate;
        task.RepeatUnit = previousTask.RepeatUnit;
        task.RepeatInterval = previousTask.RepeatInterval;
        task.RepeatWeekdays = previousTask.RepeatWeekdays;
        task.SeriesId = previousTask.SeriesId;
        task.RepeatGeneratedUntil = previousTask.RepeatGeneratedUntil;

        var template = TaskExtension.Clone(editedTask);
        template.Id = null;
        template.IsDone = false;
        template.CompletedAt = null;
        template.IsTemplate = true;
        template.CreationDate = _timeProvider.GetLocalNow().DateTime;
        template.TaskDate = null;

        await _taskCacheService.UpdateTaskAsync(task).ConfigureAwait(false);
        await _taskSeriesService.HandleSavedTaskAsync(task, previousTask, _seriesScope).ConfigureAwait(false);
        await _taskCacheService.AddTaskAsync(template).ConfigureAwait(false);
    }

    private IReadOnlyList<TaskEditTemplateOptionModel> GetTemplateOptions(
        IReadOnlyList<TaskDto> templateSource)
    {
        var options = new TaskFilterOptions
        {
            IsOpenTemplate = true,
            IsSortDescending = true
        };

        return TaskFilterService.Apply(options, templateSource)
            .Where(task => task.Id.HasValue)
            .Select(task => new TaskEditTemplateOptionModel
            {
                TaskId = task.Id!.Value,
                Text = BuildTemplateOptionText(task),
                IsPositive = task.IsPositive
            })
            .ToList();
    }

    private static bool TryGetTemplate(
        Guid? templateTaskId,
        IReadOnlyList<TaskDto> templateSource,
        out TaskDto templateTask)
    {
        templateTask = null!;
        if (!templateTaskId.HasValue)
        {
            return false;
        }

        var matchedTask = templateSource.FirstOrDefault(task =>
            task.Id == templateTaskId.Value &&
            !task.IsDone &&
            TaskFilterService.IsOpenTemplate(task));

        if (matchedTask is null)
        {
            return false;
        }

        templateTask = matchedTask;
        return true;
    }

    private string BuildTemplateOptionText(TaskDto task)
    {
        var note = string.IsNullOrWhiteSpace(task.Note)
            ? _textProvider.GetText("Untitled")
            : task.Note.Trim();

        return _textProvider.GetText("TemplateOption", note, _priorityFormatter.Format(task.Priority));
    }
}
