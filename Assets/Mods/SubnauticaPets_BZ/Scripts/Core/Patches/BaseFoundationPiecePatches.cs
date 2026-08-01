using HarmonyLib;
using UnityEngine;
using DaftAppleGames.SubnauticaPets.Pets;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Patches
{
    [HarmonyPatch(typeof(BaseFoundationPiece))] internal class BaseFoundationPiecePatches
    {
        /// <summary>
        ///     Patches the Start method, adding a trigger that redirects pets away from the Moonpool opening.
        /// </summary>
        [HarmonyPatch(nameof(BaseFoundationPiece.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(BaseFoundationPiece __instance)
        {
            if (__instance.gameObject.name != "BaseMoonpool(Clone)") return;

            // Check the config setting and only create the pet protection trigger if enabled.
            if (ConfigFile.DisableMoonpoolCollider)
            {
                ModDebugLog.LogDebug(
                    "DisableMoonpoolCollider is set to true. Skipping creation of the Moonpool pet trigger...");
                return;
            }

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

            GameObject petTriggerGameObject = new GameObject("PetMoonpoolTrigger");
            petTriggerGameObject.layer = poolColliderTransform.gameObject.layer;
            petTriggerGameObject.transform.SetParent(poolColliderTransform, false);

            BoxCollider petTriggerCollider = petTriggerGameObject.AddComponent<BoxCollider>();
            petTriggerCollider.center = fishCollider.center;
            petTriggerCollider.size = new Vector3(fishCollider.size.x, Mathf.Max(fishCollider.size.y, 3.0f),
                fishCollider.size.z);
            petTriggerCollider.isTrigger = true;

            MoonpoolPetTrigger petTrigger = petTriggerGameObject.AddComponent<MoonpoolPetTrigger>();
            petTrigger.Init(petTriggerCollider);
        }
    }
}
