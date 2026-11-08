namespace FokusApp.Core.Interfaces;

public interface IDataTransferTextProvider
{
    string GetText(string key, params object[] arguments);
}
