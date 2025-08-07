using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Eternal.Economy;

[Serializable, NetSerializable]
public sealed class BankTransferMessage : CartridgeMessageEvent
{
    public int ToAccountId { get; }
    public int Amount { get; }
    public int Pin { get; }
    public string? Comment { get; }

    public BankTransferMessage(int toAccountId, int amount, int pin, string? comment = null)
    {
        ToAccountId = toAccountId;
        Amount = amount;
        Pin = pin;
        Comment = comment;
    }
}
