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
using Content.Shared.Tag;

namespace Content.Server._Eternal.Paper;

public sealed class PenModeSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PenModeComponent, GetVerbsEvent<InteractionVerb>>(AddModeVerb);
        SubscribeLocalEvent<ChameleonPenComponent, GetVerbsEvent<InteractionVerb>>(AddChameleonVerbs);
        SubscribeLocalEvent<ChameleonPenComponent, BoundUIOpenedEvent>(OnBuiOpened);
        SubscribeLocalEvent<ChameleonPenComponent, ChameleonPenBuiSetMessage>(OnBuiMessage);

        // Добавляем глаголы для обычных ручек
        SubscribeAllEvent<GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(GetVerbsEvent<InteractionVerb> args)
    {
        var uid = args.Target;

        // Проверяем, что это обычная ручка (имеет тег Pen, но не имеет PenModeComponent или ChameleonPenComponent)
        if (!_tagSystem.HasTag(uid, "Pen") ||
            HasComp<PenModeComponent>(uid) ||
            HasComp<ChameleonPenComponent>(uid))
            return;

        if (!args.CanAccess || !args.CanInteract)
            return;

        var signVerb = new InteractionVerb
        {
            Act = () => SignWithNormalPen(uid, args.User),
            Priority = 10,
            Text = Loc.GetString("pen-verb-sign"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Objects/Misc/pens.rsi/pen.png"))
        };
        args.Verbs.Add(signVerb);
    }

    private void SignWithNormalPen(EntityUid pen, EntityUid user)
    {
        // Обычные ручки подписывают обычными чернилами
        // Логика подписи обрабатывается в PaperSystem при взаимодействии с бумагой
        _popup.PopupEntity("Используйте ручку на бумаге для подписи", pen, user);
    }

    private void AddModeVerb(EntityUid uid, PenModeComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var verb = new InteractionVerb
        {
            Act = () => ToggleMode(uid, comp, args.User),
            Priority = 10,
            Text = Loc.GetString(comp.Mode == PenMode.Write ? "pen-verb-sign" : "pen-verb-write"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Objects/Misc/pens.rsi/pen.png"))
        };
        args.Verbs.Add(verb);
    }

    private void AddChameleonVerbs(EntityUid uid, ChameleonPenComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var modeVerb = new InteractionVerb
        {
            Act = () => ToggleChameleonMode(uid, comp, args.User),
            Priority = 10,
            Text = Loc.GetString(comp.Mode == ChameleonPenMode.Write ? "pen-verb-sign" : "pen-verb-write"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Objects/Misc/pens.rsi/pen.png"))
        };
        args.Verbs.Add(modeVerb);

        var forgeVerb = new InteractionVerb
        {
            Act = () => OpenChameleonPenUi(uid, args.User),
            Text = Loc.GetString("chameleon-pen-verb-forge"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Objects/Misc/pens.rsi/pen.png")),
            Priority = 10
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
        var state = new ChameleonPenBuiState(comp.ForgedSignatureColor, comp.ForgedSignatureText, comp.SignatureType);
        _uiSystem.SetUiState(uid, ChameleonPenUiKey.Key, state);
    }

    private void OnBuiMessage(EntityUid uid, ChameleonPenComponent comp, ChameleonPenBuiSetMessage msg)
    {
        comp.ForgedSignatureColor = msg.ForgedSignatureColor;
        comp.ForgedSignatureText = msg.ForgedSignatureText;
        comp.SignatureType = msg.SignatureType;
        Dirty(uid, comp);
    }
}
