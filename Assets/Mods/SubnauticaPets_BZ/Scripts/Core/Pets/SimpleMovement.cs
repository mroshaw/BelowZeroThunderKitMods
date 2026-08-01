using UnityEngine;
using UnityEngine.Events;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    ///     Simple movement using Unity CharacterController
    /// </summary>
    [RequireComponent(typeof(PetAnimator), typeof(Pet), typeof(PetStateController))]
    internal class SimpleMovement : MonoBehaviour
    {
        private const float BoundaryEventCooldown = 0.25f;
        private const float GroundProbeHeight = 0.4f;
        private const float GroundProbeDistance = 0.6f;
        private const float GroundProbeRadiusScale = 0.45f;
        private const float LookAheadDistance = 0.2f;
        private const float SafePositionInterval = 0.2f;
        private const float SpawnSettlementMinimumDistance = 0.75f;
        private const float SpawnSettlementMinimumDrop = 0.35f;
        private const float SpawnSettlementTimeout = 6.0f;
        private const float UngroundedRecoveryDelay = 0.5f;
        private const float StuckCheckInterval = 1.0f;
        private const float StuckDistanceThreshold = 0.05f;

        [Header("Movement Settings")] [SerializeField] private float moveSpeed = 0.8f;
        [SerializeField] private float rotateSpeed = 4.0f;
        [SerializeField] private float arrivalTolerance = 0.05f;

        [Header("Debug")] [Header("Debug Movement")] [SerializeField] private Transform targetMarker;
        [SerializeField] private bool isGrounded;
        [SerializeField] private Vector3 moveDirection;
        [SerializeField] private Vector3 moveTarget;
        [SerializeField] private float distanceToTarget;
        [SerializeField] private bool isMoving;
        [SerializeField] private Vector3 lastSafePosition;
        [SerializeField] private bool hasSafePosition;
        [SerializeField] private bool isSettlingAfterSpawn;

        [SerializeField] internal UnityEvent onArrived = new UnityEvent();
        [SerializeField] internal MovementBoundaryEvent OnUnsafeBoundary = new MovementBoundaryEvent();

        private CharacterController _charController;
        private PetAnimator _petAnimator;
        private Pet _pet;
        private PetStateController _stateController;
        private Rigidbody _rigidbody;
        private readonly RaycastHit[] groundHits = new RaycastHit[8];
        private float boundaryEventTimer;
        private float safePositionTimer;
        private float stuckCheckTimer;
        private float ungroundedTimer;
        private Vector3 lastStuckCheckPosition;
        private Vector3 spawnSettlementDirection;
        private Vector3 spawnSettlementStartPosition;
        private float spawnSettlementStartHeight;
        private float spawnSettlementTimer;
        private bool hasLeftSpawnSurface;
        [SerializeField] internal ControllerColliderHitEvent OnHitObstacle = new ControllerColliderHitEvent();

        /// <summary>
        ///     Private setter for IsMoving
        /// </summary>
        private bool IsMoving
        {
            get => isMoving;
            set
            {
                isMoving = value;
                _petAnimator.SetMoving(value);
            }
        }

        private void Awake()
        {
            _charController = gameObject.GetComponent<CharacterController>();
            _petAnimator = GetComponent<PetAnimator>();
            _pet = GetComponent<Pet>();
            _stateController = GetComponent<PetStateController>();
            lastStuckCheckPosition = transform.position;
        }

        private void Update()
        {
            boundaryEventTimer -= Time.deltaTime;

            Vector3 velocity = Vector3.zero;
            if (isSettlingAfterSpawn)
            {
                if (!IsMoving) IsMoving = true;
                moveDirection = spawnSettlementDirection;
                velocity = moveDirection * moveSpeed;
                RotateToTarget();
            }
            else if (IsMoving)
            {
                SetMoveDirection();
                if (HasFloorAhead())
                {
                    velocity = moveDirection * moveSpeed;
                    RotateToTarget();
                }
                else
                {
                    NotifyUnsafeBoundary(-moveDirection);
                }
            }

            _charController.SimpleMove(velocity);
            if (isSettlingAfterSpawn)
            {
                UpdateSpawnSettlement();
                if (isSettlingAfterSpawn) return;
            }

            CheckGroundSafety();
            CheckForStuckMovement();

            if (IsMoving && HasArrived())
            {
                IsMoving = false;
                onArrived?.Invoke();
            }
        }

        private void OnEnable()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Check for ground hit
            // If the normal points up, it's probably ground
            if (Vector3.Angle(hit.normal, Vector3.up) < 45f)
                // Ground contact — ignore
                return;
            // ModDebugLog.LogDebug($"{gameObject.name} hit: {hit.gameObject.name}");
            if (boundaryEventTimer > 0.0f) return;

            boundaryEventTimer = BoundaryEventCooldown;
            OnHitObstacle?.Invoke(Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized);
        }

        internal void SetMoveSpeed(float newMoveSpeed)
        {
            moveSpeed = newMoveSpeed;
        }

        internal void MoveToNewTarget(Vector3 target)
        {
            moveTarget = target;
            IsMoving = true;

            if (targetMarker) targetMarker.position = target;
        }

        internal void Stop()
        {
            IsMoving = false;
        }

        /// <summary>
        ///     Permanently disables locomotion and controller collision for a dead pet.
        /// </summary>
        internal void DisableForDeath()
        {
            isSettlingAfterSpawn = false;
            Stop();
            if (_charController) _charController.enabled = false;
            enabled = false;
        }

        /// <summary>
        ///     Briefly allows a newly fabricated pet to walk off the Fabricator before normal edge protection begins.
        /// </summary>
        internal void BeginSpawnSettlement(Vector3 exitDirection)
        {
            Vector3 horizontalDirection = Vector3.ProjectOnPlane(exitDirection, Vector3.up).normalized;
            if (horizontalDirection == Vector3.zero) horizontalDirection = transform.forward;

            spawnSettlementDirection = horizontalDirection;
            spawnSettlementStartPosition = transform.position;
            spawnSettlementStartHeight = transform.position.y;
            spawnSettlementTimer = 0.0f;
            hasLeftSpawnSurface = false;
            hasSafePosition = false;
            isSettlingAfterSpawn = true;
            IsMoving = true;

            Debug.Log($"[SubnauticaPets] Spawn settlement started for {gameObject.name} at {transform.position}; " +
                      $"direction={spawnSettlementDirection}; minimumDistance={SpawnSettlementMinimumDistance:F2}m.");
        }

        /// <summary>
        ///     Set the direction to the target
        /// </summary>
        private void SetMoveDirection()
        {
            moveTarget.y = transform.position.y;
            moveDirection = (moveTarget - transform.position).normalized;
            moveDirection.y = 0;
        }

        private bool HasFloorAhead()
        {
            if (moveDirection == Vector3.zero) return true;

            float footprintOffset = _charController.radius * 0.55f;
            Vector3 probeCenter = transform.position + moveDirection * (_charController.radius + LookAheadDistance);
            Vector3 sideDirection = Vector3.Cross(Vector3.up, moveDirection).normalized;

            return HasValidFloor(probeCenter) &&
                   HasValidFloor(probeCenter + sideDirection * footprintOffset) &&
                   HasValidFloor(probeCenter - sideDirection * footprintOffset);
        }

        private void RotateToTarget()
        {
            // Rotate smoothly towards the target
            if (moveDirection != Vector3.zero)
            {
                var lookRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotateSpeed * Time.deltaTime);
            }
        }

        private bool HasValidFloor(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * GroundProbeHeight;
            float probeRadius = Mathf.Max(0.03f, _charController.radius * GroundProbeRadiusScale);
            int hitCount = Physics.SphereCastNonAlloc(origin, probeRadius, Vector3.down, groundHits,
                GroundProbeHeight + GroundProbeDistance, ~0, QueryTriggerInteraction.Ignore);

            float closestDistance = float.MaxValue;
            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                RaycastHit hit = groundHits[hitIndex];
                if (!hit.collider || hit.collider.transform.IsChildOf(transform)) continue;
                if (Vector3.Angle(hit.normal, Vector3.up) > _charController.slopeLimit) continue;
                if (!BelongsToPetBase(hit.collider)) continue;
                if (hit.distance >= closestDistance) continue;

                closestDistance = hit.distance;
            }

            return closestDistance < float.MaxValue;
        }

        private bool BelongsToPetBase(Collider floorCollider)
        {
            if (!_pet || !_pet.Base) return true;
            if (floorCollider.transform.IsChildOf(_pet.Base.transform)) return true;

            Base floorBase = floorCollider.GetComponentInParent<Base>();
            return floorBase && floorBase == _pet.Base;
        }

        private void CheckGroundSafety()
        {
            isGrounded = _charController.isGrounded && HasValidFloor(transform.position);
            if (isGrounded)
            {
                ungroundedTimer = 0.0f;
                safePositionTimer += Time.deltaTime;
                if (safePositionTimer >= SafePositionInterval)
                {
                    lastSafePosition = transform.position;
                    hasSafePosition = true;
                    safePositionTimer = 0.0f;
                }

                return;
            }

            safePositionTimer = 0.0f;
            ungroundedTimer += Time.deltaTime;
            if (hasSafePosition && ungroundedTimer >= UngroundedRecoveryDelay) RecoverToLastSafePosition();
        }

        private void UpdateSpawnSettlement()
        {
            spawnSettlementTimer += Time.deltaTime;
            Vector3 horizontalDisplacement = Vector3.ProjectOnPlane(
                transform.position - spawnSettlementStartPosition, Vector3.up);
            float horizontalDistance = horizontalDisplacement.magnitude;
            float verticalDrop = spawnSettlementStartHeight - transform.position.y;

            if (!_charController.isGrounded ||
                verticalDrop >= SpawnSettlementMinimumDrop)
            {
                if (!hasLeftSpawnSurface)
                    Debug.Log($"[SubnauticaPets] {gameObject.name} left its Fabricator spawn surface at " +
                              $"{transform.position} after {spawnSettlementTimer:F2}s; " +
                              $"horizontalDistance={horizontalDistance:F2}m; verticalDrop={verticalDrop:F2}m.");
                hasLeftSpawnSurface = true;
            }

            bool landed = hasLeftSpawnSurface && _charController.isGrounded &&
                          horizontalDistance >= SpawnSettlementMinimumDistance &&
                          verticalDrop >= SpawnSettlementMinimumDrop;
            bool timedOut = spawnSettlementTimer >= SpawnSettlementTimeout;
            if (landed || timedOut)
            {
                isSettlingAfterSpawn = false;
                IsMoving = false;
                ungroundedTimer = 0.0f;
                safePositionTimer = 0.0f;
                lastStuckCheckPosition = transform.position;

                Debug.Log($"[SubnauticaPets] Spawn settlement {(landed ? "completed" : "timed out")} for " +
                          $"{gameObject.name} at {transform.position}; grounded={_charController.isGrounded}; " +
                          $"leftSurface={hasLeftSpawnSurface}; horizontalDistance={horizontalDistance:F2}m; " +
                          $"verticalDrop={verticalDrop:F2}m.");

                if (_stateController)
                    _stateController.SetNewState(landed ? PetState.Wandering : PetState.Idle);
            }
        }

        private void RecoverToLastSafePosition()
        {
            Vector3 recoveryDirection = Vector3.ProjectOnPlane(lastSafePosition - transform.position, Vector3.up);
            _charController.enabled = false;
            transform.position = lastSafePosition;
            _charController.enabled = true;
            ungroundedTimer = 0.0f;
            NotifyUnsafeBoundary(recoveryDirection.normalized);
        }

        private void CheckForStuckMovement()
        {
            stuckCheckTimer += Time.deltaTime;
            if (stuckCheckTimer < StuckCheckInterval) return;

            if (IsMoving && Vector3.Distance(transform.position, lastStuckCheckPosition) < StuckDistanceThreshold)
                NotifyUnsafeBoundary(-moveDirection);

            lastStuckCheckPosition = transform.position;
            stuckCheckTimer = 0.0f;
        }

        private void NotifyUnsafeBoundary(Vector3 safeDirection)
        {
            if (boundaryEventTimer > 0.0f) return;

            boundaryEventTimer = BoundaryEventCooldown;
            OnUnsafeBoundary?.Invoke(safeDirection);
        }

        private bool HasArrived()
        {
            distanceToTarget = Vector3.Distance(transform.position, moveTarget);
            return distanceToTarget < arrivalTolerance;
        }

        internal class ControllerColliderHitEvent : UnityEvent<Vector3>
        {
        }

        internal class MovementBoundaryEvent : UnityEvent<Vector3>
        {
        }
    }
}
