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
    /// <summary>
    /// Plugin that adds a <c>/back</c> command to teleport to the player's previous position.
    /// </summary>
    [ApiVersion(2, 1)]
    public class TPBack : TerrariaPlugin
    {
        /// <summary>
        /// Packet 65 (Teleport) flag indicating that the target is an NPC rather than a player.
        /// </summary>
        /// <seealso href="https://tshock.readme.io/docs/multiplayer-packet-structure#player-npc-teleport-65"/>
        private const byte TELEPORT_FLAGS_NPC = 0b00000001;

        public override string Name => "TPBack";
        public override Version Version => new(0, 1);
        public override string Author => "Jonathan Rascher";
        public override string Description
            => "Plugin that adds a /back command to teleport to the player's previous position.";

        /// <summary>
        /// Array of <c>/back</c> command positions for all players in <see cref="TShock.Players"/>.
        /// </summary>
        private static Vector2[] backPositions = new Vector2[Main.maxPlayers];

        /// <summary>
        /// Array of most recently seen positions for all players in <see cref="TShock.Players"/>.
        /// </summary>
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

        /// <summary>
        /// Teleports the player executing the command to their most recent
        /// <see cref="backPositions">back position</see>.
        /// </summary>
        /// 
        /// <param name="e">arguments passed to the command.</param>
        private static void OnBackCommand(CommandArgs e)
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

        /// <summary>
        /// Saves back position in response to respawns at the player's home location. For example:
        /// 
        /// <list type="bullet">
        /// <item><description>Game event: Respawn after death</description></item>
        /// <item><description>Item use: Shellphone (Home) / Magic Mirror / Ice
        /// Mirror</description></item>
        /// <item><description>Item use: Potion of Return</description></item>
        /// <item><description>Item use: Recall Potion</description></item>
        /// <item><description>Server command: <c>/home</c></description></item>
        /// </list>
        /// </summary>
        /// 
        /// <param name="sender">ignored.</param>
        /// <param name="e">arguments from received
        /// <see cref="PacketTypes.PlayerSpawn"><c>PlayerSpawn</c>
        /// packet</see>.</param>
        private static void OnSpawn(object? sender, GetDataHandlers.SpawnEventArgs e)
        {
            String debugContext
                = $"OnSpawn (target {e.PlayerId}, dest ({e.SpawnX}, {e.SpawnY}), timer {e.RespawnTimer}, context {e.SpawnContext})";

            // On connect, the client appears to send a PlayerSpawn packet with type RecallFromItem
            // while the player's position is still (0, 0). Weird, but we just ignore it.
            if (e.SpawnContext == PlayerSpawnContext.SpawningIntoWorld
                || e.Player.X == 0 && e.Player.Y == 0)
            {
                TShock.Log.ConsoleDebug($"[TPBack] Ignored initial spawn: {debugContext}.");
                return;
            }

            SaveBackPosition(e.Player, debugContext);
        }

        /// <summary>
        /// Saves back position in response to server teleport packets. This covers all
        /// teleportation to destinations other than the players's home location. For example:
        /// 
        /// <list type="bullet">
        /// <item><description>Game event: Pylon network activation</description></item>
        /// <item><description>Game event: Potion of Return portal activation</description></item>
        /// <item><description>Game event: Teleporter activation</description></item>
        /// <item><description>Item use: Hook of Dissonance [ignored]</description></item>
        /// <item><description>Item use: Rod of Discord / Rod of Harmony
        /// [ignored]</description></item>
        /// <item><description>Item use: Shellphone (Ocean)</description></item>
        /// <item><description>Item use: Shellphone (Spawn) / Magic Conch</description></item>
        /// <item><description>Item use: Shellphone (Underworld) / Demon Conch</description></item>
        /// <item><description>Item use: Teleportation Potion</description></item>
        /// <item><description>Item use: Wormhole Potion</description></item>
        /// <item><description>Server command: <c>/back</c></description></item>
        /// <item><description>Server command: <c>/spawn</c></description></item>
        /// <item><description>Server command: <c>/tp</c></description></item>
        /// <item><description>Server command: <c>/tpnpc</c></description></item>
        /// <item><description>Server command: <c>/tppos</c></description></item>
        /// </list>
        /// </summary>
        /// 
        /// <param name="e">arguments from sent packet.</param>
        private static void OnSendData(SendDataEventArgs e)
        {
            if (e.MsgId != PacketTypes.Teleport)
            {
                return;
            }

            // See https://tshock.readme.io/docs/multiplayer-packet-structure#player-npc-teleport-65
            // for packet structure.
            int flags = e.number;
            int target = (int)e.number2;
            int destX = (int)e.number3;
            int destY = (int)e.number4;
            int style = e.number5;
            int extra = e.number6;

            String debugContext
                = $"OnSendData (Teleport, remote {e.remoteClient}, ignore {e.ignoreClient}, target {target}, dest ({destX}, {destY}), flags {flags}, style {style}, extra {extra})";

            // We only handle teleports targeting players, not NPCs.
            if ((flags & TELEPORT_FLAGS_NPC) != 0)
            {
                TShock.Log.ConsoleDebug($"[TPBack] Ignored NPC teleport: {debugContext}.");
                return;
            }

            // If we're not teleporting an NPC, the target ID must be the teleported player.
            TSPlayer? player = GetPlayer(target);
            if (player == null)
            {
                TShock.Log.ConsoleWarn(
                    $"[TPBack] Invalid teleport target player {target}: {debugContext}.");
                return;
            }

            // The Rod of Discord, Rod of Harmony, and the Hook of Dissonance are technically
            // teleportation items; however, they're only used to travel short distances and it's
            // inconvenient for them to overwrite the saved back position. We ignore them.
            //
            // Note that TShock also uses the RodOfDiscord teleport style for server teleport
            // commands. We can identify use of the actual Rod of Discord item by checking if the
            // packet is being broadcast to every client *except* the target player. (When using a
            // rod or hook item, the client sends a Teleport packet to server and expects the server
            // to relay that packet to other connected clients.)
            if (e.remoteClient == -1 && e.ignoreClient == target
                && (style == TeleportationStyleID.RodOfDiscord
                || style == TeleportationStyleID.QueenSlimeHook))
            {
                TShock.Log.ConsoleDebug(
                    $"[TPBack] Ignored rod/hook teleport for \"{player.Name}\" ({target}): {debugContext}.");
                return;
            }

            SaveBackPosition(player, debugContext);
        }

        /// <summary>
        /// Records the most recent position of each player after every game tick. We need do this
        /// since by the time a Teleport packet is sent from the server, the teleporting player has
        /// already been moved and their previous location has been lost.
        /// 
        /// <para>Ideally, we'd listen to <see cref="PacketTypes.PlayerUpdate"><c>PlayerUpdate</c>
        /// packets</see> rather than updating the location on tick; however, <c>PlayerUpdate</c>
        /// doesn't seem to be received by the server frequently enough, and this loop is cheap
        /// enough (plus performs zero allocations) that the naive approach works.
        /// </para>
        /// </summary>
        /// 
        /// <param name="e">ignored.</param>
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

        /// <summary>
        /// Records the specified player's current position for use in the next <c>/back</c> command
        /// sent by that player.
        /// </summary>
        /// 
        /// <param name="player">the player whose position should be saved.</param>
        /// <param name="debugContext">a human-readable string identifying the caller, including
        /// information from any relevant packet(s).</param>
        private static void SaveBackPosition(TSPlayer player, string debugContext)
        {
            int i = player.Index;
            if (lastPositions[i] == Vector2.Zero)
            {
                TShock.Log.ConsoleWarn(
                    $"[TPBack] Unknown last position for \"{player.Name}\" ({i}): {debugContext}.");
                return;
            }

            TShock.Log.ConsoleDebug(
                $"[TPBack] Set back position ({lastPositions[i].X}, {lastPositions[i].Y}) for \"{player.Name}\" ({i}): {debugContext}.");
            backPositions[i].X = lastPositions[i].X;
            backPositions[i].Y = lastPositions[i].Y;
        }

        /// <summary>
        /// Gets the TShock player with the specified index (slot).
        /// </summary>
        /// 
        /// <param name="i">the player index to look up. Not required to be in bounds, as it may
        /// come directly from a received packet.</param>
        /// <returns>the player with index <paramref name="i"/>, or <see langword="null"/> if no
        /// such player exists.</returns>
        private static TSPlayer? GetPlayer(int i)
        {
            return i >= 0 && i < Main.maxPlayers ? TShock.Players[i] : null;
        }
    }
}
