using Content.Shared.Components;
using Content.Shared.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Content.Shared.MobCarry;
using Robust.Shared.Timing;
using Content.Server.Wieldable;
using Content.Shared.Wieldable.Components;
using Content.Shared.Item;
using Robust.Shared.Log;
using Content.Shared.Wieldable;
using Content.Shared.Hands;
using Content.Shared.Throwing;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Hands.Components;
using Content.Shared.Follower.Components;
using Content.Shared.Hands;
using System.Numerics;
using Content.Shared.Movement.Events;

namespace Content.Server.Systems;

public sealed class MobCarrySystem : SharedMobCarrySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly WieldableSystem _wieldable = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    private static readonly ISawmill Sawmill = Logger.GetSawmill("mobcarry");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobCarryComponent, MobCarryDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<MobCarriedComponent, ThrowAttemptEvent>(OnThrowAttempt);
        SubscribeLocalEvent<MobCarriedComponent, ThrowItemAttemptEvent>(OnThrowItemAttempt);
        SubscribeLocalEvent<ItemComponent, ThrowAttemptEvent>(OnItemThrowAttemptBlockIfCarried);
        SubscribeLocalEvent<ItemComponent, ThrowItemAttemptEvent>(OnItemThrowItemAttemptBlockIfCarried);
        SubscribeLocalEvent<ItemComponent, DropAttemptEvent>(OnItemDropAttemptBlockIfCarried);
        SubscribeLocalEvent<MobCarriedComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<VirtualItemComponent, DropAttemptEvent>(OnVirtualItemDropAttemptForceDelete);
        SubscribeLocalEvent<MobCarriedComponent, UpdateCanMoveEvent>(OnCarriedUpdateCanMove);
    }

    protected override void OnCarryVerbActivated(EntityUid target, EntityUid user, MobCarryComponent component)
    {
        if (_entMan.HasComponent<MobCarriedComponent>(user))
            return;
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

        var carrierXform = _entMan.GetComponent<TransformComponent>(user);
        var mobXform = _entMan.GetComponent<TransformComponent>(target);
        mobXform.AttachParent(user);
        mobXform.LocalPosition = Vector2.Zero;

        _standing.Down(target, playSound: false, dropHeldItems: false);

        if (!_virtualItem.TrySpawnVirtualItemInHand(target, user, out var virt1, true))
            return;
        if (!_virtualItem.TrySpawnVirtualItemInHand(target, user, out var virt2, true))
        {
            _virtualItem.DeleteInHandsMatching(user, target);
            return;
        }

        var carriedComp = _entMan.EnsureComponent<MobCarriedComponent>(target);
        carriedComp.Carrier = user;
        carriedComp.CarriedAt = (float)_gameTiming.CurTime.TotalSeconds;
        Dirty(target, carriedComp);

        args.Handled = true;
    }

    private void OnThrowAttempt(EntityUid uid, MobCarriedComponent component, ThrowAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnThrowItemAttempt(EntityUid uid, MobCarriedComponent component, ref ThrowItemAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnItemThrowAttemptBlockIfCarried(EntityUid uid, ItemComponent component, ThrowAttemptEvent args)
    {
        if (_entMan.HasComponent<MobCarriedComponent>(uid))
        {
            args.Cancel();
        }
    }

    private void OnItemThrowItemAttemptBlockIfCarried(EntityUid uid, ItemComponent component, ref ThrowItemAttemptEvent args)
    {
        if (_entMan.HasComponent<MobCarriedComponent>(uid))
        {
            args.Cancelled = true;
        }
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

    private void OnVirtualItemDropAttemptForceDelete(EntityUid uid, VirtualItemComponent comp, DropAttemptEvent args)
    {
        args.Cancel();
        _virtualItem.DeleteVirtualItem((uid, comp), args.Uid);
    }

    private void OnCarriedUpdateCanMove(EntityUid uid, MobCarriedComponent comp, UpdateCanMoveEvent args)
    {
        args.Cancel();
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
