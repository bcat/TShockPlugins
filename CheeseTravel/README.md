# CheeseTravel plugin

(Part of bcat's [TShock plugins suite](https://github.com/bcat/TShockPlugins).)

This plugin allows cheesing the Traveling Merchant's RNG. (I know it's a bit
subjective, but simply spawning in the relevant items seems like a bridge too
far.)

## Commands

### `/resettravel`

Re-randomizes the Traveling Merchant's inventory, if present. Note that players
must close and reopen the merchant's shop to see the new items.

### `/spawntravel`

Spawns the Traveling Merchant, if not already present. Note that the merchant
will only spawn at times when vanilla Terraria would allow it (e.g., not at
night, during a boss fight, or while an invasion is ongoing).

## Permissions

| Permission | Effect |
| --- | --- |
| `bcat.cheesetravel.reset` | Use the `/resettravel` command. |
| `bcat.cheesetravel.spawn` | Use the `/spawntravel` command. |
