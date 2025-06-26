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

    /// <summary>
    /// Скорость анимации эффекта фазирования.
    /// </summary>
    [DataField("animationSpeed")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float AnimationSpeed = 1.1f;

    /// <summary>
    /// Сила сдвигов (множитель для смещений).
    /// </summary>
    [DataField("distortionStrength")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float DistortionStrength = 1.0f;

    /// <summary>
    /// Минимальное количество полос деления.
    /// </summary>
    [DataField("bandMin")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BandMin = 3.0f;

    /// <summary>
    /// Максимальное количество полос деления.
    /// </summary>
    [DataField("bandMax")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BandMax = 8.0f;

    /// <summary>
    /// Частота появления глюков (0.0 - 1.0).
    /// </summary>
    [DataField("glitchFrequency")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float GlitchFrequency = 0.7f;

    /// <summary>
    /// Сила разрыва полос (0.0 - 1.0).
    /// </summary>
    [DataField("bandSplitStrength")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BandSplitStrength = 0.3f;

    /// <summary>
    /// Частота разрыва полос (0.0 - 1.0).
    /// </summary>
    [DataField("bandSplitFrequency")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BandSplitFrequency = 0.85f;
}

[Serializable, NetSerializable]
public sealed class PhasingComponentState : ComponentState
{
    public bool Enabled;
    public float AnimationSpeed;
    public float DistortionStrength;
    public float BandMin;
    public float BandMax;
    public float GlitchFrequency;
    public float BandSplitStrength;
    public float BandSplitFrequency;

    public PhasingComponentState(PhasingComponent component)
    {
        Enabled = component.Enabled;
        AnimationSpeed = component.AnimationSpeed;
        DistortionStrength = component.DistortionStrength;
        BandMin = component.BandMin;
        BandMax = component.BandMax;
        GlitchFrequency = component.GlitchFrequency;
        BandSplitStrength = component.BandSplitStrength;
        BandSplitFrequency = component.BandSplitFrequency;
    }
}
