using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Text;
using System.Text.Json;

namespace FocusAppTests.Services;

public class DataImportExportServiceTests
{
    private const string ExportFileName = "FokosAppData.json";

    private static readonly DateTimeOffset Now = new(2026, 3, 10, 14, 30, 0, TimeSpan.Zero);

    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtonesRepository _ringtonesRepository = Substitute.For<IRingtonesRepository>();
    private readonly IDataImportRepository _importRepository = Substitute.For<IDataImportRepository>();
    private readonly IDataTransferFileService _fileService = Substitute.For<IDataTransferFileService>();
    private readonly IDataTransferTextProvider _textProvider = Substitute.For<IDataTransferTextProvider>();
    private readonly IAppDataCoordinator _coordinator = Substitute.For<IAppDataCoordinator>();
    private readonly IAppLocalizationService _localizationService = Substitute.For<IAppLocalizationService>();
    private readonly IAppThemeService _themeService = Substitute.For<IAppThemeService>();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly DataImportExportService _service;

    public DataImportExportServiceTests()
    {
        _ringtonesRepository.GetAllAsync().Returns([]);
        _settingRepository.GetAllAsync().Returns([]);
        _taskRepository.GetAllAsync().Returns([]);
        _textProvider.GetText(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgAt<string>(0));
        _service = new DataImportExportService(
            _taskRepository,
            _settingRepository,
            _ringtonesRepository,
            _importRepository,
            _fileService,
            _textProvider,
            _coordinator,
            _localizationService,
            _themeService,
            _timeProvider);
    }

    [Fact]
    public async Task ExportAsync_StoredData_SharesJsonWithAllEntitiesAndFileName()
    {
        var ringtone = new RingtonesEntity { Id = Guid.NewGuid(), RingtoneFilePath = "Raw/A.mp3" };
        var setting = new SettingEntity { Id = Guid.NewGuid(), Theme = 2, Language = 1, RingtoneId = ringtone.Id };
        var task = CreateTask();
        _ringtonesRepository.GetAllAsync().Returns([ringtone]);
        _settingRepository.GetAllAsync().Returns([setting]);
        _taskRepository.GetAllAsync().Returns([task]);
        string? sharedJson = null;
        await _fileService.ShareExportFileAsync(
            ExportFileName,
            Arg.Any<string>(),
            Arg.Do<string>(json => sharedJson = json));

        var result = await _service.ExportAsync();

        var payload = JsonSerializer.Deserialize<DataTransferPayload>(sharedJson!);
        Assert.NotNull(payload);
        Assert.True(payload.IsValid());
        Assert.Equal(Now.UtcDateTime, payload.ExportedAtUtc);
        Assert.Equal(ringtone.Id, payload.Ringtones!.Single().Id);
        Assert.Equal(setting.Id, payload.Settings!.Single().Id);
        Assert.Equal(task.Id, payload.Tasks!.Single().Id);
        Assert.True(result.IsSuccess);
        Assert.Equal("ExportCreated", result.Message);
    }

    [Fact]
    public async Task ExportAsync_Always_UsesLocalizedShareTitleAndFileNameInMessage()
    {
        _textProvider.GetText("ExportShareTitle").Returns("Teilen");

        await _service.ExportAsync();

        await _fileService.Received(1).ShareExportFileAsync(ExportFileName, "Teilen", Arg.Any<string>());
        _textProvider.Received(1).GetText("ExportCreated", Arg.Is<object[]>(args => args.Length == 1 && (string)args[0] == ExportFileName));
    }

    [Fact]
    public async Task ImportAsync_PickerCanceled_ReturnsCanceledWithoutChangingData()
    {
        _fileService.OpenImportFileAsync(Arg.Any<string>()).Returns((Stream?)null);

        var result = await _service.ImportAsync();

        Assert.True(result.IsCanceled);
        Assert.False(result.IsSuccess);
        Assert.Equal("ImportCanceled", result.Message);
        await _importRepository.DidNotReceiveWithAnyArgs().ReplaceAllAsync(default!, default!, default!);
        await _coordinator.DidNotReceive().ReloadAsync();
    }

