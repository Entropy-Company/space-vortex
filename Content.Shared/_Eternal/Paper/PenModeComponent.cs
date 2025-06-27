using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared._Eternal.Paper;

public enum PenMode
{
    Write,
    Sign
}

public enum SignatureType
{
    Normal,     // Обычная подпись
    Glowing,    // Светящаяся подпись (видна под УФ)
    Invisible   // Невидимая подпись (видна только под УФ)
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PenModeComponent : Component
{
    [DataField, AutoNetworkedField]
    public PenMode Mode = PenMode.Write;

    [DataField, AutoNetworkedField]
    public Color SignatureColor = Color.FromHex("#3166f5");

    [DataField, AutoNetworkedField]
    public string StampState = "paper_stamp-trader";

    [DataField, AutoNetworkedField]
    public SignatureType SignatureType = SignatureType.Normal;
}
