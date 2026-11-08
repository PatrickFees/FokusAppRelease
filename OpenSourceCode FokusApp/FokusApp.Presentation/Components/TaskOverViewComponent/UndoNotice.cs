using FokusApp.Core.Enums;

namespace FokusApp.Components.TaskOverViewComponent;

public sealed record UndoNotice(UndoNoticeKind Kind, int Count);
