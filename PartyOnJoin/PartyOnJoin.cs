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
    /// <summary>
    /// Plugin that automatically adds players to the correct party on join. (Currently hardcoded to
    /// assign all players to the green party.)
    /// </summary>
    [ApiVersion(2, 1)]
    public class PartyOnJoin : TerrariaPlugin
    {
        private const string PLAYER_DATA_SET_TEAM = "Bcat.TShockPlugins.PartyOnJoin.setTeam";

        private const int GREEN_TEAM = 2;

        public override string Name => "PartyOnJoin";
        public override Version Version => new(0, 1);
        public override string Author => "Jonathan Rascher";
        public override string Description
            => "Plugin that automatically adds players to the correct party on join.";

        public PartyOnJoin(Main game) : base(game) { }

        public override void Initialize()
        {
            // TODO(bcat): This works, but is it actually the optimal hook to use? Experiment a bit.
            GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Checks if we're already set the party for a connecting player and assigns them to the
        /// correct party if not.
        /// 
        /// <para>We listen for <see cref="PacketTypes.PlayerUpdate"><c>PlayerUpdate</c>
        /// packets</see> since that's the earliest we can change the player's team and expect the
        /// change to stick.</para>
        /// </summary>
        /// 
        /// <param name="sender">ignored.</param>
        /// <param name="e">arguments from received
        /// <see cref="PacketTypes.PlayerUpdate"><c>PlayerUpdate</c> packet</see>.</param>
        private void OnPlayerUpdate(object? sender, GetDataHandlers.PlayerUpdateEventArgs e)
        {
            if (e.Player.GetData<bool>(PLAYER_DATA_SET_TEAM))
            {
                return;
            }

            // TODO(bcat): Make the team selection configurable. It'd be nice to have three tiers of
            // config: user assignments, group assignments, and default assignment. (That probably
            // means we need to wait for *login* to set the team, not just *join*.)
            e.Player.SetTeam(GREEN_TEAM);
            e.Player.SetData(PLAYER_DATA_SET_TEAM, true);
            TShock.Log.ConsoleInfo($"[PartyOnJoin] Set team to green for \"{e.Player.Name}\".");
        }
    }
}
