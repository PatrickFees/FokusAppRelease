namespace FokusApp.Core.Interfaces;

public interface IDataTransferFileService
{
    Task ShareExportFileAsync(string fileName, string title, string content, string mimeType = "application/json");

    Task ShareTextAsync(string title, string text);

    Task<Stream?> OpenImportFileAsync(string pickerTitle);
}
