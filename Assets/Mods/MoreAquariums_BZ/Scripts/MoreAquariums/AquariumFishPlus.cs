using System.Collections.Generic;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Implements procedural movement and path-finding, within the bounds
    /// of one of more colliders
    /// </summary>
    public class AquariumFishPlus : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private List<Collider> movementColliders = new List<Collider>();
        [SerializeField] private FishManager fishManager;
        [SerializeField] private FishSettings fishSettings;
        
        [Header("Debug")]
        [SerializeField]  private Vector3 currentDirection;
        [SerializeField]  private float currentSpeed;
        [SerializeField]  private Vector3 targetPosition =  Vector3.zero;
        [SerializeField] private Vector3 directionToTargetRaw;
        [SerializeField] private Vector3 directionToTargetModified;

        [SerializeField]  private bool isDarting;
        [SerializeField]  private float dartEndTime;
        [SerializeField]  private float nextDartTime;
        [SerializeField]  private bool atInitialisePosition;
        
        private float _noiseOffsetX, _noiseOffsetY, _noiseOffsetZ;

        private bool _fishManagerSet;
        
        /// <summary>
        /// Register with the FishManager
        /// </summary>
        private void OnEnable()
        {
            if (fishManager)
            {
                fishManager.AddActiveFish(this);
            }
        }

        /// <summary>
        /// Unregister if disabled. Won't be considered for fish/fish collision detection
        /// </summary>
        private void OnDisable()
        {
            if (fishManager)
            {
                fishManager.RemoveActiveFish(this);
            }
        }
        
        /// <summary>
        /// Set the initial position and pick a target
        /// </summary>
        private void Start()
        {
            if (!fishManager)
            {
                return;
            }
            
            if (!fishSettings)
            {
                ModDebugLog.LogError("Fish has no fish settings!");
                return;
            }
            
            if (movementColliders == null || movementColliders.Count == 0)
            {
                ModDebugLog.LogError("Fish has no movement colliders!");
                return;
            }
           
            currentDirection = Random.onUnitSphere;
            currentSpeed = fishSettings.baseSpeed + (Random.Range(0, fishSettings.randomSpeedModifier));

            _noiseOffsetX = Random.value * 100f;
            _noiseOffsetY = Random.value * 100f;
            _noiseOffsetZ = Random.value * 100f;
            
            PickNewTarget();
            ScheduleNextDart();
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
                // Too close to compute a valid direction; just keep moving forward
                transform.position += currentDirection * (currentSpeed * Time.deltaTime);
                return;
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

            // Select new target if needed
            if (Vector3.Distance(transform.position, targetPosition) < fishSettings.arrivalDistance)
            {
                PickNewTarget();
            }
        }
        
        /// <summary>
        /// Public setter for Fish Manager
        /// </summary>
        public void SetFishManager(FishManager newFishManager)
        {
            if (!newFishManager)
            {
                ModDebugLog.LogError("SetFishManager: newFishManager is null!");
            }
            
            ModDebugLog.LogDebug($"Setting fish manager on {gameObject.name}...");
            fishManager = newFishManager;
            fishSettings = newFishManager.FishSettings;
            movementColliders = newFishManager.MovementColliders;
            _fishManagerSet = true;
            ModDebugLog.LogDebug($"Fish manager set to {newFishManager}");
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
                    if (!atInitialisePosition)
                    {
                        transform.position = candidate;
                        atInitialisePosition = true;
                        continue;
                    }

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
                if (!IsPointInsideAnyCollider(p))
                    return false;
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
                Vector3 halfSize = box.size * 0.5f;
                Vector3 steer = Vector3.zero;

                if (Mathf.Abs(localPos.x) > halfSize.x - fishSettings.boundaryMargin)
                    steer.x = -Mathf.Sign(localPos.x);
                if (Mathf.Abs(localPos.y) > halfSize.y - fishSettings.boundaryMargin)
                    steer.y = -Mathf.Sign(localPos.y);
                if (Mathf.Abs(localPos.z) > halfSize.z - fishSettings.boundaryMargin)
                    steer.z = -Mathf.Sign(localPos.z);

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

            foreach (AquariumFishPlus fish in fishManager.ActiveFishList)
            {
                // Don't avoid self
                if (fish == this)
                {
                    continue;
                }
                float dist = Vector3.Distance(transform.position, fish.transform.position);
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
        /// Returns a direction Vector away from the boundary
        /// </summary>
        private Vector3 GetBoundsDirection(Collider boundsCollider)
        {
            if (boundsCollider is BoxCollider box)
            {
                Vector3 localPos = box.transform.InverseTransformPoint(transform.position);
                Vector3 halfSize = box.size * 0.5f;
                Vector3 steer = Vector3.zero;

                // Gradually steer back when approaching walls
                if (Mathf.Abs(localPos.x) > halfSize.x - fishSettings.boundaryMargin)
                    steer.x = -Mathf.Sign(localPos.x);
                if (Mathf.Abs(localPos.y) > halfSize.y - fishSettings.boundaryMargin)
                    steer.y = -Mathf.Sign(localPos.y);
                if (Mathf.Abs(localPos.z) > halfSize.z - fishSettings.boundaryMargin)
                    steer.z = -Mathf.Sign(localPos.z);

                return box.transform.TransformDirection(steer.normalized * fishSettings.boundarySteerStrength);
            }

            if (boundsCollider is SphereCollider sphere)
            {
                Vector3 center = sphere.transform.TransformPoint(sphere.center);
                float radius = sphere.radius * Mathf.Max(
                    sphere.transform.lossyScale.x,
                    sphere.transform.lossyScale.y,
                    sphere.transform.lossyScale.z
                );

                Vector3 toCenter = center - transform.position;
                float dist = toCenter.magnitude;
                if (dist > radius - fishSettings.boundaryMargin)
                {
                    float strength = Mathf.InverseLerp(radius, radius - fishSettings.boundaryMargin, dist);
                    return toCenter.normalized * (strength * fishSettings.boundarySteerStrength);
                }
            }

            return Vector3.zero;
        }

        /*
        
        /// <summary>
        /// Returns a clamped position on the collider bounds
        /// </summary>
        private Vector3 GetBoundaryClampPosition(Vector3 pos)
        {
            if (aquariumBounds is BoxCollider box)
            {
                // Convert to local collider space
                Vector3 local = box.transform.InverseTransformPoint(pos);
                Vector3 half = box.size * 0.5f;
                Vector3 center = box.center;

                // Clamp relative to collider's local center
                local.x = Mathf.Clamp(local.x, center.x - half.x, center.x + half.x);
                local.y = Mathf.Clamp(local.y, center.y - half.y, center.y + half.y);
                local.z = Mathf.Clamp(local.z, center.z - half.z, center.z + half.z);

                // Convert back to world space
                return box.transform.TransformPoint(local);
            }
            else if (aquariumBounds is SphereCollider sphere)
            {
                Vector3 center = sphere.transform.TransformPoint(sphere.center);
                float radius = sphere.radius * Mathf.Max(
                    sphere.transform.lossyScale.x,
                    sphere.transform.lossyScale.y,
                    sphere.transform.lossyScale.z
                );

                Vector3 dir = pos - center;
                float dist = dir.magnitude;

                if (dist > radius)
                    return center + dir.normalized * (radius - 0.01f);

                return pos;
            }

            return pos;
        }

        */

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
                currentSpeed = fishSettings.baseSpeed;
                ScheduleNextDart();
            }
            else if (!isDarting && Time.time > nextDartTime)
            {
                isDarting = true;
                currentSpeed = fishSettings.baseSpeed * fishSettings.dartSpeedMultiplier;
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