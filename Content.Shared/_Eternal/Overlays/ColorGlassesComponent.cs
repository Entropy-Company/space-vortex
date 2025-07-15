using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Eternal.Overlays;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ColorGlassesComponent : Component
{
    private Vector3 _tintVec = new(0.0f, 0.7f, 1.0f);

    [ViewVariables(VVAccess.ReadWrite), DataField("tint"), AutoNetworkedField]
    public string Tint
    {
        get => "#" + ((int)(_tintVec.X * 255)).ToString("X2") + ((int)(_tintVec.Y * 255)).ToString("X2") + ((int)(_tintVec.Z * 255)).ToString("X2");
        set
        {
            var color = Color.FromHex(value);
            _tintVec = new Vector3(color.R, color.G, color.B);
        }
    }

    public Vector3 TintVec => _tintVec;
}
