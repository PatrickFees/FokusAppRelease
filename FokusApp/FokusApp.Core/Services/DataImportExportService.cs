using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;
using System.Text.Json;

namespace FokusApp.Core.Services;

public sealed class DataImportExportService : IDataImportExportService
{
    private const string ExportFileName = "FokosAppData.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly ITaskRepository _taskRepository;
    private readonly ISettingRepository _settingRepository;
    private readonly IRingtonesRepository _ringtonesRepository;
    private readonly IDataImportRepository _dataImportRepository;
    private readonly IDataTransferFileService _fileService;
    private readonly IDataTransferTextProvider _textProvider;
    private readonly IAppDataCoordinator _appDataCoordinator;
    private readonly IAppLocalizationService _appLocalizationService;
    private readonly IAppThemeService _appThemeService;
    private readonly TimeProvider _timeProvider;

    public DataImportExportService(
        ITaskRepository taskRepository,
        ISettingRepository settingRepository,
        IRingtonesRepository ringtonesRepository,
        IDataImportRepository dataImportRepository,
        IDataTransferFileService fileService,
        IDataTransferTextProvider textProvider,
        IAppDataCoordinator appDataCoordinator,
        IAppLocalizationService appLocalizationService,
        IAppThemeService appThemeService,
        TimeProvider timeProvider)
    {
        _taskRepository = taskRepository;
        _settingRepository = settingRepository;
        _ringtonesRepository = ringtonesRepository;
        _dataImportRepository = dataImportRepository;
        _fileService = fileService;
        _textProvider = textProvider;
        _appDataCoordinator = appDataCoordinator;
        _appLocalizationService = appLocalizationService;
        _appThemeService = appThemeService;
        _timeProvider = timeProvider;
    }

    public async Task<DataTransferResult> ExportAsync()
    {
        var settings = await _settingRepository.GetAllAsync().ConfigureAwait(false);
        var tasks = await _taskRepository.GetAllAsync().ConfigureAwait(false);
        var ringtones = await _ringtonesRepository.GetAllAsync().ConfigureAwait(false);

        var payload = new DataTransferPayload
        {
            ExportedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Settings = settings
                .Select(setting => SettingDataTransferModel.FromEntity(setting))
                .ToList(),
            Tasks = tasks
                .Select(task => TaskDataTransferModel.FromEntity(task))
                .ToList(),
            Ringtones = ringtones
                .Select(ringtone => RingtoneDataTransferModel.FromEntity(ringtone))
                .ToList(),
            FocusTimers = []
        };

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var shareTitle = _textProvider.GetText("ExportShareTitle");
        await _fileService.ShareExportFileAsync(ExportFileName, shareTitle, json).ConfigureAwait(false);

        return DataTransferResult.Success(_textProvider.GetText("ExportCreated", ExportFileName));
    }

    public async Task<DataTransferResult> ImportAsync()
    {
        var pickerTitle = _textProvider.GetText("ImportPickerTitle");
        var sourceStream = await _fileService.OpenImportFileAsync(pickerTitle).ConfigureAwait(false);

        if (sourceStream is null)
        {
            return DataTransferResult.Canceled(_textProvider.GetText("ImportCanceled"));
        }

        await using (sourceStream.ConfigureAwait(false))
        {
            DataTransferPayload? payload;
            try
            {
                payload = await JsonSerializer.DeserializeAsync<DataTransferPayload>(sourceStream, SerializerOptions)
                    .ConfigureAwait(false);
            }
            catch (JsonException)
            {
                return DataTransferResult.Failure(_textProvider.GetText("ImportReadFailed"));
            }

            if (payload is null || !payload.IsValid())
            {
                return DataTransferResult.Failure(_textProvider.GetText("ImportInvalid"));
            }

            await ReplaceCurrentDataAsync(payload).ConfigureAwait(false);
        }

        await _appDataCoordinator.ReloadAsync().ConfigureAwait(false);
        await _appLocalizationService.ReloadAsync().ConfigureAwait(false);
        await _appThemeService.ReloadAsync().ConfigureAwait(false);
        return DataTransferResult.Success(_textProvider.GetText("ImportCompleted"));
    }

    public async Task<DataTransferResult> ResetToStartDataAsync()
    {
        var ringtones = StartupDataFactory.CreateRingtones().ToArray();
        var defaultRingtone = StartupDataFactory.SelectDefaultRingtone(ringtones);
        var setting = StartupDataFactory.CreateSetting(defaultRingtone);
        var tasks = StartupDataFactory.CreateStartingTasks(_timeProvider.GetLocalNow()).ToArray();

        await _dataImportRepository
            .ReplaceAllAsync(ringtones, [setting], tasks)
            .ConfigureAwait(false);

        await _appDataCoordinator.ReloadAsync().ConfigureAwait(false);
        await _appLocalizationService.ReloadAsync().ConfigureAwait(false);
        await _appThemeService.ReloadAsync().ConfigureAwait(false);
        return DataTransferResult.Success(_textProvider.GetText("ResetCompleted"));
    }

    private async Task ReplaceCurrentDataAsync(DataTransferPayload payload)
    {
        var ringtones = payload.Ringtones!
            .Select(item => item.ToEntity())
            .ToArray();
        var settings = payload.GetSettingsForImport()
            .Select(item => item.ToEntity())
            .ToArray();
        var tasks = payload.CreateTaskEntities().ToArray();

        await _dataImportRepository
            .ReplaceAllAsync(ringtones, settings, tasks)
            .ConfigureAwait(false);
    }
}
