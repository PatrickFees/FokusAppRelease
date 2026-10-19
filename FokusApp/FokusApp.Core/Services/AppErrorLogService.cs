using FokusApp.Core.DataBase.ErrorLogging.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class AppErrorCaptureContext
{
    public string Source { get; init; } = "Unknown";
    public bool IsFatal { get; init; }
    public bool IsUiFailure { get; init; }
    public string? Route { get; init; }
    public string? Platform { get; init; }
    public string? OsVersion { get; init; }
    public string? AppVersion { get; init; }
    public int ThreadId { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

public class AppErrorLogService : IAppErrorLogService
{
    private readonly IAppErrorLogRepository _repository;

    public AppErrorLogService(IAppErrorLogRepository repository)
    {
        _repository = repository;
    }

    public async Task LogExceptionAsync(Exception exception, AppErrorCaptureContext context)
    {
        context ??= new AppErrorCaptureContext();

        var entry = new AppErrorLogEntity
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            Source = NormalizeRequired(context.Source, "Unknown"),
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
            Message = NormalizeRequired(exception.Message, exception.GetType().Name),
            StackTrace = NormalizeOptional(exception.StackTrace),
            InnerException = BuildInnerExceptionDetails(exception.InnerException),
            Route = NormalizeOptional(context.Route),
            Platform = NormalizeOptional(context.Platform),
            OsVersion = NormalizeOptional(context.OsVersion),
            AppVersion = NormalizeOptional(context.AppVersion),
            IsFatal = context.IsFatal,
            IsUiFailure = context.IsUiFailure,
            ThreadId = context.ThreadId == 0 ? Environment.CurrentManagedThreadId : context.ThreadId,
            PayloadJson = AppErrorMetadataSanitizer.Serialize(context.Metadata)
        };

        await _repository.AddAsync(entry).ConfigureAwait(false);
    }

    public async Task<List<AppErrorLogEntity>> GetLatestAsync()
    {
        return await _repository.GetLatestAsync().ConfigureAwait(false);
    }

    private static string NormalizeRequired(string? raw, string fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        return raw.Trim();
    }

    private static string? NormalizeOptional(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim();
    }

    private static string? BuildInnerExceptionDetails(Exception? innerException)
    {
        if (innerException is null)
        {
            return null;
        }

        var chunks = new List<string>();

        while (innerException is not null)
        {
            chunks.Add($"{innerException.GetType().FullName}: {innerException.Message}");
            innerException = innerException.InnerException;
        }

        return string.Join(Environment.NewLine, chunks);
    }

}
