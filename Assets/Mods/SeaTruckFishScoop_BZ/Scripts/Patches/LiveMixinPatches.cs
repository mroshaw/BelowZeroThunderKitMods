using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.SeaTruckFishScoop_BZ.SeaTruckFishScoopPluginBz;

namespace DaftAppleGames.SeaTruckFishScoop_BZ
{
    /// <summary>
    /// Patches for the SeaTruck Fish Scoop Mod
    /// Patches for the LiveMixin class
    /// </summary>
    [HarmonyPatch(typeof(LiveMixin))]
    public class LiveMixinPatches
    {
        /// <summary>
        /// Here, we're prefixing the TakeDamage method to intercept damage being dealt to a
        /// Creature by the SeaTruck cab.
        /// For context, "taker" is the object taking damage, "dealer" is the object dealing damage.
        /// </summary>
        [HarmonyPatch(nameof(LiveMixin.TakeDamage))]
        [HarmonyPrefix]
        public static bool TakeDamage_Prefix(LiveMixin __instance, GameObject dealer = null)
        {
            if (dealer == null)
            {
                return true;
            }

            // Get the root context of the damage taker
            GameObject taker = __instance.gameObject;
            // Log.LogDebug($"Damage: {dealer.name} did damage to: {taker.name}");
            GameObject rootTaker = UWE.Utils.GetEntityRoot(__instance.gameObject);
            if (rootTaker == null)
            {
                rootTaker = taker;
            }

            // Get the root context of the damage dealer
            GameObject rootDealer = UWE.Utils.GetEntityRoot(dealer);
            if (rootDealer == null)
            {
                rootDealer = dealer;
            }
            // Log.LogDebug($"Dealer root: {rootDealer.name}. Taker root: {rootTaker.name}");

            // Let's see if whatever dealt the damage was a SeaTruck main cab
            SeaTruckSegment SeaTruckSegment = rootDealer.GetComponent<SeaTruckSegment>();
            if (SeaTruckSegment == null)
            {
                // Log.LogDebug("SeaTruckSegment is null. No Scoop.");
                return true;
            }
            if (!SeaTruckSegment.isMainCab)
            {
                // Log.LogDebug("SeaTruckSegment is not Main Cab. No Scoop.");
                return true;
            }

            // Invoke the might of the scoop
            FishScoop fishScoop = dealer.gameObject.GetComponent<FishScoop>();
            if (fishScoop != null)
            {
                Log.LogDebug("Calling Scoop...");
                // Set caught fish to maximum health
                __instance.ResetHealth();
                bool scoopSuccess = fishScoop.Scoop(rootTaker);
                
                return !scoopSuccess;
            }

            // Allow the method to run
            return true;
        }
    }
}