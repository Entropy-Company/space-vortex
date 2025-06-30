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
        // Инициализируем панели при первом открытии
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

        // Обновляем только информацию (очки), не пересоздаем панели
        _consoleMenu?.UpdateInformationPanel(castState);

        // При первом получении состояния всегда обновляем панели
        if (!_panelsInitialized && _consoleMenu != null)
        {
            _consoleMenu.UpdatePanels(castState);
            _panelsInitialized = true;
            return;
        }

        // Проверяем, изменились ли очки - если да, то обновляем панели
        if (_consoleMenu != null && _consoleMenu._localState.Points != castState.Points)
        {
            _consoleMenu.UpdatePanels(castState);
        }
    }
}
