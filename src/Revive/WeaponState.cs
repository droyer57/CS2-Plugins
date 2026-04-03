namespace Revive;

public struct WeaponState
{
    public string Name { get; }
    public int Count { get; }
    public bool IsGrenade { get; }

    public WeaponState(string name, int count, bool isGrenade)
    {
        Name = name;
        Count = count;
        IsGrenade = isGrenade;
    }
}