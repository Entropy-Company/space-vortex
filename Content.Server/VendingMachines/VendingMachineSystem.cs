using System.Linq;
using System.Numerics;
using Content.Server.Cargo.Systems;
using Content.Server.Emp;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Robust.Server.GameObjects;
using Content.Server.Store.Components;
using Content.Server.ADT.Economy;
using Content.Shared.ADT.Economy;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Interaction;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.UserInterface;
using Content.Shared.VendingMachines;
using Content.Shared.Cargo;
using Content.Shared.Power;
using Content.Shared.Emag.Systems;
using Content.Server.Advertise.EntitySystems;
using Content.Server.Vocalization.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Advertise.Components;
using Content.Shared.Wall;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.IdentityManagement;

namespace Content.Server.VendingMachines
{
    public sealed class VendingMachineSystem : SharedVendingMachineSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SpeakOnUIClosedSystem _speakOnUIClosed = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        //ADT-Economy-Start
        [Dependency] private readonly BankCardSystem _bankCard = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        //ADT-Economy-End

        private const float WallVendEjectDistanceFromWall = 1f;
        private const double GlobalPriceMultiplier = 2.0; //ADT-Economy

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VendingMachineComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<VendingMachineComponent, BreakageEventArgs>(OnBreak);

            SubscribeLocalEvent<VendingMachineComponent, DamageChangedEvent>(OnDamage); //ADT-Economy
            SubscribeLocalEvent<VendingMachineComponent, PriceCalculationEvent>(OnVendingPrice);
            SubscribeLocalEvent<VendingMachineComponent, EmpPulseEvent>(OnEmpPulse);
            SubscribeLocalEvent<VendingMachineComponent, TryVocalizeEvent>(OnTryVocalize);

