using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.UserInterface;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Random.Helpers;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;
using static Content.Shared.Paper.PaperComponent;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Robust.Shared.Timing;
using Robust.Shared.Maths;
using Content.Shared._Eternal.Paper;
using Robust.Shared.Audio;
using System.Text.RegularExpressions;

namespace Content.Shared.Paper;

public sealed class PaperSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<TagPrototype> WriteIgnoreStampsTag = "WriteIgnoreStamps";
    private static readonly ProtoId<TagPrototype> WriteIgnoreSignsTag = "WriteIgnoreSigns";
    private static readonly ProtoId<TagPrototype> WriteTag = "Write";

    private EntityQuery<PaperComponent> _paperQuery;
    private Dictionary<EntityUid, TimeSpan> _penCooldowns = new();
    private Dictionary<EntityUid, TimeSpan> _errorCooldowns = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PaperComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PaperComponent, BeforeActivatableUIOpenEvent>(BeforeUIOpen);
        SubscribeLocalEvent<PaperComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PaperComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PaperComponent, PaperInputTextMessage>(OnInputTextMessage);

        SubscribeLocalEvent<RandomPaperContentComponent, MapInitEvent>(OnRandomPaperContentMapInit);

        SubscribeLocalEvent<ActivateOnPaperOpenedComponent, PaperWriteEvent>(OnPaperWrite);

        _paperQuery = GetEntityQuery<PaperComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Clean up old cooldown entries every 30 seconds
        if (_timing.CurTime.TotalSeconds % 30 < frameTime)
        {
            CleanupCooldowns();
        }
    }

    private void OnMapInit(Entity<PaperComponent> entity, ref MapInitEvent args)
    {
        if (!string.IsNullOrEmpty(entity.Comp.Content))
        {
            SetContent(entity, Loc.GetString(entity.Comp.Content));
        }
    }

    private void OnInit(Entity<PaperComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Mode = PaperAction.Read;
        UpdateUserInterface(entity);

        if (TryComp<AppearanceComponent>(entity, out var appearance))
        {
            if (entity.Comp.Content != "")
                _appearance.SetData(entity, PaperVisuals.Status, PaperStatus.Written, appearance);

            if (entity.Comp.StampState != null)
                _appearance.SetData(entity, PaperVisuals.Stamp, entity.Comp.StampState, appearance);
        }
    }

    private void BeforeUIOpen(Entity<PaperComponent> entity, ref BeforeActivatableUIOpenEvent args)
    {
        entity.Comp.Mode = PaperAction.Read;
        UpdateUserInterface(entity);
    }

    private void OnExamined(Entity<PaperComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(PaperComponent)))
        {
            if (entity.Comp.Content != "")
            {
                args.PushMarkup(
                    Loc.GetString(
                        "paper-component-examine-detail-has-words",
                        ("paper", entity)
                    )
                );
            }

            if (entity.Comp.StampedBy.Count > 0)
            {
                var commaSeparated =
                    string.Join(", ", entity.Comp.StampedBy.Select(s => Loc.GetString(s.StampedName)));
                args.PushMarkup(
                    Loc.GetString(
                        "paper-component-examine-detail-stamped-by",
                        ("paper", entity),
                        ("stamps", commaSeparated))
                );
            }

            // Выводим подписи (SingBy)
            if (entity.Comp.SingBy.Count > 0)
            {
                if (!EntityManager.TryGetComponent(entity.Owner, out MetaDataComponent? meta))
                    return;
                var paperName = Loc.GetString(meta.EntityName);
                var names = entity.Comp.SingBy.Select(s => Loc.GetString(s.StampedName));
                var commaSeparated = string.Join(", ", names);
                args.PushMarkup($"На {paperName} имеются следующие подписи: {commaSeparated}");
            }

            // Corvax-Next-FaxMark-Start
            if (entity.Comp.Sender is not null)
                args.PushMarkup(Loc.GetString("paper-component-examine-detail-sender", ("fax", entity.Comp.Sender)));
            // Corvax-Next-FaxMark-End
        }
    }

    private static StampDisplayInfo GetStampInfo(StampComponent stamp)
    {
        return new StampDisplayInfo
        {
            StampedName = stamp.StampedName,
            StampedColor = stamp.StampedColor
        };
    }

    private void OnInteractUsing(Entity<PaperComponent> entity, ref InteractUsingEvent args)
    {
        // only allow editing if there are no stamps or when using a cyberpen
        var hasWriteIgnoreStamps = _tagSystem.HasTag(args.Used, WriteIgnoreStampsTag);
        var hasWriteIgnoreSigns = _tagSystem.HasTag(args.Used, WriteIgnoreSignsTag);
        var editable = (hasWriteIgnoreStamps || entity.Comp.StampedBy.Count == 0) &&
                      (hasWriteIgnoreSigns || entity.Comp.SingBy.Count == 0);

        // PenModeComponent (обычная ручка)
        if (TryComp<PenModeComponent>(args.Used, out var penMode))
        {
            if (penMode.Mode == PenMode.Write)
            {
                if (_tagSystem.HasTag(args.Used, WriteTag))
                {
                    if (editable)
                    {
                        if (entity.Comp.EditingDisabled)
                        {
                            var paperEditingDisabledMessage = Loc.GetString("paper-tamper-proof-modified-message");
                            _popupSystem.PopupClient(paperEditingDisabledMessage, entity, args.User);
                            args.Handled = true;
                            return;
                        }
                        var ev = new PaperWriteAttemptEvent(entity.Owner);
                        RaiseLocalEvent(args.User, ref ev);
                        if (ev.Cancelled)
                        {
                            if (ev.FailReason is not null)
                            {
                                var fileWriteMessage = Loc.GetString(ev.FailReason);
                                _popupSystem.PopupClient(fileWriteMessage, entity.Owner, args.User);
                            }
                            args.Handled = true;
                            return;
                        }
                        var writeEvent = new PaperWriteEvent(args.User, entity);
                        RaiseLocalEvent(args.Used, ref writeEvent);
                        entity.Comp.Mode = PaperAction.Write;
                        _uiSystem.OpenUi(entity.Owner, PaperUiKey.Key, args.User);
                        UpdateUserInterface(entity);
                    }
                    args.Handled = true;
                    return;
                }
            }
            else if (penMode.Mode == PenMode.Sign)
            {
                args.Handled = true;
                var ownerName = Identity.Name(args.User, EntityManager);
                var info = new StampDisplayInfo
                {
                    StampedName = ownerName,
                    StampedColor = penMode.SignatureColor
                };

                // Check signature limits
                var signRepeatLimit = GetSignatureRepeatLimit(entity.Comp.Content);
                var signLimit = GetSignatureLimit(entity.Comp.Content);

                // First check total signature limit
                if (signLimit > 0 && entity.Comp.SingBy.Count >= signLimit)
                {
                    // Check error cooldown to prevent spam
                    var now = _timing.CurTime;
                    if (_errorCooldowns.TryGetValue(args.User, out var lastErrorTime))
                    {
                        if (now < lastErrorTime + TimeSpan.FromSeconds(2))
                            return;
                    }
                    _errorCooldowns[args.User] = now;

                    _popupSystem.PopupEntity(
                        Loc.GetString("pen-signature-total-limit-reached", ("limit", signLimit)),
                        entity.Owner);
                    return;
                }

                // Then check repeat limit for this specific signature
                var existingCount = CountExistingSignatures(entity, ownerName, penMode.SignatureColor);
                if (existingCount >= signRepeatLimit)
                {
                    // Check error cooldown to prevent spam
                    var now = _timing.CurTime;
                    if (_errorCooldowns.TryGetValue(args.User, out var lastErrorTime))
                    {
                        if (now < lastErrorTime + TimeSpan.FromSeconds(2))
                            return;
                    }
                    _errorCooldowns[args.User] = now;

                    _popupSystem.PopupEntity(
                        Loc.GetString("pen-signature-repeat-limit-reached", ("name", ownerName), ("limit", signRepeatLimit)),
                        entity.Owner);
                    return;
                }

                var now2 = _timing.CurTime;
                if (_penCooldowns.TryGetValue(args.Used, out var lastTime))
                {
                    if (now2 < lastTime + TimeSpan.FromSeconds(1))
                        return;
                }
                _penCooldowns[args.Used] = now2;
                entity.Comp.SingBy.Add(info);
                Dirty(entity);

                // Show success message to everyone (including the signer)
                _popupSystem.PopupEntity(
                    Loc.GetString("pen-signature-success", ("name", ownerName)),
                    entity.Owner);

                UpdateUserInterface(entity);
                _audio.PlayPvs(new SoundCollectionSpecifier("PaperScribbles"), entity.Owner);
                return;
            }
        }

        // ChameleonPenComponent (хамелеон-ручка)
        if (TryComp<ChameleonPenComponent>(args.Used, out var chameleonPen))
        {
            if (chameleonPen.Mode == ChameleonPenMode.Write)
            {
                if (_tagSystem.HasTag(args.Used, WriteTag))
                {
                    if (editable)
                    {
                        if (entity.Comp.EditingDisabled)
                        {
                            var paperEditingDisabledMessage = Loc.GetString("paper-tamper-proof-modified-message");
                            _popupSystem.PopupClient(paperEditingDisabledMessage, entity, args.User);
                            args.Handled = true;
                            return;
                        }
                        var ev = new PaperWriteAttemptEvent(entity.Owner);
                        RaiseLocalEvent(args.User, ref ev);
                        if (ev.Cancelled)
                        {
                            if (ev.FailReason is not null)
                            {
                                var fileWriteMessage = Loc.GetString(ev.FailReason);
                                _popupSystem.PopupClient(fileWriteMessage, entity.Owner, args.User);
                            }
                            args.Handled = true;
                            return;
                        }
                        var writeEvent = new PaperWriteEvent(args.User, entity);
                        RaiseLocalEvent(args.Used, ref writeEvent);
                        entity.Comp.Mode = PaperAction.Write;
                        _uiSystem.OpenUi(entity.Owner, PaperUiKey.Key, args.User);
                        UpdateUserInterface(entity);
                    }
                    args.Handled = true;
                    return;
                }
            }
            else if (chameleonPen.Mode == ChameleonPenMode.Sign)
            {
                args.Handled = true;
                string ownerName;
                Color signatureColor;
                if (!string.IsNullOrEmpty(chameleonPen.ForgedSignatureText))
                {
                    ownerName = chameleonPen.ForgedSignatureText;
                    signatureColor = chameleonPen.ForgedSignatureColor ?? Color.FromHex("#3166f5");
                }
                else
                {
                    ownerName = Identity.Name(args.User, EntityManager);
                    signatureColor = chameleonPen.ForgedSignatureColor ?? Color.FromHex("#3166f5");
                }
                var info = new StampDisplayInfo
                {
                    StampedName = ownerName,
                    StampedColor = signatureColor
                };

                // Check signature limits
                var signRepeatLimit = GetSignatureRepeatLimit(entity.Comp.Content);
                var signLimit = GetSignatureLimit(entity.Comp.Content);

                // First check total signature limit
                if (signLimit > 0 && entity.Comp.SingBy.Count >= signLimit)
                {
                    // Check error cooldown to prevent spam
                    var now = _timing.CurTime;
                    if (_errorCooldowns.TryGetValue(args.User, out var lastErrorTime))
                    {
                        if (now < lastErrorTime + TimeSpan.FromSeconds(2))
                            return;
                    }
                    _errorCooldowns[args.User] = now;

                    _popupSystem.PopupEntity(
                        Loc.GetString("pen-signature-total-limit-reached", ("limit", signLimit)),
                        entity.Owner);
                    return;
                }

                // Then check repeat limit for this specific signature
                var existingCount = CountExistingSignatures(entity, ownerName, signatureColor);
                if (existingCount >= signRepeatLimit)
                {
                    // Check error cooldown to prevent spam
                    var now = _timing.CurTime;
                    if (_errorCooldowns.TryGetValue(args.User, out var lastErrorTime))
                    {
                        if (now < lastErrorTime + TimeSpan.FromSeconds(2))
                            return;
                    }
                    _errorCooldowns[args.User] = now;

                    _popupSystem.PopupEntity(
                        Loc.GetString("pen-signature-repeat-limit-reached", ("name", ownerName), ("limit", signRepeatLimit)),
                        entity.Owner);
                    return;
                }

                var now2 = _timing.CurTime;
                if (_penCooldowns.TryGetValue(args.Used, out var lastTime))
                {
                    if (now2 < lastTime + TimeSpan.FromSeconds(1))
                        return;
                }
                _penCooldowns[args.Used] = now2;
                entity.Comp.SingBy.Add(info);
                Dirty(entity);

                // Show success message to everyone (including the signer)
                _popupSystem.PopupEntity(
                    Loc.GetString("pen-signature-success", ("name", ownerName)),
                    entity.Owner);

                UpdateUserInterface(entity);
                _audio.PlayPvs(new SoundCollectionSpecifier("PaperScribbles"), entity.Owner);
                return;
            }
        }

        // Обычные пишущие инструменты
        if (_tagSystem.HasTag(args.Used, WriteTag))
        {
            if (editable)
            {
                if (entity.Comp.EditingDisabled)
                {
                    var paperEditingDisabledMessage = Loc.GetString("paper-tamper-proof-modified-message");
                    _popupSystem.PopupClient(paperEditingDisabledMessage, entity, args.User);
                    args.Handled = true;
                    return;
                }
                var ev = new PaperWriteAttemptEvent(entity.Owner);
                RaiseLocalEvent(args.User, ref ev);
                if (ev.Cancelled)
                {
                    if (ev.FailReason is not null)
                    {
                        var fileWriteMessage = Loc.GetString(ev.FailReason);
                        _popupSystem.PopupClient(fileWriteMessage, entity.Owner, args.User);
                    }
                    args.Handled = true;
                    return;
                }
                var writeEvent = new PaperWriteEvent(args.User, entity);
                RaiseLocalEvent(args.Used, ref writeEvent);
                entity.Comp.Mode = PaperAction.Write;
                _uiSystem.OpenUi(entity.Owner, PaperUiKey.Key, args.User);
                UpdateUserInterface(entity);
            }
            args.Handled = true;
            return;
        }

        // Штампы
        if (TryComp<StampComponent>(args.Used, out var stampComp) && TryStamp(entity, GetStampInfo(stampComp), stampComp.StampState))
        {
            // успешно поставлен штамп
            var stampPaperOtherMessage = Loc.GetString("paper-component-action-stamp-paper-other",
                    ("user", args.User),
                    ("target", args.Target),
                    ("stamp", args.Used));

            _popupSystem.PopupEntity(stampPaperOtherMessage, args.User, Filter.PvsExcept(args.User, entityManager: EntityManager), true);
            var stampPaperSelfMessage = Loc.GetString("paper-component-action-stamp-paper-self",
                    ("target", args.Target),
                    ("stamp", args.Used));
            _popupSystem.PopupClient(stampPaperSelfMessage, args.User, args.User);

            _audio.PlayPredicted(stampComp.Sound, entity, args.User);

            UpdateUserInterface(entity);
            args.Handled = true;
            return;
        }
    }

    private void OnInputTextMessage(Entity<PaperComponent> entity, ref PaperInputTextMessage args)
    {
        var ev = new PaperWriteAttemptEvent(entity.Owner);
        RaiseLocalEvent(args.Actor, ref ev);
        if (ev.Cancelled)
            return;

        if (args.Text.Length <= entity.Comp.ContentSize)
        {
            SetContent(entity, args.Text);

            var paperStatus = string.IsNullOrWhiteSpace(args.Text) ? PaperStatus.Blank : PaperStatus.Written;

            if (TryComp<AppearanceComponent>(entity, out var appearance))
                _appearance.SetData(entity, PaperVisuals.Status, paperStatus, appearance);

            if (TryComp(entity, out MetaDataComponent? meta))
                _metaSystem.SetEntityDescription(entity, "", meta);

            _adminLogger.Add(LogType.Chat,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} has written on {ToPrettyString(entity):entity} the following text: {args.Text}");

            _audio.PlayPvs(entity.Comp.Sound, entity);
        }

        entity.Comp.Mode = PaperAction.Read;
        UpdateUserInterface(entity);
    }

    private void OnRandomPaperContentMapInit(Entity<RandomPaperContentComponent> ent, ref MapInitEvent args)
    {
        if (!_paperQuery.TryComp(ent, out var paperComp))
        {
            Log.Warning($"{EntityManager.ToPrettyString(ent)} has a {nameof(RandomPaperContentComponent)} but no {nameof(PaperComponent)}!");
            RemCompDeferred(ent, ent.Comp);
            return;
        }
        var dataset = _protoMan.Index(ent.Comp.Dataset);
        // Intentionally not using the Pick overload that directly takes a LocalizedDataset,
        // because we want to get multiple attributes from the same pick.
        var pick = _random.Pick(dataset.Values);

        // Name
        _metaSystem.SetEntityName(ent, Loc.GetString(pick));
        // Description
        _metaSystem.SetEntityDescription(ent, Loc.GetString($"{pick}.desc"));
        // Content
        SetContent((ent, paperComp), Loc.GetString($"{pick}.content"));

        // Our work here is done
        RemCompDeferred(ent, ent.Comp);
    }

    private void OnPaperWrite(Entity<ActivateOnPaperOpenedComponent> entity, ref PaperWriteEvent args)
    {
        _interaction.UseInHandInteraction(args.User, entity);
    }

    /// <summary>
    ///     Accepts the name and state to be stamped onto the paper, returns true if successful.
    /// </summary>
    public bool TryStamp(Entity<PaperComponent> entity, StampDisplayInfo stampInfo, string spriteStampState)
    {
        if (!entity.Comp.StampedBy.Contains(stampInfo))
        {
            entity.Comp.StampedBy.Add(stampInfo);
            Dirty(entity);
            if (entity.Comp.StampState == null && TryComp<AppearanceComponent>(entity, out var appearance))
            {
                entity.Comp.StampState = spriteStampState;
                // Would be nice to be able to display multiple sprites on the paper
                // but most of the existing images overlap
                _appearance.SetData(entity, PaperVisuals.Stamp, entity.Comp.StampState, appearance);
            }
        }
        return true;
    }

    /// <summary>
    ///     Copy any stamp information from one piece of paper to another.
    /// </summary>
    public void CopyStamps(Entity<PaperComponent?> source, Entity<PaperComponent?> target)
    {
        if (!Resolve(source, ref source.Comp) || !Resolve(target, ref target.Comp))
            return;

        target.Comp.StampedBy = new List<StampDisplayInfo>(source.Comp.StampedBy);
        target.Comp.StampState = source.Comp.StampState;
        Dirty(target);

        if (TryComp<AppearanceComponent>(target, out var appearance))
        {
            // delete any stamps if the stamp state is null
            _appearance.SetData(target, PaperVisuals.Stamp, target.Comp.StampState ?? "", appearance);
        }
    }

    public void SetContent(EntityUid entity, string content)
    {
        if (!TryComp<PaperComponent>(entity, out var paper))
            return;
        SetContent((entity, paper), content);
    }

    public void SetContent(Entity<PaperComponent> entity, string content)
    {
        entity.Comp.Content = content;
        Dirty(entity);
        UpdateUserInterface(entity);

        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        var status = string.IsNullOrWhiteSpace(content)
            ? PaperStatus.Blank
            : PaperStatus.Written;

        _appearance.SetData(entity, PaperVisuals.Status, status, appearance);
    }

    private void UpdateUserInterface(Entity<PaperComponent> entity)
    {
        _uiSystem.SetUiState(entity.Owner, PaperUiKey.Key, new PaperBoundUserInterfaceState(entity.Comp.Content, entity.Comp.StampedBy, entity.Comp.SingBy, entity.Comp.Mode));
    }

    /// <summary>
    /// Parses the sign_repeat_limit tag from paper content and returns the limit value.
    /// Default is 1 if no tag is found or invalid value.
    /// </summary>
    private int GetSignatureRepeatLimit(string content)
    {
        var match = Regex.Match(content, @"<sign_repeat_limit\s*=\s*(\d+)>", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int limit) && limit > 0)
        {
            return limit;
        }
        return 1; // Default limit
    }

    /// <summary>
    /// Parses the sign_limit tag from paper content and returns the limit value.
    /// Default is -1 if no tag is found or invalid value.
    /// </summary>
    private int GetSignatureLimit(string content)
    {
        var match = Regex.Match(content, @"<sign_limit\s*=\s*(\d+)>", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int limit) && limit > 0)
        {
            return limit;
        }
        return -1; // No limit
    }

    /// <summary>
    /// Counts how many signatures with the same name and color already exist on the paper.
    /// </summary>
    private int CountExistingSignatures(Entity<PaperComponent> entity, string signatureName, Color signatureColor)
    {
        var key = $"{signatureName}|{signatureColor.ToHexNoAlpha()}";
        return entity.Comp.SingBy.Count(sig => $"{sig.StampedName}|{sig.StampedColor.ToHexNoAlpha()}" == key);
    }

    /// <summary>
    /// Cleans up old cooldown entries to prevent memory leaks.
    /// </summary>
    private void CleanupCooldowns()
    {
        var now = _timing.CurTime;
        var cutoffTime = now - TimeSpan.FromMinutes(5);

        // Clean up pen cooldowns
        var expiredPenCooldowns = _penCooldowns.Where(kvp => kvp.Value < cutoffTime).ToList();
        foreach (var kvp in expiredPenCooldowns)
        {
            _penCooldowns.Remove(kvp.Key);
        }

        // Clean up error cooldowns
        var expiredErrorCooldowns = _errorCooldowns.Where(kvp => kvp.Value < cutoffTime).ToList();
        foreach (var kvp in expiredErrorCooldowns)
        {
            _errorCooldowns.Remove(kvp.Key);
        }
    }
}

/// <summary>
/// Event fired when using a pen on paper, opening the UI.
/// </summary>
[ByRefEvent]
public record struct PaperWriteEvent(EntityUid User, EntityUid Paper);

/// <summary>
/// Cancellable event for attempting to write on a piece of paper.
/// </summary>
/// <param name="paper">The paper that the writing will take place on.</param>
[ByRefEvent]
public record struct PaperWriteAttemptEvent(EntityUid Paper, string? FailReason = null, bool Cancelled = false);
