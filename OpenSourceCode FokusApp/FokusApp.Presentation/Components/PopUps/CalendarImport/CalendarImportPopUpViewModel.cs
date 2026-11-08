using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Core;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;

namespace FokusApp.Components.PopUps.CalendarImport;

public sealed partial class CalendarImportPopUpViewModel : ObservableObject, ICalendarImportPopUpViewModel
{
    private readonly ICalendarImportService _calendarImportService;
    private readonly IScopeService _scopeService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly IUiTextProvider _textProvider;
    private readonly TimeProvider _timeProvider;
    private readonly HashSet<string> _selectedCalendarIds = [];
    private IReadOnlyList<DeviceCalendarInfo> _availableCalendars = [];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    private DateTime from;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    private DateTime to;

    [ObservableProperty]
    private bool canChangeScope;

    [ObservableProperty]
    private bool isWorkTask;

    [ObservableProperty]
    private string? resultMessage;

    [ObservableProperty]
    private bool? resultIsSuccess;

    public CalendarImportPopUpViewModel(
        ICalendarImportService calendarImportService,
        IScopeService scopeService,
        IAppErrorCaptureService appErrorCaptureService,
        IUiTextProvider textProvider,
        TimeProvider timeProvider)
    {
        _calendarImportService = calendarImportService;
        _scopeService = scopeService;
        _appErrorCaptureService = appErrorCaptureService;
        _textProvider = textProvider;
        _timeProvider = timeProvider;
    }

    public IReadOnlyList<CalendarOptionModel> Calendars => _availableCalendars
        .Select(calendar => new CalendarOptionModel(
            calendar.Id, calendar.DisplayName, calendar.AccountName, _selectedCalendarIds.Contains(calendar.Id)))
        .ToList();

    public bool CanImport => !IsLoading && _selectedCalendarIds.Count > 0 && From.Date <= To.Date;

    public async Task OpenAsync()
    {
        var today = _timeProvider.GetLocalNow().Date;
        From = today;
        To = today.AddDays(AppConstants.RepeatHorizonDays);
        IsWorkTask = _scopeService.IsWorkScope;
        CanChangeScope = _scopeService.CanSwitch;
        ResultMessage = null;
        ResultIsSuccess = null;
        _selectedCalendarIds.Clear();
        _availableCalendars = [];
        IsLoading = true;
        NotifyEverythingChanged();

        try
        {
            _availableCalendars = await _calendarImportService.GetAvailableCalendarsAsync();
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService
                .CaptureUiErrorAsync(ex, $"{nameof(CalendarImportPopUpViewModel)}.{nameof(OpenAsync)}");
        }
        finally
        {
            IsLoading = false;
            NotifyEverythingChanged();
        }
    }

    public void ToggleCalendar(string calendarId)
    {
        if (!_selectedCalendarIds.Remove(calendarId))
        {
            _selectedCalendarIds.Add(calendarId);
        }

        OnPropertyChanged(nameof(Calendars));
        OnPropertyChanged(nameof(CanImport));
    }

    public void SetFrom(DateTime date)
    {
        From = date.Date;
    }

    public void SetTo(DateTime date)
    {
        To = date.Date;
    }

    public void SetIsWorkTask(bool isWorkTask)
    {
        if (!CanChangeScope)
        {
            return;
        }

        IsWorkTask = isWorkTask;
    }

    public async Task ImportAsync()
    {
        if (!CanImport)
        {
            return;
        }

        IsLoading = true;
        ResultMessage = null;
        ResultIsSuccess = null;
        NotifyEverythingChanged();

        try
        {
            var result = await _calendarImportService
                .ImportAsync(_selectedCalendarIds.ToList(), From, To, IsWorkTask);
            ResultMessage = result.Message;
            ResultIsSuccess = result.IsSuccess;
        }
        catch (Exception ex)
        {
            ResultMessage = _textProvider.GetText("DataActionFailed");
            ResultIsSuccess = false;
            await _appErrorCaptureService
                .CaptureUiErrorAsync(ex, $"{nameof(CalendarImportPopUpViewModel)}.{nameof(ImportAsync)}");
        }
        finally
        {
            IsLoading = false;
            NotifyEverythingChanged();
        }
    }

    private void NotifyEverythingChanged()
    {
        OnPropertyChanged(nameof(Calendars));
        OnPropertyChanged(nameof(CanImport));
        OnPropertyChanged(string.Empty);
    }
}
