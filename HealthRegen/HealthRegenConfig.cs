using CounterStrikeSharp.API.Core;

namespace HealthRegen;

public sealed class HealthRegenConfig : BasePluginConfig
{
    public bool Enable { get; init; } = true;
    public float StartRegenDelay { get; init; } = 5f;
    public float TimeToHeal { get; init; } = 3f;
}