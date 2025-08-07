using System;
using Robust.Shared.Serialization;
using Robust.Shared.Maths;

namespace Content.Shared._Eternal.Economy;

[Serializable, NetSerializable]
public sealed class TransactionRecord
{
    public enum TransactionType
    {
        Purchase,
        TransferSent,
        TransferReceived,
        Deposit,
        Withdraw
    }

    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string? CounterpartyAccount { get; set; }
    public string? CounterpartyName { get; set; }
    public string? Comment { get; set; }

    public Color Color { get; set; } = Color.White;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public TransactionRecord() {}
    public TransactionRecord(TransactionType type, string description, int amount, Color color, DateTime timestamp,
        string? counterpartyAccount = null, string? counterpartyName = null, string? comment = null)
    {
        Type = type;
        Description = description;
        Amount = amount;
        Color = color;
        Timestamp = timestamp;
        CounterpartyAccount = counterpartyAccount;
        CounterpartyName = counterpartyName;
        Comment = comment;
    }
}
