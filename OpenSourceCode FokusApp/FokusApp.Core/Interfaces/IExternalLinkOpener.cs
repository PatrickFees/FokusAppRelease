namespace FokusApp.Core.Interfaces;

public interface IExternalLinkOpener
{
    Task OpenAsync(string url);
}
