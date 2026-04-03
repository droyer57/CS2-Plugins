using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Utils;

namespace InfiniteAmmo;

public sealed class InfiniteAmmo : BasePlugin
{
    public override string ModuleName => "InfiniteAmmo";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Exec";
    public override string ModuleDescription => "Infinite ammo for weapons.";

    private bool _enabled = true;
    private bool _botNoReload;
    private bool _botOnly;

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
    };

    private static readonly HashSet<string> ShotgunsClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "weapon_xm1014",
        "weapon_nova",
        "weapon_sawedoff"
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

    private HookResult OnWeaponFire(EventWeaponFire evt, GameEventInfo info)
    {
        if (!_enabled) return HookResult.Continue;

        var player = evt.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        RefillActiveWeapon(player);

        return HookResult.Continue;
    }

    private HookResult OnWeaponReload(EventWeaponReload evt, GameEventInfo info)
    {
        if (!_enabled) return HookResult.Continue;

        var player = evt.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        RefillActiveWeapon(player);

        return HookResult.Continue;
    }

    private void RefillActiveWeapon(CCSPlayerController player)
    {
        var activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon == null || !activeWeapon.IsValid)
            return;
        if (IsExcludeWeapon(activeWeapon))
            return;

        var fallbackReserve = FallbackReserve;
        if (ShotgunsClasses.Contains(activeWeapon.DesignerName))
            fallbackReserve = 32;

        if (!_botOnly || player.IsBot)
        {
            activeWeapon.ReserveAmmo[0] = fallbackReserve;
        }

        if (_botNoReload && player.IsBot)
        {
            activeWeapon.Clip1 = fallbackReserve;
        }
    }

    private static bool IsExcludeWeapon(CBasePlayerWeapon weapon)
    {
        if (ExcludeClasses.Contains(weapon.DesignerName)) return true;
        if (weapon is CBaseCSGrenade) return true;
        return false;
    }

    [ConsoleCommand("css_infiniteammo", "Enable or disable switch side plugin")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnInfiniteAmmoCommand(CCSPlayerController? player, CommandInfo command)
    {
        Utility.UseCommand(command, ref _enabled);
    }

    [ConsoleCommand("css_infiniteammo_botnoreload", "Enable or disable bot no reload")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnBotNoReloadCommand(CCSPlayerController? player, CommandInfo command)
    {
        Utility.UseCommand(command, ref _botNoReload);
    }

    [ConsoleCommand("css_infiniteammo_botonly", "Enable or disable bot only")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnBotOnlyCommand(CCSPlayerController? player, CommandInfo command)
    {
        Utility.UseCommand(command, ref _botOnly);
    }
}