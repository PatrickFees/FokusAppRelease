using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Components.PopUps.CalendarImport;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;

namespace FokusApp.Components.PopUps.CalendarAutoSync;

public sealed partial class CalendarAutoSyncPopUpViewModel : ObservableObject, ICalendarAutoSyncPopUpViewModel
{
    private readonly ICalendarImportService _calendarImportService;
    private readonly ICalendarAutoSyncSettingsService _autoSyncSettingsService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly IUiTextProvider _textProvider;
    private readonly HashSet<string> _selectedCalendarIds = [];
    private IReadOnlyList<DeviceCalendarInfo> _availableCalendars = [];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? resultMessage;

    [ObservableProperty]
    private bool? resultIsSuccess;

    public CalendarAutoSyncPopUpViewModel(
        ICalendarImportService calendarImportService,
        ICalendarAutoSyncSettingsService autoSyncSettingsService,
        IAppErrorCaptureService appErrorCaptureService,
        IUiTextProvider textProvider)
    {
        _calendarImportService = calendarImportService;
        _autoSyncSettingsService = autoSyncSettingsService;
        _appErrorCaptureService = appErrorCaptureService;
        _textProvider = textProvider;
    }

    public IReadOnlyList<CalendarOptionModel> Calendars => _availableCalendars
        .Select(calendar => new CalendarOptionModel(
            calendar.Id, calendar.DisplayName, calendar.AccountName, _selectedCalendarIds.Contains(calendar.Id), calendar.IsLocalDefault))
        .ToList();

    public bool CanSave => !IsLoading;

    public async Task OpenAsync()
    {
        ResultMessage = null;
        ResultIsSuccess = null;
        _availableCalendars = [];
        _selectedCalendarIds.Clear();
        IsLoading = true;
        NotifyEverythingChanged();

        try
        {
            _availableCalendars = await _calendarImportService.GetAvailableCalendarsAsync();
            var selected = await _autoSyncSettingsService.GetSelectedCalendarIdsAsync();
            foreach (var calendarId in selected)
            {
                _selectedCalendarIds.Add(calendarId);
            }
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService
                .CaptureUiErrorAsync(ex, $"{nameof(CalendarAutoSyncPopUpViewModel)}.{nameof(OpenAsync)}");
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
    }

    public async Task SaveAsync()
    {
        IsLoading = true;
        ResultMessage = null;
        ResultIsSuccess = null;
        NotifyEverythingChanged();

        try
        {
            await _autoSyncSettingsService.SetSelectedCalendarIdsAsync(_selectedCalendarIds.ToList());
            ResultIsSuccess = true;
        }
        catch (Exception ex)
        {
            ResultMessage = _textProvider.GetText("DataActionFailed");
            ResultIsSuccess = false;
            await _appErrorCaptureService
                .CaptureUiErrorAsync(ex, $"{nameof(CalendarAutoSyncPopUpViewModel)}.{nameof(SaveAsync)}");
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
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(string.Empty);
    }
}
