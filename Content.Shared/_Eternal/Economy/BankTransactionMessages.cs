using System.Collections.Generic;
using Robust.Shared.Serialization;
using Content.Shared.CartridgeLoader;

namespace Content.Shared._Eternal.Economy;

[Serializable, NetSerializable]
public sealed class BankTransactionHistoryRequestMessage : CartridgeMessageEvent
{
    public int AccountId;
    public int Count;
    public BankTransactionHistoryRequestMessage(int accountId, int count = 50)
    {
        AccountId = accountId;
        Count = count;
    }
}

[Serializable, NetSerializable]
public sealed class BankTransactionHistoryResponseMessage : BoundUserInterfaceState
{
    public List<TransactionRecord> Records;
    public BankTransactionHistoryResponseMessage(List<TransactionRecord> records)
    {
        Records = records;
    }
}
