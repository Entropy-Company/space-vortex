using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Eternal.Economy
{
    [Serializable, NetSerializable]
    public sealed class BankChangePinMessage : CartridgeMessageEvent
    {
        public int OldPin { get; }
        public int NewPin { get; }

        public BankChangePinMessage(int oldPin, int newPin)
        {
            OldPin = oldPin;
            NewPin = newPin;
        }
    }
}