            SubscribeLocalEvent<VendingMachineComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);

            SubscribeLocalEvent<VendingMachineComponent, VendingMachineSelfDispenseEvent>(OnSelfDispense);

            SubscribeLocalEvent<VendingMachineComponent, RestockDoAfterEvent>(OnDoAfter);

            //ADT-Economy-Start
            SubscribeLocalEvent<VendingMachineComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<VendingMachineComponent, VendingMachineWithdrawMessage>(OnWithdrawMessage);
            //ADT-Economy-End

            SubscribeLocalEvent<VendingMachineRestockComponent, PriceCalculationEvent>(OnPriceCalculation);
        }

        private void OnVendingPrice(EntityUid uid, VendingMachineComponent component, ref PriceCalculationEvent args)
        {
            var price = 0.0;

            foreach (var entry in component.Inventory.Values)
            {
                if (!PrototypeManager.TryIndex<EntityPrototype>(entry.ID, out var proto))
                {
                    Log.Error($"Unable to find entity prototype {entry.ID} on {ToPrettyString(uid)} vending.");
                    continue;
                }

                price += entry.Amount * _pricing.GetEstimatedPrice(proto);
            }

            args.Price += price;
        }

        protected override void OnMapInit(EntityUid uid, VendingMachineComponent component, MapInitEvent args)
        {
            base.OnMapInit(uid, component, args);

            if (HasComp<ApcPowerReceiverComponent>(uid))
            {
                TryUpdateVisualState((uid, component));
            }
        }

        private void OnActivatableUIOpenAttempt(EntityUid uid, VendingMachineComponent component, ActivatableUIOpenAttemptEvent args)
        {
            if (component.Broken)
                args.Cancel();
        }

        private void OnBoundUIOpened(EntityUid uid, VendingMachineComponent component, BoundUIOpenedEvent args)
        {
            UpdateVendingMachineInterfaceState(uid, component);
        }

        private void UpdateVendingMachineInterfaceState(EntityUid uid, VendingMachineComponent component)
        {
            var state = new VendingMachineInterfaceState(GetAllInventory(uid, component), GetPriceMultiplier(component),
                component.Credits); //ADT-Economy

            _userInterfaceSystem.SetUiState(uid, VendingMachineUiKey.Key, state);
        }

        private void OnInventoryEjectMessage(EntityUid uid, VendingMachineComponent component, VendingMachineEjectMessage args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            if (args.Actor is not { Valid: true } entity || Deleted(entity))
                return;

            AuthorizedVend(uid, entity, args.Type, args.ID, component);
        }

        private void OnPowerChanged(EntityUid uid, VendingMachineComponent component, ref PowerChangedEvent args)
        {
            TryUpdateVisualState((uid, component));
        }

        private void OnBreak(EntityUid uid, VendingMachineComponent component, BreakageEventArgs eventArgs)
        {
            component.Broken = true;
            TryUpdateVisualState(uid, component);
        }

        private void OnEmagged(EntityUid uid, VendingMachineComponent component, ref GotEmaggedEvent args)
        {
            //ADT-Economy-Start
            args.Handled = component.EmaggedInventory.Count > 0 || component.PriceMultiplier > 0;
            UpdateVendingMachineInterfaceState(uid, component);
            //ADT-Economy-End
        }

        private void OnDamage(EntityUid uid, VendingMachineComponent component, DamageChangedEvent args) //ADT-Economy
        {
            if (component.Broken || component.DispenseOnHitCoolingDown ||
                component.DispenseOnHitChance == null || args.DamageDelta == null)
                return;

            if (args.DamageIncreased && args.DamageDelta.GetTotal() >= component.DispenseOnHitThreshold &&
                _random.Prob(component.DispenseOnHitChance.Value))
            {
                if (component.DispenseOnHitCooldown != null)
                {
                    component.DispenseOnHitEnd = Timing.CurTime + component.DispenseOnHitCooldown.Value;
                }

                EjectRandom(uid, throwItem: true, forceEject: true, component);
            }
        }

        private void OnSelfDispense(EntityUid uid, VendingMachineComponent component, VendingMachineSelfDispenseEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            EjectRandom(uid, throwItem: true, forceEject: false, component);
        }

        private void OnDoAfter(EntityUid uid, VendingMachineComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Used == null)
                return;

            if (!TryComp<VendingMachineRestockComponent>(args.Args.Used, out var restockComponent))
            {
                Log.Error($"{ToPrettyString(args.Args.User)} tried to restock {ToPrettyString(uid)} with {ToPrettyString(args.Args.Used.Value)} which did not have a VendingMachineRestockComponent.");
                return;
            }

            TryRestockInventory(uid, component);

            Popup.PopupEntity(Loc.GetString("vending-machine-restock-done-self", ("target", uid)), args.Args.User, args.Args.User, PopupType.Medium);
            var othersFilter = Filter.PvsExcept(args.Args.User);
            Popup.PopupEntity(Loc.GetString("vending-machine-restock-done-others", ("user", Identity.Entity(args.User, EntityManager)), ("target", uid)), args.Args.User, othersFilter, true, PopupType.Medium); // Ensure using Content.Shared.IdentityManagement;

            Audio.PlayPvs(restockComponent.SoundRestockDone, uid, AudioParams.Default.WithVolume(-2f).WithVariation(0.2f));

            Del(args.Args.Used.Value);

            args.Handled = true;
        }

        //ADT-Economy-Start
        private void OnInteractUsing(EntityUid uid, VendingMachineComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (component.Broken || !this.IsPowered(uid, EntityManager))
                return;

            if (!TryComp<CurrencyComponent>(args.Used, out var currency) ||
                !currency.Price.Keys.Contains(component.CurrencyType))
                return;

            var stack = Comp<StackComponent>(args.Used);
            component.Credits += stack.Count;
            Del(args.Used);
            UpdateVendingMachineInterfaceState(uid, component);
            Audio.PlayPvs(component.SoundInsertCurrency, uid);
            args.Handled = true;
        }

        protected override int GetEntryPrice(EntityPrototype proto)
        {
            var price = (int) _pricing.GetEstimatedPrice(proto);
            return price > 0 ? price : 25;
        }

        private int GetPrice(VendingMachineInventoryEntry entry, VendingMachineComponent comp)
        {
            return (int) (entry.Price * GetPriceMultiplier(comp));
        }

        private double GetPriceMultiplier(VendingMachineComponent comp)
        {
            return comp.PriceMultiplier * GlobalPriceMultiplier;
        }

        private void OnWithdrawMessage(EntityUid uid, VendingMachineComponent component, VendingMachineWithdrawMessage args)
        {
            _stackSystem.Spawn(component.Credits, component.CreditStackPrototype, Transform(uid).Coordinates);
            component.Credits = 0;
            Audio.PlayPvs(component.SoundWithdrawCurrency, uid);

            UpdateVendingMachineInterfaceState(uid, component);
        }
        //ADT-Economy-End

        /// <summary>
        /// Sets the <see cref="VendingMachineComponent.CanShoot"/> property of the vending machine.
        /// </summary>
        public void SetShooting(EntityUid uid, bool canShoot, VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.CanShoot = canShoot;
        }

        /// <summary>
        /// Sets the <see cref="VendingMachineComponent.Contraband"/> property of the vending machine.
        /// </summary>
        public bool SetContraband(EntityUid uid, bool contraband, EntityUid? sender = null, VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
            {
                component.Contraband = contraband;
                return true;
            }

            if (sender != null && (_accessReader.IsAllowed(sender.Value, uid, accessReader) || HasComp<EmaggedComponent>(uid)))
            {
                component.Contraband = contraband;
                return true;
            }

            if (sender != null)
                Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-access-denied"), uid, sender.Value);
            Deny((uid, component), sender);
            return false;
        }

        /// <summary>
        /// Tries to eject the provided item. Will do nothing if the vending machine is incapable of ejecting, already ejecting
        /// or the item doesn't exist in its inventory.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="type">The type of inventory the item is from</param>
        /// <param name="itemId">The prototype ID of the item</param>
        /// <param name="throwItem">Whether the item should be thrown in a random direction after ejection</param>
        /// <param name="component"></param>
        public void TryEjectVendorItem(EntityUid uid, InventoryType type, string itemId, bool throwItem, VendingMachineComponent? component = null, EntityUid? sender = null) //ADT-Economy
        {
            if (!Resolve(uid, ref component))
                return;

            if (component.Ejecting || component.Broken || !this.IsPowered(uid, EntityManager))
            {
                return;
            }

            var entry = GetEntry(uid, itemId, type, component);

            if (entry == null)
            {
                //ADT-Economy-Start
                if (sender.HasValue)
                    Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-invalid-item"), uid, sender.Value);
                //ADT-Economy-End

                Deny((uid, component), sender);
                return;
            }

            if (entry.Amount <= 0)
            {
                //ADT-Economy-Start
                if (sender.HasValue)
                    Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-out-of-stock"), uid, sender.Value);
                //ADT-Economy-End

                Deny((uid, component), sender);
                return;
            }

            if (string.IsNullOrEmpty(entry.ID))
                return;

            //ADT-Economy-Start
            var price = GetPrice(entry, component);
            if (price > 0 && sender.HasValue && !_tag.HasTag(sender.Value, "IgnoreBalanceChecks"))
            {
                var success = false;
                if (component.Credits >= price)
                {
                    component.Credits -= price;
                    success = true;
                }
                else
                {
                    var items = _accessReader.FindPotentialAccessItems(sender.Value);
                    foreach (var item in items)
                    {
                        var nextItem = item;
                        if (TryComp(item, out PdaComponent? pda) && pda.ContainedId is { Valid: true } id)
                            nextItem = id;

                        if (!TryComp<BankCardComponent>(nextItem, out var bankCard) || !bankCard.AccountId.HasValue
                            || !_bankCard.TryGetAccount(bankCard.AccountId.Value, out var account)
                            || account.Balance < price)
                            continue;

                        _bankCard.TryChangeBalance(bankCard.AccountId.Value, -price);
                        success = true;
                        break;
                    }
                }

                if (!success)
                {
                    Popup.PopupEntity(Loc.GetString("vending-machine-component-no-balance"), uid);
                    Deny((uid, component), sender);
                    return;
                }
            }
            //ADT-Economy-End

            // Start Ejecting, and prevent users from ordering while anim playing
            // component.Ejecting = true; // Property is read-only, remove assignment or replace with appropriate method if exists.
            component.NextItemToEject = entry.ID;
            component.ThrowNextItem = throwItem;

            if (TryComp(uid, out SpeakOnUIClosedComponent? speakComponent))
                _speakOnUIClosed.TrySetFlag((uid, speakComponent));

            entry.Amount--;
            UpdateVendingMachineInterfaceState(uid, component);
            TryUpdateVisualState(uid, component);
            Audio.PlayPvs(component.SoundVend, uid);
        }

        /// <summary>
        /// Checks whether the user is authorized to use the vending machine, then ejects the provided item if true
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="sender">Entity that is trying to use the vending machine</param>
        /// <param name="type">The type of inventory the item is from</param>
        /// <param name="itemId">The prototype ID of the item</param>
        /// <param name="component"></param>
        public void AuthorizedVend(EntityUid uid, EntityUid sender, InventoryType type, string itemId, VendingMachineComponent component)
        {
            if (IsAuthorized(uid, sender, component))
            {
                TryEjectVendorItem(uid, type, itemId, component.CanShoot, component, sender); //ADT-Economy-Start
            }
        }

        /// <summary>
        /// Tries to update the visuals of the component based on its current state.
        /// </summary>
        public void TryUpdateVisualState(EntityUid uid, VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var finalState = VendingMachineVisualState.Normal;
            if (component.Broken)
{
    finalState = VendingMachineVisualState.Broken;
}
// Removed assignment to read-only property component.Ejecting

            else if (component.Denying)
            {
                finalState = VendingMachineVisualState.Deny;
            }
            else if (!this.IsPowered(uid, EntityManager))
            {
                finalState = VendingMachineVisualState.Off;
            }

            _appearanceSystem.SetData(uid, VendingMachineVisuals.VisualState, finalState);
        }

        /// <summary>
        /// Ejects a random item from the available stock. Will do nothing if the vending machine is empty.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="throwItem">Whether to throw the item in a random direction after dispensing it.</param>
        /// <param name="forceEject">Whether to skip the regular ejection checks and immediately dispense the item without animation.</param>
        /// <param name="component"></param>
        public void EjectRandom(EntityUid uid, bool throwItem, bool forceEject = false, VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var availableItems = GetAvailableInventory(uid, component);
            if (availableItems.Count <= 0)
                return;

            var item = _random.Pick(availableItems);

            if (forceEject)
            {
                component.NextItemToEject = item.ID;
                component.ThrowNextItem = throwItem;
                var entry = GetEntry(uid, item.ID, item.Type, component);
                if (entry != null)
                    entry.Amount--;
                EjectItem(uid, component, forceEject);
            }
            else
            {
                TryEjectVendorItem(uid, item.Type, item.ID, throwItem, component);
            }
        }

        protected override void EjectItem(EntityUid uid, VendingMachineComponent? component = null, bool forceEject = false)
        {
            if (!Resolve(uid, ref component))
                return;

            // No need to update the visual state because we never changed it during a forced eject
            if (!forceEject)
                TryUpdateVisualState((uid, component));

            if (string.IsNullOrEmpty(component.NextItemToEject))
            {
                component.ThrowNextItem = false;
                return;
            }

            // Default spawn coordinates
            var xform = Transform(uid);
            var spawnCoordinates = xform.Coordinates;

            //Make sure the wallvends spawn outside of the wall.
            if (TryComp<WallMountComponent>(uid, out var wallMountComponent))
            {
                var offset = (wallMountComponent.Direction + xform.LocalRotation - Math.PI / 2).ToVec() * WallVendEjectDistanceFromWall;
                spawnCoordinates = spawnCoordinates.Offset(offset);
            }

            var ent = Spawn(component.NextItemToEject, spawnCoordinates);

            if (component.ThrowNextItem)
            {
                var range = component.NonLimitedEjectRange;
                var direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                _throwingSystem.TryThrow(ent, direction, component.NonLimitedEjectForce);
            }

            component.NextItemToEject = null;
            component.ThrowNextItem = false;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var disabled = EntityQueryEnumerator<EmpDisabledComponent, VendingMachineComponent>();
            while (disabled.MoveNext(out var uid, out _, out var comp))
            {
                if (comp.NextEmpEject < _timing.CurTime)
                {
                    EjectRandom(uid, true, false, comp);
                    comp.NextEmpEject += (5 * comp.EjectDelay);
                }
            }
        }

        public void TryRestockInventory(EntityUid uid, VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            RestockInventoryFromPrototype(uid, component);

            Dirty(uid, component);
            TryUpdateVisualState((uid, component));
        }

        private void OnPriceCalculation(EntityUid uid, VendingMachineRestockComponent component, ref PriceCalculationEvent args)
        {
            List<double> priceSets = new();

            // Find the most expensive inventory and use that as the highest price.
            foreach (var vendingInventory in component.CanRestock)
            {
                double total = 0;

                if (PrototypeManager.TryIndex(vendingInventory, out VendingMachineInventoryPrototype? inventoryPrototype))
                {
                    foreach (var (item, amount) in inventoryPrototype.StartingInventory)
                    {
                        if (PrototypeManager.TryIndex(item, out EntityPrototype? entity))
                            total += _pricing.GetEstimatedPrice(entity) * amount;
                    }
                }

                priceSets.Add(total);
            }

            args.Price += priceSets.Max();
        }

        private void OnEmpPulse(EntityUid uid, VendingMachineComponent component, ref EmpPulseEvent args)
        {
            if (!component.Broken && this.IsPowered(uid, EntityManager))
            {
                args.Affected = true;
                args.Disabled = true;
                component.NextEmpEject = _timing.CurTime;
            }
        }

        private void OnTryVocalize(Entity<VendingMachineComponent> ent, ref TryVocalizeEvent args)
        {
            args.Cancelled |= ent.Comp.Broken;
        }
    }
}
