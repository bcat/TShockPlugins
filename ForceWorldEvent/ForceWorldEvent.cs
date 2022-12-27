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
    /// Plugin that allows toggling continuous world events on and off. (Currently only supports
    /// rain.)
    /// 
    /// <para>Defines the following commands:</para>
    /// 
    /// <list type="table">
    /// <item>
    ///     <term><c>/forceworldevent</c></term>
    ///     <description>Toggles a forced world event on or off.</description>
    /// </item>
    /// </list>
    /// 
    /// <para>Uses the following permissions:</para>
    /// 
    /// <list type="table">
    /// <item>
    ///     <term><c>bcat.forceworldevent.rain</c></term>
    ///     <description>Force the rain world event.</description>
    /// </item>
    /// </list>
    /// </summary>
    [ApiVersion(2, 1)]
    public class ForceWorldEvent : TerrariaPlugin
    {
        /// <summary>
        /// TShock permission to force the rain world event.
        /// </summary>
        private const string PERMISSION_RAIN = "bcat.forceworldevent.rain";

        public override string Name => "ForceWorldEvent";
        public override Version Version => new(0, 1);
        public override string Author => "Jonathan Rascher";
        public override string Description
            => "Plugin that allows toggling continuous world events on and off.";

        private bool forcedRain = false;

        public ForceWorldEvent(Main game) : base(game) { }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command(OnForceWorldEvent, "forceworldevent")
            {
                HelpText = "Toggles a forced world event on or off.",
            });

            // TODO(bcat): Add config to automatically force events on server startup.
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Toggles the specified forced world event on or off. Note that toggling a forced event
        /// off does not immediately stop the event.
        /// </summary>
        /// 
        /// <param name="e">arguments passed to the command.</param>
        private void OnForceWorldEvent(CommandArgs e)
        {
            if (e.Parameters.Count < 1)
            {
                e.Player.SendErrorMessage(
                    $"Invalid syntax. Proper syntax is {Commands.Specifier}forceworldevent <event type>.");
                return;
            }

            // TODO(bcat): Support other event types besides rain.
            switch (e.Parameters[0])
            {
                case "rain":
                    forcedRain = !forcedRain;
                    e.Player.SendSuccessMessage(
                        "Rain is now" + (forcedRain ? "" : " not") + " forced");
                    break;

                default:
                    e.Player.SendErrorMessage("Invalid event type.");
                    break;
            }
        }

        /// <summary>
        /// On each game tick, checks is any forced events have ended and restarts them if needed.
        /// </summary>
        /// 
        /// <param name="e">ignored.</param>
        private void OnGameUpdate(EventArgs e)
        {
            if (forcedRain && !Main.raining)
            {
                Main.StartRain();
                TSPlayer.All.SendData(PacketTypes.WorldInfo);
                TShock.Log.ConsoleInfo("[ForceWorldEvent] Restarted the rain.");
            }
        }
    }
}
