using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
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
    private Timer? _timer;
    public bool IsPendingDestroy { get; set; }
    private int _playerInZoneCount;
    private readonly Dictionary<int, bool> _playerInZone = [];
    private float _lastTick;
    private float _propRotation;

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

        foreach (var beam in _beams)
        {
            beam.Remove();
        }

        _worldText.Remove();
        _prop?.Remove();

        Utility.PlaySoundToAllPlayers("TeammateRevived");
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

        _beams = Helpers.DrawBeaconCircleOnPlayer(DeathPosition);
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

    public void OnTick()
    {
        var currentTime = Server.CurrentTime;
        var deltaTime = currentTime - _lastTick;
        _lastTick = currentTime;

        if (_prop?.IsValid == true)
        {
            _propRotation = (_propRotation + 180f * deltaTime) % 360f;
            _prop.Teleport(null, new QAngle(0, _propRotation, 0));
        }

        foreach (var player in Utilities.GetPlayers().Where(x => x.Slot != Slot))
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

            if (distance <= 50 && !isInZone)
            {
                OnZoneEnter();
                isInZone = true;
            }
            else if (distance > 50 && isInZone)
            {
                OnZoneExit();
                isInZone = false;
            }

            _playerInZone[player.Slot] = isInZone;
        }
    }

    private void OnZoneEnter()
    {
        _playerInZoneCount++;
        if (_playerInZoneCount != 1)
            return;

        _timer = _plugin.AddTimer(3, () =>
        {
            Respawn();
            IsPendingDestroy = true;
        });
    }

    private void OnZoneExit()
    {
        _playerInZoneCount--;
        if (_playerInZoneCount != 0)
            return;

        _timer?.Kill();
        _timer = null;
    }
}