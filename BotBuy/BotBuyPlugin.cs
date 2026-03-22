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

    private List<string> _weapons =
    [
        // Pistols
        "weapon_glock", // T
        "weapon_usp_silencer", // CT
        "weapon_hkp2000", // CT
        "weapon_elite", // T
        "weapon_p250",
        "weapon_deagle",
        "weapon_revolver",
        "weapon_fiveseven", // CT
        "weapon_tec9", // T
        "weapon_cz75a",

        // SMGs
        "weapon_mac10", // T
        "weapon_mp5sd",
        "weapon_mp7", // CT
        "weapon_mp9", // CT
        "weapon_p90",
        "weapon_bizon", // CT
        "weapon_ump45",

        // Rifles
        "weapon_ak47", // T
        "weapon_m4a1", // CT
        "weapon_m4a1_silencer", // CT
        "weapon_aug", // CT
        "weapon_sg556", // T
        "weapon_galilar", // T
        "weapon_famas", // CT
        "weapon_scar20", // CT
        "weapon_g3sg1", // T
        "weapon_awp",
        "weapon_ssg08",

        // Shotguns
        "weapon_nova",
        "weapon_xm1014",
        "weapon_sawedoff", // T
        "weapon_mag7", // CT

        // Machine Guns
        "weapon_m249",
        "weapon_negev",
    ];

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

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
    }

    private void OnMapStart(string mapName)
    {
        _nextRoundPistol = true;
        ReadPool("weapons");
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (Utility.IsWarmup)
        {
            return HookResult.Continue;
        }

        Queue<string>? poolQueue = null;
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

            if (player.Team == CsTeam.CounterTerrorist) // todo: handle hostage rescue
            {
                player.GiveNamedItem("item_defuser");
            }

            poolQueue ??= GetPoolQueue(player.Team);
            _nextRoundPistol = false;
            var weaponName = poolQueue.Dequeue();
            player.GiveNamedItem($"weapon_{weaponName}");
            if (weaponName == "awp")
            {
                AddTimer(1f, () => Utility.PlaySoundToAllPlayers("sounds/ui/armsrace_final_kill_tone.vsnd_c"));
            }
        }

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