    [Theory]
    [InlineData("das ist kein json")]
    [InlineData("{ \"Tasks\": [")]
    public async Task ImportAsync_MalformedJson_ReturnsReadFailure(string content)
    {
        SetupImportFile(content);

        var result = await _service.ImportAsync();

        Assert.False(result.IsSuccess);
        Assert.False(result.IsCanceled);
        Assert.Equal("ImportReadFailed", result.Message);
        await _importRepository.DidNotReceiveWithAnyArgs().ReplaceAllAsync(default!, default!, default!);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task ImportAsync_JsonWithoutRequiredData_ReturnsInvalidFailure(string content)
    {
        SetupImportFile(content);

        var result = await _service.ImportAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("ImportInvalid", result.Message);
        await _importRepository.DidNotReceiveWithAnyArgs().ReplaceAllAsync(default!, default!, default!);
        await _coordinator.DidNotReceive().ReloadAsync();
    }

    [Fact]
    public async Task ImportAsync_ValidPayload_ReplacesDataAndReloadsCoordinatorAndLanguage()
    {
        var payload = CreateValidPayload(out var ringtoneId, out var settingId, out var taskId);
        SetupImportFile(JsonSerializer.Serialize(payload));
        IReadOnlyCollection<RingtonesEntity>? importedRingtones = null;
        IReadOnlyCollection<SettingEntity>? importedSettings = null;
        IReadOnlyCollection<TaskEntity>? importedTasks = null;
        await _importRepository.ReplaceAllAsync(
            Arg.Do<IReadOnlyCollection<RingtonesEntity>>(items => importedRingtones = items),
            Arg.Do<IReadOnlyCollection<SettingEntity>>(items => importedSettings = items),
            Arg.Do<IReadOnlyCollection<TaskEntity>>(items => importedTasks = items),
            Arg.Any<CancellationToken>());

        var result = await _service.ImportAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("ImportCompleted", result.Message);
        Assert.Equal(ringtoneId, importedRingtones!.Single().Id);
        Assert.Equal(settingId, importedSettings!.Single().Id);
        Assert.Equal(taskId, importedTasks!.Single().Id);
        await _coordinator.Received(1).ReloadAsync();
        await _localizationService.Received(1).ReloadAsync();
        await _themeService.Received(1).ReloadAsync();
    }

    [Fact]
    public async Task ImportAsync_ValidPayload_DisposesSourceStream()
    {
        var payload = CreateValidPayload(out _, out _, out _);
        var stream = SetupImportFile(JsonSerializer.Serialize(payload));

        await _service.ImportAsync();

        Assert.False(stream.CanRead);
    }

    [Fact]
    public async Task ImportAsync_RepositoryThrows_PropagatesAndDoesNotReload()
    {
        var payload = CreateValidPayload(out _, out _, out _);
        SetupImportFile(JsonSerializer.Serialize(payload));
        _importRepository
            .ReplaceAllAsync(default!, default!, default!, Arg.Any<CancellationToken>())
            .ThrowsAsyncForAnyArgs(new InvalidOperationException("Datenbankfehler"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ImportAsync());

        await _coordinator.DidNotReceive().ReloadAsync();
        await _localizationService.DidNotReceive().ReloadAsync();
    }

    private MemoryStream SetupImportFile(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        _fileService.OpenImportFileAsync(Arg.Any<string>()).Returns(stream);

        return stream;
    }

    private static DataTransferPayload CreateValidPayload(out Guid ringtoneId, out Guid settingId, out Guid taskId)
    {
        ringtoneId = Guid.NewGuid();
        settingId = Guid.NewGuid();
        taskId = Guid.NewGuid();
        var taskEntity = CreateTask();
        taskEntity.Id = taskId;

        return new DataTransferPayload
        {
            ExportedAtUtc = Now.UtcDateTime,
            Settings = [new SettingDataTransferModel(settingId, 0, 0, ringtoneId)],
            Tasks = [TaskDataTransferModel.FromEntity(taskEntity)],
            Ringtones = [new RingtoneDataTransferModel(ringtoneId, "Raw/A.mp3")],
            FocusTimers = []
        };
    }

    [Fact]
    public async Task ResetToStartDataAsync_Always_ReplacesAllDataWithStartDataAndReloads()
    {
        IReadOnlyCollection<RingtonesEntity>? replacedRingtones = null;
        IReadOnlyCollection<SettingEntity>? replacedSettings = null;
        IReadOnlyCollection<TaskEntity>? replacedTasks = null;
        await _importRepository.ReplaceAllAsync(
            Arg.Do<IReadOnlyCollection<RingtonesEntity>>(items => replacedRingtones = items),
            Arg.Do<IReadOnlyCollection<SettingEntity>>(items => replacedSettings = items),
            Arg.Do<IReadOnlyCollection<TaskEntity>>(items => replacedTasks = items),
            Arg.Any<CancellationToken>());

        var result = await _service.ResetToStartDataAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("ResetCompleted", result.Message);
        Assert.NotNull(replacedRingtones);
        Assert.NotNull(replacedSettings);
        Assert.NotNull(replacedTasks);
        Assert.Equal(4, replacedRingtones.Count);
        var setting = Assert.Single(replacedSettings);
        var defaultRingtone = replacedRingtones.Single(ringtone => ringtone.Id == setting.RingtoneId);
        Assert.Equal("Raw/Happy with Clap.mp3", defaultRingtone.RingtoneFilePath);
        Assert.Equal(0, setting.Language);
        Assert.Equal(2, replacedTasks.Count);
        Assert.All(replacedTasks, task => Assert.Equal(Now.Date, task.TaskDate));
        await _coordinator.Received(1).ReloadAsync();
        await _localizationService.Received(1).ReloadAsync();
        await _themeService.Received(1).ReloadAsync();
    }

    [Fact]
    public async Task ResetToStartDataAsync_ReplacingFails_DoesNotReloadAndThrows()
    {
        var failure = new InvalidOperationException("Datenbankfehler");
        _importRepository
            .ReplaceAllAsync(
                Arg.Any<IReadOnlyCollection<RingtonesEntity>>(),
                Arg.Any<IReadOnlyCollection<SettingEntity>>(),
                Arg.Any<IReadOnlyCollection<TaskEntity>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(failure);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ResetToStartDataAsync());

        Assert.Same(failure, thrown);
        await _coordinator.DidNotReceive().ReloadAsync();
        await _localizationService.DidNotReceive().ReloadAsync();
    }

    private static TaskEntity CreateTask()
    {
        return new TaskEntity
        {
            Id = Guid.NewGuid(),
            Note = "Aufgabe",
            IsWorkTask = true,
            CreationDate = Now.DateTime,
            TaskDate = Now.Date,
            Priority = TaskPriority.Mittel,
            BreakMinutes = 5,
            TimerMinutes = 25
        };
    }
}
