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
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

namespace BcatTShockPlugins
{
    [ApiVersion(2, 1)]
    public class TPBack : TerrariaPlugin
    {
        private const byte TELEPORT_FLAGS_NPC = 0b00000001;

        public override string Name => "TPBack";
        public override Version Version => new(0, 1);
        public override string Author => "Jonathan Rascher";
        public override string Description
            => "Plugin that adds a /back command to teleport to the player's previous position.";

        private static Vector2[] backPositions = new Vector2[Main.maxPlayers];
        private static Vector2[] lastPositions = new Vector2[Main.maxPlayers];

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
            ServerApi.Hooks.NetSendData.Register(this, OnSendData);
            ServerApi.Hooks.GamePostUpdate.Register(this, OnPostUpdate);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GamePostUpdate.Deregister(this, OnPostUpdate);
                ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
                GetDataHandlers.PlayerSpawn -= OnSpawn;
            }
            base.Dispose(disposing);
        }

        private void OnBackCommand(CommandArgs e)
        {
            int i = e.Player.Index;
            if (backPositions[i] == Vector2.Zero)
            {
                e.Player.SendErrorMessage("Can't go back since you haven't teleported yet.");
                return;
            }

            e.Player.Teleport(backPositions[i].X, backPositions[i].Y);
            e.Player.SendSuccessMessage("Teleported back to your previous location.");
        }

        // Saves back position in response to respawns at the player's home location. For example:
        //
        // * Game event: Respawn after death
        // * Item use: Shellphone (Home) / Magic Mirror / Ice Mirror
        // * Item use: Potion of Return
        // * Item use: Recall Potion
        // * Server command: /home
        private void OnSpawn(object? sender, GetDataHandlers.SpawnEventArgs e)
        {
            if (e.SpawnContext != PlayerSpawnContext.SpawningIntoWorld)
            {
                SaveBackPos(e.Player, $"OnSpawn ({e.SpawnContext})");
            }
        }

        // Saves back position in response to server teleport packets. This covers all teleportation
        // to destinations other than the players's home location. For example:
        //
        // * Game event: Pylon network activation
        // * Game event: Potion of Return portal activation
        // * Game event: Teleporter activation
        // * Item use: Hook of Dissonance [ignored]
        // * Item use: Rod of Discord / Rod of Harmony [ignored]
        // * Item use: Shellphone (Ocean)
        // * Item use: Shellphone (Spawn) / Magic Conch
        // * Item use: Shellphone (Underworld) / Demon Conch
        // * Item use: Teleportation Potion
        // * Item use: Wormhole Potion
        // * Server command: /back
        // * Server command: /spawn
        // * Server command: /tp
        // * Server command: /tpnpc
        // * Server command: /tppos
        private void OnSendData(SendDataEventArgs e)
        {
            if (e.MsgId != PacketTypes.Teleport)
            {
                return;
            }

            int flags = e.number;
            int target = (int)e.number2;
            int destX = (int)e.number3;
            int destY = (int)e.number4;
            int style = e.number5;
            int extra = e.number6;

            String caller
                = $"OnSendData (packet Teleport, remote {e.remoteClient}, ignore {e.ignoreClient}, flags {flags}, target {target}, dest ({destX}, {destY}), style {style}, extra {extra})";

            // We only handle teleports targeting players, not NPCs.
            if ((flags & TELEPORT_FLAGS_NPC) != 0)
            {
                TShock.Log.ConsoleDebug($"[TPBack] Ignored NPC teleport: {caller}.");
                return;
            }

            // If we're not teleporting an NPC, the target ID must be the teleported player.
            TSPlayer? player = GetPlayer(target);
            if (player == null)
            {
                TShock.Log.ConsoleWarn(
                    $"[TPBack] Invalid teleport target player {target}: {caller}.");
                return;
            }

            // The Rod of Discord, Rod of Harmony, and the Hook of Dissonance are technically
            // teleportation items; however, they're only used to travel short distances and it's
            // inconvenient for them to overwrite the saved back position. We ignore them.
            //
            // Note that TShock also uses the RodOfDiscord teleport style for server teleport
            // commands. We can identify use of the actual Rod of Discord item by checking if the
            // packet is being broadcast to every client *except* the target player. (The client
            // using the rod sends a Teleport packet to server and expects the server to relay that
            // packet to other connected clients.)
            if (e.remoteClient == -1 && e.ignoreClient == target
                && (style == TeleportationStyleID.RodOfDiscord
                || style == TeleportationStyleID.QueenSlimeHook))
            {
                TShock.Log.ConsoleDebug(
                    $"[TPBack] Ignored rod/hook teleport for \"{player.Name}\" ({target}): {caller}.");
                return;
            }

            SaveBackPos(player, caller);
        }

        // Records the most recent position of each player after every game tick. We need do this
        // since by the time a Teleport packet is sent from the server, the teleporting player has
        // already been moved and their previous location has been lost.
        //
        // Ideally, we'd listen to PlayerUpdate packets rather than updating the location on tick;
        // however, PlayerUpdate doesn't seem to be received by the server frequently enough, and
        // this loop is cheap enough (and performs zero allocations) that the naive approach works.
        private static void OnPostUpdate(EventArgs e)
        {
            for (int i = 0; i < Main.maxPlayers; ++i)
            {
                TSPlayer? player = TShock.Players[i];
                if (player != null && player.Active)
                {
                    lastPositions[i].X = player.X;
                    lastPositions[i].Y = player.Y;
                }
                else
                {
                    lastPositions[i].X = 0;
                    lastPositions[i].Y = 0;
                }
            }
        }

        private static void SaveBackPos(TSPlayer player, string caller)
        {
            int i = player.Index;
            if (lastPositions[i] == Vector2.Zero)
            {
                TShock.Log.ConsoleWarn(
                    $"[TPBack] Unknown last position for \"{player.Name}\" ({i}): {caller}.");
                return;
            }

            TShock.Log.ConsoleDebug(
                $"[TPBack] Set back position ({lastPositions[i].X}, {lastPositions[i].Y}) for \"{player.Name}\" ({i}): {caller}.");
            backPositions[i].X = lastPositions[i].X;
            backPositions[i].Y = lastPositions[i].Y;
        }

        private static TSPlayer? GetPlayer(int i)
        {
            return i >= 0 && i < Main.maxPlayers ? TShock.Players[i] : null;
        }
    }
}
