using Content.Client.UserInterface.Fragments;
using Content.Shared._Eternal.Economy;
using Content.Shared.CartridgeLoader;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Eternal.Economy.UI;

[UsedImplicitly]
public sealed partial class BankUi : UIFragment
{
    private BankUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new BankUiFragment();

        _fragment.OnLinkAttempt += message => userInterface.SendMessage(new CartridgeUiMessage(message));
        _fragment.OnChangePinAttempt += message => userInterface.SendMessage(new CartridgeUiMessage(message));
        _fragment.OnTransferAttempt += message => userInterface.SendMessage(new CartridgeUiMessage(message));
        _fragment.OnRefreshRequested += () => userInterface.SendMessage(new CartridgeUiMessage(new CartridgeUiRefreshMessage()));
        _fragment.OnRequestHistory += (accountId, count) =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(new BankTransactionHistoryRequestMessage(accountId, count)));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is BankTransactionHistoryResponseMessage history)
        {
            _fragment?.ShowTransactionHistory(history.Records);
            return;
        }
        if (state is not BankCartridgeUiState bankState)
            return;

        if (_fragment != null && !_fragment.HistoryMode)
            _fragment.UpdateState(bankState);
    }
}
