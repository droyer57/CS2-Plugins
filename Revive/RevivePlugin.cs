using System.Drawing;
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
        RegisterListener<Listeners.OnServerPrecacheResources>(manifest =>
        {
            manifest.AddResource("models/coop/challenge_coin.vmdl");
        });

        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            //
            _playerStates.Clear();

            // Force to cache the model first
            Server.NextFrame(() =>
            {
                var prop = Helpers.CreateProp(Vector.Zero);
                AddTimer(5, () => { prop.Remove(); });
            });
        });

        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Pre);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);

        RegisterListener<Listeners.OnTick>(OnTick);
    }

    private void OnTick()
    {
        foreach (var (slot, playerState) in _playerStates)
        {
            playerState.OnTick();

            if (playerState.IsPendingDestroy)
            {
                _playerStates.Remove(slot);
            }
        }
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
            _playerStates[player.Slot] = playerState;
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath evt, GameEventInfo info)
    {
        var player = evt.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (_playerStates.TryGetValue(player.Slot, out var playerState))
        {
            playerState.OnDeath();
        }

        return HookResult.Continue;
    }


    private PlayerState? GetPlayerState(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || playerPawn.WeaponServices == null)
            return null;

        var itemServices = playerPawn.ItemServices!.As<CCSPlayer_ItemServices>();
        var hasHelmet = itemServices.HasHelmet;
        var hasDefuser = itemServices.HasDefuser;
        var playerState = new PlayerState(player, this, playerPawn.ArmorValue, hasHelmet, hasDefuser);

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