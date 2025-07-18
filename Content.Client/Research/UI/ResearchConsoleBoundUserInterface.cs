using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Research.UI;

[UsedImplicitly]
public sealed class ResearchConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ResearchConsoleMenu? _consoleMenu;
    private bool _panelsInitialized = false;

    public ResearchConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        var owner = Owner;

        _consoleMenu = this.CreateWindow<ResearchConsoleMenu>();
        _consoleMenu.SetEntity(owner);

        _consoleMenu.OnTechnologyCardPressed += id =>
        {
            SendMessage(new ConsoleUnlockTechnologyMessage(id));
        };

        _consoleMenu.OnServerButtonPressed += () =>
        {
            SendMessage(new ConsoleServerSelectionMessage());
        };

        _panelsInitialized = false;
        if (State is ResearchConsoleBoundInterfaceState initialState)
        {
            _consoleMenu.UpdatePanels(initialState);
            _consoleMenu.UpdateInformationPanel(initialState);
            _panelsInitialized = true;
        }
    }

    public override void OnProtoReload(PrototypesReloadedEventArgs args)
    {
        base.OnProtoReload(args);

        if (!args.WasModified<TechnologyPrototype>())
            return;

        if (State is not ResearchConsoleBoundInterfaceState rState)
            return;

        _consoleMenu?.UpdatePanels(rState);
        _consoleMenu?.UpdateInformationPanel(rState);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ResearchConsoleBoundInterfaceState castState)
            return;


        if (_consoleMenu != null)
        {
            _consoleMenu.UpdatePanels(castState);
            _consoleMenu.UpdateInformationPanel(castState);
            _panelsInitialized = true;
        }
    }

    private int GetCurrentServerId()
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        if (entMan.TryGetComponent<ResearchClientComponent>(Owner, out var client))
        {
            if (client.Server != null && entMan.TryGetComponent<ResearchServerComponent>(client.Server.Value, out var serverComp))
                return serverComp.Id;
        }
        return -1;
    }
}
