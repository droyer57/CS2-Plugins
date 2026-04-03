using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Utils;

namespace OneInTheChamber;

public sealed class OneInTheChamber : BasePlugin
{
    public override string ModuleName => "OneInTheChamber";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Exec";
    public override string ModuleDescription => "One pistol, one bullet. Instant kill. Bullet is restored on kill.";

    private readonly Dictionary<int, CCSWeaponBase> _weapons = [];
    private bool _enabled;
    private string _pistolName = "weapon_p250";
    private bool _teamMode;

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnEntityTakeDamagePre>(OnEntityTakeDamagePre);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    }

    private void OnMapStart(string mapName)
    {
        _weapons.Clear();
    }

    private HookResult OnEntityTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
    {
        if (!_enabled || Utility.IsWarmup)
            return HookResult.Continue;

        if (entity.DesignerName != "player")
            return HookResult.Continue;

        if ((info.BitsDamageType & DamageTypes_t.DMG_BULLET) == 0 &&
            (info.BitsDamageType & DamageTypes_t.DMG_SLASH) == 0)
            return HookResult.Continue;

        var attackerEntity = info.Attacker.Value;
        if (attackerEntity == null)
            return HookResult.Continue;

        var attackerPawn = new CCSPlayerPawn(attackerEntity.Handle);
        if (!attackerPawn.IsValid || attackerPawn.Controller.Value == null)
            return HookResult.Continue;

        var attackerController = new CCSPlayerController(attackerPawn.Controller.Value.Handle);
        if (!attackerController.IsValid || attackerController.IsBot)
            return HookResult.Continue;

        info.Damage = 999;

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart evt, GameEventInfo info)
    {
        if (!_enabled || Utility.IsWarmup)
            return HookResult.Continue;

        _weapons.Clear();

        foreach (var player in Utility.HumanPlayers)
        {
            player.ResetInventory(this, "weapon_knife");

            Server.NextFrame(() =>
            {
                var weapon = player.GiveWeapon(_pistolName);
                weapon.ReserveAmmo[0] = 0;
                weapon.Clip1 = 1;

                _weapons.Add(player.Slot, weapon);

                var playerMoney = player.InGameMoneyServices;
                if (playerMoney == null)
                    return;

                player.GiveNamedItem("item_assaultsuit");

                if (player.Team == CsTeam.CounterTerrorist && !player.HasDefuser())
                    player.GiveNamedItem("item_defuser");

                playerMoney.Account = 0;
                Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
            });
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath evt, GameEventInfo info)
    {
        if (!_enabled || Utility.IsWarmup)
            return HookResult.Continue;

        var attackerController = evt.Attacker;
        if (attackerController == null || !attackerController.IsValid)
            return HookResult.Continue;

        if (!_weapons.TryGetValue(attackerController.Slot, out var weapon))
        {
            return HookResult.Continue;
        }

        Server.NextFrame(() =>
        {
            if (_teamMode)
            {
                foreach (var (slot, currentWeapon) in _weapons)
                {
                    var player = Utilities.GetPlayerFromSlot(slot);
                    if (player == null || !player.IsValid || player.Team != attackerController.Team)
                        continue;

                    currentWeapon.Clip1 = 1;
                    Utilities.SetStateChanged(currentWeapon, "CBasePlayerWeapon", "m_iClip1");
                }
            }
            else
            {
                weapon.Clip1 = 1;
                Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
            }
        });

        return HookResult.Continue;
    }

    [ConsoleCommand("css_oneinthechamber", "Enable or disable one in the chamber")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnOneInTheChamberCommand(CCSPlayerController? player, CommandInfo command)
    {
        Utility.UseCommand(command, ref _enabled);
    }

    [ConsoleCommand("css_oneinthechamber_pistol", "Get or set the pistol")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnPistolCommand(CCSPlayerController? player, CommandInfo command)
    {
        Utility.UseCommand(command, ref _pistolName);
    }

    [ConsoleCommand("css_oneinthechamber_team", "Enable or disable the team mode")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTeamCommand(CCSPlayerController? player, CommandInfo command)
    {
        Utility.UseCommand(command, ref _teamMode);
    }
}