using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace SwitchSide;

public sealed class SwitchSidePlugin : BasePlugin
{
    public override string ModuleName => "SwitchSide";
    public override string ModuleVersion => "1.0.0";

    private bool _isFirstRound = true;

    private int _teamAScore;
    private int _teamBScore;
    private bool _teamAIsT = true;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterListener<Listeners.OnMapStart>(_ => Reset());
    }

    private void Reset()
    {
        _isFirstRound = true;
        _teamAIsT = true;
        _teamAScore = 0;
        _teamBScore = 0;
    }

    private HookResult OnRoundEnd(EventRoundEnd evt, GameEventInfo info)
    {
        if (_isFirstRound)
        {
            _isFirstRound = false;
            return HookResult.Continue;
        }

        var winner = (CsTeam)evt.Winner;
        if (winner is CsTeam.Terrorist or CsTeam.CounterTerrorist)
        {
            var teamAPlayedSide = _teamAIsT ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
            if (winner == teamAPlayedSide) _teamAScore++;
            else _teamBScore++;
        }

        UpdateScore();

        AddTimer(6.9f, () =>
        {
            _teamAIsT = !_teamAIsT;
            SwapAllPlayers();
            UpdateScore();
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    private void UpdateScore()
    {
        var tScore = _teamAIsT ? _teamAScore : _teamBScore;
        var ctScore = _teamAIsT ? _teamBScore : _teamAScore;
        SetMatchScores(tScore, ctScore);
    }

    private static void SwapAllPlayers()
    {
        var players = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

        foreach (var player in players)
        {
            if (!player.IsValid)
                continue;

            if (player.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
                continue;

            var newTeam = player.Team == CsTeam.Terrorist
                ? CsTeam.CounterTerrorist
                : CsTeam.Terrorist;

            player.SwitchTeam(newTeam);
        }
    }

    private static void SetMatchScores(int t, int ct)
    {
        try
        {
            var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager").ToArray();

            foreach (var team in teams)
            {
                var csTeam = (CsTeam)team.TeamNum;
                team.Score = csTeam switch
                {
                    CsTeam.Terrorist => t,
                    CsTeam.CounterTerrorist => ct,
                    _ => team.Score
                };

                Utilities.SetStateChanged(team, "CTeam", "m_iScore");
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }
}