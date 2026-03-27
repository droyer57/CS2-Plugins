using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Utils;
using Utils.Data;

namespace AutoWeapon;

public sealed class AutoWeaponPlugin : BasePlugin
{
    public override string ModuleName => "AutoWeapon";
    public override string ModuleVersion => "1.0.0";

    private Queue<string> _weaponQueue = [];
    private readonly List<string> _weapons = [];
    private Dictionary<string, WeaponItem> _weaponItems = null!;
    private bool _isEnable;

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            _weaponQueue.Clear();
            ReadPool("weapons");
            Server.ExecuteCommand("css_switchside_midreset 0");
            Server.ExecuteCommand("css_botbuy 0");
        });

        _weaponItems = Utility.WeaponItems;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart evt, GameEventInfo info)
    {
        if (!_isEnable || Utility.IsWarmup)
        {
            return HookResult.Continue;
        }

        if (_weaponQueue.Count == 0)
        {
            var newList = _weapons.ToList();
            newList.Shuffle();
            _weaponQueue = new Queue<string>(newList);
        }

        var weaponName = _weaponQueue.Dequeue();

        var numFlash = Random.Shared.Next(3);
        var numGrenade = Random.Shared.Next(2);
        var numMolotov = Random.Shared.Next(2);

        foreach (var player in Utility.Players)
        {
            if (!player.PawnIsAlive)
                continue;

            var playerMoney = player.InGameMoneyServices;
            if (playerMoney == null)
                continue;

            var keepWeapon = !player.IsBot ? "weapon_knife" : string.Empty;
            player.ResetInventory(this, keepWeapon);

            var weaponTeam = _weaponItems[weaponName].Team;
            var originalTeam = player.Team;

            if (!player.IsBot && weaponTeam != Team.Shared && (int)weaponTeam != (int)player.Team)
            {
                player.SwitchTeam((CsTeam)weaponTeam);
                player.GiveNamedItem($"weapon_{weaponName}");
                player.SwitchTeam(originalTeam);
            }
            else
            {
                player.GiveNamedItem($"weapon_{weaponName}");
            }

            playerMoney.Account = 0;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");

            var num = !player.IsBot ? 2 : Random.Shared.Next(3);
            switch (num)
            {
                case 1:
                    player.GiveNamedItem("item_kevlar");
                    break;
                case 2:
                    player.GiveNamedItem("item_assaultsuit");
                    break;
            }

            if (player.Team == CsTeam.CounterTerrorist && !player.HasDefuser())
                player.GiveNamedItem("item_defuser");

            if (!player.IsBot)
            {
                for (var i = 0; i < numFlash; i++)
                    player.GiveNamedItem("weapon_flashbang");
                if (numGrenade > 0)
                    player.GiveNamedItem("weapon_hegrenade");
                if (numMolotov > 0)
                    player.GiveNamedItem(player.Team == CsTeam.Terrorist ? "weapon_molotov" : "weapon_incgrenade");
            }
        }

        return HookResult.Continue;
    }

    private void ReadPool(string filename)
    {
        var filePath = Path.Combine(ModuleDirectory, $"{filename}.pool");
        if (!File.Exists(filePath))
        {
            return;
        }

        _weapons.Clear();

        using var sr = new StreamReader(filePath);
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            if (line.StartsWith('#') || line.Trim() == "")
                continue;

            _weapons.Add(line);
        }
    }

    [ConsoleCommand("css_autoweapon", "Enable or disable auto weapon plugin")]
    [CommandHelper(minArgs: 1, usage: "[0|1]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnAutoWeaponCommand(CCSPlayerController? player, CommandInfo command)
    {
        var arg = command.GetArg(1); // "0" or "1"

        switch (arg)
        {
            case "1":
                _isEnable = true;
                command.ReplyToCommand($"[{ModuleName}] Enabled!");
                break;
            case "0":
                _isEnable = false;
                command.ReplyToCommand($"[{ModuleName}] Disabled!");
                break;
            default:
                command.ReplyToCommand($"[{ModuleName}] Usage: css_autoweapon [0|1]");
                break;
        }
    }
}