using Content.Server.Hands.Systems;
using Content.Shared.ADT.Economy;
using Content.Shared.Access.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.Economy;

public sealed class EftposSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly BankCardSystem _bankCardSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly HandsSystem _sharedHandsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EftposComponent, EftposLockMessage>(OnLock);
        SubscribeLocalEvent<EftposComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, EftposComponent component, InteractUsingEvent args)
    {
        if (component.BankAccountId == null || !TryComp(args.Used, out BankCardComponent? bankCard) ||
            bankCard.AccountId == component.BankAccountId || component.Amount <= 0 || bankCard.CommandBudgetCard)
            return;

        if (_bankCardSystem.TryChangeBalance(bankCard.AccountId!.Value, -component.Amount) &&
            _bankCardSystem.TryChangeBalance(component.BankAccountId.Value, component.Amount))
        {
            _popupSystem.PopupEntity(Loc.GetString("eftpos-transaction-success"), uid);
            _audioSystem.PlayPvs(component.SoundApply, uid);
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("eftpos-transaction-error"), uid);
            _audioSystem.PlayPvs(component.SoundDeny, uid);
        }
    }

    private void OnLock(EntityUid uid, EftposComponent component, EftposLockMessage args)
    {
        if (!TryComp(args.Actor, out HandsComponent? hands))
            return;
        var activeHandId = hands.ActiveHandId;
        if (activeHandId == null)
            return;
        if (!_sharedHandsSystem.TryGetHeldItem((args.Actor, hands), activeHandId, out var activeEntity))
            return;
        if (!TryComp(activeEntity, out BankCardComponent? bankCard))
            return;

        if (component.BankAccountId == null)
        {
            component.BankAccountId = bankCard.AccountId;
            component.Amount = args.Amount;
        }
        else if (component.BankAccountId == bankCard.AccountId)
        {
            component.BankAccountId = null;
            component.Amount = 0;
        }

        EntityUid? ownerEntity = null;
        if (activeHandId != null && _sharedHandsSystem.TryGetHeldItem((args.Actor, hands), activeHandId, out var ent))
            ownerEntity = ent;
        UpdateUiState(uid, component.BankAccountId != null, component.Amount,
            GetOwner(ownerEntity ?? EntityUid.Invalid, component.BankAccountId));
    }

    private string GetOwner(EntityUid uid, int? bankAccountId)
    {
        if (bankAccountId == null || !_bankCardSystem.TryGetAccount(bankAccountId.Value, out var account))
            return string.Empty;

        if (TryComp(uid, out IdCardComponent? idCard) && idCard.FullName != null)
            return idCard.FullName;

        return account.Name == string.Empty ? account.AccountId.ToString() : account.Name;
    }

    private void UpdateUiState(EntityUid uid, bool locked, int amount, string owner)
    {
        var state = new EftposBuiState
        {
            Locked = locked,
            Amount = amount,
            Owner = owner
        };

        _ui.SetUiState(uid, EftposKey.Key, state);
    }
}
