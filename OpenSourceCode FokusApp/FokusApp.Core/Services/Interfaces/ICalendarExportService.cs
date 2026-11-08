namespace FokusApp.Core.Services.Interfaces;

public interface ICalendarExportService
{

    Task<DataTransferResult> ExportAsync();
}
