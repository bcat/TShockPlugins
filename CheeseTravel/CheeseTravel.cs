using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

/// <summary>
/// Plugin that allows cheesing the Traveling Merchant's RNG.
/// 
/// <para>Defines the following commands:</para>
/// 
/// <list type="table">
/// <item>
///     <term><c>/resettravel</c></term>
///     <description>Re-randomizes the Traveling Merchant's inventory, if present.</description>
/// </item>
/// <item>
///     <term><c>/spawntravel</c></term>
///     <description>Spawns the Traveling Merchant, if not already present.</description>
/// </item>
/// </list>
/// 
/// <para>Uses the following permissions:</para>
/// 
/// <list type="table">
/// <item>
///     <term><c>bcat.cheesetravel.reset</c></term>
///     <description>Use the <c>/resettravel</c> command.</description>
/// </item>
/// <item>
///     <term><c>bcat.cheesetravel.spawn</c></term>
///     <description>Use the <c>/spawntravel</c> command.</description>
/// </item>
/// </list>
/// </summary>
namespace Bcat.TShockPlugins
{
    [ApiVersion(2, 1)]
    public class CheeseTravel : TerrariaPlugin
    {
        /// <summary>
        /// TShock permission to use the <c>/resettravel</c> command.
        /// </summary>
        private const string PERMISSION_RESET = "bcat.cheesetravel.reset";

        /// <summary>
        /// TShock permission to use the <c>/spawntravel</c> command.
        /// </summary>
        private const string PERMISSION_SPAWN = "bcat.cheesetravel.spawn";
        public override string Name => "CheeseTravel";
        public override Version Version => new(0, 1);
        public override string Author => "Jonathan Rascher";
        public override string Description
            => "Plugin that allows cheesing the Traveling Merchant's RNG.";

        public CheeseTravel(Main game) : base(game) { }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command(PERMISSION_RESET, OnResetTravel, "resettravel")
            {
                HelpText = "Re-randomizes the Traveling Merchant's inventory, if present.",
            });
            Commands.ChatCommands.Add(new Command(PERMISSION_SPAWN, OnSpawnTravel, "spawntravel")
            {
                HelpText = "Spawns the Traveling Merchant, if not already present.",
            }); ;
        }

        /// <summary>
        /// Re-randomizes the Traveling Merchant's inventory, if present. 
        /// </summary>
        /// 
        /// <param name="e">arguments passed to the command.</param>
        private static void OnResetTravel(CommandArgs e)
        {
            if (!HasTravelNpc())
            {
                e.Player.SendErrorMessage(
                    $"Traveling Merchant not present. Try {Commands.Specifier}spawntravel.");
                return;
            }

            Chest.SetupTravelShop();
            NetMessage.SendTravelShop(-1);
            e.Player.SendSuccessMessage("Reset the Traveling Merchant's inventory.");
        }

        /// <summary>
        /// Spawns the Traveling Merchant, if not already present. 
        /// </summary>
        /// 
        /// <param name="e">arguments passed to the command.</param>
        private static void OnSpawnTravel(CommandArgs e)
        {
            if (HasTravelNpc())
            {
                e.Player.SendErrorMessage(
                    $"Traveling Merchant already present. Try {Commands.Specifier}resettravel.");
                return;
            }

            WorldGen.SpawnTravelNPC();
            if (!HasTravelNpc())
            {
                e.Player.SendErrorMessage(
                    "Couldn't spawn Traveling Merchant. Ensure it's not night, an eclipse, or an invasion.");
            }
        }

        /// <summary>
        /// Determines if the Traveling Merchant is currently alive in the world.
        /// </summary>
        /// 
        /// <returns>true if the Traveling Merchant is present, false otherwise.</returns>
        private static bool HasTravelNpc()
        {
            return Main.npc.Any(n => n.active && n.type == NPCID.TravellingMerchant);
        }
    }
}
