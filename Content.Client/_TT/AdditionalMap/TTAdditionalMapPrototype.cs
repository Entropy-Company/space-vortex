using Robust.Shared.Prototypes;

namespace Content.Client._TT.AdditionalMap;

[Prototype("additionalMap")]
public sealed class TTAdditionalMapPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;
}
