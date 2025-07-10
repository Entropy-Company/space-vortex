using Content.Shared.Components;
using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Player;
using Content.Shared.Movement.Events;
using Content.Shared.MobCarry;
using Content.Shared.Item;
using Content.Shared.Wieldable.Components;

namespace Content.Server.Systems;

public sealed partial class MobCarryEscapeSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CanEscapeCarryComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<CanEscapeCarryComponent, EscapeCarryDoAfterEvent>(OnEscapeDoAfter);
    }

    private void OnMoveInput(EntityUid uid, CanEscapeCarryComponent comp, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;
        if (!_entMan.HasComponent<MobCarriedComponent>(uid))
            return;
        if (_entMan.TryGetComponent(uid, out ActiveEscapeCarryDoAfterComponent? active) && active.DoAfter != null)
            return;
        var doAfterArgs = new DoAfterArgs(_entMan, uid, comp.EscapeTime, new EscapeCarryDoAfterEvent(_entMan.GetNetEntity(uid)), uid, target: uid)
        {
            NeedHand = false,
            BreakOnDamage = true,
            BreakOnHandChange = true
        };
        var doAfter = _doAfter.TryStartDoAfter(doAfterArgs);
        if (doAfter != null)
        {
            _entMan.EnsureComponent<ActiveEscapeCarryDoAfterComponent>(uid).DoAfter = doAfter;
        }
    }

    private void OnEscapeDoAfter(EntityUid uid, CanEscapeCarryComponent comp, EscapeCarryDoAfterEvent ev)
    {
        if (_entMan.HasComponent<MobCarriedComponent>(uid))
        {
            var mobCarrySystem = EntitySystem.Get<MobCarrySystem>();
            var carriedComp = _entMan.GetComponent<MobCarriedComponent>(uid);
            mobCarrySystem.StandUpCarriedMob(uid, carriedComp);
        }

        if (_entMan.HasComponent<ActiveEscapeCarryDoAfterComponent>(uid))
            _entMan.RemoveComponent<ActiveEscapeCarryDoAfterComponent>(uid);
        if (_entMan.HasComponent<ItemComponent>(uid))
            _entMan.RemoveComponent<ItemComponent>(uid);
        if (_entMan.HasComponent<WieldableComponent>(uid))
            _entMan.RemoveComponent<WieldableComponent>(uid);
    }
}
