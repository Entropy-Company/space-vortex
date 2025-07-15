using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared._Eternal.MobCarry;

[Serializable, NetSerializable]
public sealed partial class MobCarryDoAfterEvent : SimpleDoAfterEvent
{
    [DataField("target", required: true)]
    public NetEntity Target;

    private MobCarryDoAfterEvent() { }
    public MobCarryDoAfterEvent(NetEntity target) { Target = target; }
}
