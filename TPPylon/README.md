# TPPylon plugin

(Part of bcat's [TShock plugins suite](https://github.com/bcat/TShockPlugins).)

This plugin adds a <c>/pylon</c> command to teleport the player to the specified
[pylon](https://terraria.fandom.com/wiki/Pylons). Of course, pylons natively
allow teleportation, but in a limited fashion compared to what TShock enables.
Most importantly, this plugin supports teleportation directly to a pylon from
any location and any time (including during boss fights and invasions).

## Commands

### `/pylon <pylon type>`

Teleports you to the specified pylon. The pylon must be present in the world.
Supporting pylon types are:

* `cavern`
* `desert`
* `forest`
* `hallow`
* `jungle`
* `mushroom`
* `ocean`
* `universal`

Pylon types are case insensitive, and a unique prefix may be typed instead of
the full type (e.g., `/pylon f` is equivalent to `/pylon forest`).

## Permissions

| Permission | Effect |
| --- | --- |
| `bcat.tp.pylon` | Use the `/pylon` command. |
