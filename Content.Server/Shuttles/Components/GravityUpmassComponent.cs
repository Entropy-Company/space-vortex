using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;

namespace Content.Server.Shuttles.Components;

[RegisterComponent]
public sealed partial class GravityUpmassComponent : Component
{
    /// <summary>
    /// Максимальная масса шаттла для FTL (если 0 — используется глобальный лимит)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxFtlMass = 0;
}
