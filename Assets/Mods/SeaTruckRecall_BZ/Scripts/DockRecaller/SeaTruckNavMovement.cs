using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    public enum NavState { None, Idle, Moving, Blocked, Arrived }
    /// <summary>
    /// Movement based on applying forces to a rigidbody
    /// </summary>
    internal class SeaTruckNavMovement : MonoBehaviour
    {
        // Movement properties for this method of navigation
        [Header("Movement")]
        [SerializeField] private float rotateSpeed = 15.0f;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float slowDistance = 1.0f;

        [SerializeField] private float moveDistanceThreshold = 0.2f;
        [SerializeField] private float skipWaypointThreshold = 1.0f;
        [SerializeField] private float rotateAngleThreshold = 1.0f;

        [Header("Obstacle Monitoring")]
        [SerializeField] private float obstacleScanInterval = 0.25f;
        [SerializeField] private float obstacleScanRadius = 3.0f;
        [SerializeField] private float obstacleScanDistance = 12.0f;
        [SerializeField] private LayerMask obstacleLayerMask = -5;
        
        [Header("Navigation")]
        [SerializeField] List<Waypoint> waypoints = new List<Waypoint>();
        [SerializeField] private Waypoint currentWaypoint;
        [SerializeField] private int currentWaypointIndex;
        [SerializeField] private NavState currentNavState = NavState.None;

        [Header("Debug")]
        [SerializeField] private bool slowToTarget;
        [SerializeField] private bool rotateBeforeMove;
        [SerializeField] private bool isRotationComplete;
        [SerializeField] private bool isAtCurrentTarget;
        [SerializeField] private float distanceToCurrentTarget;
        [SerializeField] private float angleToCurrentTarget;
        
        [Header("Events")]
        [SerializeField] internal NavStateChangedEvent onNavStateChanged =   new NavStateChangedEvent();
        [SerializeField] internal WaypointSetEvent onWaypointSet =   new WaypointSetEvent();
        [SerializeField] internal WaypointReachedEvent onWaypointReached =   new WaypointReachedEvent();
        [SerializeField] internal DestinationReachedEvent onDestinationReached =  new DestinationReachedEvent();
        
        private int NumWaypoints => waypoints.Count;

        /// <summary>
        /// Gets the strategy currently responsible for moving the SeaTruck.
        /// </summary>
        internal IMovement Movement { get; private set; }
        
        private SeaTruckMotor _motor;
        // Rigidbody of the main SeaTruck
        private Rigidbody _mainRigidbody;
        // Rigidbodies of any attached SeaTruck segments
        private List<RigidBodyBackup> _rigidBodyBackups;
        private int _numChildRigidBodies;
        private bool _mainTruckRigidBodyBackupIsKinematic;
        private CollisionDetectionMode _mainTruckRigidBodyBackupCollisionDetectionMode;
        private RigidbodyInterpolation _mainTruckRigidBodyBackupInterpolation;
        private readonly RaycastHit[] _obstacleHitCache = new RaycastHit[32];
        private float _nextObstacleScanTime;
        private float _nextTelemetryTime;
        private bool _mainRigidbodyConfigured;
        private bool _rigidBodiesConfigured;
        private void Awake()
        {
            _motor = GetComponent<SeaTruckMotor>();
            _mainRigidbody = GetComponent<Rigidbody>();
            ConfigureMovement();
            InitialiseNav();
        }

        /// <summary>
        /// Move in FixedUpdate
        /// </summary>
        private void FixedUpdate()
        {
            if (currentNavState != NavState.Moving)
            {
                return;
            }

            EnsureSeaTruckRigidBodyCanMove();

            LogNavigationTelemetry();

            if (IsRouteBlocked())
            {
                BlockNavigation();
                return;
            }

            if (!isRotationComplete || Movement.MaintainsRotation)
            {
                if (currentWaypoint.RotateBeforeMoving && !isRotationComplete)
                {
                    // Counter small physics drift without teleporting the rigidbody or fighting collision response.
                    Vector3 positionError = currentWaypoint.Position - _mainRigidbody.position;
                    _mainRigidbody.velocity = Vector3.ClampMagnitude(positionError, 0.5f);
                }
                RotateUpdate();
            }
            CheckRotationComplete();

            if (!currentWaypoint.RotateBeforeMoving || (currentWaypoint.RotateBeforeMoving && isRotationComplete))
            {
                MoveUpdate();
            }
            CheckMoveComplete();

            if (HasArrivedAtCurrentWaypoint())
            {
                WaypointReached();
            }
        }
        
        /// <summary>
        /// Reset the Nav completely
        /// </summary>
        private void InitialiseNav()
        {
            currentWaypointIndex = -1;
            currentWaypoint = null;
            SetNavState(NavState.Idle);
        }
        
        /// <summary>
        /// Set a new NavState and inform any event listeners
        /// </summary>
        private void SetNavState(NavState newNavState)
        {
            if (currentNavState != newNavState)
            {
                ModDebugLog.LogDebug($"NavState changed from: {currentNavState} to {newNavState}");
                currentNavState = newNavState;
                onNavStateChanged?.Invoke(currentNavState);
            }
        }

        /// <summary>
        /// Sets the next waypoint, or completes navigation if there are none left
        /// </summary>
        private void SetNextWaypoint()
        {
            currentWaypointIndex++;
            isAtCurrentTarget = false;
            isRotationComplete = false;
            
            // We've reached the last waypoint, so we have arrived
            if (currentWaypointIndex == NumWaypoints )
            {
                NavComplete();
                return;
            }
            
            currentWaypoint = waypoints[currentWaypointIndex];
            slowToTarget = currentWaypoint.SlowDownToTarget;
            rotateBeforeMove = currentWaypoint.RotateBeforeMoving;
            onWaypointSet?.Invoke(currentWaypoint);
            
            // If the next waypoint is really close, skip it
            // Keep all the code above, as we still want to inform listeners about what's happening
            if (currentWaypointIndex > 0 && currentWaypoint.AllowSkip)
            {
                float distanceToNextWaypoint = Vector3.Distance(waypoints[currentWaypointIndex].Position,
                    waypoints[currentWaypointIndex - 1].Position);
                if (distanceToNextWaypoint < skipWaypointThreshold)
                {
                    SetNextWaypoint();
                }
            }
        }
        
        /// <summary>
        /// External method used to start a new navigation
        /// </summary>
        internal bool StartNavigation(List<Waypoint> newWaypoints)
        {
            if (!CanNavigate())
            {
                ModDebugLog.LogDebug($"Can't navigate to {currentWaypoint}");
                return false;
            }
            waypoints = newWaypoints;
            ConfigureMovement();
            if (!_mainRigidbodyConfigured)
            {
                CacheSeaTruckRigidBodyState();
                SetSeaTruckRigidBodyForMove();
                _mainRigidbodyConfigured = true;
            }
            if (Movement.RequiresRigidBodyIsolation && !_rigidBodiesConfigured)
            {
                CacheRigidbodies();
                ConfigureRigidBodies();
                _rigidBodiesConfigured = true;
            }
            ModDebugLog.LogDebug($"Starting navigation using the {Movement.Name} movement strategy.");
            SetNextWaypoint();
            SetNavState(NavState.Moving);
            return true;
        }

        internal void StopNavigation()
        {
            StopAndRestorePhysics();
            InitialiseNav();
        }

        /// <summary>
        /// Stops the current route while retaining movement strategy state for an immediate replan.
        /// </summary>
        internal void PauseNavigation()
        {
            Movement.Stop();
            InitialiseNav();
        }

        /// <summary>
        /// Stops movement and reports that the active route can no longer be followed safely.
        /// </summary>
        internal void BlockNavigation()
        {
            if (currentNavState != NavState.Moving)
            {
                return;
            }

            Movement.Stop();
            SetNavState(NavState.Blocked);
        }
        
        /// <summary>
        /// Determine whether this game object is able to navigate a new course
        /// </summary>
        private bool CanNavigate()
        {
            return currentNavState  == NavState.Idle;
        }
        
        /// <summary>
        /// Called when a waypoint is reached
        /// </summary>
        private void WaypointReached()
        {
            if (currentWaypoint.SlowDownToTarget)
            {
                Movement.Stop();
            }
            onWaypointReached?.Invoke(currentWaypoint);
            SetNextWaypoint();
        }
    
        /// <summary>
        /// Called when all waypoints have been reached
        /// </summary>
        private void NavComplete()
        {
            Movement.Stop();
            SetNavState(NavState.Arrived);
            InitialiseNav();
            onDestinationReached.Invoke(currentWaypoint);
        }

        private bool IsRouteBlocked()
        {
            if (!currentWaypoint.MonitorObstacles || Time.time < _nextObstacleScanTime)
            {
                return false;
            }

            _nextObstacleScanTime = Time.time + obstacleScanInterval;
            Vector3 direction = currentWaypoint.Position - _mainRigidbody.worldCenterOfMass;
            float distance = Mathf.Min(direction.magnitude, obstacleScanDistance);
            if (distance <= obstacleScanRadius)
            {
                return false;
            }

            int hitCount = Physics.SphereCastNonAlloc(_mainRigidbody.worldCenterOfMass, obstacleScanRadius,
                direction.normalized, _obstacleHitCache, distance, obstacleLayerMask,
                QueryTriggerInteraction.Ignore);
            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                Collider collider = _obstacleHitCache[hitIndex].collider;
                if (!collider || collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                GameObject entityRoot = UWE.Utils.GetEntityRoot(collider.gameObject);
                if (NavigationObstacleFilter.IsPlayerCollider(collider, entityRoot))
                {
                    continue;
                }

                if (entityRoot == gameObject || (entityRoot && entityRoot.GetComponent<Creature>()))
                {
                    continue;
                }

                GameObject colliderRoot = entityRoot ? entityRoot : collider.transform.root.gameObject;
                ModDebugLog.LogDebug($"Route blocked by '{collider.gameObject.name}' at " +
                                     $"{_obstacleHitCache[hitIndex].point}; root '{colliderRoot.name}', layer " +
                                     $"{collider.gameObject.layer}, bounds centre {collider.bounds.center}, bounds " +
                                     $"size {collider.bounds.size}, SeaTruck position {transform.position}, target " +
                                     $"{currentWaypoint.Position}, distance {distanceToCurrentTarget:F2}.");
                return true;
            }

            return false;
        }

        private void LogNavigationTelemetry()
        {
            if (Time.time < _nextTelemetryTime || currentWaypoint == null)
            {
                return;
            }

            _nextTelemetryTime = Time.time + 5.0f;
            Vector3 position = transform.position;
            Vector3 directionToTarget = currentWaypoint.Position - position;
            string downwardProbe = DescribeProbe(position + Vector3.up, Vector3.down);
            string upwardProbe = DescribeProbe(position + Vector3.down, Vector3.up);
            ModDebugLog.LogDebug($"Navigation telemetry: position {position}, rotation {transform.eulerAngles}, " +
                                 $"waypoint '{currentWaypoint.Name}' at {currentWaypoint.Position}, distance " +
                                 $"{directionToTarget.magnitude:F2}, angle " +
                                 $"{Vector3.Angle(transform.forward, directionToTarget):F1}, rotation complete " +
                                 $"{isRotationComplete}, velocity {_mainRigidbody.velocity}, angular velocity " +
                                 $"{_mainRigidbody.angularVelocity}; down probe {downwardProbe}; up probe " +
                                 $"{upwardProbe}.");
        }

        private string DescribeProbe(Vector3 origin, Vector3 direction)
        {
            RaycastHit hit;
            if (!Physics.Raycast(origin, direction, out hit, 250.0f, obstacleLayerMask,
                    QueryTriggerInteraction.Ignore))
            {
                return "no hit within 250m";
            }

            Collider collider = hit.collider;
            GameObject entityRoot = collider ? UWE.Utils.GetEntityRoot(collider.gameObject) : null;
            string rootName = entityRoot ? entityRoot.name : collider.transform.root.name;
            return $"hit '{collider.gameObject.name}' (root '{rootName}', layer {collider.gameObject.layer}) " +
                   $"at {hit.point}, distance {hit.distance:F1}";
        }

        private void StopAndRestorePhysics()
        {
            Movement.Stop();
            if (_rigidBodiesConfigured)
            {
                RestoreRigidBodies();
                _rigidBodiesConfigured = false;
            }

            if (_mainRigidbodyConfigured)
            {
                RestoreSeaTruckRigidBodyState();
                _mainRigidbodyConfigured = false;
            }
        }
        
        /// <summary>
        /// Moves towards the target position each frame.
        /// </summary>
        private void MoveUpdate()
        {
            float distance = Vector3.Distance(currentWaypoint.Position, _mainRigidbody.position);

            // Calculate speed scaling: 1 when far, 0 when very close
            float scaleFactor = currentWaypoint.SlowDownToTarget ? Mathf.Clamp01(distance / slowDistance) : 1.0f;
            float speedModifier = currentWaypoint.UseSpeedModifier ? ConfigFile.SeaTruckSpeedModifier : 1.0f;
            float scaledSpeed = moveSpeed * scaleFactor * speedModifier;

            Movement.Move(currentWaypoint.Position, scaledSpeed);
        }

        /// <summary>
        /// Implement the RotateUpdate interface method, using the Rigidbody to move the Source transform
        /// </summary>
        private void RotateUpdate()
        {
            if (currentWaypoint.UseFixedRotation)
            {
                Movement.Rotate(currentWaypoint.Rotation, rotateSpeed);
                return;
            }

            Movement.Rotate(currentWaypoint.Position, rotateSpeed);
        }
        
        /// <summary>
        /// True if we've arrived
        /// </summary>
        private bool HasArrivedAtCurrentWaypoint()
        {
            return isAtCurrentTarget && (!currentWaypoint.RotateBeforeMoving || isRotationComplete);
        }
        
        /// <summary>
        /// Determines whether the source is now facing the target
        /// </summary>
        private void CheckRotationComplete()
        {
            if (currentWaypoint.UseFixedRotation)
            {
                angleToCurrentTarget = Quaternion.Angle(transform.rotation, currentWaypoint.Rotation);
                isRotationComplete = angleToCurrentTarget <= rotateAngleThreshold;
                return;
            }

            Vector3 directionToTarget = (currentWaypoint.Position - transform.position).normalized;
            angleToCurrentTarget = Vector3.Angle(transform.forward, directionToTarget);

            isRotationComplete = angleToCurrentTarget <= rotateAngleThreshold;
        }
        
        /// <summary>
        /// Determines whether the source is within range of the target
        /// </summary>
        private void CheckMoveComplete()
        {
            distanceToCurrentTarget = Vector3.Distance(transform.position, currentWaypoint.Position);
            isAtCurrentTarget = distanceToCurrentTarget < moveDistanceThreshold;
        }

        /// <summary>
        /// Configure the rigidbody to allow our movement code to work
        /// </summary>
        private void SetSeaTruckRigidBodyForMove()
        {
            // _mainRigidbody.interpolation = RigidbodyInterpolation.None;
            _mainRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _mainRigidbody.isKinematic = false;
            _mainRigidbody.WakeUp();
        }

        private void EnsureSeaTruckRigidBodyCanMove()
        {
            if (_mainRigidbody.isKinematic)
            {
                _mainRigidbody.isKinematic = false;
                ModDebugLog.LogDebug("Reactivated the recalled SeaTruck rigidbody after the game made it kinematic.");
            }

            if (_mainRigidbody.IsSleeping())
            {
                _mainRigidbody.WakeUp();
            }
        }

        private void ConfigureMovement()
        {
            if (ConfigFile.RecallMoveMethod == RecallMoveMethod.Input)
            {
                Movement = new InputMovement(transform, _mainRigidbody, _motor);
                return;
            }

            Movement = new PhysicsMovement(transform, _mainRigidbody);
        }
        
        /// <summary>
        /// Backup the main truck rigidbody state, as we're going to change it to allow the nav movement
        /// to work
        /// </summary>
        private void CacheSeaTruckRigidBodyState()
        {
            _mainTruckRigidBodyBackupIsKinematic = _mainRigidbody.isKinematic;
            _mainTruckRigidBodyBackupCollisionDetectionMode = _mainRigidbody.collisionDetectionMode;
            // _mainTruckRigidBodyBackupInterpolation = _mainRigidbody.interpolation;
        }

        /// <summary>
        /// Restore the rigidbody state, so we don't mess with anything else in the game
        /// </summary>
        private void RestoreSeaTruckRigidBodyState()
        {
            _mainRigidbody.collisionDetectionMode = _mainTruckRigidBodyBackupCollisionDetectionMode;
            _mainRigidbody.isKinematic = _mainTruckRigidBodyBackupIsKinematic;
            // _mainRigidbody.interpolation = _mainTruckRigidBodyBackupInterpolation;
        }
        
        /// <summary>
        /// Cache all child rigidbodies and set initial states
        /// This applies to connected SeaTruck modules, to prevent them from impacting the physics
        /// applied during the move
        /// </summary>
        private void CacheRigidbodies()
        {
            // Cache child body settings
            _rigidBodyBackups = new List<RigidBodyBackup>();
            Rigidbody[] allRigidBodies = gameObject.GetComponentsInChildren<Rigidbody>(true);

            for (int curRb = 0; curRb < allRigidBodies.Length; curRb++)
            {
                if (allRigidBodies[curRb] != _mainRigidbody)
                {
                    _rigidBodyBackups.Add(new RigidBodyBackup(allRigidBodies[curRb]));
                }
            }
            _numChildRigidBodies = _rigidBodyBackups.Count;
        }
        
        /// <summary>
        /// Set drag and mass to zero of all child Rigidbodies
        /// </summary>
        private void ConfigureRigidBodies()
        {
            // Cache connected children
            ModDebugLog.LogDebug($"ZeroChildRigidBodies: {_numChildRigidBodies} child RigidBodies");
            foreach (RigidBodyBackup backup in _rigidBodyBackups)
            {
                backup.Zero();
            }
        }
        
        /// <summary>
        /// Configure all SeaTruck module rigidbodies
        /// </summary>
        private void RestoreRigidBodies()
        {
            // Restore connected children
            ModDebugLog.LogDebug($"RestoreChildRigidBodies {_numChildRigidBodies} child RigidBodies");
            foreach (RigidBodyBackup backup in _rigidBodyBackups)
            {
                backup.Restore();
            }
            ModDebugLog.LogDebug($"RestoreRigidBodies: {_numChildRigidBodies} child RigidBodies");
        }

        /// <summary>
        /// Unity Event classes
        /// </summary>
        [Serializable]
        internal class DestinationReachedEvent : UnityEvent<Waypoint>
        {
        }
        
        [Serializable]
        internal class WaypointReachedEvent : UnityEvent<Waypoint>
        {
        }
        
        [Serializable]
        internal class WaypointSetEvent : UnityEvent<Waypoint>
        {
        }
        
        [Serializable]
        internal class NavStateChangedEvent : UnityEvent<NavState>
        {
        }
    }
}
