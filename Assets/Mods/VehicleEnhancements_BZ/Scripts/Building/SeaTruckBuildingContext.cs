using System.Collections.Generic;
using UnityEngine;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ.Building
{
    /// <summary>
    /// Tracks and validates the current Habitat Builder placement inside a SeaTruck.
    /// </summary>
    internal static class SeaTruckBuildingContext
    {
        private static Constructable selectedConstructable;
        private static SeaTruckSegment placementSegment;
        private static GameObject placementSegmentRoot;
        private static GameObject placedObject;
        private static bool selectedPrefabSupported;
        private static bool checkingSubModule;
        private static bool checkingPlacementSpace;

        internal static bool IsActive =>
            ConfigFile.EnableBuildingInside &&
            Builder.isPlacing &&
            selectedPrefabSupported;

        private static bool CanPlaceOnGround =>
            selectedConstructable &&
            selectedConstructable.allowedOnGround;

        internal static void SetSelectedPrefab(GameObject prefab)
        {
            selectedConstructable = prefab ? prefab.GetComponent<Constructable>() : null;
            selectedPrefabSupported =
                selectedConstructable &&
                selectedConstructable.allowedInSub &&
                (selectedConstructable.allowedOnWall || selectedConstructable.allowedOnGround) &&
                !prefab.GetComponent<ConstructableBase>();
            ResetPlacement();
        }

        internal static void ResetPlacement()
        {
            placementSegment = null;
            placementSegmentRoot = null;
            placedObject = null;
            checkingSubModule = false;
            checkingPlacementSpace = false;
        }

        internal static bool IsPlayerInsideSeaTruck()
        {
            return Player.main && Player.main.currentInterior is SeaTruckSegment;
        }

        internal static bool IsPlayerInsideSeaTruckChain(SeaTruckSegment segment)
        {
            if (!segment || !Player.main)
            {
                return false;
            }

            SeaTruckSegment currentSegment = Player.main.currentInterior as SeaTruckSegment;
            return currentSegment &&
                   SeaTruckSegment.GetHead(currentSegment) == SeaTruckSegment.GetHead(segment);
        }

        internal static SeaTruckSegment FilterPlacementBlocker(SeaTruckSegment segment)
        {
            if (!segment || !IsActive || !IsPlayerInsideSeaTruckChain(segment))
            {
                return segment;
            }

            SetPlacementSegment(segment);
            return null;
        }

        internal static void AlignPlacementWithSegment(RaycastHit hit, ref Quaternion rotation)
        {
            if (!IsActive || !IsPlayerInsideSeaTruck() || !hit.collider)
            {
                return;
            }

            SeaTruckSegment segment = hit.collider.GetComponentInParent<SeaTruckSegment>();
            if (!segment)
            {
                return;
            }

            if (!IsPlayerInsideSeaTruckChain(segment) || !segment.IsInside(hit.collider.gameObject))
            {
                return;
            }

            SetPlacementSegment(segment);
            if (selectedConstructable.alignWithSurface)
            {
                return;
            }

            Vector3 surfaceNormal = hit.normal.normalized;
            SurfaceType surfaceType = GetSurfaceType(surfaceNormal, segment);

            if (surfaceType == SurfaceType.Wall && selectedConstructable.allowedOnWall)
            {
                Vector3 wallUp = Vector3.ProjectOnPlane(segment.transform.up, surfaceNormal);
                if (wallUp.sqrMagnitude > 0.001f)
                {
                    rotation = Quaternion.LookRotation(surfaceNormal, wallUp.normalized);
                }
            }
            else if (surfaceType == SurfaceType.Ground && CanPlaceOnGround)
            {
                Vector3 segmentUp = segment.transform.up;
                Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, segmentUp);
                if (forward.sqrMagnitude <= 0.001f)
                {
                    forward = Vector3.ProjectOnPlane(-Builder.GetAimTransform().forward, segmentUp);
                }

                if (forward.sqrMagnitude > 0.001f)
                {
                    rotation = Quaternion.LookRotation(forward.normalized, segmentUp);
                }
            }
        }

        internal static SurfaceType GetSurfaceTypeRelativeToSegment(
            Vector3 surfaceNormal,
            SurfaceType defaultSurfaceType)
        {
            if (!placementSegment || !IsActive)
            {
                return defaultSurfaceType;
            }

            return GetSurfaceType(surfaceNormal.normalized, placementSegment);
        }

        internal static GameObject InstantiateForPlacement(GameObject prefab)
        {
            GameObject instance = Object.Instantiate(prefab);
            if (!placementSegment || !IsActive)
            {
                return instance;
            }

            instance.transform.SetParent(placementSegment.transform, true);
            placedObject = instance;
            return instance;
        }

        internal static void RemoveOwningSegmentColliders(List<Collider> colliders)
        {
            if (!checkingPlacementSpace || !placementSegment || !IsActive)
            {
                return;
            }

            for (int index = colliders.Count - 1; index >= 0; index--)
            {
                Collider collider = colliders[index];
                if (IsOwningSegmentGeometry(collider))
                {
                    colliders.RemoveAt(index);
                }
            }
        }

        internal static void BeginPlacementSpaceCheck()
        {
            checkingPlacementSpace = IsActive && placementSegment;
        }

        internal static void EndPlacementSpaceCheck()
        {
            checkingPlacementSpace = false;
        }

        internal static void BeginSubModuleCheck()
        {
            checkingSubModule = true;
        }

        internal static void EndSubModuleCheck()
        {
            checkingSubModule = false;
        }

        internal static bool IsSubModuleFlagOverrideAllowed()
        {
            return checkingSubModule && IsActive && IsPlayerInsideSeaTruck();
        }

        internal static bool IsTaggedPlacementSurfaceAllowed(Collider collider)
        {
            return placementSegment && IsActive && IsPlayerInsideSeaTruckChain(placementSegment) &&
                   placementSegment.IsInside(collider.gameObject) &&
                   IsOwningSegmentGeometry(collider);
        }

        internal static void CompletePlacement(bool placementSucceeded)
        {
            if (!placementSucceeded || !placedObject || !placementSegment)
            {
                ResetPlacement();
                return;
            }

            Constructable constructable = placedObject.GetComponentInParent<Constructable>();
            if (constructable)
            {
                constructable.SetIsInside(true);
            }

            SkyEnvironmentChanged.Send(placedObject, placementSegment);
            ResetPlacement();
        }

        private static SurfaceType GetSurfaceType(Vector3 surfaceNormal, SeaTruckSegment segment)
        {
            float verticalAlignment = Vector3.Dot(surfaceNormal, segment.transform.up);
            if (verticalAlignment < -0.33f)
            {
                return SurfaceType.Ceiling;
            }

            if (verticalAlignment < 0.33f)
            {
                return SurfaceType.Wall;
            }

            return SurfaceType.Ground;
        }

        private static bool IsOwningSegmentGeometry(Collider collider)
        {
            if (!collider)
            {
                return false;
            }

            if (collider.GetComponentInParent<Constructable>())
            {
                return false;
            }

            GameObject colliderRoot = GetEntityRoot(collider.gameObject);
            return colliderRoot && colliderRoot == placementSegmentRoot;
        }

        private static GameObject GetEntityRoot(GameObject gameObject)
        {
            GameObject entityRoot = UWE.Utils.GetEntityRoot(gameObject);
            return entityRoot ? entityRoot : gameObject;
        }

        private static void SetPlacementSegment(SeaTruckSegment segment)
        {
            placementSegment = segment;
            placementSegmentRoot = GetEntityRoot(segment.gameObject);
        }
    }
}
