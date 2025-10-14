// SPDX-FileCopyrightText: 2022 Julian Giebel <juliangiebel@live.de>
// SPDX-FileCopyrightText: 2023 MishaUnity <81403616+MishaUnity@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Dylan Hunter Whittingham <45404433+DylanWhittingham@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 dylanhunter <dylan2.whittingham@live.uwe.ac.uk>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Logs;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using System.Linq;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class NotekeeperCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NotekeeperCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<NotekeeperCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(EntityUid uid, NotekeeperCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    /// <summary>
    /// The ui messages received here get wrapped by a CartridgeMessageEvent and are relayed from the <see cref="CartridgeLoaderSystem"/>
    /// </summary>
    /// <remarks>
    /// The cartridge specific ui message event needs to inherit from the CartridgeMessageEvent
    /// </remarks>
    private void OnUiMessage(EntityUid uid, NotekeeperCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not NotekeeperUiMessageEvent message)
            return;

        switch (message.Action)
        {
            case NotekeeperUiAction.CreateNew:
                CreateNewNote(uid, component, args);
                component.ViewingNoteId = null;
                break;
            case NotekeeperUiAction.Edit:
                if (message.NoteId.HasValue)
                {
                    EditNote(uid, component, message.NoteId.Value, args);
                    component.ViewingNoteId = null;
                }
                break;
            case NotekeeperUiAction.SaveNote:
                if (message.NoteId.HasValue && message.Title != null && message.Content != null)
                {
                    SaveNote(uid, component, message.NoteId.Value, message.Title, message.Content, args);
                    component.EditingNoteId = null;
                    component.ViewingNoteId = message.NoteId;
                }
                break;
            case NotekeeperUiAction.Remove:
                if (message.NoteId.HasValue)
                {
                    RemoveNote(uid, component, message.NoteId.Value, args);
                    component.ViewingNoteId = null;
                }
                break;
            case NotekeeperUiAction.BackToList:
                BackToList(uid, component, args);
                component.ViewingNoteId = null;
                break;
            case NotekeeperUiAction.View:
                if (message.NoteId.HasValue)
                {
                    component.EditingNoteId = null;
                    component.ViewingNoteId = message.NoteId;
                }
                break;
        }

        UpdateUiState(uid, GetEntity(args.LoaderUid), component);
    }

    private void CreateNewNote(EntityUid uid, NotekeeperCartridgeComponent component, CartridgeMessageEvent args)
    {
        var newNote = new NoteData("", "", component.NextNoteId);
        component.Notes.Add(newNote);
        component.NextNoteId++;
        component.EditingNoteId = newNote.Id;

        _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
            $"{ToPrettyString(args.Actor)} created a new note on PDA: {ToPrettyString(uid)}");
    }

    private void EditNote(EntityUid uid, NotekeeperCartridgeComponent component, int noteId, CartridgeMessageEvent args)
    {
        component.EditingNoteId = noteId;
    }

    private void SaveNote(EntityUid uid, NotekeeperCartridgeComponent component, int noteId, string title, string content, CartridgeMessageEvent args)
    {
        var note = component.Notes.FirstOrDefault(n => n.Id == noteId);
        if (note != null)
        {
            // Limit title to 70 characters
            note.Title = title.Length > 70 ? title.Substring(0, 70) : title;
            // Limit content to 1000 characters
            note.Content = content.Length > 1000 ? content.Substring(0, 1000) : content;

            _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
                $"{ToPrettyString(args.Actor)} saved note '{note.Title}' on PDA: {ToPrettyString(uid)}");
        }
    }

    private void RemoveNote(EntityUid uid, NotekeeperCartridgeComponent component, int noteId, CartridgeMessageEvent args)
    {
        var note = component.Notes.FirstOrDefault(n => n.Id == noteId);
        if (note != null)
        {
            component.Notes.Remove(note);
            if (component.EditingNoteId == noteId)
                component.EditingNoteId = null;

            _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
                $"{ToPrettyString(args.Actor)} deleted note '{note.Title}' from PDA: {ToPrettyString(uid)}");
        }
    }

    private void BackToList(EntityUid uid, NotekeeperCartridgeComponent component, CartridgeMessageEvent args)
    {
        component.EditingNoteId = null;
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, NotekeeperCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new NotekeeperUiState(component.Notes, component.EditingNoteId, component.ViewingNoteId);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }
}