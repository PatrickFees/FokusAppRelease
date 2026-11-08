namespace FokusApp.Core.Services.Interfaces;

public interface IDayChangeService
{

    event EventHandler<DayChangedEventArgs>? DayChanged;

    DateTime Today { get; }

    void CheckNow();
}

public sealed class DayChangedEventArgs(DateTime previousDay, DateTime currentDay) : EventArgs
{
    public DateTime PreviousDay { get; } = previousDay.Date;

    public DateTime CurrentDay { get; } = currentDay.Date;
}
