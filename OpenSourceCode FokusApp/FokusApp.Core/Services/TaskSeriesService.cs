using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class TaskSeriesService : ITaskSeriesService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskCacheService _taskCacheService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public TaskSeriesService(
        ITaskRepository taskRepository,
        ITaskCacheService taskCacheService,
        IDayChangeService dayChangeService,
        IAppErrorCaptureService appErrorCaptureService,
        TimeProvider timeProvider)
    {
        _taskRepository = taskRepository;
        _taskCacheService = taskCacheService;
        _appErrorCaptureService = appErrorCaptureService;
        _timeProvider = timeProvider;
        dayChangeService.DayChanged += OnDayChanged;
    }

    public async Task EnsureTasksAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (await CreateMissingTasksAsync().ConfigureAwait(false))
            {
                await _taskCacheService.ReloadAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task HandleSavedTaskAsync(TaskDto savedTask, TaskDto? previousTask, TaskSeriesScope scope = TaskSeriesScope.WholeSeries)
    {
        ArgumentNullException.ThrowIfNull(savedTask);

        var concernsSeries = TaskExtension.IsSeriesStart(savedTask) ||
            savedTask.SeriesId is not null ||
            previousTask is not null && TaskExtension.IsSeriesStart(previousTask);
        if (!concernsSeries || savedTask.Id is not { } savedId)
        {
            return;
        }

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var changed = false;

            if (TaskExtension.IsSeriesStart(savedTask))
            {

                if (previousTask is not null && RestartsSeries(previousTask, savedTask, scope))
                {
                    changed |= await RestartSeriesAsync(savedTask, savedId, exceptTaskId: null).ConfigureAwait(false);
                }
            }
            else if (previousTask is not null && TaskExtension.IsSeriesStart(previousTask))
            {

                changed |= await RemoveFutureOpenTasksAsync(savedId, exceptTaskId: null).ConfigureAwait(false);
            }
            else if (savedTask.SeriesId is { } seriesId && previousTask is not null)
            {
                if (scope == TaskSeriesScope.WholeSeries && HasSharedChanges(previousTask, savedTask))
                {
                    changed |= await ApplyEditToSeriesAsync(seriesId, savedTask, savedId, includeFields: true).ConfigureAwait(false);
                }
                else if (HasRuleChanges(previousTask, savedTask))
                {
                    changed |= await ApplyEditToSeriesAsync(seriesId, savedTask, savedId, includeFields: false).ConfigureAwait(false);
                }
            }

            changed |= await CreateMissingTasksAsync().ConfigureAwait(false);
            if (changed)
            {
                await _taskCacheService.ReloadAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<bool> RestartSeriesAsync(TaskDto seriesStart, Guid seriesStartId, Guid? exceptTaskId)
    {
        var removed = await RemoveFutureOpenTasksAsync(seriesStartId, exceptTaskId).ConfigureAwait(false);

        var restarted = TaskExtension.Clone(seriesStart);
        restarted.RepeatGeneratedUntil = null;
        await _taskRepository.UpdateAsync(TaskExtension.MapToEntity(restarted)).ConfigureAwait(false);
        return removed || seriesStart.RepeatGeneratedUntil is not null;
    }

    private async Task<bool> ApplyEditToSeriesAsync(Guid seriesStartId, TaskDto editedTask, Guid editedTaskId, bool includeFields)
    {
        var all = await _taskRepository.GetAllAsync().ConfigureAwait(false);
        var seriesStartEntity = all.FirstOrDefault(task => task.Id == seriesStartId);
        if (seriesStartEntity is null)
        {
            return false;
        }

        var seriesStart = TaskExtension.MapToDto(seriesStartEntity);
        if (includeFields)
        {
            seriesStart.Note = editedTask.Note;
            seriesStart.Priority = editedTask.Priority;
            seriesStart.TimerMinutes = editedTask.TimerMinutes;
            seriesStart.BreakMinutes = editedTask.BreakMinutes;
            seriesStart.IsPositive = editedTask.IsPositive;
            seriesStart.IsWorkTask = editedTask.IsWorkTask;
            seriesStart.TaskTime = editedTask.TaskTime;
            seriesStart.ReminderMinutesBefore = editedTask.ReminderMinutesBefore;
        }

        seriesStart.RepeatUnit = editedTask.RepeatUnit;
        seriesStart.RepeatInterval = editedTask.RepeatInterval;
        seriesStart.RepeatWeekdays = editedTask.RepeatWeekdays;
        seriesStart.RepeatWeekdayOrdinal = editedTask.RepeatWeekdayOrdinal;
        seriesStart.RepeatMonth = editedTask.RepeatMonth;
        seriesStart.RepeatWeekStart = editedTask.RepeatWeekStart;
        seriesStart.RepeatEndDate = editedTask.RepeatEndDate;

        await RestartSeriesAsync(seriesStart, seriesStartId, editedTaskId).ConfigureAwait(false);
        return true;
    }

    public async Task<Guid?> DeleteTaskAsync(Guid taskId, TaskSeriesScope scope, Guid? keepTaskId = null)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var all = await _taskRepository.GetAllAsync().ConfigureAwait(false);
            var entity = all.FirstOrDefault(task => task.Id == taskId);
            if (entity is null || taskId == keepTaskId)
            {
                return null;
            }

            var task = TaskExtension.MapToDto(entity);
            Guid? newStartId = null;
            if (scope == TaskSeriesScope.WholeSeries && TaskExtension.GetSeriesId(task) is { } seriesId)
            {
                await DeleteSeriesAsync(all, seriesId, keepTaskId).ConfigureAwait(false);
            }
            else
            {
                if (TaskExtension.IsSeriesStart(task))
                {
                    newStartId = await HandOverRuleAsync(all, task).ConfigureAwait(false);
                }

                await _taskRepository.DeleteAsync(taskId).ConfigureAwait(false);
            }

            await _taskCacheService.ReloadAsync().ConfigureAwait(false);
            return newStartId;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task DeleteSeriesAsync(IReadOnlyList<TaskEntity> all, Guid seriesStartId, Guid? keepTaskId)
    {
        var seriesTasks = all
            .Where(task => task.Id == seriesStartId || task.SeriesId == seriesStartId)
            .ToList();
        var idsToRemove = seriesTasks
            .Where(task => !task.IsDone && task.Id != keepTaskId)
            .Select(task => task.Id)
            .ToList();
        await _taskRepository.DeleteRangeAsync(idsToRemove).ConfigureAwait(false);

        foreach (var remaining in seriesTasks.Where(task => !idsToRemove.Contains(task.Id)))
        {
            var detached = TaskExtension.MapToDto(remaining);
            detached.SeriesId = null;
            detached.RepeatUnit = TaskRepeatUnit.None;
            detached.RepeatInterval = 1;
            detached.RepeatWeekdays = TaskWeekdays.None;
            detached.RepeatGeneratedUntil = null;
            detached.RepeatWeekdayOrdinal = null;
            detached.RepeatMonth = null;
            detached.RepeatWeekStart = null;
            detached.RepeatEndDate = null;
            await _taskRepository.UpdateAsync(TaskExtension.MapToEntity(detached)).ConfigureAwait(false);
        }
    }

    private async Task<Guid?> HandOverRuleAsync(IReadOnlyList<TaskEntity> all, TaskDto seriesStart)
    {
        var startDay = seriesStart.TaskDate!.Value.Date;
        var members = all.Where(task => task.SeriesId == seriesStart.Id).ToList();
        var nextTask = members
            .Where(task => !task.IsDone && task.TaskDate.HasValue)
            .OrderBy(task => task.TaskDate)
            .ToList();
        var successor = nextTask.FirstOrDefault(task => IsOccurrence(seriesStart, task.TaskDate!.Value.Date)) ??
            nextTask.FirstOrDefault();

        TaskDto newStart;
        if (successor is not null)
        {
            newStart = TaskExtension.MapToDto(successor);
            newStart.SeriesId = null;
        }
        else
        {
            var today = _timeProvider.GetLocalNow().Date;
            var taken = members.Where(task => task.TaskDate.HasValue).Select(task => task.TaskDate!.Value.Date).ToHashSet();
            var nextDay = TaskRepeatCalculator
                .GetOccurrences(
                    startDay,
                    seriesStart.RepeatUnit,
                    seriesStart.RepeatInterval,
                    seriesStart.RepeatWeekdays,
                    today > startDay ? today : startDay.AddDays(1),
                    startDay.AddYears(AppConstants.RepeatSearchYears),
                    seriesStart.RepeatWeekdayOrdinal,
                    seriesStart.RepeatMonth,
                    seriesStart.RepeatWeekStart,
                    seriesStart.RepeatEndDate)
                .Where(day => day != startDay && !taken.Contains(day))
                .Select(day => (DateTime?)day)
                .FirstOrDefault();
            if (nextDay is null)
            {
                return null;
            }

            newStart = TaskExtension.CreateSeriesTask(seriesStart, nextDay.Value, _timeProvider.GetLocalNow().DateTime);
            newStart.SeriesId = null;
        }

        newStart.RepeatUnit = seriesStart.RepeatUnit;
        newStart.RepeatInterval = seriesStart.RepeatInterval;
        newStart.RepeatWeekdays = seriesStart.RepeatWeekdays;
        newStart.RepeatGeneratedUntil = seriesStart.RepeatGeneratedUntil;
        newStart.RepeatWeekdayOrdinal = seriesStart.RepeatWeekdayOrdinal;
        newStart.RepeatMonth = seriesStart.RepeatMonth;
        newStart.RepeatWeekStart = seriesStart.RepeatWeekStart;
        newStart.RepeatEndDate = seriesStart.RepeatEndDate;

        if (successor is null)
        {
            await _taskRepository.AddAsync(TaskExtension.MapToEntity(newStart)).ConfigureAwait(false);
        }
        else
        {
            await _taskRepository.UpdateAsync(TaskExtension.MapToEntity(newStart)).ConfigureAwait(false);
        }

        foreach (var member in members.Where(task => task.Id != successor?.Id))
        {
            var repointed = TaskExtension.MapToDto(member);
            repointed.SeriesId = newStart.Id;
            await _taskRepository.UpdateAsync(TaskExtension.MapToEntity(repointed)).ConfigureAwait(false);
        }

        return newStart.Id;
    }

    private static bool IsOccurrence(TaskDto seriesStart, DateTime day)
    {
        return TaskRepeatCalculator
            .GetOccurrences(
                seriesStart.TaskDate!.Value.Date,
                seriesStart.RepeatUnit,
                seriesStart.RepeatInterval,
                seriesStart.RepeatWeekdays,
                day,
                day,
                seriesStart.RepeatWeekdayOrdinal,
                seriesStart.RepeatMonth,
                seriesStart.RepeatWeekStart,
                seriesStart.RepeatEndDate)
            .Count > 0;
    }

    private async Task<bool> RemoveFutureOpenTasksAsync(Guid seriesStartId, Guid? exceptTaskId)
    {
        var today = _timeProvider.GetLocalNow().Date;
        var all = await _taskRepository.GetAllAsync().ConfigureAwait(false);
        var idsToRemove = all
            .Where(task =>
                task.SeriesId == seriesStartId &&
                !task.IsDone &&
                task.Id != exceptTaskId &&
                task.TaskDate.HasValue && task.TaskDate.Value.Date >= today)
            .Select(task => task.Id)
            .ToList();

        await _taskRepository.DeleteRangeAsync(idsToRemove).ConfigureAwait(false);
        return idsToRemove.Count > 0;
    }

    private async Task<bool> CreateMissingTasksAsync()
    {
        var now = _timeProvider.GetLocalNow();
        var today = now.Date;
        var horizon = today.AddDays(AppConstants.RepeatHorizonDays);
        var all = await _taskRepository.GetAllAsync().ConfigureAwait(false);
        var newTasks = new List<TaskEntity>();
        var changed = false;

        foreach (var entity in all)
        {
            var seriesStart = TaskExtension.MapToDto(entity);
            if (!TaskExtension.IsSeriesStart(seriesStart))
            {
                continue;
            }

            var startDay = seriesStart.TaskDate!.Value.Date;
            var firstDay = seriesStart.RepeatGeneratedUntil is { } generatedUntil
                ? generatedUntil.Date.AddDays(1)
                : startDay.AddDays(1);
            if (firstDay < today)
            {
                firstDay = today;
            }

            if (firstDay > horizon)
            {
                continue;
            }

            var existingDays = all
                .Where(task => task.SeriesId == entity.Id && task.TaskDate.HasValue)
                .Select(task => task.TaskDate!.Value.Date)
                .ToHashSet();

            var occurrences = TaskRepeatCalculator.GetOccurrences(
                startDay,
                seriesStart.RepeatUnit,
                seriesStart.RepeatInterval,
                seriesStart.RepeatWeekdays,
                firstDay,
                horizon,
                seriesStart.RepeatWeekdayOrdinal,
                seriesStart.RepeatMonth,
                seriesStart.RepeatWeekStart,
                seriesStart.RepeatEndDate);

            foreach (var day in occurrences.Where(day => day != startDay && !existingDays.Contains(day)))
            {
                var seriesTask = TaskExtension.CreateSeriesTask(seriesStart, day, now.DateTime);
                newTasks.Add(TaskExtension.MapToEntity(seriesTask));
            }

            seriesStart.RepeatGeneratedUntil = horizon;
            await _taskRepository.UpdateAsync(TaskExtension.MapToEntity(seriesStart)).ConfigureAwait(false);
            changed = true;
        }

        if (newTasks.Count > 0)
        {
            await _taskRepository.AddRangeAsync(newTasks).ConfigureAwait(false);
        }

        return changed;
    }

    private static bool HasRuleChanges(TaskDto previous, TaskDto saved)
    {
        return previous.RepeatUnit != saved.RepeatUnit ||
            previous.RepeatInterval != saved.RepeatInterval ||
            previous.RepeatWeekdays != saved.RepeatWeekdays ||
            previous.RepeatWeekdayOrdinal != saved.RepeatWeekdayOrdinal ||
            previous.RepeatMonth != saved.RepeatMonth ||
            previous.RepeatWeekStart != saved.RepeatWeekStart ||
            previous.RepeatEndDate != saved.RepeatEndDate;
    }

    private static bool RestartsSeries(TaskDto previous, TaskDto savedStart, TaskSeriesScope scope)
    {
        return scope == TaskSeriesScope.WholeSeries
            ? HasSeriesChanges(previous, savedStart)
            : HasRuleChanges(previous, savedStart) || previous.TaskDate?.Date != savedStart.TaskDate?.Date;
    }

    private static bool HasSeriesChanges(TaskDto previous, TaskDto saved)
    {
        return HasSharedChanges(previous, saved) || previous.TaskDate?.Date != saved.TaskDate?.Date;
    }

    private static bool HasSharedChanges(TaskDto previous, TaskDto saved)
    {
        return HasRuleChanges(previous, saved) ||
            !string.Equals(previous.Note, saved.Note, StringComparison.Ordinal) ||
            previous.Priority != saved.Priority ||
            previous.TimerMinutes != saved.TimerMinutes ||
            previous.BreakMinutes != saved.BreakMinutes ||
            previous.IsPositive != saved.IsPositive ||
            previous.IsWorkTask != saved.IsWorkTask ||
            previous.TaskTime != saved.TaskTime ||
            previous.ReminderMinutesBefore != saved.ReminderMinutesBefore;
    }

    private void OnDayChanged(object? sender, DayChangedEventArgs e)
    {
        _ = EnsureTasksSafelyAsync();
    }

    private async Task EnsureTasksSafelyAsync()
    {
        try
        {
            await EnsureTasksAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureAsync(
                ex,
                new AppErrorCaptureOptions
                {
                    Source = $"{nameof(TaskSeriesService)}.{nameof(EnsureTasksAsync)}",
                    IsFatal = false
                }).ConfigureAwait(false);
        }
    }
}
