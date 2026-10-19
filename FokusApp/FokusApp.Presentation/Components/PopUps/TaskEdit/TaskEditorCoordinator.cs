using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;

namespace FokusApp.Components.PopUps.TaskEdit;

/// <summary>
/// Shared task-editor popup state and regular-template handling used by
/// both TaskOverViewViewModel and CalenderViewModel.
/// </summary>
public sealed class TaskEditorCoordinator
{
    private readonly IScopeService _scopeService;
    private readonly ITaskCacheService _taskCacheService;
    private readonly IUiTextProvider _textProvider;
    private readonly ITaskPriorityFormatter _priorityFormatter;

    public TaskEditorCoordinator(
        IScopeService scopeService,
        ITaskCacheService taskCacheService,
        IUiTextProvider textProvider,
        ITaskPriorityFormatter priorityFormatter)
    {
        _scopeService = scopeService;
        _taskCacheService = taskCacheService;
        _textProvider = textProvider;
        _priorityFormatter = priorityFormatter;
    }

    public TaskEditPopUpInputModel? Input { get; private set; }
    public bool IsOpen => Input is not null;

    /// <summary>When set, editors opened by this coordinator show the task date but do not let the user change it.</summary>
    public bool LockTaskDate { get; set; }

    public void Open(
        TaskDto? task,
        DateTime contextualTaskDate,
        IReadOnlyList<TaskDto> regularTaskSource,
        bool? initialIsPositive = null)
    {
        Input = new TaskEditPopUpInputModel
        {
            Task = task,
            ContextualTaskDate = contextualTaskDate.Date,
            IsWorkScope = _scopeService.IsWorkScope,
            IsTaskDateLocked = LockTaskDate,
            InitialIsPositive = initialIsPositive,
            RegularTemplateOptions = GetRegularTemplateOptions(regularTaskSource)
        };
    }

    public void Close()
    {
        Input = null;
    }

    public bool TryOpenRegularTemplateCopy(
        Guid? templateTaskId,
        DateTime contextualTaskDate,
        IReadOnlyList<TaskDto> regularTaskSource)
    {
        if (!TryGetRegularTemplate(templateTaskId, regularTaskSource, out var templateTask))
        {
            return false;
        }

        var taskCopy = TaskExtension.Clone(templateTask);
        taskCopy.Id = null;
        taskCopy.IsDone = false;
        taskCopy.IsRegularTask = false;
        taskCopy.CreationDate = default;
        taskCopy.TaskDate = contextualTaskDate.Date;

        Open(taskCopy, contextualTaskDate, regularTaskSource);
        return true;
    }

    public async Task<TaskEditorCloseResult> HandleCloseAsync(
        TaskEditPopUpClosePayload payload,
        DateTime contextualTaskDate,
        IReadOnlyList<TaskDto> regularTaskSource)
    {
        if (!IsOpen)
        {
            return new TaskEditorCloseResult(TaskEditorCloseOutcome.Ignored);
        }

        switch (payload.Action)
        {
            case TaskEditPopUpCloseAction.CopyRegularTemplate:
                var copyOpened = TryOpenRegularTemplateCopy(
                    payload.RegularTemplateTaskId,
                    contextualTaskDate,
                    regularTaskSource);
                return new TaskEditorCloseResult(copyOpened
                    ? TaskEditorCloseOutcome.TemplateCopyOpened
                    : TaskEditorCloseOutcome.Ignored);
            case TaskEditPopUpCloseAction.EditRegularTemplate:
                return new TaskEditorCloseResult(payload.RegularTemplateTaskId.HasValue
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
                await _taskCacheService.UpdateTaskAsync(payload.Task).ConfigureAwait(false);
            }
        }
        else
        {
            await _taskCacheService.AddTaskAsync(payload.Task).ConfigureAwait(false);
        }

        Close();
        return true;
    }

    private IReadOnlyList<TaskEditRegularTemplateOptionModel> GetRegularTemplateOptions(
        IReadOnlyList<TaskDto> regularTaskSource)
    {
        var options = new TaskFilterOptions
        {
            IsRegularTemplate = true,
            IsSortDescending = true
        };

        return TaskFilterService.Apply(options, regularTaskSource)
            .Where(task => task.Id.HasValue)
            .Select(task => new TaskEditRegularTemplateOptionModel
            {
                TaskId = task.Id!.Value,
                Text = BuildRegularTemplateOptionText(task)
            })
            .ToList();
    }

    private static bool TryGetRegularTemplate(
        Guid? templateTaskId,
        IReadOnlyList<TaskDto> regularTaskSource,
        out TaskDto templateTask)
    {
        templateTask = null!;
        if (!templateTaskId.HasValue)
        {
            return false;
        }

        var matchedTask = regularTaskSource.FirstOrDefault(task =>
            task.Id == templateTaskId.Value &&
            !task.IsDone &&
            TaskFilterService.IsRegularTemplate(task));

        if (matchedTask is null)
        {
            return false;
        }

        templateTask = matchedTask;
        return true;
    }

    private string BuildRegularTemplateOptionText(TaskDto task)
    {
        var note = string.IsNullOrWhiteSpace(task.Note)
            ? _textProvider.GetText("Untitled")
            : task.Note.Trim();

        return _textProvider.GetText("RegularTemplateOption", note, _priorityFormatter.Format(task.Priority));
    }
}
