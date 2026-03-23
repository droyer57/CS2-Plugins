using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Utils;

public static class PlayerExtensions
{
    public static void ResetInventory(this CCSPlayerController player, BasePlugin plugin, params string[] keepWeapons)
    {
        if (!player.IsValid || !player.PlayerPawn.IsValid)
            return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn?.WeaponServices == null || playerPawn.ItemServices == null)
            return;

        var c4 = playerPawn.WeaponServices.MyWeapons
            .Select(w => w.Value)
            .FirstOrDefault(w => w?.IsValid == true && w.DesignerName == "weapon_c4");

        if (c4 != null)
        {
            var itemServices = playerPawn.ItemServices.As<CCSPlayer_ItemServices>();
            itemServices.DropActivePlayerWeapon(c4);

            plugin.AddTimer(3f, () => { c4.Teleport(playerPawn.AbsOrigin, playerPawn.AbsRotation, new Vector()); });
        }

        var toKeep = playerPawn.WeaponServices.MyWeapons
            .Select(w => w.Value)
            .Where(w => w?.IsValid == true && keepWeapons.Contains(w.DesignerName))
            .Select(w => w!.DesignerName)
            .ToList();

        player.RemoveWeapons();

        foreach (var weaponName in toKeep)
            player.GiveNamedItem(weaponName);
    }

    public static CCSWeaponBase GiveWeapon(this CCSPlayerController player, string name)
    {
        var handle = player.GiveNamedItem(name);
        return new CCSWeaponBase(handle);
    }

    public static bool HasDefuser(this CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null)
            return false;

        if (playerPawn.ItemServices == null)
            return false;

        var itemServices = new CCSPlayer_ItemServices(playerPawn.ItemServices.Handle);
        return itemServices.HasDefuser;
    }
}