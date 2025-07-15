using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Eternal.MobCarry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class MobCarriedComponent : Component
{
    [DataField("carrier"), AutoNetworkedField]
    public EntityUid? Carrier;

    [DataField("carriedAt")]
    public float CarriedAt;
}
