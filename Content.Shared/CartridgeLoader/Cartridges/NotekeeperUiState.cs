using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NotekeeperUiState : BoundUserInterfaceState
{
    public List<NoteData> Notes;
    public int? EditingNoteId;
    public int? ViewingNoteId;

    public NotekeeperUiState(List<NoteData> notes, int? editingNoteId = null, int? viewingNoteId = null)
    {
        Notes = notes;
        EditingNoteId = editingNoteId;
        ViewingNoteId = viewingNoteId;
    }
}

[Serializable, NetSerializable]
public sealed class NoteData
{
    public string Title;
    public string Content;
    public int Id;

    public NoteData(string title, string content, int id)
    {
        Title = title;
        Content = content;
        Id = id;
    }
}
