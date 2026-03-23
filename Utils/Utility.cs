using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

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

    public static void PlaySoundToAllPlayers(string path)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            player.ExecuteClientCommand($"play {path}");
        }
    }
}