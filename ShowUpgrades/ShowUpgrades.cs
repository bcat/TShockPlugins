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

using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Bcat.TShockPlugins
{
    /// <summary>
    /// Plugin that shows the status of permanent player and world upgrades.
    /// 
    /// <para>Defines the following commands:</para>
    /// 
    /// <list type="table">
    /// <item>
    ///     <term><c>/showupgrades</c></term>
    ///     <description>Shows a player's permanent upgrades.</description>
    /// </item>
    /// <item>
    ///     <term><c>/showworldupgrades</c></term>
    ///     <description>Shows the world's permanent upgrades.</description>
    /// </item>
    /// </list>
    /// 
    /// <para>Uses the following permissions:</para>
    /// 
    /// <list type="table">
    /// <item>
    ///     <term><c>bcat.showupgrades.self</c></term>
    ///     <description>Use the <c>/showupgrades</c> command.</description>
    /// </item>
    /// <item>
    ///     <term><c>bcat.showupgrades.others</c></term>
    ///     <description>Use the <c>/showupgrades &lt;player&gt;</c> command.</description>
    /// </item>
    /// <item>
    ///     <term><c>bcat.showupgrades.world</c></term>
    ///     <description>Use the <c>/showworldupgrades</c> command.</description>
    /// </item>
    /// </list>
    /// </summary>
    [ApiVersion(2, 1)]
    public class ShowUpgrades : TerrariaPlugin
    {
        /// <summary>
        /// TShock permission to use the <c>/showupgrades</c> command.
        /// </summary>
        private const string PERMISSION_SELF = "bcat.showupgrades.self";

        /// <summary>
        /// TShock permission to use the <c>/showupgrades &lt;player&gt;</c> command.
        /// </summary>
        private const string PERMISSION_OTHERS = "bcat.showupgrades.others";

        /// <summary>
        /// TShock permission to use the <c>/showupgrades</c> command.
        /// </summary>
        private const string PERMISSION_WORLD = "bcat.showupgrades.world";

        /// <summary>
        /// Target line length for <see cref="WrapList"/>.
        /// </summary>
        private const int MESSAGE_LINE_LENGTH = 100;

        public override string Name => "ShowUpgrades";
        public override Version Version => new(1, 0);
        public override string Author => "Jonathan Rascher";
        public override string Description
            => "Plugin that shows the status of permanent player and world upgrades.";

        public ShowUpgrades(Main game) : base(game) { }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command(PERMISSION_SELF, OnShowUpgrades, "showupgrades")
            {
                HelpText = "Shows a player's permanent upgrades.",
            });
            Commands.ChatCommands.Add(
                new Command(PERMISSION_WORLD, OnShowWorldUpgrades, "showworldupgrades")
                {
                    HelpText = "Shows the world's permanent upgrades.",
                });
        }

        /// <summary>
        /// Shows a player's permanent upgrades.
        /// </summary>
        /// 
        /// <param name="e">arguments passed to the command.</param>
        private void OnShowUpgrades(CommandArgs e)
        {
            Dictionary<string, bool>? upgrades = null;
            switch (e.Parameters.Count)
            {
                case 0:
                    if (e.Player is TSServerPlayer)
                    {
                        e.Player.SendErrorMessage(
                            "Server can't have permanent upgrades. Must specify a player name.");
                        break;
                    }
                    upgrades = GetUpgrades(e.Player);
                    break;
                case 1:
                    upgrades = GetUpgrades(e.Player, e.Parameters[0]);
                    break;
                default:
                    e.Player.SendErrorMessage(
                        $"Invalid syntax. Proper syntax: {Commands.Specifier}showupgrades [player].");
                    break;
            }

            if (upgrades != null)
            {
                SendUpgradeMessages(e.Player, upgrades);
            }
        }

        /// <summary>
        /// Gets permanent upgrades for the player matching the search string.
        /// </summary>
        /// 
        /// <param name="recipient">the player to whom the upgrades should be shown. Not necessarily
        /// the player whose upgrades should be looked up.</param>
        /// <param name="search">TShock player search string.</param>
        /// <returns>a dictionary mapping upgrade names to statuses (<see langword="true"/> for
        /// active, <see langword="false"/> for inactive). Will be <see langword="null"/> if the
        /// requesting player lacks appropriate permissions or the search does not yield a unique
        /// player.</returns>
        private static Dictionary<string, bool>? GetUpgrades(TSPlayer recipient, String search)
        {
            if (!recipient.HasPermission(PERMISSION_OTHERS))
            {
                recipient.SendErrorMessage(
                    "You do not have permission to view other players' permanent upgrades.");
                return null;
            }

            List<TSPlayer> otherPlayers = TSPlayer.FindByNameOrID(search);
            switch (otherPlayers.Count)
            {
                case 1:
                    return GetUpgrades(otherPlayers.Single());
                case 0:
                    recipient.SendErrorMessage("Invalid player.");
                    return null;
                default:
                    recipient.SendMultipleMatchError(otherPlayers.Select(p => p.Name));
                    return null;
            }
        }

        /// <summary>
        /// Gets permanent upgrades for the specified player.
        /// </summary>
        /// 
        /// <param name="player">the player whose upgrades should be returned.</param>
        /// <returns>a dictionary mapping upgrade names to statuses (<see langword="true"/> for
        /// active, <see langword="false"/> for inactive).</returns>
        private static Dictionary<string, bool> GetUpgrades(TSPlayer player)
        {
            return new Dictionary<string, bool>
            {
                { "Aegis Fruit", player.TPlayer.usedAegisFruit },
                { "Ambrosia", player.TPlayer.usedAmbrosia },
                { "Arcane Crystal", player.TPlayer.usedArcaneCrystal },
                { "Artisan Loaf", player.TPlayer.ateArtisanBread },
                { "Demon Heart", player.TPlayer.extraAccessory },
                { "Galaxy Pearl", player.TPlayer.usedGalaxyPearl },
                { "Gummy Worm", player.TPlayer.usedGummyWorm },
                { "Minecart Upgrade Kit", player.TPlayer.unlockedSuperCart },
                { "Torch God's Favor", player.TPlayer.unlockedBiomeTorches },
                { "Vital Crystal", player.TPlayer.usedAegisCrystal },
            };
        }

        /// <summary>
        /// Shows the world's permanent upgrades.
        /// </summary>
        /// 
        /// <param name="e">arguments passed to the command.</param>
        private static void OnShowWorldUpgrades(CommandArgs e)
        {
            SendUpgradeMessages(e.Player, new()
            {
                { "Advanced Combat Techniques", NPC.combatBookWasUsed },
                { "Advanced Combat Techniques: Volume Two", NPC.combatBookVolumeTwoWasUsed },
                { "Peddler's Satchel", NPC.peddlersSatchelWasUsed },
            });
        }

        /// <summary>
        /// Sends lists of active and inactive permanent upgrades to the specified player.
        /// </summary>
        /// 
        /// <param name="recipient">the player to whom messages should be sent.</param>
        /// <param name="upgrades">a dictionary mapping upgrade names to statuses
        /// (<see langword="true"/> for active, <see langword="false"/> for inactive).</param>
        private static void SendUpgradeMessages(
            TSPlayer recipient, Dictionary<string, bool> upgrades)
        {
            WrapList(recipient.SendInfoMessage, upgrades.Where(u => u.Value).Select(u => u.Key),
                prefix: "Active: ");
            WrapList(recipient.SendInfoMessage, upgrades.Where(u => !u.Value).Select(u => u.Key),
                prefix: "Inactive: ");
        }

        /// <summary>
        /// Wraps a list of values to approximately <see cref="MESSAGE_LINE_LENGTH"/> characters per
        /// line. Does not guarantee exact wrapping, but should be sufficient to avoid lines longer
        /// than the average client window.
        /// </summary>
        /// 
        /// <param name="sendLine">callback invoked for each line.</param>
        /// <param name="values">the values to be wrapped. If empty, no lines will sent.</param>
        /// <param name="prefix">string to be prepened to the first line.</param>
        /// <param name="continuation">string to be prepened to each line after the first.</param>
        /// <param name="separator">string to be included between values.</param>
        private static void WrapList(Action<string> sendLine, IEnumerable<string> values,
            string prefix = "", string continuation = "    ", string separator = ", ")
        {
            StringBuilder messageBuilder = new(MESSAGE_LINE_LENGTH);

            foreach (string value in values)
            {
                if (messageBuilder.Length == 0)
                {
                    // Add at least one list item per line, even if it exceeds the max length.
                    messageBuilder.Append(prefix);
                    messageBuilder.Append(value);
                }
                else if (messageBuilder.Length + separator.Length + value.Length
                    <= MESSAGE_LINE_LENGTH)
                {
                    // If the next list item fits on the current line, simply append it.
                    messageBuilder.Append(separator);
                    messageBuilder.Append(value);
                }
                else
                {
                    // Otherwise, send the current line and start building the next one. (Again,
                    // always add at least one list item per line, regardless of length.)
                    messageBuilder.Append(separator);
                    sendLine(messageBuilder.ToString());
                    messageBuilder.Clear();
                    messageBuilder.Append(continuation);
                    messageBuilder.Append(value);
                }
            }

            // Send partial line after last list item.
            if (messageBuilder.Length > 0)
            {
                sendLine(messageBuilder.ToString());
            }
        }
    }
}
