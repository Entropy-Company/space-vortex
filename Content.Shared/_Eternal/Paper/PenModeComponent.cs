using Robust.Shared.GameStates;

namespace Content.Shared._Eternal.Paper;

public enum PenMode
{
    Write,
    Sign
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
} 