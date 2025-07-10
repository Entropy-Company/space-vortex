using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CanEscapeCarryComponent : Component
{
    [DataField]
    public float EscapeTime = 2.0f;
}
