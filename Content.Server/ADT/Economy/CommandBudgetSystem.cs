using Content.Shared.Cargo.Components;
using Content.Server.Paper;
using Content.Shared.Paper;
using Content.Server.Station.Systems;

namespace Content.Server.ADT.Economy;

public sealed class CommandBudgetSystem : EntitySystem
{
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CommandBudgetPinPaperComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, CommandBudgetPinPaperComponent component, MapInitEvent args)
    {
        if (!TryComp(_station.GetOwningStation(uid), out Content.Shared.Cargo.Components.StationBankAccountComponent? account))
            return;

        // TODO: The shared StationBankAccountComponent does not have BankAccount.AccountPin. Replace with correct PIN retrieval logic if available.
var pin = "0000"; // Placeholder or retrieve from account.Accounts if structure allows.
        _paper.SetContent(uid,Loc. GetString("command-budget-pin-message", ("pin", pin)));
    }
}
