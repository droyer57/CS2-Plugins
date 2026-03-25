using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Utils;

namespace Misc;

public sealed class MiscPlugin : BasePlugin
{
    public override string ModuleName => "Misc";
    public override string ModuleVersion => "1.0.0";

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart evt, GameEventInfo info)
    {
        if (Utility.IsWarmup)
        {
            return HookResult.Continue;
        }

        var buyTime = ConVar.Find("mp_buytime")?.GetPrimitiveValue<float>() ?? 20;
        AddTimer(buyTime, () => Server.ExecuteCommand("mp_death_drop_gun 0"));

        Server.ExecuteCommand("mp_death_drop_gun 1");

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath evt, GameEventInfo info)
    {
        if (Utility.IsWarmup)
            return HookResult.Continue;

        var player = evt.Userid;

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        foreach (var otherPlayer in Utilities.GetPlayers())
        {
            if (otherPlayer.Slot == player.Slot || otherPlayer.Team != player.Team)
                continue;

            if (!otherPlayer.PawnIsAlive)
                continue;

            RecipientFilter filter = [otherPlayer];
            otherPlayer.EmitSound("TeammateDown", recipients: filter);
        }

        return HookResult.Continue;
    }
}