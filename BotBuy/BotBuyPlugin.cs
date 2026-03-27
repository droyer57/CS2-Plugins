using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Utils;
using Utils.Data;

// ReSharper disable InconsistentNaming

namespace BotBuy;

public sealed class BotBuyPlugin : BasePlugin
{
    public override string ModuleName => "BotBuy";
    public override string ModuleVersion => "1.0.0";

    private Dictionary<string, WeaponItem> _weaponItems = null!;

    private readonly List<string> _poolTPistol = [];
    private readonly List<string> _poolCTPistol = [];
    private readonly List<string> _poolTRifle = [];
    private readonly List<string> _poolCTRifle = [];
    private bool _isRoundPistol = true;

    private readonly HashSet<int> _awpPlayers = [];
    private bool _isEnable = true;

    public override void Load(bool hotReload)
    {
        _weaponItems = Utility.WeaponItems;

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
    }

    private void OnMapStart(string mapName)
    {
        _isRoundPistol = true;
        _awpPlayers.Clear();
        ReadPool("weapons");
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (!_isEnable || Utility.IsWarmup)
        {
            return HookResult.Continue;
        }

        _awpPlayers.Clear();

        var poolQueue = new Queue<string>();
        foreach (var player in Utility.BotPlayers)
        {
            if (!player.IsValid || !player.PlayerPawn.IsValid)
                continue;

            var playerMoney = player.InGameMoneyServices;
            if (playerMoney == null)
                continue;

            playerMoney.Account = 0;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");

            ResetPlayer(player);

            var num = _isRoundPistol ? Random.Shared.Next(2) : Random.Shared.Next(3);

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
            {
                player.GiveNamedItem("item_defuser");
            }

            if (poolQueue.Count == 0)
            {
                poolQueue = GetPoolQueue(player.Team);
            }

            var weaponName = poolQueue.Dequeue();

            player.GiveWeapon($"weapon_{weaponName}");

            if (weaponName == "awp")
            {
                _awpPlayers.Add(player.Slot);
                AddTimer(1f, () => Utility.PlaySoundToAllPlayers("BotWithAWP"));
            }
        }

        _isRoundPistol = false;

        return HookResult.Continue;
    }

    private void ResetPlayer(CCSPlayerController player)
    {
        if (!player.IsValid || !player.PlayerPawn.IsValid)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn?.WeaponServices == null || playerPawn.ItemServices == null)
            return;

        var toRemove = playerPawn.WeaponServices.MyWeapons
            .Select(w => w.Value)
            .Where(w => w?.IsValid == true && w.DesignerName != "weapon_c4");

        foreach (var weapon in toRemove)
        {
            weapon!.AddEntityIOEvent("Kill", weapon, delay: 0.1f);
        }
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath evt, GameEventInfo info)
    {
        if (!_isEnable)
        {
            return HookResult.Continue;
        }

        var player = evt.Userid;

        if (player == null || !player.IsValid || !player.IsBot)
            return HookResult.Continue;

        if (_awpPlayers.Contains(player.Slot))
        {
            Utility.PlaySoundToAllPlayers("BotWithAWPDead");
        }

        return HookResult.Continue;
    }

    private Queue<string> GetPoolQueue(CsTeam team)
    {
        var isT = team == CsTeam.Terrorist;
        var pool = _isRoundPistol ? isT ? _poolTPistol : _poolCTPistol : isT ? _poolTRifle : _poolCTRifle;
        var newPool = pool.ToList();
        newPool.Shuffle();

        if (!_isRoundPistol && Random.Shared.Next(10) == 0)
        {
            var count = Utility.Players.Count(p => p.Team == team);
            newPool.Insert(Random.Shared.Next(count), "awp");
        }

        return new Queue<string>(newPool);
    }

    private void ReadPool(string filename)
    {
        var filePath = Path.Combine(ModuleDirectory, $"{filename}.pool");
        if (!File.Exists(filePath))
        {
            return;
        }

        _poolTPistol.Clear();
        _poolCTPistol.Clear();
        _poolTRifle.Clear();
        _poolCTRifle.Clear();

        using var sr = new StreamReader(filePath);
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            if (line.StartsWith('#') || line.Trim() == "")
                continue;

            var data = line.Split('=');
            var weaponName = data[0];
            var count = int.Parse(data[1]);

            for (var i = 0; i < count; i++)
            {
                var weaponItem = _weaponItems[weaponName];

                if (weaponItem.RoundType == RoundType.Pistol)
                {
                    if (weaponItem.Team != Team.CounterTerrorist)
                        _poolTPistol.Add(weaponName);
                    if (weaponItem.Team != Team.Terrorist)
                        _poolCTPistol.Add(weaponName);
                }
                else if (weaponItem.RoundType == RoundType.Rifle)
                {
                    if (weaponItem.Team != Team.CounterTerrorist)
                        _poolTRifle.Add(weaponName);
                    if (weaponItem.Team != Team.Terrorist)
                        _poolCTRifle.Add(weaponName);
                }
            }
        }
    }

    [ConsoleCommand("css_botbuy_nextroundpistol")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnNextRoundPistolCommand(CCSPlayerController? player, CommandInfo command)
    {
        _isRoundPistol = true;
    }

    [ConsoleCommand("css_botbuy", "Enable or disable bot buy plugin")]
    [CommandHelper(minArgs: 1, usage: "[0|1]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnBotBuyCommand(CCSPlayerController? player, CommandInfo command)
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
                command.ReplyToCommand($"[{ModuleName}] Usage: css_botbuy [0|1]");
                break;
        }
    }
}