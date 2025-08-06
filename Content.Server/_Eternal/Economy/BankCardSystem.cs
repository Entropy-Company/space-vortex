using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using System.Linq;
using Content.Server.Access.Systems;
using Content.Shared.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.CartridgeLoader;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Roles.Jobs;
using Content.Server.Station.Systems;
using Content.Shared._Eternal.Economy;
using Content.Shared.Access.Components;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Configuration;

namespace Content.Server._Eternal.Economy;

public sealed class BankCardSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IdCardSystem _idCardSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly BankCartridgeSystem _bankCartridge = default!;
    [Dependency] private readonly JobSystem _job = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private int SalaryDelay => _cfg.GetCVar(CCVars.SalaryTime);

    private SalaryPrototype _salaries = default!;
    private readonly List<BankAccount> _accounts = new();
    private float _salaryTimer;

    [Dependency] private readonly IConsoleHost _consoleHost = default!;

    public override void Initialize()
    {
        _salaries = _protoMan.Index<SalaryPrototype>("Salaries");

        if (!_consoleHost.AvailableCommands.ContainsKey("bankaccountcreate"))
            _consoleHost.RegisterCommand(new Content.Server.Commands.BankAccountCreateCommand());

        SubscribeLocalEvent<BankCardComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTicker.RunLevel != GameRunLevel.InRound)
        {
            _salaryTimer = 0f;
            return;
        }

        _salaryTimer += frameTime;

        if (_salaryTimer <= SalaryDelay)
            return;

        _salaryTimer = 0f;
        PaySalary();
    }

    private void PaySalary()
    {
        foreach (var account in _accounts.Where(account =>
                     account.Mind is {Comp.UserId: not null, Comp.CurrentEntity: not null} &&
                     _playerManager.TryGetSessionById(account.Mind.Value.Comp.UserId!.Value, out _) &&
                     !_mobState.IsDead(account.Mind.Value.Comp.CurrentEntity!.Value)))
        {
            account.Balance += GetSalary(account.Mind);
        }

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("salary-pay-announcement"),
            colorOverride: Color.FromHex("#18abf5"));
    }

    private int GetSalary(EntityUid? mind)
    {
        if (mind == null)
            return 0;

        if (!TryComp<MindComponent>(mind.Value, out var mindComponent))
            return 0;

        var entity = mindComponent.CurrentEntity;
        if (entity == null)
            return 0;

        if (!_idCardSystem.TryFindIdCard(entity.Value, out var idCard) || !TryComp<IdCardComponent>(idCard, out var idCardComp))
            return 0;

        var jobIcon = $"{idCardComp.JobIcon}";
        if (string.IsNullOrEmpty(jobIcon))
            return 0;
        var jobKey = jobIcon.StartsWith("JobIcon") ? jobIcon.Substring(7) : jobIcon;
        if (_salaries.Salaries.TryGetValue(jobKey, out var salary))
            return salary;
        return 0;
    }

    private void OnMapInit(EntityUid uid, BankCardComponent component, MapInitEvent args)
    {
        if (component.CommandBudgetCard &&
            TryComp(_station.GetOwningStation(uid), out Content.Shared.Cargo.Components.StationBankAccountComponent? stationBankAccount))
        {
            component.AccountId = 0;
            return;
        }

        if (component.AccountId.HasValue)
        {
            var acc = CreateAccount(component.AccountId.Value, component.StartingBalance);
            component.Pin = acc.AccountPin;
            return;
        }

        var account = CreateAccount(default, component.StartingBalance);
        component.AccountId = account.AccountId;
        component.Pin = account.AccountPin;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _accounts.Clear();
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (_idCardSystem.TryFindIdCard(ev.Mob, out var id) && TryComp<MindContainerComponent>(ev.Mob, out var mind))
        {
            var cardEntity = id.Owner;
            var bankCardComponent = EnsureComp<BankCardComponent>(cardEntity);

            if (!bankCardComponent.AccountId.HasValue || !TryGetAccount(bankCardComponent.AccountId.Value, out var bankAccount))
                return;

            // Sync PIN
            bankCardComponent.Pin = bankAccount.AccountPin;

            if (!TryComp(mind.Mind, out MindComponent? mindComponent))
                return;

            bankAccount.Balance = GetSalary(mind.Mind) + 100;
            mindComponent.AddMemory(new Memory("PIN", bankAccount.AccountPin.ToString()));
            mindComponent.AddMemory(new Memory(Loc.GetString("character-info-memories-account-number"),
                bankAccount.AccountId.ToString()));
            bankAccount.Mind = (mind.Mind.Value, mindComponent);
            bankAccount.Name = Name(ev.Mob);

            if (!_inventorySystem.TryGetSlotEntity(ev.Mob, "id", out var pdaUid))
                return;

            BankCartridgeComponent? comp = null;

            var programs = _cartridgeLoader.GetInstalled(pdaUid.Value);

            var program = programs.ToList().Find(program => TryComp(program, out comp));
            if (comp == null)
                return;

            bankAccount.CartridgeUid = program;
            comp.AccountId = bankAccount.AccountId;
        }
    }

    public BankAccount CreateAccount(int accountId = default, int startingBalance = 0)
    {
        if (TryGetAccount(accountId, out var acc))
            return acc;

        BankAccount account;
        if (accountId == default)
        {
            int accountNumber;
            do
            {
                accountNumber = _random.Next(100000, 999999);
            } while (AccountExist(accountNumber));
            account = new BankAccount(accountNumber, startingBalance, _random);
        }
        else
        {
            account = new BankAccount(accountId, startingBalance, _random);
        }

        _accounts.Add(account);

        return account;
    }

    public bool AccountExist(int accountId)
    {
        return _accounts.Any(x => x.AccountId == accountId);
    }

    public bool TryGetAccount(int accountId, [NotNullWhen(true)] out BankAccount? account)
    {
        account = _accounts.FirstOrDefault(x => x.AccountId == accountId);
        return account != null;
    }

    public int GetBalance(int accountId)
    {
        if (TryGetAccount(accountId, out var account))
        {
            return account.Balance;
        }

        return 0;
    }

    public bool TryChangeBalance(int accountId, int amount)
    {
        if (!TryGetAccount(accountId, out var account) || account.Balance + amount < 0)
            return false;

        if (account.CommandBudgetAccount)
        {
            while (AllEntityQuery<StationBankAccountComponent>().MoveNext(out var uid, out var stationBankAccount))
            {
                var sharedAccount = CompOrNull<StationBankAccountComponent>(uid);
                if (sharedAccount != null)
                {
                    _cargo.UpdateBankAccount(new Entity<StationBankAccountComponent?>(uid, stationBankAccount), amount, sharedAccount.PrimaryAccount);
                    return true;
                }
            }
        }

        account.Balance += amount;
        if (account.CartridgeUid != null)
            _bankCartridge.UpdateUiState(account.CartridgeUid.Value);

        return true;
    }
}
