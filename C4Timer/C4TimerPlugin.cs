using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace C4Timer;

public class C4TimerConfig : BasePluginConfig
{
    [JsonPropertyName("EnableTimer")] public bool EnableTimer { get; set; } = true;

    [JsonPropertyName("EnableProgressBar")]
    public bool EnableProgressBar { get; set; } = true;

    [JsonPropertyName("TimerStarting")] public int TimerStarting { get; set; } = 45;

    [JsonPropertyName("LeftSideTimer")] public string LeftSideTimer { get; set; } = "-[ ";

    [JsonPropertyName("RightSideTimer")] public string RightSideTimer { get; set; } = " ]-";

    [JsonPropertyName("EnableColorMessage")]
    public bool EnableColorMessage { get; set; } = true;

    [JsonPropertyName("SidesTimerColor")] public string SidesTimerColor { get; set; } = "45:white";

    [JsonPropertyName("TimeColor")] public string TimeColor { get; set; } = "20:yellow, 10:red, 5:darkred";

    [JsonPropertyName("ProgressBarColor")]
    public string ProgressBarColor { get; set; } = "20:yellow, 10:red, 5:darkred";
}

public class C4TimerPlugin : BasePlugin, IPluginConfig<C4TimerConfig>
{
    public override string ModuleName => "C4 Timer";
    public override string ModuleVersion => "1.6";
    public override string ModuleAuthor => "belom0r";

    private readonly Dictionary<int, string> _timeColor = new();
    private readonly Dictionary<int, string> _progressBarColor = new();
    private readonly Dictionary<int, string> _sidesTimerColor = new();

    private bool _plantedC4;

    private int _timerLength;
    private int _timerСountdown;

    private string _messageCountdown = "";

    private Timer? _countdownToExplosion;

    public required C4TimerConfig Config { get; set; }

    public void OnConfigParsed(C4TimerConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventBombPlanted>(BombPlantedPost); //bPlantedC4 = true
        RegisterEventHandler<EventRoundPrestart>((_, _) =>
        {
            _plantedC4 = false;
            return HookResult.Continue;
        });
        RegisterEventHandler<EventBombExploded>((_, _) =>
        {
            _plantedC4 = false;
            return HookResult.Continue;
        });
        RegisterEventHandler<EventBombDefused>((_, _) =>
        {
            _plantedC4 = false;
            return HookResult.Continue;
        });

        if (Config.EnableColorMessage)
        {
            RegisterListener<Listeners.OnTick>(OnTick);
        }

        ColorMsg(Config.TimeColor, _timeColor);
        ColorMsg(Config.ProgressBarColor, _progressBarColor);
        ColorMsg(Config.SidesTimerColor, _sidesTimerColor);
    }

    private HookResult BombPlantedPost(EventBombPlanted @event, GameEventInfo info)
    {
        var planted = GetPlantedC4();

        if (planted == null)
            return HookResult.Continue;

        _plantedC4 = true;

        _timerLength = _timerСountdown = (int)(planted.TimerLength + 1.0f);

        Config.TimerStarting = Math.Clamp(Config.TimerStarting, 0, _timerLength);

        _countdownToExplosion = new Timer(1.0f, CountdownToExplosionC4, TimerFlags.REPEAT);

        Timers.Add(_countdownToExplosion);

        return HookResult.Continue;
    }

    private void OnTick()
    {
        if (string.IsNullOrEmpty(_messageCountdown) || !_plantedC4)
            return;

        foreach (var player in GetPlayers().Where(x => x.IsValid))
        {
            player.PrintToCenterHtml(_messageCountdown);
        }
    }

    private void CountdownToExplosionC4()
    {
        _timerСountdown--;

        if (_timerСountdown == 0)
        {
            _messageCountdown = WrapWithColor("C4 bomb exploded !!!", "darkred");
        }
        else if (_timerСountdown < 0 || !_plantedC4)
        {
            Server.NextFrame(() =>
            {
                _countdownToExplosion!.Kill();
                Timers.Remove(_countdownToExplosion);
                _countdownToExplosion = null;

                _timerLength = 0;
                _timerСountdown = 0;
                _messageCountdown = "";
            });
            return;
        }
        else
        {
            _messageCountdown = GenerateCountdownMessage();
        }

        if (!Config.EnableColorMessage)
        {
            Server.NextFrame(() =>
            {
                VirtualFunctions.ClientPrintAll(HudDestination.Center, _messageCountdown, 0, 0, 0, 0, 0);
            });
        }
    }

    private string GenerateCountdownMessage()
    {
        if (_timerСountdown > Config.TimerStarting)
            return "";

        var timerStyle = GenerateTimerStyle();
        var progressBarStyle = GenerateProgressBarStyle();

        return Config.EnableColorMessage
            ? $"{timerStyle}{progressBarStyle}"
            : ConnectStrings(timerStyle, progressBarStyle);
    }

    private string GenerateTimerStyle()
    {
        if (!Config.EnableTimer)
            return "";

        var leftSide = WrapWithColor(Config.LeftSideTimer, _sidesTimerColor[_timerСountdown]);
        var time = WrapWithColor(_timerСountdown.ToString(), _timeColor[_timerСountdown]);
        var rightSide = WrapWithColor(Config.RightSideTimer, _sidesTimerColor[_timerСountdown]);

        return $"{leftSide}{time}{rightSide}";
    }

    private string GenerateProgressBarStyle()
    {
        if (!Config.EnableProgressBar)
            return "";

        var total = Math.Min(Config.TimerStarting, _timerLength);

        var progressBar = new char[total];
        for (var i = 0; i < total; i++)
            progressBar[i] = i >= _timerСountdown ? '-' : '|';

        var progressBarTxt = WrapWithColor(new string(progressBar), _progressBarColor[_timerСountdown]);

        return Config.EnableTimer && Config.EnableColorMessage ? "<br>" + progressBarTxt : progressBarTxt;
    }

    private string WrapWithColor(string text, string color)
    {
        return Config.EnableColorMessage ? $"<font color='{color}'>{text}</font>" : text;
    }

    private CPlantedC4? GetPlantedC4()
    {
        var plantedC4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4");

        if (plantedC4 == null || !plantedC4.Any())
            return null;

        return plantedC4.FirstOrDefault();
    }

    private string ConnectStrings(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str2))
            return str1;

        return $"{str1}{Environment.NewLine}{str2}";
    }

    private void ColorMsg(string msg, Dictionary<int, string> colorDictionary)
    {
        colorDictionary.Clear();

        for (var i = 0; i <= Config.TimerStarting; i++)
            colorDictionary[i] = "white";

        if (!Config.EnableColorMessage || string.IsNullOrEmpty(msg))
            return;

        foreach (var color in msg.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var elements = color.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (elements.Length != 2) continue;

                var index = int.Parse(elements[0]);
                var colorValue = elements[1];

                for (var i = index; i >= 0; i--)
                    colorDictionary[i] = colorValue;
            }
            catch
            {
                Logger.LogError($"Invalid color format: {color}");
            }
        }
    }

    private List<CCSPlayerController> GetPlayers()
    {
        return Utilities.GetPlayers().Where(player =>
            player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected).ToList();
    }
}