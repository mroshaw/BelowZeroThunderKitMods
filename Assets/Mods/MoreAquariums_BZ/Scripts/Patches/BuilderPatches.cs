using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums.Patches
{
    /// <summary>
    /// Removes explicitly ignored colliders from Builder obstacle results.
    /// </summary>
    [HarmonyPatch(typeof(Builder))]
    internal static class BuilderPatches
    {
        /// <summary>
        /// Prevents marked aquarium colliders from blocking base-piece placement.
        /// </summary>
        [HarmonyPatch(nameof(Builder.GetObstacles))]
        [HarmonyPostfix]
        private static void GetObstacles_Postfix(List<GameObject> results)
        {
            if (results == null)
            {
                return;
            }

            for (int index = results.Count - 1; index >= 0; index--)
            {
                GameObject obstacle = results[index];
                if (obstacle &&
                    obstacle.GetComponentInParent<BuilderIgnoreCollider>())
                {
                    ModDebugLog.LogDebug(
                        $"Builder ignored marked collider obstacle '{obstacle.name}'.");
                    results.RemoveAt(index);
                }
            }
        }
    }
}
