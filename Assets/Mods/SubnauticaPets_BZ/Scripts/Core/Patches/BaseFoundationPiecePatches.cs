using HarmonyLib;
using UnityEngine;
using DaftAppleGames.SubnauticaPets.Pets;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Patches
{
    [HarmonyPatch(typeof(BaseFoundationPiece))] internal class BaseFoundationPiecePatches
    {
        private const float MoonpoolWaterWidth = 10.35f;
        private const float MoonpoolWaterDepth = 6.9f;
        private const float MoonpoolBlockerHeight = 3.0f;
        private const float CollisionFilterHorizontalMargin = 2.0f;
        private const float CollisionFilterVerticalMargin = 2.0f;

        /// <summary>
        ///     Adds a physical Pet blocker and a surrounding filter that lets every non-Pet collider pass through it.
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

            Vector3 originalFishColliderSize = fishCollider.size;
            Vector3 blockerSize = new Vector3(MoonpoolWaterWidth, MoonpoolBlockerHeight, MoonpoolWaterDepth);

            GameObject collisionFilterGameObject = new GameObject("PetMoonpoolCollisionFilter");
            collisionFilterGameObject.layer = LayerMask.NameToLayer("Default");
            collisionFilterGameObject.transform.SetParent(poolColliderTransform, false);

            BoxCollider collisionFilterTrigger = collisionFilterGameObject.AddComponent<BoxCollider>();
            collisionFilterTrigger.center = fishCollider.center;
            collisionFilterTrigger.size = new Vector3(
                blockerSize.x + CollisionFilterHorizontalMargin * 2.0f,
                blockerSize.y + CollisionFilterVerticalMargin * 2.0f,
                blockerSize.z + CollisionFilterHorizontalMargin * 2.0f);
            collisionFilterTrigger.isTrigger = true;

            MoonpoolPetCollisionFilter collisionFilter =
                collisionFilterGameObject.AddComponent<MoonpoolPetCollisionFilter>();
            collisionFilter.Init(fishCollider, collisionFilterTrigger);
            collisionFilter.PrimeExistingOverlaps();

            // Expand the physical collider only after existing non-Pet collision pairs have been ignored.
            fishCollider.size = blockerSize;
            Physics.SyncTransforms();

            Debug.Log($"[SubnauticaPets] Moonpool pet protection initialized on {__instance.gameObject.name}; " +
                      $"surface={poolColliderTransform.name}; originalSurfaceSize={originalFishColliderSize}; " +
                      $"petBlockerSize={fishCollider.size}; collisionFilterSize={collisionFilterTrigger.size}; " +
                      "non-Pet colliders selectively ignored.");
        }
    }
}
