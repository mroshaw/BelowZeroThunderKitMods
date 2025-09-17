using HarmonyLib;
using static DaftAppleGames.SeaTruckFishScoop_BZ.SeaTruckFishScoopPluginBz;

namespace DaftAppleGames.SeaTruckFishScoop_BZ
{
    /// <summary>
    /// Patches for the SeaTruck Fish Scoop Mod
    /// </summary>
    ///
    [HarmonyPatch(typeof(SeaTruckMotor))]
    public class SeatruckMotorPatches
    {
        /// <summary>
        /// Disable the fishscoop if we stop piloting the Sea Truck
        /// </summary>
        [HarmonyPatch(nameof(SeaTruckMotor.StopPiloting))]
        [HarmonyPostfix]
        public static void StopPiloting_Postfix(SeaTruckMotor __instance)
        {
            FishScoop fishScoop = __instance.gameObject.GetComponent<FishScoop>();
            if (!fishScoop)
            {
                return;
            }
            
            Log.LogDebug("Stopped piloting SeaTruck with a Fish Scoop. Setting scoop state...");
            fishScoop.StopPiloting();
        }
    }
}