// SPDX-FileCopyrightText: 2022 Julian Giebel <juliangiebel@live.de>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 MishaUnity <81403616+MishaUnity@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT
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
