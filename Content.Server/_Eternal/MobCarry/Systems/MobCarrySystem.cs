using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Content.Shared._Eternal.MobCarry;
using Robust.Shared.Timing;
using Content.Server.Wieldable;
using Content.Shared.Wieldable.Components;
using Content.Shared.Item;
using Content.Shared.Throwing;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using System.Numerics;
using Content.Shared.Hands;
using Content.Shared._Eternal.MobCarry.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Events;
using Content.Shared.Pulling.Events;

namespace Content.Server._Eternal.MobCarry.Systems;

public sealed class MobCarrySystem : SharedMobCarrySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly WieldableSystem _wieldable = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    [Dependency] private readonly Content.Shared.Movement.Pulling.Systems.PullingSystem _pullingSystem = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobCarryComponent, MobCarryDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ItemComponent, DropAttemptEvent>(OnItemDropAttemptBlockIfCarried);
        SubscribeLocalEvent<MobCarriedComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<VirtualItemComponent, DropAttemptEvent>(OnVirtualItemDropAttemptForceDelete);
        SubscribeLocalEvent<MobCarriedComponent, UpdateCanMoveEvent>(OnCarriedUpdateCanMove);
        SubscribeLocalEvent<VirtualItemComponent, VirtualItemThrownEvent>(OnVirtualItemThrown);
        SubscribeLocalEvent<PullableComponent, Content.Shared.Pulling.Events.StartPullAttemptEvent>(OnPullableStartPullAttemptBlockIfCarried);
        SubscribeLocalEvent<MobCarriedComponent, Content.Shared.Pulling.Events.StartPullAttemptEvent>(OnMobCarriedStartPullAttemptBlock);
        SubscribeLocalEvent<MobCarriedComponent, ComponentInit>(OnMobCarriedComponentInit);
    }

    protected override void OnCarryVerbActivated(EntityUid target, EntityUid user, MobCarryComponent component)
    {
        if (_entMan.HasComponent<MobCarriedComponent>(user))
            return;

        var freeHands = 0;
        foreach (var handId in _hands.EnumerateHands(user))
        {
            if (_hands.GetHeldItem(user, handId) == null)
                freeHands++;
        }

        if (freeHands < 2)
        {
            _popup.PopupEntity(Loc.GetString("mob-carry-hands-full"), user, user);
            return;
        }
        var doAfterArgs = new DoAfterArgs(_entMan, user, component.CarryDoAfter, new MobCarryDoAfterEvent(_entMan.GetNetEntity(target)), target, target)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnDamage = true
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid uid, MobCarryComponent component, MobCarryDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var user = args.Args.User;
        var target = _entMan.GetEntity(args.Target);

        if (!_entMan.EntityExists(target) || HasComp<MobCarriedComponent>(target))
            return;

        // Reserve both hands with virtual items. If it fails, show popup and abort.
        if (!_virtualItem.TrySpawnVirtualItemInHand(target, user, out var virt1, false) ||
            !_virtualItem.TrySpawnVirtualItemInHand(target, user, out var virt2, false))
        {
            _virtualItem.DeleteInHandsMatching(user, target);
            _popup.PopupEntity(Loc.GetString("mob-carry-hands-full"), user, user);
            return;
        }

        // Now that hands are locked in, attach the carried mob.
        var carrierXform = _entMan.GetComponent<TransformComponent>(user);
        var mobXform = _entMan.GetComponent<TransformComponent>(target);
        mobXform.AttachParent(user);
        mobXform.LocalPosition = Vector2.Zero;

        _standing.Down(target, playSound: false, dropHeldItems: false);

        var carriedComp = _entMan.EnsureComponent<MobCarriedComponent>(target);
        carriedComp.Carrier = user;
        carriedComp.CarriedAt = (float)_gameTiming.CurTime.TotalSeconds;
        Dirty(target, carriedComp);

        args.Handled = true;
    }


    private void OnItemDropAttemptBlockIfCarried(EntityUid uid, ItemComponent component, DropAttemptEvent args)
    {
        if (_entMan.HasComponent<MobCarriedComponent>(uid))
        {
            args.Cancel();
            var carried = _entMan.GetComponent<MobCarriedComponent>(uid);
            StandUpCarriedMob(uid, carried);
        }
    }

    private void OnVirtualItemDeleted(EntityUid uid, MobCarriedComponent comp, VirtualItemDeletedEvent args)
    {
        if (args.BlockingEntity != uid)
            return;
        StandUpCarriedMob(uid, comp);
        if (comp.Carrier != null)
            _virtualItem.DeleteInHandsMatching(comp.Carrier.Value, uid);
    }

    /// <summary>
    /// Prevents pulling of entities that are currently being carried.
    /// </summary>
    private void OnPullableStartPullAttemptBlockIfCarried(EntityUid uid, PullableComponent component, ref Content.Shared.Pulling.Events.StartPullAttemptEvent args)
    {
        // uid is the entity being pulled
        if (_entMan.HasComponent<MobCarriedComponent>(uid))
        {
            args.Cancel();
            if (_entMan.EntityExists(args.Puller))
            {
                _popup.PopupEntity(Loc.GetString("mob-carry-pull-blocked"), args.Puller, args.Puller);
            }
        }
    }

    /// <summary>
    /// Prevents pulling attempts on any mob that is currently being carried (regardless of component order).
    /// </summary>
    private void OnMobCarriedStartPullAttemptBlock(EntityUid uid, MobCarriedComponent comp, ref Content.Shared.Pulling.Events.StartPullAttemptEvent args)
    {
        // uid is the carried mob, args.Puller is the one trying to pull
        args.Cancel();
        if (_entMan.EntityExists(args.Puller))
        {
            _popup.PopupEntity(Loc.GetString("mob-carry-pull-blocked"), args.Puller, args.Puller);
        }
    }

    /// <summary>
    /// When a mob becomes carried, forcibly stop any pulls involving it (as puller or pullable).
    /// </summary>
    private void OnMobCarriedComponentInit(EntityUid uid, MobCarriedComponent comp, ComponentInit args)
    {
        // Use PullingSystem API to forcibly stop any pulls involving this entity (as puller or pullable)
        _pullingSystem.StopAllPulls(uid);
    }

    private void OnVirtualItemDropAttemptForceDelete(EntityUid uid, VirtualItemComponent comp, DropAttemptEvent args)
    {
        args.Cancel();
        _virtualItem.DeleteVirtualItem((uid, comp), args.Uid);
    }

    private void OnCarriedUpdateCanMove(EntityUid uid, MobCarriedComponent comp, UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    /// <summary>
    /// When the carrier throws the virtual item (Ctrl+Q), detach the mob and throw it instead.
    /// VirtualItemThrownEvent is raised early in the throw pipeline, before the actual item leaves the hand.
    /// </summary>
    private void OnVirtualItemThrown(EntityUid uid, VirtualItemComponent comp, VirtualItemThrownEvent args)
    {
        var mobUid = args.BlockingEntity;

        if (!_entMan.EntityExists(mobUid) || !_entMan.TryGetComponent(mobUid, out MobCarriedComponent? carried))
            return;

        // Detach the mob and clean up virtual items (this also frees the hand).
        StandUpCarriedMob(mobUid, carried);

        // Finally, throw the mob in the intended direction.
        _throwing.TryThrow(mobUid, args.Direction, user: args.User);
    }

    public void StandUpCarriedMob(EntityUid mobUid, MobCarriedComponent carried)
    {
        if (!carried.Carrier.HasValue || !_entMan.EntityExists(carried.Carrier.Value))
            return;
        var mobXform = _entMan.GetComponent<TransformComponent>(mobUid);
        mobXform.AttachToGridOrMap();
        if (_entMan.HasComponent<ItemComponent>(mobUid))
            _entMan.RemoveComponent<ItemComponent>(mobUid);
        if (_entMan.HasComponent<WieldableComponent>(mobUid))
            _entMan.RemoveComponent<WieldableComponent>(mobUid);
        _entMan.RemoveComponent<MobCarriedComponent>(mobUid);
        _standing.Stand(mobUid);
        if (carried.Carrier != null)
            _virtualItem.DeleteInHandsMatching(carried.Carrier.Value, mobUid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var comp in _entMan.EntityQuery<MobCarriedComponent>())
        {
            var uid = comp.Owner;
            var carrier = comp.Carrier;
            if (!carrier.HasValue || !_entMan.EntityExists(carrier.Value))
                continue;
            var mobXform = _entMan.GetComponent<TransformComponent>(uid);
            var carrierXform = _entMan.GetComponent<TransformComponent>(carrier.Value);
            if (mobXform.ParentUid != carrier.Value)
                mobXform.AttachParent(carrier.Value);
            mobXform.LocalPosition = Vector2.Zero;
        }
    }
}
