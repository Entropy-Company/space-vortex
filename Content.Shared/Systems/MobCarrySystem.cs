using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Content.Shared.Verbs;
using Content.Shared.Components;

namespace Content.Shared.Systems;

public abstract class SharedMobCarrySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobCarryComponent, GetVerbsEvent<ActivationVerb>>(OnGetCarryVerb);
    }

    private void OnGetCarryVerb(EntityUid uid, MobCarryComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!HasComp<CanCarryMobsComponent>(args.User))
            return;

        if (HasComp<MobCarriedComponent>(uid))
            return;

        if (HasComp<MobCarriedComponent>(args.User))
            return;

        if (args.User == uid)
            return;

        var verb = new ActivationVerb
        {
            Act = () => OnCarryVerbActivated(uid, args.User, component),
            Text = "Поднять на руки",
            Priority = 1
        };
        args.Verbs.Add(verb);
    }

    protected abstract void OnCarryVerbActivated(EntityUid target, EntityUid user, MobCarryComponent component);
}
