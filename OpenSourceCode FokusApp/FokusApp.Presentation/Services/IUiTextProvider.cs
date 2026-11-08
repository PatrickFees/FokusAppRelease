namespace FokusApp.Services;

public interface IUiTextProvider
{
    string GetText(string key, params object[] arguments);
}
