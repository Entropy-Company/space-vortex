using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Phasing;

/// <summary>
/// Добавьте этот компонент к entity, чтобы оно выглядело сфазированным (глючит, рваные смещения).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PhasingComponent : Component
{
    /// <summary>
    /// Включен ли эффект фазирования.
    /// </summary>
    [DataField("enabled")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;
}
