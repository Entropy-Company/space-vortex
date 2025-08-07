using System.Linq;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Eternal.Economy;

public sealed class BankAccount
{
    private const int MaxTransactions = 1000;
    private readonly Queue<TransactionRecord> _transactions = new();

    public void AddTransaction(TransactionRecord record)
    {
        if (_transactions.Count >= MaxTransactions)
            _transactions.Dequeue();
        _transactions.Enqueue(record);
    }

    public List<TransactionRecord> GetTransactions(int count = 1000)
    {
        if (count > MaxTransactions)
            count = MaxTransactions;
        return _transactions.ToList().AsEnumerable().Reverse().Take(count).ToList();
    }

    public readonly int AccountId;
    public int AccountPin;
    public int Balance;
    public bool CommandBudgetAccount;
    public Entity<MindComponent>? Mind;
    public string Name = string.Empty;
    public ProtoId<CargoAccountPrototype>? AccountPrototype;
    public EntityUid? CartridgeUid;

    public BankAccount(int accountId, int balance, IRobustRandom random)
    {
        AccountId = accountId;
        Balance = balance;
        AccountPin = random.Next(1000, 10000);
    }
}

