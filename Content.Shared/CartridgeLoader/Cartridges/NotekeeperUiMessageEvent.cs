// SPDX-FileCopyrightText: 2022 Julian Giebel <juliangiebel@live.de>
// SPDX-FileCopyrightText: 2023 MishaUnity <81403616+MishaUnity@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

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
