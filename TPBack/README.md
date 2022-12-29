# TPBack plugin

(Part of bcat's [TShock plugins suite](https://github.com/bcat/TShockPlugins).)

This plugin that adds a `/back` command to teleport to the player's previous
position. It intends to handle all forms of teleportation, both via in-game
functionality (client server) and TShock commands (server side). Known to
support the following as of Terraria 1.4.4.9:

* Game event: [Pylon](https://terraria.fandom.com/wiki/Pylons) network
  activation
* Game event: [Respawn](https://terraria.fandom.com/wiki/Spawn) after death
* Game event: [Teleporter](https://terraria.fandom.com/wiki/Teleporter)
  activation
* Item use: [Demon Conch](https://terraria.fandom.com/wiki/Demon_Conch)
* Item use: [Magic Conch](https://terraria.fandom.com/wiki/Magic_Conch)
* Item use: [Magic Mirror / Ice
  Mirror](https://terraria.fandom.com/wiki/Magic_Mirrors)
* Item use: [Shellphone](https://terraria.fandom.com/wiki/Shellphone)
* Item use: [Potion of
  Return](https://terraria.fandom.com/wiki/Potion_of_Return)
* Item use: [Recall Potion](https://terraria.fandom.com/wiki/Recall_Potion)
* Item use: [Teleportation
  Potion](https://terraria.fandom.com/wiki/Teleportation_Potion)
* Item use: [Wormhole Potion](https://terraria.fandom.com/wiki/Wormhole_Potion)
* Server command: `/back`
* Server command: `/home`
* Server command: `/spawn`
* Server command: `/tp`
* Server command: `/tpnpc`
* Server command: `/tppos`

Note that although the [Rod of
Discord](https://terraria.fandom.com/wiki/Rod_of_Discord) and [Rod of
Harmony](https://terraria.fandom.com/wiki/Rod_of_Harmony) are both technically
teleportation items, they're only used to travel short distances and are easily
reversed, so this plugin ignores them.

## Commands

### `/back`

Teleports you back to your previously location (before your last teleport). If
you haven't teleported anywhere since joining the server, this command does
nothing. Teleportation using the Rods of Discord and Harmony are ignored for the
purposes of this command. Additionally, only a single previous position is
saved, not a full history. (Executing `/back` twice simply takes you to where
you started.)

Your mileage may vary if your previous position is no longer safe (e.g., you
teleport into the air without a mount, into lava, within solid blocks, etc.).

## Permissions

| Permission | Effect |
| --- | --- |
| `bcat.tp.back` | Use the `/back` command. |
