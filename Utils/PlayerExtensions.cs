using CounterStrikeSharp.API.Core;

namespace Utils;

public static class PlayerExtensions
{
    public static void ResetInventory(this CCSPlayerController player, params string[] excludeWeapons)
    {
        if (!player.IsValid || !player.PlayerPawn.IsValid)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn?.WeaponServices == null)
            return;

        var toRemove = playerPawn.WeaponServices.MyWeapons
            .Where(w => w.IsValid && w.Value != null
                                  // && w.Value.DesignerName != "weapon_c4"
                                  && !excludeWeapons.Contains(w.Value.DesignerName))
            .ToList();

        foreach (var weapon in toRemove)
        {
            weapon.Value!.Remove();
        }
    }

    public static CCSWeaponBase GiveWeapon(this CCSPlayerController player, string name)
    {
        var handle = player.GiveNamedItem(name);
        return new CCSWeaponBase(handle);
    }
}