// Copyright 2022 Jonathan Rascher
//
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted, provided that the above
// copyright notice and this permission notice appear in all copies.
//
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.NetModules;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

namespace BcatTShockPlugins
{
    [ApiVersion(2, 1)]
    public class TPBack : TerrariaPlugin
    {
        private const byte TELEPORT_FLAGS_NPC = 0b00000001;
        private const byte TELEPORT_FLAGS_P2P = 0b00000010;

        private const string PLAYER_DATA_BACK_POS = "BcatTShockPlugins.TPBack.backPos";
        private const string PLAYER_DATA_UPCOMING_BACK_POS
            = "BcatTShockPlugins.TPBack.upcomingBackPos";

        public override string Name => "TPBack";
        public override Version Version => new(0, 1);
        public override string Author => "Jonathan Rascher";
        public override string Description
            => "Plugin that adds a /back command to teleport to the player's previous position.";

        public TPBack(Main game) : base(game) { }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("bcat.tpback.allow", OnBackCommand, "back")
            {
                AllowServer = false,
                HelpText
                    = "Teleports you back to your previous location (before your last teleport).",
            });

            GetDataHandlers.PlayerSpawn += OnSpawn;
            GetDataHandlers.Teleport += OnTeleport;
            GetDataHandlers.ReadNetModule += OnNetModule;
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            ServerApi.Hooks.NetSendData.Register(this, OnSendData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                GetDataHandlers.ReadNetModule -= OnNetModule;
                GetDataHandlers.Teleport -= OnTeleport;
                GetDataHandlers.PlayerSpawn -= OnSpawn;
            }
            base.Dispose(disposing);
        }

        private void OnBackCommand(CommandArgs e)
        {
            if (!e.Player.ContainsData(PLAYER_DATA_BACK_POS))
            {
                e.Player.SendErrorMessage("Can't go back since you haven't teleported yet.");
                return;
            }

            var prevPos = e.Player.GetData<Vector2>(PLAYER_DATA_BACK_POS);
            e.Player.Teleport(prevPos.X, prevPos.Y);
            e.Player.SendSuccessMessage("Teleported back to your previous location.");
        }

        // Stores back position in response to client spawns to the player's home location. For
        // example:
        //
        // * Potion of Return, initial recall
        // * Recall Potion
        // * Shellphone (Home) / Ice Mirror / Magic Mirror
        // 
        // Also handles respawn after death, as well as server /home commands.
        private void OnSpawn(object? sender, GetDataHandlers.SpawnEventArgs e)
        {
            // Note that on connect, the client appears to send a PlayerSpawn packet with type
            // RecallFromItem while the player's position is still (0, 0). Weird, but we ignore it.
            if (e.SpawnContext == PlayerSpawnContext.SpawningIntoWorld
                || e.Player.X == 0 && e.Player.Y == 0)
            {
                return;
            }

            SetBackPos(e.Player, new(e.Player.X, e.Player.Y), $"OnSpawn ({e.SpawnContext})");
        }

        // Stores back position in response to client teleportation items where the client chooses
        // the exact destination. For example:
        //
        // * Potion of Return, return portal
        // * Wormhole Potion
        //
        // * Hook of Dissonance [ignored]
        // * Rod of Discord / Rod of Harmony [ignored]
        private void OnTeleport(object? sender, GetDataHandlers.TeleportEventArgs e)
        {
            String caller
                = $"OnTeleport (target {e.ID} ({e.X}, {e.Y}), flags {e.Flag}, style {e.Style}, extra {e.ExtraInfo})";

            // We only handle teleports targeting players, not NPCs. (It's unclear if clients should
            // ever *send* NPC teleports, but it seems prudent to be safe.)
            if ((e.Flag & TELEPORT_FLAGS_NPC) != 0)
            {
                TShock.Log.ConsoleDebug(
                    $"[TPBack] Ignored client NPC teleport from \"{e.Player.Name}\" ({e.Player.Index}): {caller}.");
                return;
            }

            // When the client teleports to another player using a Wormhole Potion, the server
            // clears the player-to-player flag and then echoes the packet back to the sending
            // client. (This differs from other client teleportation items where the server
            // explicitly does *not* echo the packet back to the sender.) As such, we set an
            // upcoming back position only in anticipation of the server's teleport broadcast.
            if ((e.Flag & TELEPORT_FLAGS_P2P) != 0)
            {
                SetUpcomingBackPos(e.Player, new(e.Player.X, e.Player.Y), caller);
                return;
            }

            switch (e.Style)
            {
                // The Rod of Discord/Harmony and the Hook of Dissonance are technically
                // teleportation items; however, they're only used to travel short distances and
                // it's inconvenient for them to overwrite the saved back position. We ignore them.
                case TeleportationStyleID.RodOfDiscord:
                case TeleportationStyleID.QueenSlimeHook:
                    TShock.Log.ConsoleDebug(
                        $"[TPBack] Ignored client rod/hook teleport from \"{e.Player.Name}\" ({e.Player.Index}): {caller}.");
                    break;

                // For all other client teleportation items, the server does not echo the packet
                // back to the sending client, so we need to set the back position immediately.
                default:
                    SetBackPos(e.Player, new(e.Player.X, e.Player.Y), caller);
                    break;
            }
        }

        private void OnNetModule(object? sender, GetDataHandlers.ReadNetModuleEventArgs e)
        {
            switch (e.ModuleType)
            {
                // Stores possible back position in response to client pylon network activation.
                case GetDataHandlers.NetModuleType.TeleportPylon:
                    // Rewind stream to start of TeleportPylon net module data (immediately after
                    // 16-bit module type).
                    e.Data.Position = sizeof(ushort);

                    if (e.Data.ReadByte()
                        == (int)NetTeleportPylonModule.SubPacketType.PlayerRequestsTeleport)
                    {
                        SetUpcomingBackPos(e.Player, new(e.Player.X, e.Player.Y),
                            "OnNetModule (module TeleportPylon, type PlayerRequestsTeleport)");
                    }
                    break;
            }
        }

        private void OnGetData(GetDataEventArgs e)
        {
            TSPlayer? player = GetPlayer(e.Msg.whoAmI);
            if (player == null)
            {
                return;
            }

            switch (e.MsgID)
            {
                // Stores possible back position in response to client teleportation items where the
                // server chooses the exact destination. For example:
                //
                // * Shellphone (Ocean) / Magic Conch
                // * Shellphone (Spawn)
                // * Shellphone (Underworld) / Demon Conch
                // * Teleportation Potion
                case PacketTypes.TeleportationPotion:
                    int type = -1;
                    if (e.Length >= sizeof(byte) + sizeof(byte))
                    {
                        type = e.Msg.readBuffer[e.Index];
                    }

                    SetUpcomingBackPos(player, new(player.X, player.Y),
                        $"OnGetData (packet TeleportationPotion, type {type})");
                    break;
            }
        }

        private void OnSendData(SendDataEventArgs e)
        {
            TSPlayer? player;

            switch (e.MsgId)
            {
                // Before teleporting a player in response to a server command, TShock sends the
                // client a TileSendSquare packet centered at the teleport destination. When this
                // packet is sent, the user is still located at their previous position, so we stash
                // that position to use in case a Teleport packet is subsequently sent from the
                // server for the same player.
                case PacketTypes.TileSendSquare:
                    player = GetPlayer(e.remoteClient);
                    if (player == null)
                    {
                        break;
                    }

                    SetUpcomingBackPos(player, new(player.X, player.Y),
                        $"OnSendData (packet TileSendSquare, remote {e.remoteClient}, ignore {e.ignoreClient})");
                    break;

                // Stores back position in response to server teleports. This includes the server's
                // handling of certain client teleportation items (e.g., pylons, Teleportation
                // Potions, etc.) as well as TShock's teleport commands (e.g., /spawn, /tp, /tpnpc,
                // /tppos, etc.).
                case PacketTypes.Teleport:
                    String caller
                        = $"OnSendData (packet Teleport, remote {e.remoteClient}, ignore {e.ignoreClient}, target {e.number2} ({e.number3}, {e.number4}), flags {e.number}, style {e.number5}, extra {e.number6})";

                    // We only handle teleports targeting players, not NPCs.
                    if ((e.number & TELEPORT_FLAGS_NPC) != 0)
                    {
                        TShock.Log.ConsoleDebug($"[TPBack] Ignored server NPC teleport: {caller}.");
                        break;
                    }

                    // If we're not teleporting an NPC, the target ID must be the teleported player.
                    player = GetPlayer((int)e.number2);
                    if (player == null)
                    {
                        break;
                    }

                    // When the client sends a Teleport packet to the server, the server relays that
                    // packet to every connected client *except* the client that initiated the
                    // teleport. Since we already recorded the back position via OnClientTeleport in
                    // that case, don't try to record it again.
                    if (e.ignoreClient == player.Index)
                    {
                        TShock.Log.ConsoleDebug(
                            $"[TPBack] Ignored server relayed teleport for \"{player.Name}\" ({player.Index}): {caller}.");
                        break;
                    }

                    // Since this event event occurs after the player has moved, we have to have
                    // stored the (possible) back position previously. However, nothing guarantees
                    // we've caught all the possible sequences of packets.
                    //
                    // TODO(bcat): The only known case we don't currently handle are Teleporters, as
                    // they are activated via mechanisms and it's not clear how to intercept the
                    // activation in a general way. (Is handling the HitSwitch packet sufficient?)
                    if (!player.ContainsData(PLAYER_DATA_UPCOMING_BACK_POS))
                    {
                        TShock.Log.ConsoleWarn(
                            $"[TPBack] Unknown back pos for \"{player.Name}\" ({player.Index}): {caller}.");
                        break;
                    }

                    // Since this packet is sent *after* the player location has already been
                    // changed, we need to use the player's location recorded immediately before
                    // the location change instead.
                    SetBackPos(player, player.GetData<Vector2>(PLAYER_DATA_UPCOMING_BACK_POS),
                        caller);
                    player.RemoveData(PLAYER_DATA_UPCOMING_BACK_POS);
                    break;
            }
        }

        private static void SetUpcomingBackPos(TSPlayer player, Vector2 pos, string caller)
        {
            TShock.Log.ConsoleDebug(
                $"[TPBack] Set upcoming back pos ({pos.X}, {pos.Y}) for \"{player.Name}\" ({player.Index}): {caller}, held \"{player.ItemInHand.Name}\", selected \"{player.SelectedItem.Name}\".");
            player.SetData(PLAYER_DATA_UPCOMING_BACK_POS, pos);
        }

        private static void SetBackPos(TSPlayer player, Vector2 pos, string caller)
        {
            TShock.Log.ConsoleDebug(
                $"[TPBack] Set back pos ({pos.X}, {pos.Y}) for \"{player.Name}\" ({player.Index}): {caller}, held \"{player.ItemInHand.Name}\", selected \"{player.SelectedItem.Name}\".");
            player.SetData(PLAYER_DATA_BACK_POS, pos);
        }

        private static TSPlayer? GetPlayer(int i)
        {
            return i >= 0 && i < Main.maxPlayers ? TShock.Players[i] : null;
        }
    }
}
