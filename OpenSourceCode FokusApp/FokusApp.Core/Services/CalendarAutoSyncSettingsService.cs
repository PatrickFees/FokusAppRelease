using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class CalendarAutoSyncSettingsService : ICalendarAutoSyncSettingsService
{
    private readonly ISettingStore _settingStore;
    private readonly ICalendarAutoSyncCalendarRepository _calendarRepository;
    private readonly ICalendarAutoSyncBackgroundScheduler _backgroundScheduler;

    public CalendarAutoSyncSettingsService(
        ISettingStore settingStore,
        ICalendarAutoSyncCalendarRepository calendarRepository,
        ICalendarAutoSyncBackgroundScheduler backgroundScheduler)
    {
        _settingStore = settingStore;
        _calendarRepository = calendarRepository;
        _backgroundScheduler = backgroundScheduler;
    }

    public bool Enabled => _settingStore.Read(setting => setting?.CalendarAutoSyncEnabled ?? false);

    public DateTime? LastRunAt => _settingStore.Read(setting => setting?.CalendarAutoSyncLastRunAt);

    public async Task<IReadOnlyList<string>> GetSelectedCalendarIdsAsync()
    {
        var calendars = await _calendarRepository.GetAllAsync().ConfigureAwait(false);
        return calendars.Select(calendar => calendar.CalendarId).ToList();
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        await _settingStore.UpdateAsync(current =>
        {
            if (current.CalendarAutoSyncEnabled == enabled)
            {
                return null;
            }

            var updated = SettingSnapshot.Copy(current);
            updated.CalendarAutoSyncEnabled = enabled;
            return updated;
        }).ConfigureAwait(false);

        if (enabled)
        {
            _backgroundScheduler.Enable();
        }
        else
        {
            _backgroundScheduler.Disable();
        }
    }

    public Task SetSelectedCalendarIdsAsync(IReadOnlyList<string> calendarIds) =>
        _calendarRepository.ReplaceAllAsync(calendarIds);

    public Task RecordRunAsync(DateTime runAt) => _settingStore.UpdateAsync(current =>
    {
        var updated = SettingSnapshot.Copy(current);
        updated.CalendarAutoSyncLastRunAt = runAt;
        return updated;
    });
}
