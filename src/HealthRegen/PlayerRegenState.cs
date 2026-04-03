using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace HealthRegen;

public class PlayerRegenState
{
    private readonly HealthRegen _plugin;
    private readonly int _slot;

    private Timer? _startDelayTimer;
    private Timer? _tickTimer;

    public PlayerRegenState(HealthRegen plugin, int slot)
    {
        _plugin = plugin;
        _slot = slot;
    }

    private HealthRegenConfig Config => _plugin.Config;

    private CCSPlayerController? GetController()
    {
        var controller = Utilities.GetPlayerFromSlot(_slot);
        return controller?.IsValid == true ? controller : null;
    }

    private static CCSPlayerPawn? GetPawn(CCSPlayerController controller)
        => controller.PlayerPawn.Value;

    private static bool IsAlive(CCSPlayerController controller)
        => controller is { IsValid: true, PawnIsAlive: true };

    public void ScheduleRegen()
    {
        Stop(); // cancel previous timers cleanly

        if (!Config.Enable)
            return;

        // Start after delay
        _startDelayTimer = _plugin.AddTimer(
            Config.StartRegenDelay,
            StartTicking,
            TimerFlags.STOP_ON_MAPCHANGE
        );
    }

    private void StartTicking()
    {
        var controller = GetController();
        if (controller is null || !IsAlive(controller))
            return;

        var pawn = GetPawn(controller);
        if (pawn is null)
            return;

        var maxHealth = pawn.MaxHealth;
        if (maxHealth <= 0)
            return;

        // Example interpretation:
        // TimeToHeal = total seconds to go from 0 to MaxHealth by +1 each tick
        // So tick interval = TimeToHeal / MaxHealth
        var tickInterval = (float)(Config.TimeToHeal / (double)maxHealth);

        // Safety clamp
        if (tickInterval < 0.01f)
            tickInterval = 0.01f;

        _tickTimer = _plugin.AddTimer(
            tickInterval,
            () => Tick(controller),
            TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE
        );
    }

    private void Tick(CCSPlayerController controller)
    {
        if (!Config.Enable || !controller.IsValid || !IsAlive(controller))
        {
            Stop();
            return;
        }

        var pawn = GetPawn(controller);
        if (pawn is null || pawn.Health >= pawn.MaxHealth)
        {
            Stop();
            return;
        }

        pawn.Health++;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }

    public void Stop()
    {
        _startDelayTimer?.Kill();
        _startDelayTimer = null;

        _tickTimer?.Kill();
        _tickTimer = null;
    }
}