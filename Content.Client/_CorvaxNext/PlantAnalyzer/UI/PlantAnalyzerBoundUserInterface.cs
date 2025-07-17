using Content.Shared._CorvaxNext.PlantAnalyzer;
using JetBrains.Annotations;

namespace Content.Client._CorvaxNext.PlantAnalyzer.UI;

[UsedImplicitly]
public sealed class PlantAnalyzerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PlantAnalyzerWindow? _window;
    private PlantAnalyzerScannedSeedPlantInformation? _pendingInfo;

    public PlantAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new PlantAnalyzerWindow(this)
        {
            Title = Loc.GetString("plant-analyzer-interface-title"),
        };
        _window.OnClose += Close;
        _window.OpenCenteredLeft();

        if (_pendingInfo != null)
        {
            _window.Populate(_pendingInfo);
            _pendingInfo = null;
        }
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is not PlantAnalyzerScannedSeedPlantInformation cast)
            return;

        if (_window == null)
        {
            _pendingInfo = cast;
            return;
        }

        _window.Populate(cast);
    }

    public void AdvPressed(bool scanMode)
    {
        SendMessage(new PlantAnalyzerSetMode(scanMode));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window != null)
            _window.OnClose -= Close;

        _window?.Dispose();
    }
}
