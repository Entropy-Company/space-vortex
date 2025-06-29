using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using System.Linq;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class NotekeeperUi : UIFragment
{
    private NotekeeperUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new NotekeeperUiFragment();

        _fragment.OnViewNote += noteId => SendNotekeeperMessage(NotekeeperUiAction.View, noteId: noteId, userInterface: userInterface);
        _fragment.OnNoteSelected += noteId => SendNotekeeperMessage(NotekeeperUiAction.Edit, noteId: noteId, userInterface: userInterface);
        _fragment.OnBackButtonPressed += () => SendNotekeeperMessage(NotekeeperUiAction.BackToList, userInterface: userInterface);
        _fragment.OnCreateNewNote += () => SendNotekeeperMessage(NotekeeperUiAction.CreateNew, userInterface: userInterface);
        _fragment.OnSaveNote += (noteId, title, content) => SendNotekeeperMessage(NotekeeperUiAction.SaveNote, noteId: noteId, title: title, content: content, userInterface: userInterface);
        _fragment.OnDeleteNote += noteId => SendNotekeeperMessage(NotekeeperUiAction.Remove, noteId: noteId, userInterface: userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not NotekeeperUiState notekeeperState)
            return;

        if (notekeeperState.EditingNoteId.HasValue)
        {
            var note = notekeeperState.Notes.FirstOrDefault(n => n.Id == notekeeperState.EditingNoteId.Value);
            if (note != null)
            {
                _fragment?.UpdateEditorState(note);
                return;
            }
        }
        if (notekeeperState.ViewingNoteId.HasValue)
        {
            var note = notekeeperState.Notes.FirstOrDefault(n => n.Id == notekeeperState.ViewingNoteId.Value);
            if (note != null)
            {
                _fragment?.UpdateViewState(note);
                return;
            }
        }
        _fragment?.UpdateListState(notekeeperState.Notes);
    }

    private void SendNotekeeperMessage(NotekeeperUiAction action, int? noteId = null, string? title = null, string? content = null, BoundUserInterface? userInterface = null)
    {
        var notekeeperMessage = new NotekeeperUiMessageEvent(action, null, noteId, title, content);
        var message = new CartridgeUiMessage(notekeeperMessage);
        userInterface?.SendMessage(message);
    }
}
