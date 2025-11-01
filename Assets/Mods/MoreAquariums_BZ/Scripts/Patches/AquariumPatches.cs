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
            public static void Start_Postfix(Aquarium __instance)
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
    }
}
