using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class NotekeeperCartridgeComponent : Component
{
    /// <summary>
    /// The list of notes that got written down
    /// </summary>
    [DataField("notes")]
    public List<NoteData> Notes = new();

    /// <summary>
    /// Next note ID for unique identification
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int NextNoteId = 1;

    /// <summary>
    /// Currently editing note ID
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int? EditingNoteId = null;

    /// <summary>
    /// Currently viewing note ID
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int? ViewingNoteId = null;
}
