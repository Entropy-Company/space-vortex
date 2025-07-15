using Robust.Shared.GameStates;

namespace Content.Shared._Eternal.MobCarry;

[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveEscapeCarryDoAfterComponent : Component
{
    public object? DoAfter;
}
