# bcat's TShock plugins

These are some small [TShock](https://github.com/Pryaxis/TShock) plugins I use
on [Terraria](https://terraria.org/) servers I run for friends and family. I
figured some of them might be more generally useful, so I've uploaded them for
folks to play around with.

## Plugin index

I've divided the plugins into two categories: plugins that are "generally
available" (feature complete and reasonably well tested) and those that are
"early access" (lacking features, buggy, or otherwise incomplete).

### Generally available

* ShowUpgrades: Plugin that shows the status of permanent player and world
  upgrades.
* TPBack: Plugin that adds a `/back` command to teleport to the player's
  previous position.
* TPPylon: Plugin that adds a `/pylon` command to teleport the player to the
  specified pylon.

### Early access

* ForceWorldEvent: Plugin that allows toggling continuous world events on and
  off. (Currently only supports rain.)
* PartyOnJoin: Plugin that automatically adds players to the correct party on
  join. (Currently hardcoded to assign all players to the green party.)

## (Lack of) support

I've tested these plugins with TShock 5.1.3 (Terraria 1.4.4.9) and they all work
to the best of my knowledge; however, they exist for my personal use first and
foremost, and I provide no guarantee of support. Please do feel free to use
them, report issues, and send pull requests, though!

## Building from source

This repository contains a solution file you can load directly in Visual Studio
2022 or later. TShock library dependencies are not included; after cloning the
repo, you'll need to populate a `lib` directory with `OTAPI.dll`,
`TerrariaServer.dll`, and `TShockAPI.dll` from [the latest TShock
release](https://github.com/Pryaxis/TShock/releases).
