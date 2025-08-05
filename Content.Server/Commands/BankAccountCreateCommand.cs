using Content.Server._Eternal.Economy;
using Content.Server.Commands;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._Eternal.Economy;
using Robust.Shared.Console;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Shared.IoC;
using Robust.Server.Player;
using Robust.Shared.Log;
using Content.Shared.PDA;
using Content.Server.CartridgeLoader;
using Content.Server.PDA;
using Content.Server.Inventory;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Access.Components;
using Content.Shared.Mind.Components;
using System.Linq;

namespace Content.Server.Commands;

[AdminCommand(AdminFlags.Admin)]
internal sealed class BankAccountCreateCommand : IConsoleCommand
{

    public string Command => "bankaccountcreate";
    public string Description => "Создать и привязать банковский аккаунт игроку: bankaccountcreate <_netId> <account number> <pin>";
    public string Help => "bankaccountcreate <_netId> <account number> <pin> -- _netId это сущность персонажа игрока, у которого в инвентаре должен быть КПК с ID-картой и катриджем банка. account number и pin будут установлены.";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteLine("Использование: bankaccountcreate <_netId> <account number> <pin>");
            return;
        }
        if (!NetEntity.TryParse(args[0], out var netId))
        {
            shell.WriteLine($"Неверный формат _netId: {args[0]}");
            return;
        }
        if (!int.TryParse(args[1], out var accountNumber) || accountNumber < 1)
        {
            shell.WriteLine($"Неверный номер аккаунта: {args[1]}");
            return;
        }
        if (!int.TryParse(args[2], out var pin) || pin < 0 || pin > 9999)
        {
            shell.WriteLine($"Неверный PIN: {args[2]}");
            return;
        }

        var entMan = IoCManager.Resolve<IEntityManager>();
        var bankCardSys = entMan.EntitySysManager.GetEntitySystem<BankCardSystem>();
        var invSys = entMan.EntitySysManager.GetEntitySystem<InventorySystem>();
        var cartridgeLoader = entMan.EntitySysManager.GetEntitySystem<CartridgeLoaderSystem>();
        
        var mob = EntityUid.Parse(args[0]);
        if (!entMan.EntityExists(mob))
        {
            shell.WriteLine("Сущность с таким _netId не найдена.");
            return;
        }
        if (!invSys.TryGetSlotEntity(mob, "id", out var pdaUid) || !entMan.EntityExists(pdaUid.Value))
        {
            shell.WriteLine("У сущности нет КПК в слоте id.");
            return;
        }
        if (!entMan.TryGetComponent<PdaComponent>(pdaUid.Value, out var pda) || !pda.ContainedId.HasValue)
        {
            shell.WriteLine("В КПК не найдена ID-карта.");
            return;
        }
        var idCardUid = pda.ContainedId.Value;
        if (!entMan.HasComponent<BankCardComponent>(idCardUid))
        {
            shell.WriteLine("На ID-карте нет компонента BankCardComponent.");
            return;
        }
        var bankCard = entMan.GetComponent<BankCardComponent>(idCardUid);
        BankCartridgeComponent? bankCartridge = null;
        var programs = cartridgeLoader.GetInstalled(pdaUid.Value);
        EntityUid bankCartridgeUid = EntityUid.Invalid;
        foreach (var prog in programs)
        {
            if (entMan.TryGetComponent<BankCartridgeComponent>(prog, out bankCartridge))
            {
                bankCartridgeUid = prog;
                break;
            }
        }
        if (bankCartridge == null)
        {
            shell.WriteLine("В КПК не найден катридж банка.");
            return;
        }
        var account = bankCardSys.CreateAccount(accountNumber);
        account.AccountPin = pin;

        string? ownerName = null;
        if (entMan.TryGetComponent<IdCardComponent>(idCardUid, out var idCardComp) && !string.IsNullOrWhiteSpace(idCardComp.FullName))
            ownerName = idCardComp.FullName;
        else
            ownerName = "Unknown";
        account.Name = ownerName;

        account.CartridgeUid = bankCartridgeUid;
        bankCartridge.AccountId = account.AccountId;
        bankCard.AccountId = account.AccountId;
        bankCard.Pin = pin;
        if (entMan.TryGetComponent<MindContainerComponent>(mob, out var mind) && mind.Mind != null && entMan.TryGetComponent<Content.Shared.Mind.MindComponent>(mind.Mind.Value, out var mindComp))
        {
            mindComp.AddMemory(new Memory("PIN", pin.ToString()));
            mindComp.AddMemory(new Memory("Account", account.AccountId.ToString()));
        }
        shell.WriteLine($"Банковский аккаунт {account.AccountId} с PIN {pin} создан и привязан к ID-карте и катриджу банка игрока с _netId {args[0]}.");
    }
}
