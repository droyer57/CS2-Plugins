using CounterStrikeSharp.API.Modules.Utils;

namespace Revive;

public struct PlayerState
{
    public Vector DeathPosition { get; private set; } = null!;
    public QAngle DeathAngle { get; private set; } = null!;
    public int ArmorValue { get; }
    public bool HasHelmet { get; }
    public bool HasDefuser { get; }
    public List<WeaponState> WeaponStates { get; } = [];

    public PlayerState(int armorValue, bool hasHelmet, bool hasDefuser)
    {
        ArmorValue = armorValue;
        HasHelmet = hasHelmet;
        HasDefuser = hasDefuser;
    }

    public void SetDeathPositionAndAngle(Vector deathPosition, QAngle deathAngle)
    {
        DeathPosition = deathPosition;
        DeathAngle = deathAngle;
    }
}