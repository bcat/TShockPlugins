# ShowUpgrades plugin

(Part of bcat's [TShock plugins suite](https://github.com/bcat/TShockPlugins).)

This plugin shows the status of [permanent player and world
upgrades](https://terraria.fandom.com/wiki/Consumables#Permanent_boosters). As
of Terraria 1.4.4.9, there's no way to view which upgrade items have been
consumed without loading the relevant player or world file in a third-party
editor, so this plugin offers a convenient in-game alternative for you and your
players.

## Commands

### `/showupgrades`

Shows the current player's permanent upgrades. Used (unlocked) and not used
(locked) upgrades will be listed separately.

### `/showupgrades <player>`

Shows the specified player's permanent upgrades. Used (unlocked) and not used
(locked) upgrades will be listed separately. The `<player>` parameter follows
standard TShock syntax and may be a player name (e.g., `Silvarren`), name prefix
(e.g., `sil` for `Silvarren`), or ID (e.g., `0`).

### `/showworldupgrades`

Shows the world's permanent upgrades. Used (unlocked) and not used (locked) upgrades
will be listed separately.

## Permissions

| Permission | Effect |
| --- | --- |
| `bcat.showupgrades.self` | Use the `/showupgrades` command. |
| `bcat.showupgrades.others` | Use the `/showupgrades <player>` command. |
| `bcat.showupgrades.world` | Use the `/showworldupgrades` command. |
