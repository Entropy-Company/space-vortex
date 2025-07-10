using Robust.Shared.GameObjects;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Map;
using Content.Shared.Item;
using Content.Shared.Wieldable.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Components;

namespace Content.Server.Systems;

public sealed class MobCarryDropSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        CommandBinds.Builder
            .BindBefore(ContentKeyFunctions.Drop, new PointerInputCmdHandler(OnDropPressed))
            .Register<MobCarryDropSystem>();
    }

    private bool OnDropPressed(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (session?.AttachedEntity is not { } player)
            return false;
        if (!_entMan.TryGetComponent(player, out HandsComponent? hands) || hands.ActiveHandEntity is not { } carried)
            return false;
        var mobCarrySystem = EntitySystem.Get<MobCarrySystem>();
        var carriedComp = _entMan.GetComponent<MobCarriedComponent>(carried);
        mobCarrySystem.StandUpCarriedMob(carried, carriedComp);
        return false;
    }
}
