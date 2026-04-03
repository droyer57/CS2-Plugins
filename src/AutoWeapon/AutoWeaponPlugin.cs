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
            if (!player.IsValid || !player.PawnIsAlive)
                continue;

            var keepWeapon = !player.IsBot ? "weapon_knife" : string.Empty;
            player.ResetInventory(this, keepWeapon);
        }

        Server.NextFrame(() =>
        {
            foreach (var player in Utility.Players)
            {
                if (!player.IsValid || !player.PawnIsAlive)
                    continue;

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

                var playerMoney = player.InGameMoneyServices;
                if (playerMoney == null)
                    continue;

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
        });

        return HookResult.Continue;
    }

    private void ReadPool(string filename)
    {
        var filePath = Path.Combine(Utility.GetCfgDirectory(ModuleDirectory), "plugins", "AutoWeapon",
            $"{filename}.pool");
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(filePath);
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
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnAutoWeaponCommand(CCSPlayerController? player, CommandInfo command)
    {
        Utility.UseCommand(command, ref _isEnable);
    }
}