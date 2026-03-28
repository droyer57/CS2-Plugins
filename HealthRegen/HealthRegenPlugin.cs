using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Utils;

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
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnHealthRegenCommand(CCSPlayerController? player, CommandInfo command)
    {
        Config.Enable = Utility.UseCommand(command, Config.Enable);
    }

    [ConsoleCommand("css_healthregen_startregendelay", "Get or set the start regen delay")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnStartRegenDelayCommand(CCSPlayerController? player, CommandInfo command)
    {
        Config.StartRegenDelay = Utility.UseCommand(command, Config.StartRegenDelay);
    }

    [ConsoleCommand("css_healthregen_timetoheal", "Get or set the time to heal")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTimeToHealCommand(CCSPlayerController? player, CommandInfo command)
    {
        Config.TimeToHeal = Utility.UseCommand(command, Config.TimeToHeal);
    }
}