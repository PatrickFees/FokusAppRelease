namespace FokusApp.Core.Interfaces;

public interface IDataTransferFileService
{
    Task ShareExportFileAsync(string fileName, string title, string content);

    /// <summary>
    /// Lets the user pick a file to import. Returns <c>null</c> if the selection was canceled.
    /// </summary>
    Task<Stream?> OpenImportFileAsync(string pickerTitle);
}
