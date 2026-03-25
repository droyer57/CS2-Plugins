namespace Utils.Data;

public struct WeaponItem
{
    public string Name { get; }
    public Team Team { get; }
    public RoundType RoundType { get; }

    public WeaponItem(string name, Team team, RoundType roundType)
    {
        Name = name;
        Team = team;
        RoundType = roundType;
    }
}