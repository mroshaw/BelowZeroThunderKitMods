using System.Collections;
using ProtoBuf;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    ///     Simple MonoBehaviour class to manage Pet DNA collectible behaviour
    /// </summary>
    [ProtoContract]
    internal class PetDna : MonoBehaviour, IProtoEventListener
    {
        private const int CurrentPlacementVersion = 1;
        private const float NestDetectionRange = 3.0f;
        private const int NestDetectionFrameCount = 30;
        private const float GroundProbeOffset = 0.25f;
        private const float GroundProbeRadius = 1.25f;
        private const float GroundSearchDistance = 25.0f;
        private const float GroundClearance = 0.03f;
        private const float MinimumFloorNormalY = 0.5f;
        private const float MaximumFloorHeightAboveSlot = 0.5f;
        private const int GroundPlacementAttemptCount = 120;

        private static readonly Vector3[] GroundProbeDirections =
        {
            Vector3.zero,
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            new Vector3(0.7071068f, 0.0f, 0.7071068f),
            new Vector3(-0.7071068f, 0.0f, 0.7071068f),
            new Vector3(0.7071068f, 0.0f, -0.7071068f),
            new Vector3(-0.7071068f, 0.0f, -0.7071068f)
        };

        private bool wasDeserialized;

        [ProtoMember(1)]
        private int placementVersion;

        private IEnumerator Start()
        {
            if (wasDeserialized && placementVersion >= CurrentPlacementVersion)
            {
                yield break;
            }

            // The nest and its loot slot can be activated in either order. Give the nest
            // manager a short window to register before deciding this is ordinary ground loot.
            for (int frame = 0; frame < NestDetectionFrameCount; frame++)
            {
                SeaMonkeyNest nest = SeaMonkeyNestsManager.GetNearestNest(transform.position, NestDetectionRange);
                if (nest != null)
                {
                    nest.AddItem(gameObject);
                    KeepUpright();
                    placementVersion = CurrentPlacementVersion;
                    yield break;
                }

                yield return null;
            }

            Bounds streamingBounds = new Bounds(transform.position, Vector3.one);
            while (LargeWorldStreamer.main == null ||
                   !LargeWorldStreamer.main.IsRangeActiveAndBuilt(streamingBounds))
            {
                yield return null;
            }

            for (int attempt = 0; attempt < GroundPlacementAttemptCount; attempt++)
            {
                if (TrySettleOnGround())
                {
                    placementVersion = CurrentPlacementVersion;
                    yield break;
                }

                yield return null;
            }

            Debug.LogWarningFormat(this,
                "Pet DNA at {0} could not find a suitable floor after terrain loading and will not be spawned.",
                transform.position);
            Object.Destroy(gameObject);
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            wasDeserialized = true;
        }

        /// <summary>
        ///     Small entity slots may be authored against walls or steep terrain. Move a newly
        ///     distributed tube to the floor beneath the slot before making it upright.
        /// </summary>
        private bool TrySettleOnGround()
        {
            Vector3 slotPosition = transform.position;
            RaycastHit hit;
            if (!TryFindFloor(slotPosition, out hit))
            {
                return false;
            }

            ApplyRestingPose(hit.normal, slotPosition);

            Bounds bounds = UWE.Utils.GetEncapsulatedAABB(gameObject);
            float boundsBottomOffset = bounds.min.y - transform.position.y;
            float rootHeight = hit.point.y + GroundClearance - boundsBottomOffset;
            transform.position = new Vector3(hit.point.x, rootHeight, hit.point.z);
            return true;
        }

        private static bool TryFindFloor(Vector3 slotPosition, out RaycastHit floorHit)
        {
            floorHit = default(RaycastHit);
            float bestDistanceSquared = float.MaxValue;
            bool foundFloor = false;

            for (int index = 0; index < GroundProbeDirections.Length; index++)
            {
                Vector3 horizontalOffset = GroundProbeDirections[index] * GroundProbeRadius;
                Vector3 upperOrigin = slotPosition + horizontalOffset + Vector3.up * GroundProbeOffset;
                foundFloor |= TryUseCloserFloor(upperOrigin, slotPosition, ref floorHit, ref bestDistanceSquared);

                Vector3 lowerOrigin = slotPosition + horizontalOffset - Vector3.up * GroundProbeOffset;
                foundFloor |= TryUseCloserFloor(lowerOrigin, slotPosition, ref floorHit, ref bestDistanceSquared);
            }

            return foundFloor;
        }

        private static bool TryUseCloserFloor(Vector3 origin, Vector3 slotPosition, ref RaycastHit floorHit,
            ref float bestDistanceSquared)
        {
            RaycastHit candidate;
            if (!Physics.Raycast(origin, Vector3.down, out candidate, GroundSearchDistance,
                    Voxeland.GetTerrainLayerMask(), QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (candidate.normal.y < MinimumFloorNormalY ||
                candidate.point.y > slotPosition.y + MaximumFloorHeightAboveSlot)
            {
                return false;
            }

            float distanceSquared = (candidate.point - slotPosition).sqrMagnitude;
            if (distanceSquared >= bestDistanceSquared)
            {
                return false;
            }

            floorHit = candidate;
            bestDistanceSquared = distanceSquared;
            return true;
        }

        /// <summary>
        ///     Give the tube a stable, natural-looking pose relative to the floor. Most tubes
        ///     lie down, while a smaller number lean or remain upright.
        /// </summary>
        private void ApplyRestingPose(Vector3 floorNormal, Vector3 slotPosition)
        {
            System.Random random = CreatePoseRandom(slotPosition);
            float poseSelection = (float)random.NextDouble();
            float tilt;

            if (poseSelection < 0.15f)
            {
                tilt = 0.0f;
            }
            else if (poseSelection < 0.35f)
            {
                tilt = Mathf.Lerp(20.0f, 60.0f, (float)random.NextDouble());
            }
            else
            {
                tilt = Mathf.Lerp(80.0f, 100.0f, (float)random.NextDouble());
            }

            float yaw = (float)random.NextDouble() * 360.0f;
            Quaternion surfaceAlignment = Quaternion.FromToRotation(Vector3.up, floorNormal);
            Quaternion yawRotation = Quaternion.AngleAxis(yaw, floorNormal);
            Vector3 tiltAxis = yawRotation * (surfaceAlignment * Vector3.forward);

            transform.rotation = Quaternion.AngleAxis(tilt, tiltAxis) * yawRotation * surfaceAlignment;
        }

        private static System.Random CreatePoseRandom(Vector3 slotPosition)
        {
            int seed;
            unchecked
            {
                seed = 17;
                seed = seed * 31 + Mathf.RoundToInt(slotPosition.x * 100.0f);
                seed = seed * 31 + Mathf.RoundToInt(slotPosition.y * 100.0f);
                seed = seed * 31 + Mathf.RoundToInt(slotPosition.z * 100.0f);
            }

            return new System.Random(seed);
        }

        /// <summary>
        ///     Preserve the spawn slot's yaw while removing pitch and roll that could rotate
        ///     a nest item below the surrounding nest geometry.
        /// </summary>
        private void KeepUpright()
        {
            float yaw = transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);
        }
    }
}
