using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class DayChangeService : IDayChangeService, IDisposable
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly TimeProvider _timeProvider;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly Lock _lock = new();
    private readonly ITimer _timer;
    private DateTime _today;

    public DayChangeService(TimeProvider timeProvider, IAppErrorCaptureService appErrorCaptureService)
    {
        _timeProvider = timeProvider;
        _appErrorCaptureService = appErrorCaptureService;
        _today = _timeProvider.GetLocalNow().Date;
        _timer = _timeProvider.CreateTimer(_ => CheckNow(), null, CheckInterval, CheckInterval);
    }

    public event EventHandler<DayChangedEventArgs>? DayChanged;

    public DateTime Today
    {
        get
        {
            lock (_lock)
            {
                return _today;
            }
        }
    }

    public void CheckNow()
    {
        DateTime previousDay;
        var currentDay = _timeProvider.GetLocalNow().Date;

        lock (_lock)
        {
            if (currentDay == _today)
            {
                return;
            }

            previousDay = _today;
            _today = currentDay;
        }

        RaiseDayChanged(new DayChangedEventArgs(previousDay, currentDay));
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    private void RaiseDayChanged(DayChangedEventArgs args)
    {
        var handlers = DayChanged;
        if (handlers is null)
        {
            return;
        }

        // Ein fehlerhafter Empfänger darf weder die anderen Empfänger noch den Zeitgeber-Thread beenden.
        foreach (var handler in handlers.GetInvocationList().Cast<EventHandler<DayChangedEventArgs>>())
        {
            try
            {
                handler(this, args);
            }
            catch (Exception ex)
            {
                _ = _appErrorCaptureService.CaptureAsync(
                    ex,
                    new AppErrorCaptureOptions
                    {
                        Source = $"{nameof(DayChangeService)}.{nameof(DayChanged)}",
                        IsUiFailure = true
                    });
            }
        }
    }
}
