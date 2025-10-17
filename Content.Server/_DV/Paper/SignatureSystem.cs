// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Solstice <solsticeofthewinter@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Paper;
using Content.Goobstation.Shared.Devil;
using Content.Server.Access.Systems;
using Content.Server.Popups;
using Content.Shared.Paper;
using Content.Server.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Shared.Player;
using System.Text.RegularExpressions;
using System.Linq;

namespace Content.Server._DV.Paper;

public sealed class SignatureSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    //Vortex added
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    //Vortex end
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    // The sprite used to visualize "signatures" on paper entities.
    private const string SignatureStampState = "paper_stamp-signature";

    public override void Initialize()
    {
        SubscribeLocalEvent<PaperComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    private void OnGetAltVerbs(Entity<PaperComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (args.Using is not {} pen || !_tags.HasTag(pen, "Write"))
            return;

        var user = args.User;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TrySignPaper(ent, user, pen);
            },
            Text = Loc.GetString("paper-sign-verb"),
            DoContactInteraction = true,
            Priority = 10
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Tries to add a signature to the paper with signer's name.
    /// </summary>
    public bool TrySignPaper(Entity<PaperComponent> paper, EntityUid signer, EntityUid pen)
    {
        var comp = paper.Comp;

        var ev = new SignAttemptEvent(paper, signer);
        RaiseLocalEvent(pen, ref ev);
        if (ev.Cancelled)
            return false;

        var paperEvent = new BeingSignedAttemptEvent(paper, signer); // Goobstation
        RaiseLocalEvent(paper.Owner, ref paperEvent);
        if (paperEvent.Cancelled)
            return false;

        var signatureName = DetermineEntitySignature(signer);

        //Vortex added
        // Parse controls from paper content
        var content = comp.Content ?? string.Empty;
        var repeatLimit = ParseIntTag(content, "sign_repeat_limit") ?? 1;
        var totalLimit = ParseIntTag(content, "sign_limit") ?? int.MaxValue;
        var hasPlaceholders = HasSignPlaceholders(content);

        // Enforce total signature limit
        if (comp.StampedBy.Count >= totalLimit)
        {
            _popup.PopupEntity(Loc.GetString("paper-signed-failure", ("target", paper.Owner)), signer, signer, PopupType.SmallCaution);
            return false;
        }

        // Enforce per-signer repeat limit (count by name)
        var existingByThisSigner = comp.StampedBy.Count(s => s.StampedName == signatureName);
        if (existingByThisSigner >= repeatLimit)
        {
            _popup.PopupEntity(Loc.GetString("paper-signed-failure", ("target", paper.Owner)), signer, signer, PopupType.SmallCaution);
            return false;
        }

        var stampInfo = new StampDisplayInfo()
        {
            StampedName = signatureName,
            StampedColor = Color.DarkSlateGray, //TODO Make this configurable depending on the pen.
        };

        // If placeholders exist, add entry but avoid visual stamp sprite; also allow duplicate entries up to repeatLimit.
        var allowDuplicate = repeatLimit > 1;
        var spriteState = hasPlaceholders ? null : SignatureStampState;
        //Vortex added
        if (hasPlaceholders)
        {
            // Clear existing world overlay so only textual signatures are visible
            comp.StampState = null;
            if (TryComp<AppearanceComponent>(paper, out var appearance))
                _appearance.SetData(paper, PaperComponent.PaperVisuals.Stamp, "", appearance);
            Dirty(paper);
        }
        //Vortex end
        //Vortex end

        if (_paper.TryAddStampInfo(paper, stampInfo, spriteState, allowDuplicate))
        {
            // Show popups and play a paper writing sound
            if (!HasComp<DevilComponent>(signer)) // Goobstation - Don't display popups for devils, it covers the others.
            {
                var signedOtherMessage = Loc.GetString("paper-signed-other", ("user", signer), ("target", paper.Owner));
                _popup.PopupEntity(signedOtherMessage, signer, Filter.PvsExcept(signer, entityManager: EntityManager), true);

                var signedSelfMessage = Loc.GetString("paper-signed-self", ("target", paper.Owner));
                _popup.PopupEntity(signedSelfMessage, signer, signer);
            }

            _audio.PlayPvs(comp.Sound, signer);

            _paper.UpdateUserInterface(paper);

            var evSignSucessfulEvent = new SignSuccessfulEvent(paper, signer); // Goobstation - Devil Antagonist
            RaiseLocalEvent(paper, ref evSignSucessfulEvent); // Goobstation - Devil Antagonist

            return true;
        }
        else
        {
            // Show an error popup
            _popup.PopupEntity(Loc.GetString("paper-signed-failure", ("target", paper.Owner)), signer, signer, PopupType.SmallCaution);

            return false;
        }
    }

    //Vortex added
    private static int? ParseIntTag(string content, string tag)
    {
        // Matches <tag=number> or [tag=number]
        var m1 = Regex.Match(content, $"<\\s*{Regex.Escape(tag)}\\s*=\\s*(\\d+)\\s*>");
        if (m1.Success && int.TryParse(m1.Groups[1].Value, out var val1))
            return val1;
        var m2 = Regex.Match(content, $"\\[\\s*{Regex.Escape(tag)}\\s*=\\s*(\\d+)\\s*\\]");
        if (m2.Success && int.TryParse(m2.Groups[1].Value, out var val2))
            return val2;
        return null;
    }

    private static bool HasSignPlaceholders(string content)
    {
        return Regex.IsMatch(content, "(<\\s*sign\\s*=\\s*\\d+\\s*>)|(\\[\\s*sign\\s*=\\s*\\d+\\s*\\])");
    }
    //Vortex end

    private string DetermineEntitySignature(EntityUid uid)
    {
        // Goobstation - Allow devils to sign their true name.
        if (TryComp<DevilComponent>(uid, out var devilComp) && !string.IsNullOrWhiteSpace(devilComp.TrueName))
            return devilComp.TrueName;

        // If the entity has an ID, use the name on it.
        if (_idCard.TryFindIdCard(uid, out var id) && !string.IsNullOrWhiteSpace(id.Comp.FullName))
            return id.Comp.FullName;

        // Alternatively, return the entity name
        return Name(uid);
    }
}
