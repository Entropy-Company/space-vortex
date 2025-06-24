using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared._Eternal.Paper;

public enum ChameleonPenMode
{
    Write,
    Sign
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChameleonPenComponent : Component
{
    [DataField, AutoNetworkedField]
    public ChameleonPenMode Mode = ChameleonPenMode.Write;

    [DataField, AutoNetworkedField]
    public Color SignatureColor = Color.FromHex("#3166f5");

    [DataField, AutoNetworkedField]
    public string StampState = "paper_stamp-trader";

    // --- Хамелеон-функционал ---
    [DataField, AutoNetworkedField]
    public Color? ForgedSignatureColor;

    [DataField, AutoNetworkedField]
    public string? ForgedSignatureText;
}
