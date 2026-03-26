using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Utils;

namespace Revive;

public sealed class RevivePlugin : BasePlugin
{
    public override string ModuleName => "Revive";
    public override string ModuleVersion => "1.0.0";

    private readonly Dictionary<int, PlayerState> _playerStates = [];

    private static readonly Dictionary<int, string> DefIndexToWeaponName = new()
    {
        { 60, "weapon_m4a1_silencer" }, // M4A1-S
        { 61, "weapon_usp_silencer" }, // USP-S
        { 23, "weapon_mp5sd" }, // MP5-SD
    };

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            //
            _playerStates.Clear();
        });

        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Pre);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    }

    private HookResult OnPlayerHurt(EventPlayerHurt evt, GameEventInfo info)
    {
        var player = evt.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || playerPawn.WeaponServices == null)
            return HookResult.Continue;

        var currentHp = playerPawn.Health;

        if (currentHp > 0)
            return HookResult.Continue;

        var playerState = GetPlayerState(player);
        if (playerState != null)
        {
            _playerStates[player.Slot] = playerState.Value;
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath evt, GameEventInfo info)
    {
        var player = evt.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        var playerState = _playerStates[player.Slot];
        var (deathPosition, deathAngle) = GetDeathPositionAndAngle(player);
        playerState.SetDeathPositionAndAngle(deathPosition, deathAngle);

        AddTimer(5, () =>
        {
            if (!player.IsValid || player.PawnIsAlive)
                return;

            player.Respawn();
            player.PlayerPawn.Value?.Teleport(playerState.DeathPosition, playerState.DeathAngle, Vector.Zero);
            player.ResetInventory(this);
            RestorePlayerInventory(player);
        });

        return HookResult.Continue;
    }

    private void RestorePlayerInventory(CCSPlayerController player)
    {
        var playerState = _playerStates[player.Slot];

        foreach (var weaponState in playerState.WeaponStates)
        {
            if (weaponState.IsGrenade)
            {
                for (var i = 0; i < weaponState.Count; i++)
                    player.GiveNamedItem(weaponState.Name);
            }
            else
            {
                var weapon = player.GiveWeapon(weaponState.Name);
                weapon.Clip1 = weaponState.Count;
            }
        }

        if (playerState.HasHelmet)
            player.GiveNamedItem("item_assaultsuit");
        else if (playerState.ArmorValue > 0)
            player.GiveNamedItem("item_kevlar");

        if (playerState.HasDefuser)
            player.GiveNamedItem("item_defuser");

        player.PlayerPawn.Value!.ArmorValue = playerState.ArmorValue;
    }

    private static (Vector deathPosition, QAngle deathAngle) GetDeathPositionAndAngle(CCSPlayerController player)
    {
        var deathPosition = player.PlayerPawn.Value?.AbsOrigin;
        var deathAngle = player.PlayerPawn.Value?.AbsRotation;

        if (deathPosition == null)
            return (Vector.Zero, QAngle.Zero);

        var savedPosition = new Vector(deathPosition.X, deathPosition.Y, deathPosition.Z);
        var savedAngle = new QAngle(deathAngle?.X ?? 0, deathAngle?.Y ?? 0, deathAngle?.Z ?? 0);

        return (savedPosition, savedAngle);
    }

    private static PlayerState? GetPlayerState(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || playerPawn.WeaponServices == null)
            return null;

        var itemServices = playerPawn.ItemServices!.As<CCSPlayer_ItemServices>();
        var hasHelmet = itemServices.HasHelmet;
        var hasDefuser = itemServices.HasDefuser;
        var playerState = new PlayerState(playerPawn.ArmorValue, hasHelmet, hasDefuser);

        foreach (var weaponHandle in playerPawn.WeaponServices.MyWeapons)
        {
            var weapon = weaponHandle.Value;
            if (weapon == null)
                continue;

            var name = GetCorrectWeaponName(weapon);

            var vdata = weapon.As<CCSWeaponBase>().VData;
            if (vdata == null)
                continue;

            var isGrenade = vdata.GearSlot == gear_slot_t.GEAR_SLOT_GRENADES;
            if (isGrenade)
            {
                int grenadeCount = playerPawn.WeaponServices.Ammo[vdata.PrimaryAmmoType];
                if (grenadeCount == 0)
                    continue;

                playerState.WeaponStates.Add(new WeaponState(name, grenadeCount, isGrenade));
            }
            else
            {
                playerState.WeaponStates.Add(new WeaponState(name, weapon.Clip1, isGrenade));
            }
        }

        return playerState;
    }

    private static string GetCorrectWeaponName(CBasePlayerWeapon weapon)
    {
        int defIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;

        if (DefIndexToWeaponName.TryGetValue(defIndex, out var correctName))
            return correctName;

        return weapon.DesignerName;
    }
}