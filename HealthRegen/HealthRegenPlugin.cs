using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace HealthRegen;

public sealed class HealthRegenPlugin : BasePlugin, IPluginConfig<HealthRegenConfig>
{
    public override string ModuleName => "HealthRegen";
    public override string ModuleVersion => "1.0.0";

    private readonly Dictionary<int, PlayerRegenState> _players = new();

    public HealthRegenConfig Config { get; set; } = null!;

    public void OnConfigParsed(HealthRegenConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnClientConnect>(OnClientConnect);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);

        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
    }

    public override void Unload(bool hotReload)
    {
        foreach (var state in _players.Values)
            state.Stop();

        _players.Clear();
    }

    private void OnClientConnect(int playerSlot, string name, string ipAddress)
    {
        if (!_players.ContainsKey(playerSlot))
            _players[playerSlot] = new PlayerRegenState(this, playerSlot);
    }

    private void OnClientDisconnect(int playerSlot)
    {
        if (_players.TryGetValue(playerSlot, out var state))
        {
            state.Stop();
            _players.Remove(playerSlot);
        }
    }

    private HookResult OnPlayerHurt(EventPlayerHurt e, GameEventInfo _)
    {
        if (!Config.Enable || e.Userid is null || !e.Userid.IsValid || e.Userid.IsBot)
            return HookResult.Continue;

        var slot = e.Userid.Slot;
        if (!_players.TryGetValue(slot, out var state))
            _players[slot] = state = new PlayerRegenState(this, slot);

        state.ScheduleRegen();

        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn e, GameEventInfo _)
    {
        if (e.Userid is null || !e.Userid.IsValid)
            return HookResult.Continue;

        if (_players.TryGetValue(e.Userid.Slot, out var state))
        {
            // Optional: always stop regen on spawn (clean state)
            state.Stop();
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath e, GameEventInfo _)
    {
        if (e.Userid is null || !e.Userid.IsValid)
            return HookResult.Continue;

        if (_players.TryGetValue(e.Userid.Slot, out var state))
            state.Stop();

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart e, GameEventInfo _)
    {
        foreach (var state in _players.Values)
            state.Stop();

        return HookResult.Continue;
    }

    [ConsoleCommand("css_healthregen", "Enable or disable health regeneration")]
    [CommandHelper(minArgs: 1, usage: "[0|1]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnHealthRegenCommand(CCSPlayerController? player, CommandInfo command)
    {
        var arg = command.GetArg(1); // "0" or "1"

        switch (arg)
        {
            case "1":
                Config.Enable = true;
                command.ReplyToCommand("[HealthRegen] Enabled!");
                break;
            case "0":
                Config.Enable = false;
                command.ReplyToCommand("[HealthRegen] Disabled!");
                break;
            default:
                command.ReplyToCommand("[HealthRegen] Usage: css_healthregen [0|1]");
                break;
        }
    }

    [ConsoleCommand("css_healthregen_startregendelay", "Get or set the start regen delay")]
    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnStartRegenDelayCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (command.ArgCount < 2)
        {
            command.ReplyToCommand($"css_healthregen_startregendelay = {Config.StartRegenDelay}");
            return;
        }

        if (int.TryParse(command.GetArg(1), out var value))
        {
            Config.StartRegenDelay = value;
            command.ReplyToCommand($"css_healthregen_startregendelay = {value}");
        }
        else
        {
            command.ReplyToCommand("Invalid value. Usage: css_healthregen_startregendelay [int]");
        }
    }

    [ConsoleCommand("css_healthregen_timetoheal", "Get or set the time to heal")]
    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTimeToHealCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (command.ArgCount < 2)
        {
            command.ReplyToCommand($"css_healthregen_timetoheal = {Config.TimeToHeal}");
            return;
        }

        if (int.TryParse(command.GetArg(1), out var value))
        {
            Config.TimeToHeal = value;
            command.ReplyToCommand($"css_healthregen_timetoheal = {value}");
        }
        else
        {
            command.ReplyToCommand("Invalid value. Usage: css_healthregen_timetoheal [int]");
        }
    }
}