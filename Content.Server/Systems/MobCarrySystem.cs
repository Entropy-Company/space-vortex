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
        SubscribeLocalEvent<MobCarriedComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<ItemComponent, ThrowAttemptEvent>(OnItemThrowAttemptBlockIfCarried);
        SubscribeLocalEvent<ItemComponent, ThrowItemAttemptEvent>(OnItemThrowItemAttemptBlockIfCarried);
        SubscribeLocalEvent<ItemComponent, DropAttemptEvent>(OnItemDropAttemptBlockIfCarried);
    }

    protected override void OnCarryVerbActivated(EntityUid target, EntityUid user, MobCarryComponent component)
    {
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

        if (!_entMan.HasComponent<ItemComponent>(target))
        {
            _entMan.AddComponent<ItemComponent>(target);
            _itemSystem.SetSize(target, "Ginormous");
        }
        if (!_entMan.HasComponent<WieldableComponent>(target))
        {
            _entMan.AddComponent<WieldableComponent>(target);
        }
        _wieldable.SetUnwieldOnUse(target, false);
        if (!_hands.TryPickupAnyHand(user, target))
            return;
        if (!_wieldable.TryWield(target, _entMan.GetComponent<WieldableComponent>(target), user))
            return;

        _standing.Down(target, playSound: false, dropHeldItems: false);

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

    private void OnDropAttempt(EntityUid uid, MobCarriedComponent component, DropAttemptEvent args)
    {
        args.Cancel();
        StandUpCarriedMob(uid, component);
    }

    public void StandUpCarriedMob(EntityUid mobUid, MobCarriedComponent carried)
    {
        if (!carried.Carrier.HasValue || !_entMan.EntityExists(carried.Carrier.Value))
            return;
        var carrierXform = _entMan.GetComponent<TransformComponent>(carried.Carrier.Value);
        var mobXform = _entMan.GetComponent<TransformComponent>(mobUid);
        mobXform.Coordinates = carrierXform.Coordinates;
        if (_entMan.HasComponent<ItemComponent>(mobUid))
            _entMan.RemoveComponent<ItemComponent>(mobUid);
        if (_entMan.HasComponent<WieldableComponent>(mobUid))
            _entMan.RemoveComponent<WieldableComponent>(mobUid);
        _entMan.RemoveComponent<MobCarriedComponent>(mobUid);
        _standing.Stand(mobUid);
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
}
