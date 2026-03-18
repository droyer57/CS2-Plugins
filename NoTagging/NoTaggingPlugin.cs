using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace NoTagging;

public sealed class NoTaggingPlugin : BasePlugin
{
    public override string ModuleName => "NoTaggingPlugin";
    public override string ModuleVersion => "1.0.0";

    public override void Load(bool hotReload)
    {
        // RegisterListener<Listeners.OnTick>(() =>
        // {
        //     foreach (var player in Utilities.GetPlayers().Where(x =>
        //                  x is { IsValid: true, PawnIsAlive: true }))
        //     {
        //         var playerPawn = player.PlayerPawn.Value;
        //
        //         if (playerPawn is { IsValid: true, VelocityModifier: < 1.0f })
        //         {
        //             playerPawn.VelocityModifier = 1.0f;
        //             Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_flVelocityModifier");
        //             Server.ExecuteCommand($"say {playerPawn.VelocityModifier}");
        //         }
        //     }
        // });

        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
    }

    private HookResult OnPlayerHurt(EventPlayerHurt e, GameEventInfo info)
    {
        ResetPlayerVelocity(e.Userid);
        return HookResult.Continue;
    }

    private void ResetPlayerVelocity(CCSPlayerController? player)
    {
        if (player?.IsValid != true)
        {
            return;
        }

        Server.NextFrame(() =>
        {
            Server.NextFrame(() =>
            {
                Server.NextFrame(() =>
                {
                    if (!player.IsValid || player.IsBot || !player.PlayerPawn.IsValid ||
                        player.PlayerPawn.Value == null)
                    {
                        return;
                    }

                    player.PlayerPawn.Value.VelocityModifier = 1.0f;
                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                });
            });
        });
    }
}