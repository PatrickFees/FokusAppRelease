using System.ComponentModel.DataAnnotations;

namespace FokusApp.Core.DataBase.Entities;

public class CalendarAutoSyncCalendarEntity
{
    [Key]
    public Guid Id { get; set; }

    public string CalendarId { get; set; } = string.Empty;
}
