using HarmonyLib;
using UnityEngine;
using DaftAppleGames.SubnauticaPets.Pets;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Patches
{
    [HarmonyPatch(typeof(BaseFoundationPiece))] internal class BaseFoundationPiecePatches
    {
        private const float MoonpoolTriggerHorizontalInset = 1.5f;

        /// <summary>
        ///     Patches the Start method, adding a trigger that redirects pets away from the Moonpool opening.
        /// </summary>
        [HarmonyPatch(nameof(BaseFoundationPiece.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(BaseFoundationPiece __instance)
        {
            if (__instance.gameObject.name != "BaseMoonpool(Clone)") return;

            Transform poolColliderTransform = __instance.transform.Find("blockfish");

            if (!poolColliderTransform)
            {
                ModDebugLog.LogError("Couldn't find 'blockfish' object on Moonpool!");
                return;
            }

            BoxCollider fishCollider = poolColliderTransform.GetComponent<BoxCollider>();
            if (!fishCollider)
            {
                ModDebugLog.LogError("Couldn't find the 'blockfish' BoxCollider on Moonpool!");
                return;
            }

            if (!poolColliderTransform.GetComponent<MoonpoolPetSurface>())
                poolColliderTransform.gameObject.AddComponent<MoonpoolPetSurface>();

            GameObject petTriggerGameObject = new GameObject("PetMoonpoolTrigger");
            petTriggerGameObject.layer = poolColliderTransform.gameObject.layer;
            petTriggerGameObject.transform.SetParent(poolColliderTransform, false);

            BoxCollider petTriggerCollider = petTriggerGameObject.AddComponent<BoxCollider>();
            petTriggerCollider.center = fishCollider.center;
            petTriggerCollider.size = new Vector3(
                Mathf.Max(0.1f, fishCollider.size.x - MoonpoolTriggerHorizontalInset * 2.0f),
                Mathf.Max(fishCollider.size.y, 3.0f),
                Mathf.Max(0.1f, fishCollider.size.z - MoonpoolTriggerHorizontalInset * 2.0f));
            petTriggerCollider.isTrigger = true;

            MoonpoolPetTrigger petTrigger = petTriggerGameObject.AddComponent<MoonpoolPetTrigger>();
            petTrigger.Init(petTriggerCollider);
            MoonpoolTriggerVisualizer triggerVisualizer =
                petTriggerGameObject.AddComponent<MoonpoolTriggerVisualizer>();
            triggerVisualizer.Init(petTriggerCollider);

            Debug.Log($"[SubnauticaPets] Moonpool pet protection initialized on {__instance.gameObject.name}; " +
                      $"surface={poolColliderTransform.name}; triggerCenter={petTriggerCollider.center}; " +
                      $"sourceSize={fishCollider.size}; triggerSize={petTriggerCollider.size}; " +
                      $"horizontalInset={MoonpoolTriggerHorizontalInset:F2}m; visualization=magenta wireframe.");
        }
    }
}
