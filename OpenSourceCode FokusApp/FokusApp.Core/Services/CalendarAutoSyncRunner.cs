using FokusApp.Core.Interfaces;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class CalendarAutoSyncRunner : ICalendarAutoSyncRunner
{
    private readonly ICalendarAutoSyncSettingsService _settingsService;
    private readonly ICalendarImportService _calendarImportService;
    private readonly ICalendarImportPlatformAdapter _platformAdapter;
    private readonly IScopeService _scopeService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly TimeProvider _timeProvider;

    public CalendarAutoSyncRunner(
        ICalendarAutoSyncSettingsService settingsService,
        ICalendarImportService calendarImportService,
        ICalendarImportPlatformAdapter platformAdapter,
        IScopeService scopeService,
        IAppErrorCaptureService appErrorCaptureService,
        TimeProvider timeProvider)
    {
        _settingsService = settingsService;
        _calendarImportService = calendarImportService;
        _platformAdapter = platformAdapter;
        _scopeService = scopeService;
        _appErrorCaptureService = appErrorCaptureService;
        _timeProvider = timeProvider;
    }

    public async Task RunIfDueAsync()
    {
        if (!_settingsService.Enabled)
        {
            return;
        }

        var now = _timeProvider.GetLocalNow();
        if (_settingsService.LastRunAt is { } lastRunAt &&
            now.DateTime - lastRunAt < AppConstants.CalendarAutoSyncMinInterval)
        {
            return;
        }

        var calendarIds = await _settingsService.GetSelectedCalendarIdsAsync().ConfigureAwait(false);
        if (calendarIds.Count == 0)
        {
            return;
        }

        if (!await _platformAdapter.HasPermissionWithoutPromptAsync().ConfigureAwait(false))
        {
            return;
        }

        try
        {
            await _calendarImportService.SyncAsync(calendarIds, _scopeService.IsWorkScope).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureAsync(
                ex,
                new AppErrorCaptureOptions
                {
                    Source = $"{nameof(CalendarAutoSyncRunner)}.{nameof(RunIfDueAsync)}",
                    IsFatal = false
                }).ConfigureAwait(false);
        }
        finally
        {
            await _settingsService.RecordRunAsync(now.DateTime).ConfigureAwait(false);
        }
    }
}
