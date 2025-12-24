using HarmonyLib;

namespace DaftAppleGames.StartupCommand
{
    [HarmonyPatch(typeof(Player))]
    public class PlayerPatches
    {
        /// <summary>
        /// Run the command list when the Player starts
        /// </summary>
        [HarmonyPatch(nameof(Player.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(Player __instance)
        {
            StartupCommands.RunCommands();
        }
    }
}