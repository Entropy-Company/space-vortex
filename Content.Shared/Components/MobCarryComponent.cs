using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MobCarryComponent : Component
{
    [DataField]
    public float CarryDoAfter = 3.0f;
}
