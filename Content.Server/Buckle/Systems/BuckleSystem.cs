using Content.Shared.Buckle;
using Content.Server.Systems;
using Content.Shared.Components;
using Content.Shared.Buckle.Components;

namespace Content.Server.Buckle.Systems;

public sealed class BuckleSystem : SharedBuckleSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BuckleComponent, BuckleAttemptEvent>(OnBuckleAttempt);
    }

    private void OnBuckleAttempt(EntityUid uid, BuckleComponent component, ref BuckleAttemptEvent args)
    {
        if (EntityManager.HasComponent<MobCarriedComponent>(uid))
        {
            var mobCarrySystem = EntitySystem.Get<MobCarrySystem>();
            var carried = EntityManager.GetComponent<MobCarriedComponent>(uid);
            mobCarrySystem.StandUpCarriedMob(uid, carried);
        }
    }
}
