namespace FokusApp.Services;

/// <summary>
/// Liefert lokalisierte Texte für ViewModels, ohne dass diese von
/// IStringLocalizer und den Ressourcen des UI-Projekts abhängen.
/// </summary>
public interface IUiTextProvider
{
    string GetText(string key, params object[] arguments);
}
