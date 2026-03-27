using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Utils.Data;

namespace Utils;

public static class Utility
{
    public static bool IsWarmup
    {
        get
        {
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
                .FirstOrDefault()?.GameRules ?? throw new NullReferenceException();
            return gameRules.WarmupPeriod;
        }
    }

    public static IEnumerable<CCSPlayerController> Players =>
        Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

    public static IEnumerable<CCSPlayerController> HumanPlayers
    {
        get
        {
            for (var i = 0; i < Server.MaxPlayers; i++)
            {
                var controller = Utilities.GetPlayerFromSlot(i);

                if (controller == null || !controller.IsValid ||
                    controller.Connected != PlayerConnectedState.PlayerConnected)
                    continue;

                yield return controller;
            }
        }
    }

    public static void PlaySoundToAllPlayers(string soundEventName)
    {
        foreach (var player in HumanPlayers)
        {
            RecipientFilter filter = [player];
            player.EmitSound(soundEventName, recipients: filter);
        }
    }

    public static Dictionary<string, WeaponItem> WeaponItems =>
        new()
        {
            { "glock", new WeaponItem("glock", Team.Terrorist, RoundType.Pistol) },
            { "elite", new WeaponItem("elite", Team.Terrorist, RoundType.Pistol) },
            { "tec9", new WeaponItem("tec9", Team.Terrorist, RoundType.Pistol) },

            { "usp_silencer", new WeaponItem("usp_silencer", Team.CounterTerrorist, RoundType.Pistol) },
            { "hkp2000", new WeaponItem("hkp2000", Team.CounterTerrorist, RoundType.Pistol) },
            { "fiveseven", new WeaponItem("fiveseven", Team.CounterTerrorist, RoundType.Pistol) },

            { "p250", new WeaponItem("p250", Team.Shared, RoundType.Pistol) },
            { "deagle", new WeaponItem("deagle", Team.Shared, RoundType.Pistol) },

            { "mac10", new WeaponItem("mac10", Team.Terrorist, RoundType.Rifle) },
            { "ak47", new WeaponItem("ak47", Team.Terrorist, RoundType.Rifle) },
            { "sg556", new WeaponItem("sg556", Team.Terrorist, RoundType.Rifle) },
            { "galilar", new WeaponItem("galilar", Team.Terrorist, RoundType.Rifle) },
            { "g3sg1", new WeaponItem("g3sg1", Team.Terrorist, RoundType.Rifle) },

            { "mp9", new WeaponItem("mp9", Team.CounterTerrorist, RoundType.Rifle) },
            { "m4a1", new WeaponItem("m4a1", Team.CounterTerrorist, RoundType.Rifle) },
            { "m4a1_silencer", new WeaponItem("m4a1_silencer", Team.CounterTerrorist, RoundType.Rifle) },
            { "aug", new WeaponItem("aug", Team.CounterTerrorist, RoundType.Rifle) },
            { "famas", new WeaponItem("famas", Team.CounterTerrorist, RoundType.Rifle) },
            { "scar20", new WeaponItem("scar20", Team.CounterTerrorist, RoundType.Rifle) },

            { "ump45", new WeaponItem("ump45", Team.Shared, RoundType.Rifle) },
            { "mp7", new WeaponItem("mp7", Team.Shared, RoundType.Rifle) },
            { "mp5sd", new WeaponItem("mp5sd", Team.Shared, RoundType.Rifle) },
            { "p90", new WeaponItem("p90", Team.Shared, RoundType.Rifle) },
            { "bizon", new WeaponItem("bizon", Team.Shared, RoundType.Rifle) },
            { "xm1014", new WeaponItem("xm1014", Team.Shared, RoundType.Rifle) },
            { "nova", new WeaponItem("nova", Team.Shared, RoundType.Rifle) },
            { "ssg08", new WeaponItem("ssg08", Team.Shared, RoundType.Rifle) },
            { "awp", new WeaponItem("awp", Team.Shared, RoundType.Rifle) },
            { "negev", new WeaponItem("negev", Team.Shared, RoundType.Rifle) },
            { "m249", new WeaponItem("m249", Team.Shared, RoundType.Rifle) }
        };
}