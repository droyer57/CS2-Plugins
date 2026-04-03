using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace BotInfiniteMoney;

public sealed class BotInfiniteMoneyPlugin : BasePlugin
{
    public override string ModuleName => "Bot Infinite Money";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleDescription => "Gives bots $16000 at the start of each round so they can buy freely.";

    private int _roundCount;

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(_ => Reset());
    }

    public override void Unload(bool hotReload)
    {
    }

    private void Reset()
    {
        _roundCount = 0;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        _roundCount++;
        if (_roundCount <= 1)
        {
            return HookResult.Continue;
        }

        GiveMoneyToBots();

        return HookResult.Continue;
    }

    private void GiveMoneyToBots()
    {
        var players = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");

        foreach (var player in players)
        {
            // Only target valid, connected bots
            if (!player.IsValid)
                continue;

            if (!player.IsBot)
                continue;

            if (!player.PlayerPawn.IsValid)
                continue;

            // Access the player's in-game money
            var playerMoney = player.InGameMoneyServices;
            if (playerMoney == null)
                continue;

            playerMoney.Account = 16000;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        }
    }
}