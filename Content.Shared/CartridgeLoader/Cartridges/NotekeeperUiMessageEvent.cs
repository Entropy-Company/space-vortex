using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NotekeeperUiMessageEvent : CartridgeMessageEvent
{
    public readonly NotekeeperUiAction Action;
    public readonly string? Note;
    public readonly int? NoteId;
    public readonly string? Title;
    public readonly string? Content;

    public NotekeeperUiMessageEvent(NotekeeperUiAction action, string? note = null, int? noteId = null, string? title = null, string? content = null)
    {
        Action = action;
        Note = note;
        NoteId = noteId;
        Title = title;
        Content = content;
    }
}

[Serializable, NetSerializable]
public enum NotekeeperUiAction
{
    Add,
    Remove,
    Edit,
    CreateNew,
    SaveNote,
    BackToList,
    View
}
