using Content.Shared.CriminalRecords.Components;
using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;
using Content.Shared._CD.Records;
using JetBrains.Annotations;

namespace Content.Client._CD.Records.UI;

[UsedImplicitly]
public sealed class CharacterRecordConsoleBoundUserInterface(EntityUid owner, Enum key) : BoundUserInterface(owner, key)
{
    [ViewVariables] private CharacterRecordViewer? _window;
    private CharacterRecordConsoleState? _lastState;

    protected override void UpdateState(BoundUserInterfaceState baseState)
    {
        base.UpdateState(baseState);
        if (baseState is not CharacterRecordConsoleState state)
            return;

        _lastState = state;

        if (_window?.IsSecurity() ?? false)
        {
            if (EntMan.HasComponent<CriminalRecordsConsoleComponent>(Owner))
            {
                var comp = EntMan.GetComponent<CriminalRecordsConsoleComponent>(Owner);
                _window!.SecurityWantedStatusMaxLength = comp.MaxStringLength;
            }
        }

        _window?.UpdateState(state);
    }

    protected override void Open()
    {
        base.Open();

        _window = new();
        _window.OnClose += Close;
        _window.OnListingItemSelected += meta =>
        {
            SendMessage(new CharacterRecordConsoleSelectMsg(meta?.CharacterRecordKey));

            // If we are a security records console, we also need to inform the criminal records
            // system of our state.
            if (_window.IsSecurity() && meta?.StationRecordKey != null)
            {
                SendMessage(new SelectStationRecord(meta.Value.StationRecordKey.Value));
                _window.SetSecurityStatusEnabled(true);
            }
            else
            {
                // If the user does not have criminal records for some reason, we should not be able
                // to set their wanted status
                _window.SetSecurityStatusEnabled(false);
            }
        };

        _window.OnFiltersChanged += (ty, txt) =>
        {
            SendMessage(new CharacterRecordsConsoleFilterMsg(new StationRecordsFilter(ty, txt ?? "")));
        };

        _window.OnTabChanged += tabType =>
        {
            SendMessage(new CharacterRecordConsoleTabChangedMsg(tabType));
            // УДАЛЕНО: обновление UI старыми данными — теперь ждем новое состояние от сервера!
        };

        _window.OnSetSecurityStatus += (status, reason) =>
        {
            SendMessage(new CriminalRecordChangeStatus(status, reason));
            // УДАЛЕНО: обновление UI старыми данными — теперь ждем новое состояние от сервера!
        };

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
