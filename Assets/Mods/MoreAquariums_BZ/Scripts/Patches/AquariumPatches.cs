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
        /// Defers activation while a dynamically created Aquarium is being configured.
        /// </summary>
        [HarmonyPatch(nameof(Aquarium.OnEnable))]
        [HarmonyPrefix]
        private static bool OnEnable_Prefix(Aquarium __instance)
        {
            if (__instance.storageContainer)
            {
                return true;
            }

            ModDebugLog.LogDebug(
                $"Deferring Aquarium.OnEnable for '{__instance.name}' until its " +
                $"storage container has been assigned.");
            return false;
        }

        /// <summary>
        /// Adds a bubble Custom Emitter to the vanilla Aquarium if selected in mod config.
        /// </summary>
        [HarmonyPatch(nameof(Aquarium.Start))]
        [HarmonyPostfix]
        private static void Start_Postfix(Aquarium __instance)
        {
            ModDebugLog.LogDebug("In Aquarium.Start...");

            if (!ConfigFile.BubbleAudioEnabled)
            {
                return;
            }

            if (__instance.GetComponent<CustomAquarium>())
            {
                ModDebugLog.LogDebug("Custom aquarium. Skipping bubble emitter...");
                return;
            }

            Transform bubblesTransform =
                __instance.gameObject.transform.Find("Bubbles");
            if (bubblesTransform)
            {
                ModDebugLog.LogDebug("Adding bubble emitter to vanilla aquarium...");
                AquariumConfigurator.AddCustomEmitter(bubblesTransform.gameObject);
            }
        }

        /// <summary>
        /// Handles adding fish to custom movement aquariums.
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
        /// Captures the occupied track before a fish is removed.
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
        /// Handles removing fish from custom movement aquariums.
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
