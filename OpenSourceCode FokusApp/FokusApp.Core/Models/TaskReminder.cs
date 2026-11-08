namespace FokusApp.Core.Models;

public sealed record TaskReminder(Guid TaskId, string Note, DateTime TriggerAt, DateTime DueAt, int MinutesBefore);
