using System.ComponentModel.DataAnnotations;

namespace FokusApp.Core.DataBase.ErrorLogging.Entities;

public class AppErrorLogEntity
{
    [Key]
    public Guid Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }
    public string Source { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? InnerException { get; set; }
    public string? Route { get; set; }
    public string? Platform { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public bool IsFatal { get; set; }
    public bool IsUiFailure { get; set; }
    public int ThreadId { get; set; }
    public string? PayloadJson { get; set; }
}
