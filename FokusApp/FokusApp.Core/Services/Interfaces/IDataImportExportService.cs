namespace FokusApp.Core.Services.Interfaces;

public interface IDataImportExportService
{
    Task<DataTransferResult> ExportAsync();
    Task<DataTransferResult> ImportAsync();
    Task<DataTransferResult> ResetToStartDataAsync();
}

public sealed record DataTransferResult(bool IsSuccess, bool IsCanceled, string Message)
{
    public static DataTransferResult Success(string message) => new(true, false, message);
    public static DataTransferResult Failure(string message) => new(false, false, message);
    public static DataTransferResult Canceled(string message) => new(false, true, message);
}
