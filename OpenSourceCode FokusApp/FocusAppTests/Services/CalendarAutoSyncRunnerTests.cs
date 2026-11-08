using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.Services;

public class CalendarAutoSyncRunnerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly ICalendarAutoSyncSettingsService _settingsService = Substitute.For<ICalendarAutoSyncSettingsService>();
    private readonly ICalendarImportService _calendarImportService = Substitute.For<ICalendarImportService>();
    private readonly ICalendarImportPlatformAdapter _platformAdapter = Substitute.For<ICalendarImportPlatformAdapter>();
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly IAppErrorCaptureService _appErrorCaptureService = Substitute.For<IAppErrorCaptureService>();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly CalendarAutoSyncRunner _runner;

    public CalendarAutoSyncRunnerTests()
    {
        _settingsService.Enabled.Returns(true);
        _settingsService.LastRunAt.Returns((DateTime?)null);
        _settingsService.GetSelectedCalendarIdsAsync().Returns(["calendar-1"]);
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(true);
        _scopeService.IsWorkScope.Returns(true);
        _calendarImportService
            .SyncAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>())
            .Returns(CalendarSyncResult.Success(0));

        _runner = new CalendarAutoSyncRunner(
            _settingsService, _calendarImportService, _platformAdapter, _scopeService, _appErrorCaptureService, _timeProvider);
    }

    [Fact]
    public async Task RunIfDueAsync_Disabled_DoesNotSyncOrRecordRun()
    {
        _settingsService.Enabled.Returns(false);

        await _runner.RunIfDueAsync();

        await _calendarImportService.DidNotReceive().SyncAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>());
        await _settingsService.DidNotReceive().RecordRunAsync(Arg.Any<DateTime>());
    }

    [Fact]
    public async Task RunIfDueAsync_LastRunLessThanOneHourAgo_DoesNotSync()
    {
        _settingsService.LastRunAt.Returns(Now.DateTime.AddMinutes(-30));

        await _runner.RunIfDueAsync();

        await _calendarImportService.DidNotReceive().SyncAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>());
        await _settingsService.DidNotReceive().RecordRunAsync(Arg.Any<DateTime>());
    }

    [Fact]
    public async Task RunIfDueAsync_LastRunAtLeastOneHourAgo_Syncs()
    {
        _settingsService.LastRunAt.Returns(Now.DateTime.AddHours(-1));

        await _runner.RunIfDueAsync();

        await _calendarImportService.Received(1).SyncAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task RunIfDueAsync_NoCalendarsSelected_DoesNotSyncOrRecordRun()
    {
        _settingsService.GetSelectedCalendarIdsAsync().Returns(Array.Empty<string>());

        await _runner.RunIfDueAsync();

        await _calendarImportService.DidNotReceive().SyncAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>());
        await _settingsService.DidNotReceive().RecordRunAsync(Arg.Any<DateTime>());
    }

    [Fact]
    public async Task RunIfDueAsync_NoPermission_DoesNotSyncOrRecordRunAndNeverPrompts()
    {
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(false);

        await _runner.RunIfDueAsync();

        await _calendarImportService.DidNotReceive().SyncAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>());
        await _settingsService.DidNotReceive().RecordRunAsync(Arg.Any<DateTime>());
        _ = _platformAdapter.DidNotReceive().HasPermissionAsync();
    }

    [Fact]
    public async Task RunIfDueAsync_Due_SyncsWithSelectedCalendarsAndCurrentScopeThenRecordsRun()
    {
        await _runner.RunIfDueAsync();

        var expectedCalendarIds = new List<string> { "calendar-1" };
        await _calendarImportService.Received(1).SyncAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids.SequenceEqual(expectedCalendarIds)), true);
        await _settingsService.Received(1).RecordRunAsync(Now.DateTime);
    }

    [Fact]
    public async Task RunIfDueAsync_SyncThrows_CapturesErrorButStillRecordsRun()
    {
        _calendarImportService
            .SyncAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<bool>())
            .Returns(Task.FromException<CalendarSyncResult>(new InvalidOperationException("boom")));

        await _runner.RunIfDueAsync();

        await _appErrorCaptureService.Received(1).CaptureAsync(
            Arg.Any<Exception>(),
            Arg.Is<AppErrorCaptureOptions>(options => options.Source == "CalendarAutoSyncRunner.RunIfDueAsync"));
        await _settingsService.Received(1).RecordRunAsync(Now.DateTime);
    }
}
