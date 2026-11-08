using FokusApp.Components.PopUps.TaskSeriesChoice;
using FokusApp.Components.SharedUI;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Components.YourDay;

public sealed class SeriesSelectionFlow
{
    private readonly TaskDaySourceCache _taskSource;
    private readonly IActiveTaskService _activeTaskService;
    private readonly Action _closeTaskActionPopup;
    private readonly Action<Guid, TaskSeriesScope> _startEditTask;
    private readonly Func<Guid, TaskSeriesScope, Task> _deleteSeriesTaskAsync;

    private Guid? _pendingTaskId;
    private TaskSeriesAction _pendingAction;

    public SeriesSelectionFlow(
        TaskDaySourceCache taskSource,
        IActiveTaskService activeTaskService,
        Action closeTaskActionPopup,
        Action<Guid, TaskSeriesScope> startEditTask,
        Func<Guid, TaskSeriesScope, Task> deleteSeriesTaskAsync)
    {
        _taskSource = taskSource;
        _activeTaskService = activeTaskService;
        _closeTaskActionPopup = closeTaskActionPopup;
        _startEditTask = startEditTask;
        _deleteSeriesTaskAsync = deleteSeriesTaskAsync;
    }

    public event EventHandler? ChoiceChanged;

    public bool IsOpen => _pendingTaskId.HasValue;

    public TaskSeriesAction PendingAction => _pendingAction;

    public bool BelongsToSeries(Guid taskId)
    {
        var task = _taskSource.FindById(taskId);
        return task is not null && !IsActiveTask(task) && TaskExtension.GetSeriesId(task) is not null;
    }

    public void AskChoice(Guid taskId, TaskSeriesAction action)
    {
        _pendingTaskId = taskId;
        _pendingAction = action;
        _closeTaskActionPopup();

        ChoiceChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Cancel()
    {
        Clear();
        ChoiceChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ChooseScopeAsync(TaskSeriesScope scope)
    {
        if (_pendingTaskId is not Guid taskId)
        {
            return;
        }

        var action = _pendingAction;
        Clear();
        ChoiceChanged?.Invoke(this, EventArgs.Empty);

        if (action == TaskSeriesAction.Edit)
        {
            _startEditTask(taskId, scope);
            return;
        }

        await _deleteSeriesTaskAsync(taskId, scope);
    }

    public void ClearIfTaskGone()
    {
        if (_pendingTaskId.HasValue && _taskSource.FindById(_pendingTaskId.Value) is null)
        {
            Clear();
        }
    }

    private void Clear() => _pendingTaskId = null;

    private bool IsActiveTask(TaskDto task) => task.Id is Guid taskId && _activeTaskService.IsTaskActive(taskId);
}
