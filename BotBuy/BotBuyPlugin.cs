using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Utils;

// ReSharper disable InconsistentNaming

namespace BotBuy;

public sealed class BotBuyPlugin : BasePlugin
{
    public override string ModuleName => "Bot Buy";
    public override string ModuleVersion => "1.0.0";

    private readonly List<WeaponItem> _weaponItems =
    [
        // Pistol
        new("glock", Team.Terrorist, RoundType.Pistol),
        new("elite", Team.Terrorist, RoundType.Pistol),
        new("tec9", Team.Terrorist, RoundType.Pistol),

        new("usp_silencer", Team.CounterTerrorist, RoundType.Pistol),
        new("hkp2000", Team.CounterTerrorist, RoundType.Pistol),
        new("fiveseven", Team.CounterTerrorist, RoundType.Pistol),

        new("p250", Team.Shared, RoundType.Pistol),
        new("deagle", Team.Shared, RoundType.Pistol),

        // Rifle
        new("mac10", Team.Terrorist, RoundType.Rifle),
        new("ak47", Team.Terrorist, RoundType.Rifle),
        new("sg556", Team.Terrorist, RoundType.Rifle),
        new("galilar", Team.Terrorist, RoundType.Rifle),

        new("mp9", Team.CounterTerrorist, RoundType.Rifle),
        new("m4a1", Team.CounterTerrorist, RoundType.Rifle),
        new("m4a1_silencer", Team.CounterTerrorist, RoundType.Rifle),
        new("aug", Team.CounterTerrorist, RoundType.Rifle),
        new("famas", Team.CounterTerrorist, RoundType.Rifle),

        new("ump45", Team.Shared, RoundType.Rifle),
        new("mp5sd", Team.Shared, RoundType.Rifle),
        new("p90", Team.Shared, RoundType.Rifle),
        new("xm1014", Team.Shared, RoundType.Rifle),
        new("ssg08", Team.Shared, RoundType.Rifle),
        new("awp", Team.Shared, RoundType.Rifle)
    ];

    private readonly List<string> _poolTPistol = [];
    private readonly List<string> _poolCTPistol = [];
    private readonly List<string> _poolTRifle = [];
    private readonly List<string> _poolCTRifle = [];
    private bool _nextRoundPistol = true;
    private readonly Dictionary<int, CCSWeaponBase> _weapons = [];

    private readonly HashSet<int> _awpPlayers = [];

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
    }

    private void OnMapStart(string mapName)
    {
        _nextRoundPistol = true;
        _awpPlayers.Clear();
        _weapons.Clear();
        ReadPool("weapons");
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (Utility.IsWarmup)
        {
            return HookResult.Continue;
        }

        _awpPlayers.Clear();
        _weapons.Clear();

        var poolQueue = new Queue<string>();
        foreach (var player in Utility.Players)
        {
            if (!player.IsValid || !player.IsBot || !player.PlayerPawn.IsValid)
                continue;

            var playerMoney = player.InGameMoneyServices;
            if (playerMoney == null)
                continue;

            playerMoney.Account = 0;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");

            player.ResetInventory("weapon_c4");

            var num = Random.Shared.Next(3);
            switch (num)
            {
                case 1:
                    player.GiveNamedItem("item_kevlar");
                    break;
                case 2:
                    player.GiveNamedItem("item_assaultsuit");
                    break;
            }

            if (player.Team == CsTeam.CounterTerrorist)
            {
                player.GiveNamedItem("item_defuser"); // todo: check if he already has an item_defuser
            }

            if (poolQueue.Count == 0)
            {
                poolQueue = GetPoolQueue(player.Team);
            }

            var weaponName = poolQueue.Dequeue();

            var weapon = player.GiveWeapon($"weapon_{weaponName}");
            _weapons.Add(player.Slot, weapon);

            if (weaponName == "awp")
            {
                _awpPlayers.Add(player.Slot);
                AddTimer(1f, () => Utility.PlaySoundToAllPlayers("sounds/ui/armsrace_final_kill_tone.vsnd_c"));
            }
        }

        _nextRoundPistol = false;

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath evt, GameEventInfo info)
    {
        var player = evt.Userid;

        if (player == null || !player.IsValid || !player.IsBot)
            return HookResult.Continue;

        if (_awpPlayers.Contains(player.Slot))
        {
            Utility.PlaySoundToAllPlayers("sounds/ui/armsrace_become_leader_match.vsnd_c");
        }

        _weapons[player.Slot].Remove();

        return HookResult.Continue;
    }

    private Queue<string> GetPoolQueue(CsTeam team)
    {
        var isT = team == CsTeam.Terrorist;
        var pool = _nextRoundPistol ? isT ? _poolTPistol : _poolCTPistol : isT ? _poolTRifle : _poolCTRifle;
        pool.Shuffle();
        return new Queue<string>(pool);
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
                var weaponItem = _weaponItems.First(x => x.Name == weaponName);

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
        _nextRoundPistol = true;
    }
}