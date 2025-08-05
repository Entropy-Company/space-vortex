using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Eternal.Economy;

[Serializable, NetSerializable]
public sealed class CartridgeUiRefreshMessage : CartridgeMessageEvent
{
    // Пустой класс, просто сигнал для обновления UI
}
