using Content.Server.Hands.Systems;
using Content.Shared._Eternal.Economy;
using Content.Shared.Access.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Content.Server.GameTicking;
using Robust.Shared.Timing;

namespace Content.Server._Eternal.Economy;

public sealed class EftposSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly BankCardSystem _bankCardSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly HandsSystem _sharedHandsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EftposComponent, EftposLockMessage>(OnLock);
        SubscribeLocalEvent<EftposComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, EftposComponent component, InteractUsingEvent args)
    {
        if (component.BankAccountId == null
            || !TryComp(args.Used, out BankCardComponent? bankCard)
            || bankCard.AccountId == component.BankAccountId
            || component.Amount <= 0
            || bankCard.CommandBudgetCard)
            return;

        int? enteredPin = null;

        if (bankCard is IEftposPinProvider pinProvider)
            enteredPin = pinProvider.Pin;

        if (enteredPin != null)
        {
            if (!_bankCardSystem.TryGetAccount(bankCard.AccountId!.Value, out var account))
            {
                _popupSystem.PopupEntity(Loc.GetString("eftpos-transaction-error"), uid);
                _audioSystem.PlayPvs(component.SoundDeny, uid);
                return;
            }
            if ((bankCard.Pin ?? account.AccountPin) != enteredPin)
            {
                _popupSystem.PopupEntity(Loc.GetString("eftpos-wrong-pin"), uid);
                _audioSystem.PlayPvs(component.SoundDeny, uid);
                return;
            }
        }

        if (_bankCardSystem.TryChangeBalance(bankCard.AccountId!.Value, -component.Amount) &&
            _bankCardSystem.TryChangeBalance(component.BankAccountId.Value, component.Amount))
        {
            _popupSystem.PopupEntity(Loc.GetString("eftpos-transaction-success"), uid);
            _audioSystem.PlayPvs(component.SoundApply, uid);

            if (_bankCardSystem.TryGetAccount(bankCard.AccountId.Value, out var payerAccount))
            {
                string receiverName = component.BankAccountId.Value.ToString();
                if (_bankCardSystem.TryGetAccount(component.BankAccountId.Value, out var receiverAcc))
                    receiverName = string.IsNullOrWhiteSpace(receiverAcc.Name) ? receiverAcc.AccountId.ToString() : receiverAcc.Name;
                payerAccount.AddTransaction(new TransactionRecord(
                    TransactionRecord.TransactionType.Purchase,
                    $"Безналичная оплата, счет: {component.BankAccountId.Value} ({receiverName})",
                    -component.Amount,
                    Robust.Shared.Maths.Color.Red,
                    DateTime.MinValue.Add(_timing.CurTime.Subtract(_gameTicker.RoundStartTimeSpan))
                ));
            }
            if (_bankCardSystem.TryGetAccount(component.BankAccountId.Value, out var receiverAccount))
            {
                string payerName = bankCard.AccountId.Value.ToString();
                if (_bankCardSystem.TryGetAccount(bankCard.AccountId.Value, out var payerAcc))
                    payerName = string.IsNullOrWhiteSpace(payerAcc.Name) ? payerAcc.AccountId.ToString() : payerAcc.Name;
                receiverAccount.AddTransaction(new TransactionRecord(
                    TransactionRecord.TransactionType.Purchase,
                    $"Безналичная оплата, счет: {bankCard.AccountId.Value} ({payerName})",
                    component.Amount,
                    Robust.Shared.Maths.Color.Lime,
                    DateTime.MinValue.Add(_timing.CurTime.Subtract(_gameTicker.RoundStartTimeSpan))
                ));
            }
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
