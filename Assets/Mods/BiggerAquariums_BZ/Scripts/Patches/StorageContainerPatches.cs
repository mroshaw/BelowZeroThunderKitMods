using HarmonyLib;
using static DaftAppleGames.BiggerAquariums.BiggerAquariumsPlugin;

namespace DaftAppleGames.BiggerAquariums
{
    /// <summary>
    /// Harmony patches for the aquarium storage container
    /// </summary>
    [HarmonyPatch(typeof(StorageContainer))]
    internal class StorageContainerPatches
    {
        [HarmonyPatch(nameof(StorageContainer.Awake))]
        [HarmonyPrefix]
        public static bool Start_Prefix(StorageContainer __instance)
        {
            ModDebugLog.LogDebug($"StorageContainer.Awake called on {__instance.gameObject.name}");

            BiggerAquarium biggerAquarium = __instance.GetComponent<BiggerAquarium>();
            if (!biggerAquarium)
            {
                return true;
            }

            ModDebugLog.LogDebug("StorageContainer.Awake: in new 'BiggerAquarium'!");
            ModDebugLog.LogDebug(
                $"Setting storage container height and width to {biggerAquarium.StorageHeight}x{biggerAquarium.StorageWidth}");
            __instance.height = biggerAquarium.StorageHeight;
            __instance.width = biggerAquarium.StorageWidth;
            ModDebugLog.LogDebug($"Done configuring storage container!");
            return true;
        }
    }
}