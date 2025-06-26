using Content.Shared.Phasing;
using Robust.Shared.GameStates;

namespace Content.Server;

public sealed class PhasingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PhasingComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<PhasingComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, PhasingComponent component, ref ComponentGetState args)
    {
        args.State = new PhasingComponentState(component);
    }

    private void OnHandleState(EntityUid uid, PhasingComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PhasingComponentState state)
            return;

        component.Enabled = state.Enabled;
        component.AnimationSpeed = state.AnimationSpeed;
        component.DistortionStrength = state.DistortionStrength;
        component.BandMin = state.BandMin;
        component.BandMax = state.BandMax;
        component.GlitchFrequency = state.GlitchFrequency;
        component.BandSplitStrength = state.BandSplitStrength;
        component.BandSplitFrequency = state.BandSplitFrequency;
    }
}
