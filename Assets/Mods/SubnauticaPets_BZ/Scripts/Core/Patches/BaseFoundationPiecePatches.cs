using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Patches
{
    [HarmonyPatch(typeof(BaseFoundationPiece))] internal class BaseFoundationPiecePatches
    {
        /// <summary>
        ///     Patches the Start method, adding a special collider to the Moon Pool to stop pets falling in
        /// </summary>
        [HarmonyPatch(nameof(BaseFoundationPiece.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(BaseFoundationPiece __instance)
        {
            if (__instance.gameObject.name != "BaseMoonpool(Clone)") return;

            // Check the config setting and only create the new collider if the preference is set
            if (ConfigFile.DisableMoonpoolCollider)
            {
                ModDebugLog.LogDebug(
                    "DisableMoonpoolCollider is set to true. Skipping creation of blocking collider...");
                return;
            }

            // Below Zero
            var poolColliderTransform = __instance.transform.Find("blockfish");

            if (!poolColliderTransform)
            {
                ModDebugLog.LogError("Couldn't find 'blockfish' object on Moonpool!");
                return;
            }

            var layer = poolColliderTransform.gameObject.layer;
            if (!poolColliderTransform)
                ModDebugLog.LogError($"Could not patch MoonPool on {__instance.gameObject.name}! Couldn't find pool collider transform!");

            var fishCollider = poolColliderTransform.GetComponent<BoxCollider>();

            var petColliderGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            petColliderGameObject.name = "petcollider";
            petColliderGameObject.layer = layer;
            petColliderGameObject.tag = poolColliderTransform.gameObject.tag;
            petColliderGameObject.transform.SetParent(__instance.transform);
            petColliderGameObject.transform.position = fishCollider.transform.position + new Vector3(0, -1f, 0);
            petColliderGameObject.transform.rotation = fishCollider.transform.rotation;
            petColliderGameObject.transform.localScale = fishCollider.size + new Vector3(0, 2f, 0);

            Object.Destroy(petColliderGameObject.GetComponent<MeshRenderer>());
            Object.Destroy(petColliderGameObject.GetComponent<MeshFilter>());
        }
    }
}