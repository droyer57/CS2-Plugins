# CS2-Plugins
CS2-Plugins is a collection of plugins designed to make the game more casual and enjoyable, especially when playing with friends against bots.

## Plugin Features
- `BotAI` and `BotState` were created by [ed0ard](https://github.com/ed0ard/CS2-Bot-Improver). These plugins improve bot behavior and pathfinding.
- `BotBuy` gives bots random weapons from a pool each round.
- `BotNoHeadshot` prevents bots from dealing headshot damage.
- `BotRandomizer` randomizes agent models and music kits for bots.
- `C4Timer` displays the bomb timer.
- `HealthRegen` regenerates health after a short time without taking damage.
- `InfiniteAmmo` enables infinite ammo for weapons only.
- `Misc` includes a variety of gameplay enhancements.
- `NoTagging` disables damage tagging.
- `Revive` allow players to revive dead teammates.
- `SkinChanger` allows assigning custom skins to specific players. Currently, this must be configured in code and requires recompilation.
- `SwitchSide` switches all players' sides each round.

## Game Modes
- `AutoWeapon` gives all players random weapons from a pool each round.
- `OneInTheChamber` gives players a pistol with a single bullet. One-shot kills. The bullet refills on kill.

## Commands
Many of these plugins include commands that you can tweak to your liking. Examples can be found in `cfg/mods/*.cfg`.

## Addon (optional)
I created an [addon](https://steamcommunity.com/sharedfiles/filedetails/?id=3691382937) that includes custom sounds. You can use [MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager) to add it to your server.

## Acknowledgements
A big thanks to ed0ard for `BotAI` and `BotState`. You can find the complete repository [here](https://github.com/ed0ard/CS2-Bot-Improver).