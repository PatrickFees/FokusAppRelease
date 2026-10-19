namespace FokusApp.Core.Services.Interfaces;

/// <summary>
/// Meldet den Wechsel des Kalendertags (z. B. über Mitternacht), damit Ansichten, die auf
/// "heute" stehen, dem Datum folgen können.
/// </summary>
public interface IDayChangeService
{
    /// <summary>Wird ausgelöst, wenn sich das lokale Datum gegenüber dem zuletzt bekannten geändert hat.</summary>
    event EventHandler<DayChangedEventArgs>? DayChanged;

    /// <summary>Das zuletzt festgestellte lokale Datum (ohne Uhrzeit).</summary>
    DateTime Today { get; }

    /// <summary>Prüft sofort auf einen Datumswechsel, z. B. beim Zurückkehren in die App.</summary>
    void CheckNow();
}

public sealed class DayChangedEventArgs(DateTime previousDay, DateTime currentDay) : EventArgs
{
    public DateTime PreviousDay { get; } = previousDay.Date;

    public DateTime CurrentDay { get; } = currentDay.Date;
}
