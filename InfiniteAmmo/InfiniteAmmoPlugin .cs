using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace InfiniteAmmo;

public sealed class InfiniteAmmoPlugin : BasePlugin
{
    public override string ModuleName => "InfiniteAmmo";
    public override string ModuleVersion => "1.1.0";

    private bool _enabled = true;

    private static readonly HashSet<string> ExcludeClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "weapon_hegrenade",
        "weapon_flashbang",
        "weapon_smokegrenade",
        "weapon_molotov",
        "weapon_incgrenade",
        "weapon_decoy",
        "weapon_tagrenade",
        "weapon_snowball",
        "weapon_bumpmine",
        "weapon_breachcharge",
        "weapon_xm1014"
    };

    // Fallback reserve amount if we can't read a weapon-specific value.
    // 999 is safely above any real CS2 reserve cap.
    private const int FallbackReserve = 999;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
        RegisterEventHandler<EventWeaponReload>(OnWeaponReload);
    }

    public override void Unload(bool hotReload)
    {
    }

    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        if (!_enabled) return HookResult.Continue;
        RefillReserveForActiveWeapon(@event.Userid, @event.Weapon);
        return HookResult.Continue;
    }

    private HookResult OnWeaponReload(EventWeaponReload @event, GameEventInfo info)
    {
        if (!_enabled) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        var activeWeapon = player.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value;
        if (activeWeapon == null || !activeWeapon.IsValid) return HookResult.Continue;
        if (IsExcludeWeapon(activeWeapon)) return HookResult.Continue;

        RefillReserve(activeWeapon);
        return HookResult.Continue;
    }

    private void RefillReserveForActiveWeapon(CCSPlayerController? player, string weaponName)
    {
        if (player == null || !player.IsValid) return;

        var weapons = player.PlayerPawn?.Value?.WeaponServices?.MyWeapons;
        if (weapons == null) return;

        foreach (var handle in weapons)
        {
            var weapon = handle.Value;
            if (weapon == null || !weapon.IsValid) continue;
            if (IsExcludeWeapon(weapon)) continue;
            if (!string.Equals(weapon.DesignerName, weaponName, StringComparison.OrdinalIgnoreCase)) continue;

            RefillReserve(weapon);
            break;
        }
    }

    private static void RefillReserve(CBasePlayerWeapon weapon)
    {
        // Use a large constant — CS2 will silently clamp it to the real per-weapon cap.
        weapon.ReserveAmmo[0] = FallbackReserve;
    }

    private static bool IsExcludeWeapon(CBasePlayerWeapon weapon)
    {
        if (ExcludeClasses.Contains(weapon.DesignerName)) return true;
        if (weapon is CBaseCSGrenade) return true;
        return false;
    }

    [ConsoleCommand("css_infiniteammo", "Enable or disable switch side plugin")]
    [CommandHelper(minArgs: 1, usage: "[0|1]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnInfiniteAmmoCommand(CCSPlayerController? player, CommandInfo command)
    {
        var arg = command.GetArg(1); // "0" or "1"

        switch (arg)
        {
            case "1":
                _enabled = true;
                command.ReplyToCommand($"[{ModuleName}] Enabled!");
                break;
            case "0":
                _enabled = false;
                command.ReplyToCommand($"[{ModuleName}] Disabled!");
                break;
            default:
                command.ReplyToCommand($"[{ModuleName}] Usage: css_infiniteammo [0|1]");
                break;
        }
    }
}