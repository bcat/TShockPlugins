# bcat's TShock plugins

These are some small [TShock](https://github.com/Pryaxis/TShock) plugins I use
on [Terraria](https://terraria.org/) servers I run for friends and family. I
figured some of them might be more generally useful, so I've uploaded them for
folks to play around with.

## Released

Should be feature complete and reasonably well tested.

* [CheeseTravel](CheeseTravel): Plugin that allows cheesing the Traveling
  Merchant's RNG.
* [ShowUpgrades](ShowUpgrades): Plugin that shows the status of permanent player
  and world upgrades.
* [TPBack](TPBack): Plugin that adds a `/back` command to teleport to the player's
  previous position.
* [TPPylon](TPPylon): Plugin that adds a `/pylon` command to teleport the player
  to the specified pylon.

## Prerelease

Likely to lack significant features and/or testing.

* ForceWorldEvent: Plugin that allows toggling continuous world events on and
  off. (Currently only supports rain.)
* PartyOnJoin: Plugin that automatically adds players to the correct party on
  join. (Currently hardcoded to assign all players to the green party.)

## Instructions

To use these plugins, simply place the relevant plugin `.dll` files into the
`ServerPlugins` subdirectory of your TShock installation. Unless otherwise
noted, no other configuration is needed, though some plugins require permissions
to be created for players to use their commands.

You can download precompiled plugin DLLs from the [latest
release](https://github.com/bcat/TShockPlugins/releases), and you can see the
[changelog](CHANGES.md) for full version history. (Note that since this
repository hosts multiple plugins with distinct individual version numbers, the
releases themselves are simply datestamped.)

## Testing

I've tested these plugins with TShock 5.1.3 (Terraria 1.4.4.9) and they all work
to the best of my knowledge; however, they exist for my personal use first and
foremost, and I provide no guarantee of support.

## Contributing

These plugins are licensed under the [ISC license](LICENSE). Please feel free to
report issues, fork, and send pull requests!

This repository contains a solution file you can load directly in Visual Studio
2022 or later. TShock library dependencies are not included; after cloning the
repo, you'll need to populate a `lib` directory with `OTAPI.dll`,
`TerrariaServer.dll`, and `TShockAPI.dll` from [the latest TShock
release](https://github.com/Pryaxis/TShock/releases/latest).
