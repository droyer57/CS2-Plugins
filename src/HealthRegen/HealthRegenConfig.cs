using CounterStrikeSharp.API.Core;

namespace HealthRegen;

public sealed class HealthRegenConfig : BasePluginConfig
{
    public bool Enable { get; set; } = true;
    public float StartRegenDelay { get; set; } = 5f;
    public float TimeToHeal { get; set; } = 3f;
}