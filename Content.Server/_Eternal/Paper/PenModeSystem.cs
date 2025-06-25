using Content.Shared.Paper;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.IdentityManagement;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared._Eternal.Paper;
using Robust.Server.GameObjects;
using Content.Shared.UserInterface;

namespace Content.Server._Eternal.Paper;

public sealed class PenModeSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PenModeComponent, GetVerbsEvent<InteractionVerb>>(AddModeVerb);
        SubscribeLocalEvent<ChameleonPenComponent, GetVerbsEvent<InteractionVerb>>(AddChameleonVerbs);
        SubscribeLocalEvent<ChameleonPenComponent, BoundUIOpenedEvent>(OnBuiOpened);
        SubscribeLocalEvent<ChameleonPenComponent, ChameleonPenBuiSetMessage>(OnBuiMessage);
    }

    private void AddModeVerb(EntityUid uid, PenModeComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var verb = new InteractionVerb
        {
            Act = () => ToggleMode(uid, comp, args.User),
            Priority = 2,
            Text = Loc.GetString(comp.Mode == PenMode.Write ? "pen-verb-sign" : "pen-verb-write"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Objects/Misc/pens.rsi/pen.png"))
        };
        args.Verbs.Add(verb);
    }

    private void AddChameleonVerbs(EntityUid uid, ChameleonPenComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Глагол для переключения режима
        var modeVerb = new InteractionVerb
        {
            Act = () => ToggleChameleonMode(uid, comp, args.User),
            Priority = 2,
            Text = Loc.GetString(comp.Mode == ChameleonPenMode.Write ? "pen-verb-sign" : "pen-verb-write"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Objects/Misc/pens.rsi/pen.png"))
        };
        args.Verbs.Add(modeVerb);

        // Глагол для подделки подписи
        var forgeVerb = new InteractionVerb
        {
            Act = () => OpenChameleonPenUi(uid, args.User),
            Text = Loc.GetString("chameleon-pen-verb-forge"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Objects/Misc/pens.rsi/pen.png")),
            Priority = 3
        };
        args.Verbs.Add(forgeVerb);
    }

    private void ToggleMode(EntityUid uid, PenModeComponent comp, EntityUid user)
    {
        if (comp.Mode == PenMode.Write)
        {
            comp.Mode = PenMode.Sign;
            _popup.PopupEntity(Loc.GetString("pen-mode-signature-on", ("pen", uid)), uid, user);
        }
        else
        {
            comp.Mode = PenMode.Write;
            _popup.PopupEntity(Loc.GetString("pen-mode-signature-off", ("pen", uid)), uid, user);
        }
        Dirty(uid, comp);
    }

    private void ToggleChameleonMode(EntityUid uid, ChameleonPenComponent comp, EntityUid user)
    {
        if (comp.Mode == ChameleonPenMode.Write)
        {
            comp.Mode = ChameleonPenMode.Sign;
            _popup.PopupEntity(Loc.GetString("pen-mode-signature-on", ("pen", uid)), uid, user);
        }
        else
        {
            comp.Mode = ChameleonPenMode.Write;
            _popup.PopupEntity(Loc.GetString("pen-mode-signature-off", ("pen", uid)), uid, user);
        }
        Dirty(uid, comp);
    }

    private void OpenChameleonPenUi(EntityUid pen, EntityUid user)
    {
        _uiSystem.OpenUi(pen, ChameleonPenUiKey.Key, user);
    }

    private void OnBuiOpened(EntityUid uid, ChameleonPenComponent comp, BoundUIOpenedEvent args)
    {
        var state = new ChameleonPenBuiState(comp.ForgedSignatureColor, comp.ForgedSignatureText);
        _uiSystem.SetUiState(uid, ChameleonPenUiKey.Key, state);
    }

    private void OnBuiMessage(EntityUid uid, ChameleonPenComponent comp, ChameleonPenBuiSetMessage msg)
    {
        comp.ForgedSignatureColor = msg.ForgedSignatureColor;
        comp.ForgedSignatureText = msg.ForgedSignatureText;
        Dirty(uid, comp);
    }
}
