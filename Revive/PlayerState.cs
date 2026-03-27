using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Revive;

public sealed class PlayerState
{
    public Vector DeathPosition { get; private set; } = null!;
    public QAngle DeathAngle { get; private set; } = null!;
    public int ArmorValue { get; }
    public bool HasHelmet { get; }
    public bool HasDefuser { get; }
    public List<WeaponState> WeaponStates { get; } = [];

    public CCSPlayerController Controller { get; }

    public int Slot => Controller.Slot;

    private CBeam[] _beams = null!;
    private CPointWorldText _worldText = null!;
    private CDynamicProp? _prop;

    private readonly BasePlugin _plugin;

    public bool IsPendingDestroy { get; private set; }
    private int _playerInZoneCount;
    private readonly Dictionary<int, bool> _playerInZone = [];
    private float _propRotation;

    public float DownTimer { get; private set; } = RevivePlugin.DownTime;
    public float RespawnTimer { get; private set; }

    public bool SomeoneInZone => _playerInZoneCount > 0;

    public PlayerState(CCSPlayerController controller, BasePlugin plugin, int armorValue, bool hasHelmet,
        bool hasDefuser)
    {
        Controller = controller;
        _plugin = plugin;
        ArmorValue = armorValue;
        HasHelmet = hasHelmet;
        HasDefuser = hasDefuser;
    }

    private void Respawn()
    {
        Controller.Respawn();
        Controller.PlayerPawn.Value?.Teleport(DeathPosition, DeathAngle, Vector.Zero);
        Controller.ResetInventory(_plugin);
        RestorePlayerInventory();
        Remove();
        Utility.PlaySoundToAllPlayers("TeammateRevived");
        IsPendingDestroy = true;
    }

    private void Dead()
    {
        Remove();
        Utility.PlaySoundToAllPlayers("TeammateDead");
        IsPendingDestroy = true;
    }

    public void Remove()
    {
        foreach (var beam in _beams)
        {
            beam.Remove();
        }

        _worldText.Remove();
        _prop?.Remove();
    }

    private void RestorePlayerInventory()
    {
        foreach (var weaponState in WeaponStates)
        {
            if (weaponState.IsGrenade)
            {
                for (var i = 0; i < weaponState.Count; i++)
                    Controller.GiveNamedItem(weaponState.Name);
            }
            else
            {
                var weapon = Controller.GiveWeapon(weaponState.Name);
                weapon.Clip1 = weaponState.Count;
            }
        }

        if (HasHelmet)
            Controller.GiveNamedItem("item_assaultsuit");
        else if (ArmorValue > 0)
            Controller.GiveNamedItem("item_kevlar");

        if (HasDefuser)
            Controller.GiveNamedItem("item_defuser");

        Controller.PlayerPawn.Value!.ArmorValue = ArmorValue;
    }

    public void OnDeath()
    {
        var (deathPosition, deathAngle) = GetDeathPositionAndAngle();
        DeathPosition = deathPosition;
        DeathAngle = deathAngle;

        _beams = Helpers.DrawBeaconCircle(DeathPosition);
        _worldText = Helpers.CreateText(DeathPosition + new Vector(0, 0, 32), Controller.PlayerName);
        _prop = Helpers.CreateProp(DeathPosition + new Vector(0, 0, 48));
    }

    private (Vector deathPosition, QAngle deathAngle) GetDeathPositionAndAngle()
    {
        var deathPosition = Controller.PlayerPawn.Value?.AbsOrigin;
        var deathAngle = Controller.PlayerPawn.Value?.AbsRotation;

        if (deathPosition == null)
            return (Vector.Zero, QAngle.Zero);

        var savedPosition = new Vector(deathPosition.X, deathPosition.Y, deathPosition.Z);
        var savedAngle = new QAngle(deathAngle?.X ?? 0, deathAngle?.Y ?? 0, deathAngle?.Z ?? 0);

        return (savedPosition, savedAngle);
    }

    public void OnTick(float deltaTime)
    {
        if (_prop?.IsValid == true)
        {
            _propRotation = (_propRotation + 180f * deltaTime) % 360f;
            _prop.Teleport(null, new QAngle(0, _propRotation, 0));
        }

        foreach (var player in Utility.HumanPlayers.Where(x => x.Slot != Slot))
        {
            var playerPosition = player.PlayerPawn.Value?.AbsOrigin;
            if (playerPosition == null)
                continue;

            var distance = Helpers.CalculateDistanceBetween(playerPosition, DeathPosition);

            if (!_playerInZone.TryGetValue(player.Slot, out var isInZone))
            {
                isInZone = false;
                _playerInZone.Add(player.Slot, isInZone);
            }

            if (distance <= RevivePlugin.RespawnDistance && !isInZone)
            {
                _playerInZoneCount++;
                isInZone = true;
            }
            else if (distance > RevivePlugin.RespawnDistance && isInZone)
            {
                _playerInZoneCount--;
                isInZone = false;
            }

            _playerInZone[player.Slot] = isInZone;
        }

        if (_playerInZoneCount > 0)
        {
            RespawnTimer += deltaTime;
            if (RespawnTimer >= RevivePlugin.RespawnTime)
            {
                RespawnTimer = RevivePlugin.RespawnTime;
                Respawn();
            }
        }
        else if (_playerInZoneCount == 0)
        {
            DownTimer -= deltaTime;
            if (DownTimer <= 0)
            {
                DownTimer = 0;
                Dead();
            }

            RespawnTimer -= deltaTime;
            if (RespawnTimer <= 0)
                RespawnTimer = 0;
        }
    }
}