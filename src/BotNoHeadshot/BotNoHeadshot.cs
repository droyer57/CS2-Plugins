using CounterStrikeSharp.API.Core;

namespace BotNoHeadshot;

public sealed class BotNoHeadshot : BasePlugin
{
    public override string ModuleName => "BotNoHeadshot";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Exec";
    public override string ModuleDescription => "Prevent bots from inflicting headshot damage.";

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnPlayerTakeDamagePre>(OnPlayerTakeDamagePre);
    }

    private HookResult OnPlayerTakeDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
    {
        if (!player.IsValid)
            return HookResult.Continue;

        var hitGroup = info.GetHitGroup();
        if (hitGroup != HitGroup_t.HITGROUP_HEAD || (info.BitsDamageType & DamageTypes_t.DMG_BULLET) == 0)
            return HookResult.Continue;

        var attackerEntity = info.Attacker.Value;
        if (attackerEntity == null) return HookResult.Continue;

        var attackerPawn = new CCSPlayerPawn(attackerEntity.Handle);
        if (!attackerPawn.IsValid || attackerPawn.Controller.Value == null) return HookResult.Continue;

        var attackerController = new CCSPlayerController(attackerPawn.Controller.Value.Handle);
        if (!attackerController.IsValid || !attackerController.IsBot) return HookResult.Continue;

        var activeWeapon = attackerPawn.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon == null) return HookResult.Continue;

        var weapon = new CCSWeaponBase(activeWeapon.Handle);
        if (weapon.IsValid)
        {
            var headshotMultiplier = weapon.VData?.HeadshotMultiplier ?? 4f;
            info.Damage /= headshotMultiplier;
        }
        else
        {
            info.Damage /= 4f;
        }

        return HookResult.Continue;
    }
}