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
using Terraria.DataStructures;
using Terraria.GameContent;
using TerrariaApi.Server;
using TShockAPI;

namespace BcatTShockPlugins
{
    /// <summary>
    /// Plugin that adds a <c>/pylon</c> command to teleport the player to the specified pylon.
    /// 
    /// <para>Defines the following commands:</para>
    /// 
    /// <list type="table">
    /// <item>
    ///     <term><c>/pylon</c></term>
    ///     <description>Teleports you to the specified pylon.</description>
    /// </item>
    /// </list>
    /// 
    /// <para>Uses the following permissions:</para>
    /// 
    /// <list type="table">
    /// <item>
    ///     <term><c>bcat.tp.pylon</c></term>
    ///     <description>Use the <c>/pylon</c> command.</description>
    /// </item>
    /// </list>
    /// </summary>
    [ApiVersion(2, 1)]
    public class TPPylon : TerrariaPlugin
    {
        /// <summary>
        /// TShock permission to use the <c>/pylon</c> command.
        /// </summary>
        private const string PERMISSION = "bcat.tp.pylon";

        private static readonly Dictionary<string, TeleportPylonType> pylonTypes = new()
        {
            { "cavern", TeleportPylonType.Underground },
            { "desert", TeleportPylonType.Desert },
            { "forest", TeleportPylonType.SurfacePurity },
            { "hallow", TeleportPylonType.Hallow },
            { "jungle", TeleportPylonType.Jungle },
            { "mushroom", TeleportPylonType.GlowingMushroom },
            { "ocean", TeleportPylonType.Beach },
            { "snow", TeleportPylonType.Snow },
            { "universal", TeleportPylonType.Victory },
        };

        public override string Name => "TPPylon";
        public override Version Version => new(0, 1);
        public override string Author => "Jonathan Rascher";
        public override string Description
            => "Plugin that adds a /pylon command to teleport the player to the specified pylon.";

        public TPPylon(Main game) : base(game) { }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command(PERMISSION, OnPylonCommand, "pylon")
            {
                AllowServer = false,
                HelpText = "Teleports you to the specified pylon.",
            });
        }

        /// <summary>
        /// Teleports the player executing the command to the pylon they specify.
        /// </summary>
        /// 
        /// <param name="e">arguments passed to the command.</param>
        private static void OnPylonCommand(CommandArgs e)
        {
            if (e.Parameters.Count != 1)
            {
                e.Player.SendErrorMessage(
                    $"Invalid syntax. Proper syntax: {Commands.Specifier}pylon <pylon type>.");
                e.Player.SendErrorMessage(
                    $"Valid pylon types: {String.Join(", ", pylonTypes.Select(type => type.Key))}.");
                return;
            }

            var matches = pylonTypes
                .Where(t => t.Key.StartsWith(e.Parameters[0].ToLowerInvariant())).ToList();
            if (matches.Count == 0)
            {
                e.Player.SendErrorMessage(
                    $"Invalid pylon type. Valid pylon types: {String.Join(", ", pylonTypes.Select(type => type.Key))}.");
                return;
            }
            if (matches.Count > 1)
            {
                e.Player.SendMultipleMatchError(matches.Select(t => t.Key));
                return;
            }

            string pylonType = matches.Single().Key;
            TeleportPylonType tpPylonType = matches.Single().Value;

            // Vanilla Terraria does not allow *placing* more than one of the same pylon, though it
            // will allow arbitrarily many pylons of the same type to be *used* if they are placed
            // via nonstandard means. For simplicity, we always choose the first pylon of a given
            // type that we find.
            //
            // TODO(bcat): Cycle pylons from left to right if multiple matches are found.
            TeleportPylonInfo pylon
                = Main.PylonSystem.Pylons.Find(p => p.TypeOfPylon == tpPylonType);
            if (pylon.PositionInTiles == Point16.Zero)
            {
                e.Player.SendErrorMessage($"No {pylonType} pylon found in the world.");
                return;
            }

            TShock.Log.ConsoleDebug(
                $"Teleporting \"{e.Player.Name}\" ({e.Player.Index}) to {pylon.TypeOfPylon} pylon at tile ({pylon.PositionInTiles.X}, {pylon.PositionInTiles.Y}).");

            Vector2 pylonCoords = pylon.PositionInTiles.ToWorldCoordinates();
            e.Player.Teleport(pylonCoords.X, pylonCoords.Y);
            e.Player.SendSuccessMessage($"Teleported to the {pylonType} pylon.");
        }
    }
}
