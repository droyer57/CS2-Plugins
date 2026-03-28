using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Utils;

namespace Revive;

public sealed class RevivePlugin : BasePlugin
{
    public override string ModuleName => "Revive";
    public override string ModuleVersion => "1.0.0";

    private readonly Dictionary<int, PlayerState> _playerStates = [];
    private float _lastTick;
    private bool _enable = true;

    public static float RespawnDistance { get; private set; } = 50;
    public static float RespawnTime { get; private set; } = 10;
    public static float DownTime { get; private set; } = 20;

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
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);

        RegisterListener<Listeners.OnTick>(OnTick);
    }

    private void OnTick()
    {
        if (!_enable || Utility.IsWarmup)
        {
            return;
        }

        var currentTime = Server.CurrentTime;
        var deltaTime = currentTime - _lastTick;
        _lastTick = currentTime;

        var message = string.Empty;

        foreach (var (slot, playerState) in _playerStates)
        {
            if (!string.IsNullOrEmpty(message))
                message += "\n";

            playerState.OnTick(deltaTime);

            var downTimer = (int)MathF.Ceiling(playerState.DownTimer);
            var respawnTimer = (int)MathF.Floor(playerState.RespawnTimer);

            var delta = playerState.SomeoneInZone ? "+" : "-";
            var timer = playerState.SomeoneInZone ? respawnTimer : downTimer;
            message += $"{playerState.Controller.PlayerName}: {timer} [ {delta} ]";

            if (playerState.IsPendingDestroy)
            {
                _playerStates.Remove(slot);
            }
        }

        if (string.IsNullOrEmpty(message))
            return;

        foreach (var player in Utility.HumanPlayers)
        {
            player.PrintToCenterAlert(message);
        }
    }

    private HookResult OnRoundEnd(EventRoundEnd evt, GameEventInfo info)
    {
        if (!_enable || Utility.IsWarmup)
        {
            return HookResult.Continue;
        }

        foreach (var (_, playerState) in _playerStates)
        {
            playerState.Remove();
        }

        _playerStates.Clear();

        return HookResult.Continue;
    }

    private HookResult OnPlayerHurt(EventPlayerHurt evt, GameEventInfo info)
    {
        if (!_enable || Utility.IsWarmup)
        {
            return HookResult.Continue;
        }

        var player = evt.Userid;
        if (player == null || !player.IsValid || player.IsBot)
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
        if (!_enable || Utility.IsWarmup)
        {
            return HookResult.Continue;
        }

        var player = evt.Userid;
        if (player == null || !player.IsValid || player.IsBot)
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
            else if (name != "weapon_knife" && name != "weapon_c4")
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

    [ConsoleCommand("css_revive", "Enable or disable the revive plugin")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnReviveCommand(CCSPlayerController? player, CommandInfo command)
    {
        Utility.UseCommand(command, ref _enable);
    }

    [ConsoleCommand("css_revive_respawndistance", "Get or set the respawn distance")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRespawnDistanceCommand(CCSPlayerController? player, CommandInfo command)
    {
        RespawnDistance = Utility.UseCommand(command, RespawnDistance);
    }

    [ConsoleCommand("css_revive_respawntime", "Get or set the respawn time")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRespawnTimeCommand(CCSPlayerController? player, CommandInfo command)
    {
        RespawnTime = Utility.UseCommand(command, RespawnTime);
    }

    [ConsoleCommand("css_revive_downtime", "Get or set the down time")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnDownTimeCommand(CCSPlayerController? player, CommandInfo command)
    {
        DownTime = Utility.UseCommand(command, DownTime);
    }
}