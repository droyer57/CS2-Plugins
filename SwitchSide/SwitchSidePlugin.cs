using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Utils;

namespace SwitchSide;

public sealed class SwitchSidePlugin : BasePlugin
{
    public override string ModuleName => "SwitchSide";
    public override string ModuleVersion => "1.0.0";

    private int _teamAScore;
    private int _teamBScore;
    private bool _teamAIsT = true;

    private bool _isEnable = true;

    private int _maxRounds = 11;
    private bool _hasReset;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterListener<Listeners.OnMapStart>(_ => Reset());
    }

    private void Reset()
    {
        _teamAIsT = true;
        _teamAScore = 0;
        _teamBScore = 0;
        _hasReset = false;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (!_isEnable || Utility.IsWarmup)
        {
            return HookResult.Continue;
        }

        var stopScore = _maxRounds / 2;
        if (_teamAScore == stopScore || _teamBScore == stopScore)
        {
            var matchPointEvent = new EventRoundAnnounceMatchPoint(true);
            matchPointEvent.FireEvent(false);
        }

        var totalScore = _teamAScore + _teamBScore;
        bool canTellReset;
        if (stopScore % 2 == 0)
            canTellReset = totalScore == stopScore;
        else
            canTellReset = totalScore == stopScore - 1;

        if (canTellReset)
        {
            Server.NextFrame(() =>
            {
                Server.ExecuteCommand("css_botbuy_nextroundpistol");
                foreach (var player in Utilities.GetPlayers())
                {
                    player.PrintToCenterAlert("Inventory reset next round !");

                    RecipientFilter filter = [player];
                    player.EmitSound("NextRoundPistol", recipients: filter);
                }
            });
        }

        bool canReset;
        if (stopScore % 2 == 0)
            canReset = totalScore == stopScore + 1;
        else
            canReset = totalScore == stopScore;

        if (canReset && !_hasReset)
        {
            _hasReset = true;

            foreach (var player in Utilities.GetPlayers())
            {
                ResetPlayer(player);
            }
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.Team == CsTeam.CounterTerrorist && !player.HasDefuser())
                player.GiveNamedItem("item_defuser");
        }

        return HookResult.Continue;
    }

    private void ResetPlayer(CCSPlayerController player)
    {
        player.ResetInventory(this, "weapon_knife");

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn != null)
        {
            playerPawn.ActionTrackingServices?.WeaponPurchasesThisRound.WeaponPurchases.RemoveAll();
            Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_pActionTrackingServices");
        }

        Server.NextFrame(() =>
        {
            player.GiveNamedItem(player.Team == CsTeam.Terrorist ? "weapon_glock" : "weapon_usp_silencer");

            var playerMoney = player.InGameMoneyServices;
            if (playerMoney == null) return;

            var startMoney = ConVar.Find("mp_startmoney");
            playerMoney.Account = startMoney?.GetPrimitiveValue<int>() ?? 800;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        });
    }

    private HookResult OnRoundEnd(EventRoundEnd evt, GameEventInfo info)
    {
        if (!_isEnable || Utility.IsWarmup)
        {
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

        var stopScore = _maxRounds / 2;
        if (_teamAScore > stopScore || _teamBScore > stopScore)
        {
            Server.ExecuteCommand("mp_maxrounds 0");
            return HookResult.Continue;
        }

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
        foreach (var player in Utility.Players)
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

    #region Commands

    [ConsoleCommand("css_switchside", "Enable or disable switch side plugin")]
    [CommandHelper(minArgs: 1, usage: "[0|1]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnSwitchSideCommand(CCSPlayerController? player, CommandInfo command)
    {
        var arg = command.GetArg(1); // "0" or "1"

        switch (arg)
        {
            case "1":
                _isEnable = true;
                command.ReplyToCommand("[SwitchSide] Enabled!");
                break;
            case "0":
                _isEnable = false;
                command.ReplyToCommand("[SwitchSide] Disabled!");
                break;
            default:
                command.ReplyToCommand("[SwitchSide] Usage: css_switchside [0|1]");
                break;
        }
    }

    [ConsoleCommand("css_switchside_maxrounds", "Get or set the max rounds")]
    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnMaxRoundsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (command.ArgCount < 2)
        {
            command.ReplyToCommand($"css_switchside_maxrounds = {_maxRounds}");
            return;
        }

        if (int.TryParse(command.GetArg(1), out var value))
        {
            _maxRounds = value;
            command.ReplyToCommand($"css_switchside_maxrounds = {value}");
        }
        else
        {
            command.ReplyToCommand("Invalid value. Usage: css_switchside_maxrounds [int]");
        }
    }

    #endregion
}