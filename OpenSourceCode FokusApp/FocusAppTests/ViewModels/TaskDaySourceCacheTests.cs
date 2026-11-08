using FokusApp.Components.SharedUI;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.ViewModels;

public class TaskDaySourceCacheTests
{
    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly TaskDaySourceCache _cache;

    public TaskDaySourceCacheTests()
    {
        _cache = new TaskDaySourceCache(_taskCache);
    }

    [Fact]
    public void Constructor_NewCache_AllListsAreEmpty()
    {
        Assert.Empty(_cache.NormalTasks);
        Assert.Empty(_cache.Templates);
        Assert.Empty(_cache.DoneTasks);
    }

    [Fact]
    public void RefreshNormal_Day_TakesTasksOfTheDayFromTheTaskCache()
    {
        var task = TestTasks.Create("Normal");
        SetNormal(task);

        _cache.RefreshNormal(TestTasks.Day);

        var loaded = Assert.Single(_cache.NormalTasks);
        Assert.Same(task, loaded);
        _taskCache.Received(1).GetNormalTasks(TestTasks.Day);
        Assert.Empty(_cache.Templates);
        Assert.Empty(_cache.DoneTasks);
    }

    [Fact]
    public void RefreshTemplate_NoDay_TakesAllTasksFromTheTaskCache()
    {
        var task = TestTasks.Create("Regelmäßig", isTemplate: true);
        SetTemplate(task);

        _cache.RefreshTemplate(null);

        var loaded = Assert.Single(_cache.Templates);
        Assert.Same(task, loaded);
        _taskCache.Received(1).GetTemplates(null);
        Assert.Empty(_cache.NormalTasks);
        Assert.Empty(_cache.DoneTasks);
    }

    [Fact]
    public void RefreshDone_Day_TakesTasksOfTheDayFromTheTaskCache()
    {
        var task = TestTasks.Create("Fertig", isDone: true);
        SetDone(task);

        _cache.RefreshDone(TestTasks.Day);

        var loaded = Assert.Single(_cache.DoneTasks);
        Assert.Same(task, loaded);
        _taskCache.Received(1).GetDoneTasks(TestTasks.Day);
        Assert.Empty(_cache.NormalTasks);
        Assert.Empty(_cache.Templates);
    }

    [Fact]
    public void RefreshNormal_SecondRefresh_ReplacesPreviousTasks()
    {
        var first = TestTasks.Create("Erste");
        var second = TestTasks.Create("Zweite");
        SetNormal(first);
        _cache.RefreshNormal(null);
        SetNormal(second);

        _cache.RefreshNormal(null);

        var loaded = Assert.Single(_cache.NormalTasks);
        Assert.Same(second, loaded);
    }

    [Fact]
    public void RefreshNormal_EmptyResult_ClearsPreviousTasks()
    {
        var task = TestTasks.Create("Erste");
        SetNormal(task);
        _cache.RefreshNormal(null);
        SetNormal();

        _cache.RefreshNormal(null);

        Assert.Empty(_cache.NormalTasks);
    }

    [Fact]
    public void FindById_TaskInEachList_IsFound()
    {
        var normal = TestTasks.Create("Normal");
        var template = TestTasks.Create("Regelmäßig", isTemplate: true);
        var done = TestTasks.Create("Fertig", isDone: true);
        LoadAll([normal], [template], [done]);

        var foundNormal = _cache.FindById(normal.Id!.Value);
        var foundTemplate = _cache.FindById(template.Id!.Value);
        var foundDone = _cache.FindById(done.Id!.Value);

        Assert.Same(normal, foundNormal);
        Assert.Same(template, foundTemplate);
        Assert.Same(done, foundDone);
    }

    [Fact]
    public void FindById_UnknownId_ReturnsNull()
    {
        var task = TestTasks.Create("Normal");
        LoadAll([task], [], []);
        var unknownId = Guid.NewGuid();

        var found = _cache.FindById(unknownId);

        Assert.Null(found);
    }

    [Fact]
    public void FindVisibleById_NormalAndDoneTask_AreFound()
    {
        var normal = TestTasks.Create("Normal");
        var done = TestTasks.Create("Fertig", isDone: true);
        LoadAll([normal], [], [done]);

        var foundNormal = _cache.FindVisibleById(normal.Id!.Value);
        var foundDone = _cache.FindVisibleById(done.Id!.Value);

        Assert.Same(normal, foundNormal);
        Assert.Same(done, foundDone);
    }

    [Fact]
    public void FindVisibleById_OpenTemplate_IsNotFound()
    {
        var template = TestTasks.Create("Vorlage", isTemplate: true);
        LoadAll([], [template], []);

        var found = _cache.FindVisibleById(template.Id!.Value);

        Assert.Null(found);
    }

    [Fact]
    public void FindVisibleById_TemplateThatIsNotATemplate_IsFound()
    {
        var finishedTemplate = TestTasks.Create("Erledigt", isTemplate: true, isDone: true);
        LoadAll([], [finishedTemplate], []);

        var found = _cache.FindVisibleById(finishedTemplate.Id!.Value);

        Assert.Same(finishedTemplate, found);
    }

    [Fact]
    public void FindVisibleById_UnknownId_ReturnsNull()
    {
        var unknownId = Guid.NewGuid();

        var found = _cache.FindVisibleById(unknownId);

        Assert.Null(found);
    }

    private void LoadAll(TaskDto[] normal, TaskDto[] template, TaskDto[] done)
    {
        SetNormal(normal);
        SetTemplate(template);
        SetDone(done);
        _cache.RefreshNormal(null);
        _cache.RefreshTemplate(null);
        _cache.RefreshDone(null);
    }

    private void SetNormal(params TaskDto[] tasks)
    {
        IReadOnlyList<TaskDto> list = tasks;
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(list);
    }

    private void SetTemplate(params TaskDto[] tasks)
    {
        IReadOnlyList<TaskDto> list = tasks;
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(list);
    }

    private void SetDone(params TaskDto[] tasks)
    {
        IReadOnlyList<TaskDto> list = tasks;
        _taskCache.GetDoneTasks(Arg.Any<DateTime?>()).Returns(list);
    }
}
