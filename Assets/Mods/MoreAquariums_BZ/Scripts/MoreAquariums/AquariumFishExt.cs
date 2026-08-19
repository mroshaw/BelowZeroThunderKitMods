using System.Collections.Generic;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Implements procedural movement and path-finding, within the bounds
    /// of one of more colliders
    /// </summary>
    public class AquariumFishExt : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private List<Collider> movementColliders = new List<Collider>();
        [SerializeField] private List<Collider> exclusionColliders = new List<Collider>();
        [SerializeField] private FishManager fishManager;
        [SerializeField] private FishSettings fishSettings;
        
        [Header("Debug")]
        [SerializeField]  private Vector3 currentDirection;
        [SerializeField]  private float currentSpeed;
        [SerializeField] private float normalSpeed;
        [SerializeField]  private Vector3 targetPosition =  Vector3.zero;
        [SerializeField] private Vector3 directionToTargetRaw;
        [SerializeField] private Vector3 directionToTargetModified;

        [SerializeField]  private bool isDarting;
        [SerializeField]  private float dartEndTime;
        [SerializeField]  private float nextDartTime;
        private float _noiseOffsetX, _noiseOffsetY, _noiseOffsetZ;
        
        /// <summary>
        /// Initializes this procedural movement component.
        /// </summary>
        internal void Initialize(FishManager newFishManager, FishSettings newFishSettings,
            List<Collider> newMovementColliders,
            List<Collider> newExclusionColliders)
        {
            if (!newFishManager || !newFishSettings || newMovementColliders == null ||
                newMovementColliders.Count == 0)
            {
                ModDebugLog.LogError("Cannot initialize fish movement with invalid settings.");
                DisableOnError();
                return;
            }

            fishManager = newFishManager;
            fishSettings = newFishSettings;
            movementColliders = newMovementColliders;
            exclusionColliders = newExclusionColliders ?? new List<Collider>();
            enabled = false;
        }

        /// <summary>
        /// Activates and resets procedural movement for an occupied track.
        /// </summary>
        internal void ActivateMovement()
        {
            if (!fishManager || !fishSettings || movementColliders == null ||
                movementColliders.Count == 0)
            {
                ModDebugLog.LogError("Cannot activate fish movement before it is initialized.");
                DisableOnError();
                return;
            }

            currentDirection = Random.onUnitSphere;
            normalSpeed = fishSettings.baseSpeed + Random.Range(0f, fishSettings.randomSpeedModifier);
            currentSpeed = normalSpeed;

            _noiseOffsetX = Random.value * 100f;
            _noiseOffsetY = Random.value * 100f;
            _noiseOffsetZ = Random.value * 100f;
            
            PickNewTarget();
            ScheduleNextDart();
            enabled = true;
        }

        /// <summary>
        /// Disables procedural movement for an empty track.
        /// </summary>
        internal void DeactivateMovement()
        {
            enabled = false;
        }

        /// <summary>
        /// Disables the component, used when there is an initialisation failure
        /// </summary>
        private void DisableOnError()
        {
            fishManager = null;
            fishSettings = null;
            movementColliders = null;
            exclusionColliders = null;
            enabled = false;
        }
        
        /// <summary>
        /// Handle movement and collision detection every frame
        /// </summary>
        private void Update()
        {
            if (movementColliders.Count == 0)
                return;

            HandleDarting();

            // Calculate desired direction
            directionToTargetRaw = (targetPosition - transform.position).normalized;
            if (directionToTargetRaw.sqrMagnitude < 0.0001f)
            {
                PickNewTarget();
                directionToTargetRaw =
                    (targetPosition - transform.position).normalized;
            }
            
            // Combine influences
            directionToTargetModified = directionToTargetRaw;

            if (fishSettings.steerFromBounds)
            {
                directionToTargetModified += GetBoundsDirectionMulti();
            }
            
            if (fishSettings.avoidOtherFish)
            {
                directionToTargetModified += AvoidOtherFish();
            }
            
            if (fishSettings.applyNoise)
            {
                directionToTargetModified += GetNoiseDirection() * fishSettings.noiseInfluence;
            }
            
            directionToTargetModified = directionToTargetModified.normalized;
            
            // Apply steering and movement
            currentDirection = Vector3.Slerp(currentDirection, directionToTargetModified, fishSettings.turnSpeed * Time.deltaTime);

            if (fishSettings.limitPitch)
            {
                // Limit roll and pitch so fish mostly rotate around the Y axis
                Quaternion targetRotation = Quaternion.LookRotation(currentDirection, Vector3.up);
                Vector3 euler = targetRotation.eulerAngles;

                // Allow a little pitch when moving vertically
                float verticalPitch = -Mathf.Clamp(Vector3.Dot(currentDirection.normalized, Vector3.up) * fishSettings.pitchScaling, fishSettings.minPitch, fishSettings.maxPitch);

                targetRotation = Quaternion.Euler(verticalPitch, euler.y, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, fishSettings.turnSpeed * Time.deltaTime);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(currentDirection, Vector3.up);
            }

            // Move towards target
            transform.position += currentDirection * (currentSpeed * Time.deltaTime);
            
            // Clamp position to remain inside collider bounds
            if (fishSettings.clampToBounds)
            {
                transform.position = GetNearestValidPoint(transform.position);
            }

            Vector3 correctedPosition = transform.position;
            if (PushOutsideExclusions(ref correctedPosition))
            {
                transform.position = correctedPosition;
                PickNewTarget();
            }

            // Select new target if needed
            if (Vector3.Distance(transform.position, targetPosition) < fishSettings.arrivalDistance)
            {
                PickNewTarget();
            }
        }
        
        private void PickNewTarget()
        {
            const int maxAttempts = 20;

            for (int i = 0; i < maxAttempts; i++)
            {
                Collider randomCol = movementColliders[Random.Range(0, movementColliders.Count)];
                Vector3 candidate = GetRandomPointInsideCollider(randomCol);

                if (IsPathContained(transform.position, candidate))
                {
                    targetPosition = candidate;
                    return;
                }
            }

            // fallback: nearest valid point
            targetPosition = GetNearestValidPoint(transform.position);
        }

        private bool IsPathContained(Vector3 from, Vector3 to)
        {
            const int steps = 10;
            for (int i = 0; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)steps);
                if (!IsPointInsideAnyCollider(p) || IsPointInsideExclusion(p))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsPointInsideAnyCollider(Vector3 pos)
        {
            foreach (Collider col in movementColliders)
                if (IsPointInsideCollider(col, pos))
                    return true;

            return false;
        }

        private bool IsPointInsideExclusion(Vector3 pos)
        {
            foreach (Collider col in exclusionColliders)
            {
                if (IsPointInsideCollider(col, pos))
                {
                    return true;
                }
            }

            return false;
        }

        private bool PushOutsideExclusions(ref Vector3 pos)
        {
            bool positionChanged = false;
            foreach (Collider col in exclusionColliders)
            {
                if (!IsPointInsideCollider(col, pos))
                {
                    continue;
                }

                pos = GetNearestPointOutsideCollider(col, pos);
                positionChanged = true;
            }

            return positionChanged;
        }

        private Vector3 GetNearestPointOutsideCollider(Collider col, Vector3 pos)
        {
            const float surfaceOffset = 0.01f;

            BoxCollider box = col as BoxCollider;
            if (box)
            {
                Vector3 local = box.transform.InverseTransformPoint(pos);
                Vector3 minimum = box.center - box.size * 0.5f;
                Vector3 maximum = box.center + box.size * 0.5f;
                float nearestDistance = local.x - minimum.x;
                int nearestFace = 0;

                float distance = maximum.x - local.x;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestFace = 1;
                }

                distance = local.y - minimum.y;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestFace = 2;
                }

                distance = maximum.y - local.y;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestFace = 3;
                }

                distance = local.z - minimum.z;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestFace = 4;
                }

                if (maximum.z - local.z < nearestDistance)
                {
                    nearestFace = 5;
                }

                switch (nearestFace)
                {
                    case 0:
                        local.x = minimum.x - surfaceOffset;
                        break;
                    case 1:
                        local.x = maximum.x + surfaceOffset;
                        break;
                    case 2:
                        local.y = minimum.y - surfaceOffset;
                        break;
                    case 3:
                        local.y = maximum.y + surfaceOffset;
                        break;
                    case 4:
                        local.z = minimum.z - surfaceOffset;
                        break;
                    case 5:
                        local.z = maximum.z + surfaceOffset;
                        break;
                }

                return box.transform.TransformPoint(local);
            }

            SphereCollider sphere = col as SphereCollider;
            if (sphere)
            {
                Vector3 center = sphere.transform.TransformPoint(sphere.center);
                float radius = sphere.radius * Mathf.Max(
                    sphere.transform.lossyScale.x,
                    sphere.transform.lossyScale.y,
                    sphere.transform.lossyScale.z);
                Vector3 direction = pos - center;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Vector3.up;
                }

                return center + direction.normalized * (radius + surfaceOffset);
            }

            return pos;
        }

        private bool IsPointInsideCollider(Collider col, Vector3 worldPos)
        {
            if (col is BoxCollider box)
            {
                Vector3 local = box.transform.InverseTransformPoint(worldPos);
                Vector3 c = box.center;
                Vector3 h = box.size * 0.5f;
                return local.x >= c.x - h.x && local.x <= c.x + h.x &&
                       local.y >= c.y - h.y && local.y <= c.y + h.y &&
                       local.z >= c.z - h.z && local.z <= c.z + h.z;
            }

            if (col is SphereCollider sphere)
            {
                Vector3 center = sphere.transform.TransformPoint(sphere.center);
                float scaledRadius = sphere.radius * Mathf.Max(sphere.transform.lossyScale.x, sphere.transform.lossyScale.y, sphere.transform.lossyScale.z);
                return Vector3.Distance(center, worldPos) <= scaledRadius;
            }
            return false;
        }

        private Vector3 GetRandomPointInsideCollider(Collider col)
        {
            if (col is BoxCollider box)
            {
                Vector3 center = box.center;
                Vector3 halfSize = box.size * 0.5f;
                Vector3 local = new Vector3(
                    Random.Range(center.x - halfSize.x, center.x + halfSize.x),
                    Random.Range(center.y - halfSize.y, center.y + halfSize.y),
                    Random.Range(center.z - halfSize.z, center.z + halfSize.z)
                );
                return box.transform.TransformPoint(local);
            }

            if (col is SphereCollider sphere)
            {
                Vector3 center = sphere.center;
                float radius = sphere.radius;
                Vector3 localTarget = Random.insideUnitSphere * radius + center;
                return sphere.transform.TransformPoint(localTarget);
            }
            return col.bounds.center;
        }

        private Vector3 GetNearestValidPoint(Vector3 pos)
        {
            Vector3 nearest = pos;
            float nearestDist = float.MaxValue;

            foreach (Collider col in movementColliders)
            {
                Vector3 candidate = ClampToCollider(col, pos);
                float d = Vector3.Distance(pos, candidate);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = candidate;
                }
            }
            return nearest;
        }

        private Vector3 ClampToCollider(Collider col, Vector3 pos)
        {
            if (col is BoxCollider box)
            {
                Vector3 local = box.transform.InverseTransformPoint(pos);
                Vector3 c = box.center;
                Vector3 h = box.size * 0.5f;
                local.x = Mathf.Clamp(local.x, c.x - h.x, c.x + h.x);
                local.y = Mathf.Clamp(local.y, c.y - h.y, c.y + h.y);
                local.z = Mathf.Clamp(local.z, c.z - h.z, c.z + h.z);
                return box.transform.TransformPoint(local);
            }

            if (col is SphereCollider sphere)
            {
                Vector3 center = sphere.transform.TransformPoint(sphere.center);
                float radius = sphere.radius * Mathf.Max(sphere.transform.lossyScale.x, sphere.transform.lossyScale.y, sphere.transform.lossyScale.z);
                Vector3 dir = pos - center;
                float dist = dir.magnitude;
                if (dist > radius)
                {
                    return center + dir.normalized * (radius - 0.01f);
                }
            }
            return pos;
        }

        private Vector3 GetBoundsDirectionMulti()
        {
            Vector3 steer = Vector3.zero;
            foreach (Collider col in movementColliders)
                steer += GetBoundsDirectionForCollider(col);
            return steer.normalized * fishSettings.boundarySteerStrength;
        }

        private Vector3 GetBoundsDirectionForCollider(Collider col)
        {
            if (col is BoxCollider box)
            {
                Vector3 localPos = box.transform.InverseTransformPoint(transform.position);
                Vector3 offsetFromCenter = localPos - box.center;
                Vector3 halfSize = box.size * 0.5f;
                Vector3 steer = Vector3.zero;

                if (Mathf.Abs(offsetFromCenter.x) > halfSize.x - fishSettings.boundaryMargin)
                {
                    steer.x = -Mathf.Sign(offsetFromCenter.x);
                }
                if (Mathf.Abs(offsetFromCenter.y) > halfSize.y - fishSettings.boundaryMargin)
                {
                    steer.y = -Mathf.Sign(offsetFromCenter.y);
                }
                if (Mathf.Abs(offsetFromCenter.z) > halfSize.z - fishSettings.boundaryMargin)
                {
                    steer.z = -Mathf.Sign(offsetFromCenter.z);
                }

                return box.transform.TransformDirection(steer);
            }
            else if (col is SphereCollider sphere)
            {
                Vector3 center = sphere.transform.TransformPoint(sphere.center);
                float radius = sphere.radius * Mathf.Max(sphere.transform.lossyScale.x, sphere.transform.lossyScale.y, sphere.transform.lossyScale.z);
                Vector3 toCenter = center - transform.position;
                float dist = toCenter.magnitude;

                if (dist > radius - fishSettings.boundaryMargin)
                {
                    float strength = Mathf.InverseLerp(radius, radius - fishSettings.boundaryMargin, dist);
                    return toCenter.normalized * strength;
                }
            }
            return Vector3.zero;
        }
        
        /// <summary>
        /// Gets some Perlin noise direction to add organic movement
        /// </summary>
        private Vector3 GetNoiseDirection()
        {
            float t = Time.time * fishSettings.noiseSpeed;
            float nx = Mathf.PerlinNoise(_noiseOffsetX, t) * 2f - 1f;
            float ny = Mathf.PerlinNoise(_noiseOffsetY, t + 10f) * 2f - 1f;
            float nz = Mathf.PerlinNoise(_noiseOffsetZ, t + 20f) * 2f - 1f;
            return new Vector3(nx, ny, nz).normalized;
        }

        /// <summary>
        /// Avoid other fish
        /// </summary>
        private Vector3 AvoidOtherFish()
        {
            Vector3 avoidance = Vector3.zero;
            
            int count = 0;

            foreach (AquariumFishExt fish in fishManager.FishList)
            {
                // Don't avoid self or disabled fish
                if (!fish || fish == this || !fish.enabled || !fish.gameObject.activeInHierarchy)
                {
                    continue;
                }
                float dist = Vector3.Distance(transform.position, fish.transform.position);
                if (dist <= 0.0001f)
                {
                    continue;
                }

                if (dist < fishSettings.avoidanceRadius)
                {
                    avoidance += (transform.position - fish.transform.position).normalized / dist;
                    count++;
                }
            }

            if (count > 0)
            {
                avoidance = (avoidance / count) * fishSettings.avoidanceStrength;
            }

            return avoidance;
        }

        /// <summary>
        /// Random darting behaviour
        /// </summary>
        private void HandleDarting()
        {
            if (!fishSettings.randomDarting)
            {
                return;
            }
            
            if (isDarting && Time.time > dartEndTime)
            {
                isDarting = false;
                currentSpeed = normalSpeed;
                ScheduleNextDart();
            }
            else if (!isDarting && Time.time > nextDartTime)
            {
                isDarting = true;
                currentSpeed = normalSpeed * fishSettings.dartSpeedMultiplier;
                dartEndTime = Time.time + fishSettings.dartDuration;
            }
        }

        /// <summary>
        /// Schedule next darting behaviour
        /// </summary>
        private void ScheduleNextDart()
        {
            nextDartTime = Time.time + Random.Range(fishSettings.dartIntervalMin, fishSettings.dartIntervalMax);
        }

        /// <summary>
        /// Debug visualisations
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (targetPosition != Vector3.zero)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(targetPosition, 0.1f);
                Gizmos.DrawLine(transform.position, targetPosition);
            }
        }
    }
}
