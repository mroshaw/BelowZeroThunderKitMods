using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums.Patches
{
    /// <summary>
    /// Patches for the Aquarium class. 
    /// </summary>
    [HarmonyPatch(typeof(Aquarium))]
    internal class AquariumPatches
    {
            /// <summary>
            /// Adds a bubble Custom Emitter to the vanilla Aquarium if selected in mod config 
            /// </summary>
            [HarmonyPatch(nameof(Aquarium.Start))]
            [HarmonyPostfix]
            private static void Start_Postfix(Aquarium __instance)
            {
                ModDebugLog.LogDebug("In Aquarium.Start...");

                // Skip if configured to not add bubble audio
                if (!ConfigFile.BubbleAudioEnabled)
                {
                    return;
                }
                
                // Custom aquariums already have emitters
                if (__instance.GetComponent<CustomAquarium>())
                {
                    ModDebugLog.LogDebug("Custom aquarium. Skipping bubble emitter...");
                    return;
                }

                // Find the bubbles Game Object and add the emitter if found
                Transform bubblesTransform = __instance.gameObject.transform.Find("Bubbles");
                if (bubblesTransform)
                {
                    ModDebugLog.LogDebug("Adding bubble emitter to vanilla aquarium...");
                    AquariumConfigurator.AddCustomEmitter(bubblesTransform.gameObject);
                }
            }
            
            /// <summary>
            /// Handle adding fish to custom movement aquariums
            /// </summary>
            [HarmonyPatch(nameof(Aquarium.AddItem))]
            [HarmonyPostfix]
            private static void AddItem_Postfix(Aquarium __instance, InventoryItem item)
            {
                FishManager fishManager = __instance.GetComponent<FishManager>();
                if (!fishManager)
                {
                    return;
                }

                Aquarium.FishTrack fishTrack =
                    __instance.GetTrackByItem(item.item.gameObject);
                if (fishTrack != null)
                {
                    fishManager.AddFish(fishTrack);
                }
            }

            /// <summary>
            /// Capture the occupied track before a fish is removed
            /// </summary>
            [HarmonyPatch(nameof(Aquarium.RemoveItem))]
            [HarmonyPrefix]
            private static void RemoveItem_Prefix(Aquarium __instance, InventoryItem item,
                out Aquarium.FishTrack __state)
            {
                if (!__instance.GetComponent<FishManager>())
                {
                    __state = null;
                    return;
                }

                __state = __instance.GetTrackByItem(item.item.gameObject);
            }

            /// <summary>
            /// Handle removing fish from custom movement aquariums
            /// </summary>
            [HarmonyPatch(nameof(Aquarium.RemoveItem))]
            [HarmonyPostfix]
            private static void RemoveItem_Postfix(Aquarium __instance,
                Aquarium.FishTrack __state)
            {
                FishManager fishManager = __instance.GetComponent<FishManager>();
                if (fishManager && __state != null)
                {
                    fishManager.RemoveFish(__state);
                }
            }
    }
}
