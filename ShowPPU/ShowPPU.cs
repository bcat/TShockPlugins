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

using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Bcat.TShockPlugins
{
    [ApiVersion(2, 1)]
    public class ShowPPU : TerrariaPlugin
    {
        private const string PERMISSION_OTHERS = "bcat.showppu.others";
        private const string PERMISSION_SELF = "bcat.showppu.self";
        private const string PERMISSION_WORLD = "bcat.showppu.world";

        public override string Name => "ShowPPU";
        public override Version Version => new(0, 1);
        public override string Author => "Jonathan Rascher";
        public override string Description => "Plugin that lists players' permanent power-ups.";

        public ShowPPU(Main game) : base(game) { }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command(PERMISSION_SELF, OnShowPPU, "showppu")
            {
                AllowServer = false,
                HelpText = "Shows a player's active permanent power-ups.",
            });
            Commands.ChatCommands.Add(new Command(PERMISSION_WORLD, OnShowWorldPPU, "showworldppu")
            {
                HelpText = "Shows the world's active permanent power-ups.",
            });
        }

        private void OnShowPPU(CommandArgs e)
        {
            Dictionary<string, bool>? ppus;

            switch (e.Parameters.Count)
            {
                case 0:
                    ppus = GetPPUs(e.Player);
                    break;
                case 1:
                    ppus = GetPPUs(e.Player, e.Parameters[0]);
                    break;
                default:
                    e.Player.SendErrorMessage(
                        $"Invalid syntax. Proper syntax: {Commands.Specifier}showppu [player].");
                    return;
            }

            if (ppus != null)
            {
                SendPPUMessage(e.Player, ppus);
            }
        }

        private static Dictionary<string, bool>? GetPPUs(TSPlayer player, String search)
        {
            if (!player.HasPermission(PERMISSION_OTHERS))
            {
                player.SendErrorMessage(
                    "You do not have permission to view other players' permanent power-ups.");
                return null;
            }

            List<TSPlayer> otherPlayers = TSPlayer.FindByNameOrID(search);
            switch (otherPlayers.Count)
            {
                case 1:
                    return GetPPUs(otherPlayers.Single());
                case 0:
                    player.SendErrorMessage("Invalid player.");
                    return null;
                default:
                    player.SendMultipleMatchError(otherPlayers.Select(p => p.Name));
                    return null;
            }
        }

        private static Dictionary<string, bool> GetPPUs(TSPlayer player)
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

        private static void OnShowWorldPPU(CommandArgs e)
        {
            SendPPUMessage(e.Player, new()
            {
                { "Advanced Combat Techniques", NPC.combatBookWasUsed},
                { "Advanced Combat Techniques: Volume Two", NPC.combatBookVolumeTwoWasUsed},
                { "Peddler's Satchel", NPC.peddlersSatchelWasUsed},
            });
        }

        private static void SendPPUMessage(TSPlayer player, Dictionary<string, bool> ppus)
        {
            player.SendInfoMessage(
                $"Active: {string.Join(", ", ppus.Where(p => p.Value).Select(p => p.Key))}");
            player.SendInfoMessage(
                $"Inactive: {string.Join(", ", ppus.Where(p => !p.Value).Select(p => p.Key))}");
        }
    }
}
