using FokusApp.Components.PopUps.CalendarImport;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.ViewModels;

public class CalendarImportPopUpViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly ICalendarImportService _calendarImportService = Substitute.For<ICalendarImportService>();
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly CalendarImportPopUpViewModel _viewModel;

    public CalendarImportPopUpViewModelTests()
    {
        _viewModel = new CalendarImportPopUpViewModel(
            _calendarImportService, _scopeService, _errorCapture, TestTexts.Create(), new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task OpenAsync_LoadsCalendarsAndResetsSelection()
    {
        _calendarImportService.GetAvailableCalendarsAsync().Returns(
            [new DeviceCalendarInfo("cal-1", "Privat", "me@example.com")]);
        _scopeService.IsWorkScope.Returns(true);
        _scopeService.CanSwitch.Returns(true);

        await _viewModel.OpenAsync();

        Assert.False(_viewModel.IsLoading);
        Assert.Single(_viewModel.Calendars);
        Assert.False(_viewModel.Calendars[0].IsSelected);
        Assert.True(_viewModel.CanChangeScope);
        Assert.True(_viewModel.IsWorkTask);
        Assert.Null(_viewModel.ResultMessage);
        Assert.False(_viewModel.CanImport);
    }

    [Fact]
    public async Task ToggleCalendar_SelectsAndDeselects()
    {
        _calendarImportService.GetAvailableCalendarsAsync().Returns(
            [new DeviceCalendarInfo("cal-1", "Privat", "me@example.com")]);
        await _viewModel.OpenAsync();

        _viewModel.ToggleCalendar("cal-1");
        Assert.True(_viewModel.Calendars[0].IsSelected);
        Assert.True(_viewModel.CanImport);

        _viewModel.ToggleCalendar("cal-1");
        Assert.False(_viewModel.Calendars[0].IsSelected);
        Assert.False(_viewModel.CanImport);
    }

    [Fact]
    public async Task SetIsWorkTask_WhenScopeFixed_IsIgnored()
    {
        _calendarImportService.GetAvailableCalendarsAsync().Returns([]);
        _scopeService.IsWorkScope.Returns(false);
        _scopeService.CanSwitch.Returns(false);
        await _viewModel.OpenAsync();

        _viewModel.SetIsWorkTask(true);

        Assert.False(_viewModel.IsWorkTask);
    }

    [Fact]
    public async Task ImportAsync_CallsServiceAndStoresResult()
    {
        _calendarImportService.GetAvailableCalendarsAsync().Returns(
            [new DeviceCalendarInfo("cal-1", "Privat", "me@example.com")]);
        await _viewModel.OpenAsync();
        _viewModel.ToggleCalendar("cal-1");

        _calendarImportService
            .ImportAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<bool>())
            .Returns(CalendarImportResult.Success(2, 1, 0, "CalendarImportCompleted:2|1|0"));

        await _viewModel.ImportAsync();

        Assert.Equal("CalendarImportCompleted:2|1|0", _viewModel.ResultMessage);
        Assert.True(_viewModel.ResultIsSuccess);
        await _calendarImportService.Received(1).ImportAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids.SequenceEqual(new[] { "cal-1" })),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<bool>());
    }
}
