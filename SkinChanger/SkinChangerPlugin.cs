using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Utils;

namespace SkinChanger;

public sealed class SkinChangerPlugin : BasePlugin
{
    public override string ModuleName => "SkinChanger";
    public override string ModuleVersion => "1.0.0";

    // Exec => 76561198018247650
    // Bipce => 76561198041968571

    private static readonly Dictionary<ulong, string[]> Models = new()
    {
        {
            76561198041968571,
            [
                @"characters\models\tm_jungle_raider\tm_jungle_raider_variantf2.vmdl",
                @"characters\models\ctm_fbi\ctm_fbi_variantb.vmdl"
            ]
        }
    };

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            foreach (var (_, models) in Models)
            {
                foreach (var model in models)
                {
                    Server.PrecacheModel(model);
                }
            }
        });

        RegisterEventHandler<EventRoundStart>(OnRoundStart);
    }

    private HookResult OnRoundStart(EventRoundStart evt, GameEventInfo info)
    {
        foreach (var player in Utility.HumanPlayers)
        {
            if (!player.IsValid
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null
                || !player.PlayerPawn.Value.IsValid)
                continue;

            if ((CsTeam)player.TeamNum != CsTeam.CounterTerrorist
                && (CsTeam)player.TeamNum != CsTeam.Terrorist)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (!Models.TryGetValue(player.SteamID, out var models))
                continue;

            Server.NextFrame(() =>
            {
                if (pawn == null || !pawn.IsValid) return;

                var model = (CsTeam)player.TeamNum == CsTeam.Terrorist ? models[0] : models[1];
                pawn.SetModel(model);
                var c = pawn.Render;
                pawn.Render = Color.FromArgb(255, c.R, c.G, c.B);
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            });
        }

        return HookResult.Continue;
    }
}