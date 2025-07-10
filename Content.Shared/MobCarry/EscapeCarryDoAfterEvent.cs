using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobCarry;

[Serializable, NetSerializable]
public sealed partial class EscapeCarryDoAfterEvent : DoAfterEvent
{
    [DataField("carrier", required: true)]
    public NetEntity Carrier;

    private EscapeCarryDoAfterEvent() { }
    public EscapeCarryDoAfterEvent(NetEntity carrier) { Carrier = carrier; }

    public override DoAfterEvent Clone() => new EscapeCarryDoAfterEvent(Carrier);
}
