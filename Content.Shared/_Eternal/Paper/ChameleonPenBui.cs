using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared._Eternal.Paper;

[Serializable, NetSerializable]
public enum ChameleonPenUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ChameleonPenBuiState : BoundUserInterfaceState
{
    public readonly Color? ForgedSignatureColor;
    public readonly string? ForgedSignatureText;
    public readonly SignatureType SignatureType;

    public ChameleonPenBuiState(Color? color, string? text, SignatureType signatureType)
    {
        ForgedSignatureColor = color;
        ForgedSignatureText = text;
        SignatureType = signatureType;
    }
}

[Serializable, NetSerializable]
public sealed class ChameleonPenBuiSetMessage : BoundUserInterfaceMessage
{
    public readonly Color ForgedSignatureColor;
    public readonly string ForgedSignatureText;
    public readonly SignatureType SignatureType;

    public ChameleonPenBuiSetMessage(Color color, string text, SignatureType signatureType)
    {
        ForgedSignatureColor = color;
        ForgedSignatureText = text;
        SignatureType = signatureType;
    }
}
