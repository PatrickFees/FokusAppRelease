namespace FokusApp.Components.PopUps.TaskEdit;

/// <summary>
/// Ergebnis von <see cref="TaskEditorCoordinator.HandleCloseAsync"/>. Das ViewModel leitet daraus
/// seine Benachrichtigungen ab.
/// </summary>
public enum TaskEditorCloseOutcome
{
    /// <summary>Der Editor war nicht offen oder die Aktion hat nichts verändert.</summary>
    Ignored,

    /// <summary>Der Editor wurde geschlossen (Abbrechen oder erfolgreiches Speichern).</summary>
    Closed,

    /// <summary>Die Aufgabe wurde nicht gespeichert (z. B. leere Notiz), der Editor bleibt offen.</summary>
    NotSaved,

    /// <summary>Der Editor wurde mit der Kopie einer regelmäßigen Vorlage neu geöffnet.</summary>
    TemplateCopyOpened,

    /// <summary>Der Nutzer will die regelmäßige Vorlage bearbeiten, das ViewModel öffnet den Editor.</summary>
    TemplateEditRequested,

    /// <summary>Beim Speichern ist ein Fehler aufgetreten, der Editor bleibt offen.</summary>
    SaveFailed
}

public sealed record TaskEditorCloseResult(TaskEditorCloseOutcome Outcome, Exception? Error = null);
