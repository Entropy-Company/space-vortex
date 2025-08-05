using System.Linq;
using Content.Shared.Cargo.Components;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.UserInterface;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    private bool _allowPrimaryAccountAllocation;
    private bool _allowPrimaryCutAdjustment;

    public void InitializeFunds()
    {
        SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleWithdrawFundsMessage>(OnWithdrawFunds);
        SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleToggleLimitMessage>(OnToggleLimit);
        SubscribeLocalEvent<FundingAllocationConsoleComponent, SetFundingAllocationBuiMessage>(OnSetFundingAllocation);
        SubscribeLocalEvent<FundingAllocationConsoleComponent, BeforeActivatableUIOpenEvent>(OnFundAllocationBuiOpen);

        _cfg.OnValueChanged(CCVars.AllowPrimaryAccountAllocation, enabled => { _allowPrimaryAccountAllocation = enabled; }, true);
        _cfg.OnValueChanged(CCVars.AllowPrimaryCutAdjustment, enabled => { _allowPrimaryCutAdjustment = enabled; }, true);
    }

    private void OnWithdrawFunds(Entity<CargoOrderConsoleComponent> ent, ref CargoConsoleWithdrawFundsMessage args)
    {
        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<Content.Shared.Cargo.Components.StationBankAccountComponent>(station, out var bank))
            return;

        var sharedBank = CompOrNull<Content.Shared.Cargo.Components.StationBankAccountComponent>(station);
        if (sharedBank == null)
            return;

        if (args.Account == ent.Comp.Account ||
            args.Amount <= 0 ||
            args.Amount > GetBalanceFromAccount(new Entity<Content.Shared.Cargo.Components.StationBankAccountComponent?>(station, sharedBank), ent.Comp.Account) * ent.Comp.TransferLimit)
            return;

        if (Timing.CurTime < ent.Comp.NextAccountActionTime)
            return;

        if (!_accessReaderSystem.IsAllowed(args.Actor, ent))
        {
            ConsolePopup(args.Actor, Loc.GetString("cargo-console-order-not-allowed"));
            PlayDenySound(ent, ent.Comp);
            return;
        }

        ent.Comp.NextAccountActionTime = Timing.CurTime + ent.Comp.AccountActionDelay;
        UpdateBankAccount(new Entity<Content.Shared.Cargo.Components.StationBankAccountComponent?>(station, bank), -args.Amount, ent.Comp.Account, dirty: false);
        _audio.PlayPvs(ApproveSound, ent);

        var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(ent, args.Actor);
        RaiseLocalEvent(tryGetIdentityShortInfoEvent);

        var ourAccount = _protoMan.Index(ent.Comp.Account);
        if (args.Account == null)
        {
            var stackPrototype = _protoMan.Index(ent.Comp.CashType);
            _stack.Spawn(args.Amount, stackPrototype, Transform(ent).Coordinates);

            if (!_emag.CheckFlag(ent, EmagType.Interaction))
            {
                var msg = Loc.GetString("cargo-console-fund-withdraw-broadcast",
                    ("name", tryGetIdentityShortInfoEvent.Title ?? Loc.GetString("cargo-console-fund-transfer-user-unknown")),
                    ("amount", args.Amount),
                    ("name1", Loc.GetString(ourAccount.Name)),
                    ("code1", Loc.GetString(ourAccount.Code)));
                _radio.SendRadioMessage(ent, msg, ourAccount.RadioChannel, ent, escapeMarkup: false);
            }
        }
        else
        {
            var otherAccount = _protoMan.Index(args.Account.Value);
            UpdateBankAccount(new Entity<Content.Shared.Cargo.Components.StationBankAccountComponent?>(station, bank), args.Amount, args.Account.Value);

            if (!_emag.CheckFlag(ent, EmagType.Interaction))
            {
                var msg = Loc.GetString("cargo-console-fund-transfer-broadcast",
                    ("name", tryGetIdentityShortInfoEvent.Title ?? Loc.GetString("cargo-console-fund-transfer-user-unknown")),
                    ("amount", args.Amount),
                    ("name1", Loc.GetString(ourAccount.Name)),
                    ("code1", Loc.GetString(ourAccount.Code)),
                    ("name2", Loc.GetString(otherAccount.Name)),
                    ("code2", Loc.GetString(otherAccount.Code)));
                _radio.SendRadioMessage(ent, msg, ourAccount.RadioChannel, ent, escapeMarkup: false);
                _radio.SendRadioMessage(ent, msg, otherAccount.RadioChannel, ent, escapeMarkup: false);
            }
        }
    }

    private void OnToggleLimit(Entity<CargoOrderConsoleComponent> ent, ref CargoConsoleToggleLimitMessage args)
    {
        if (!_accessReaderSystem.FindAccessTags(args.Actor).Intersect(ent.Comp.RemoveLimitAccess).Any())
        {
            ConsolePopup(args.Actor, Loc.GetString("cargo-console-order-not-allowed"));
            PlayDenySound(ent, ent.Comp);
            return;
        }

        _audio.PlayPvs(ent.Comp.ToggleLimitSound, ent);
        ent.Comp.TransferUnbounded = !ent.Comp.TransferUnbounded;
        Dirty(ent);
    }


    private void OnSetFundingAllocation(Entity<FundingAllocationConsoleComponent> ent, ref SetFundingAllocationBuiMessage args)
    {
        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<Content.Shared.Cargo.Components.StationBankAccountComponent>(station, out var bank))
            return;

        var sharedBank = CompOrNull<Content.Shared.Cargo.Components.StationBankAccountComponent>(station);
        if (sharedBank == null)
            return;

        var expectedCount = _allowPrimaryAccountAllocation ? sharedBank.RevenueDistribution.Count : sharedBank.RevenueDistribution.Count - 1;
        if (args.Percents.Count != expectedCount)
            return;

        var differs = false;
        foreach (var (account, percent) in args.Percents)
        {
            if (percent != (int) Math.Round(sharedBank.RevenueDistribution[account] * 100))
            {
                differs = true;
                break;
            }
        }
        differs = differs || args.PrimaryCut != sharedBank.PrimaryCut || args.LockboxCut != sharedBank.LockboxCut;

        if (!differs)
            return;

        if (args.Percents.Values.Sum() != 100)
            return;

        var primaryCut = sharedBank.RevenueDistribution[sharedBank.PrimaryAccount];
        sharedBank.RevenueDistribution.Clear();
        foreach (var (account, percent )in args.Percents)
        {
            sharedBank.RevenueDistribution.Add(account, percent / 100.0);
        }
        if (!_allowPrimaryAccountAllocation)
        {
            sharedBank.RevenueDistribution.Add(sharedBank.PrimaryAccount, 0);
        }

        if (_allowPrimaryCutAdjustment && args.PrimaryCut is >= 0.0 and <= 1.0)
        {
            sharedBank.PrimaryCut = args.PrimaryCut;
        }
        if (_lockboxCutEnabled && args.LockboxCut is >= 0.0 and <= 1.0)
        {
            sharedBank.LockboxCut = args.LockboxCut;
        }

        Dirty(station, bank);

        _audio.PlayPvs(ent.Comp.SetDistributionSound, ent);
        _adminLogger.Add(
            LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set station {ToPrettyString(station)} fund distribution: {string.Join(',', sharedBank.RevenueDistribution.Select(p => $"{p.Key}: {p.Value}").ToList())}, primary cut: {sharedBank.PrimaryCut}, lockbox cut: {sharedBank.LockboxCut}");
    }

    private void OnFundAllocationBuiOpen(Entity<FundingAllocationConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        if (_station.GetOwningStation(ent) is { } station)
            _uiSystem.SetUiState(ent.Owner, FundingAllocationConsoleUiKey.Key, new FundingAllocationConsoleBuiState(GetNetEntity(station)));
    }
}
