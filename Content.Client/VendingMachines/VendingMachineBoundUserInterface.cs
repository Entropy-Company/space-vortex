using Content.Client.UserInterface.Controls;
using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using System.Linq;

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private VendingMachineMenu? _menu;

        [ViewVariables]
        private List<VendingMachineInventoryEntry> _cachedInventory = new();

        public VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new VendingMachineMenu();
            var component = EntMan.GetComponent<VendingMachineComponent>(Owner); //Economy
            var system = EntMan.System<VendingMachineSystem>(); //Economy
            _cachedInventory = system.GetAllInventory(Owner, component); //Economy
            _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

            _menu.OnClose += Close; //Economy
            _menu.OnItemCountSelected += OnItemSelected;    // ADT vending eject count
            _menu.OnWithdraw += SendMessage; //Economy
            _menu.Populate(_cachedInventory, component.PriceMultiplier, component.Credits); //Economy-Tweak

            _menu.OpenCentered();
        }

        public void Refresh()
        {
            var system = EntMan.System<VendingMachineSystem>();
            var component = EntMan.GetComponent<VendingMachineComponent>(Owner); //Economy
            _cachedInventory = system.GetAllInventory(Owner);

            _menu?.Populate(_cachedInventory, component.PriceMultiplier, component.Credits); //Economy-Tweak
        }


        // START-TWEAK
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var system = EntMan.System<VendingMachineSystem>();

            if (state is not VendingMachineInterfaceState newState)
                return;

            _cachedInventory = system.GetAllInventory(Owner);

            _menu?.Populate(_cachedInventory, newState.PriceMultiplier, newState.Credits); //Economy-Tweak
        }

        private void OnItemSelected(VendingMachineInventoryEntry entry, VendingMachineItem item)
        {
            SendPredictedMessage(new VendingMachineEjectCountMessage(entry, item.Count.SelectedId + 1));
        }

        // END-TWEAK

        public void UpdateAmounts()
        {
            var system = EntMan.System<VendingMachineSystem>();
            var component = EntMan.GetComponent<VendingMachineComponent>(Owner);
            _cachedInventory = system.GetAllInventory(Owner);
            _menu?.Populate(_cachedInventory, component.PriceMultiplier, component.Credits);
        }

        private void OnItemSelected(GUIBoundKeyEventArgs args, ListData data)
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            if (data is not VendorItemsListData { ItemIndex: var itemIndex })
                return;

            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(itemIndex);

            if (selectedItem == null)
                return;

            SendPredictedMessage(new VendingMachineEjectMessage(selectedItem.Type, selectedItem.ID));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_menu == null)
                return;

            _menu.OnItemCountSelected -= OnItemSelected;
            _menu.OnClose -= Close;
            _menu.Dispose();
        }
    }
}