using Robust.Shared.GameStates;

namespace Content.Shared._Eternal.Economy;

[RegisterComponent, NetworkedComponent]
public sealed partial class BankCardComponent : Component, IEftposPinProvider
{
    [DataField]
    public int? AccountId;

    [DataField]
    public int StartingBalance;

    [DataField]
    public bool CommandBudgetCard;

    [DataField]
    public int? Pin;

    int? IEftposPinProvider.Pin => Pin;
}
