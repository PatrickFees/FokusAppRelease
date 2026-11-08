namespace FokusApp.Components.PopUps.TaskEdit;

public enum TaskEditorCloseOutcome
{

    Ignored,

    Closed,

    NotSaved,

    TemplateCopyOpened,

    TemplateEditRequested,

    SaveFailed
}

public sealed record TaskEditorCloseResult(TaskEditorCloseOutcome Outcome, Exception? Error = null);
