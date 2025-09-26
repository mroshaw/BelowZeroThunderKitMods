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
        [SerializeField] private float rotateAngleThreshold = 0.1f;
        
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
        
        private SeaTruckMotor _motor;
        // Rigidbody of the main SeaTruck
        private Rigidbody _mainRigidbody;
        // Rigidbodies of any attached SeaTruck segments
        private List<RigidBodyBackup> _rigidBodyBackups;
        private int _numChildRigidBodies;
        private bool _mainTruckRigidBodyBackupIsKinematic;
        private CollisionDetectionMode _mainTruckRigidBodyBackupCollisionDetectionMode;
        private RigidbodyInterpolation _mainTruckRigidBodyBackupInterpolation;
        private void Awake()
        {
            _motor = GetComponent<SeaTruckMotor>();
            _mainRigidbody = GetComponent<Rigidbody>();
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
            
            if (!currentWaypoint.RotateBeforeMoving || (currentWaypoint.RotateBeforeMoving && isRotationComplete))
            {
                MoveUpdate();
            }
            CheckMoveComplete();
        }

        /// <summary>
        /// Rotate in Update
        /// </summary>
        private void Update()
        {
            if (currentNavState != NavState.Moving)
            {
                return;
            }
            
            _mainRigidbody.isKinematic = false;
            
            if (!isRotationComplete)
            {
                RotateUpdate();
                CheckRotationComplete();
            }

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
            if (currentWaypointIndex > 0)
            {
                float distanceToNextWaypoint = Vector3.Distance(waypoints[currentWaypointIndex].Position,
                    waypoints[currentWaypointIndex - 1].Position);
                if (distanceToNextWaypoint < skipWaypointThreshold)
                {
                    currentWaypointIndex++;
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
            CacheRigidbodies();
            ConfigureRigidBodies();
            SetNextWaypoint();
            SetNavState(NavState.Moving);
            return true;
        }

        internal void StopNavigation()
        {
            InitialiseNav();
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
                _mainRigidbody.velocity = Vector3.zero;
            }
            onWaypointReached?.Invoke(currentWaypoint);
            SetNextWaypoint();
        }
    
        /// <summary>
        /// Called when all waypoints have been reached
        /// </summary>
        private void NavComplete()
        {
            RestoreRigidBodies();
            RestoreSeaTruckRigidBodyState();
            SetNavState(NavState.Arrived);
            InitialiseNav();
            onDestinationReached.Invoke(currentWaypoint);
        }
        
        /// <summary>
        /// Moves towards the target position each frame.
        /// </summary>
        private void MoveUpdate()
        {
            // Set the SeaTruck rigidbody to allow physics movement
            CacheSeaTruckRigidBodyState();
            SetSeaTruckRigidBodyForMove();
            
            Vector3 direction = currentWaypoint.Position - _mainRigidbody.position;
            float distance = direction.magnitude;
            
            // Normalize direction for consistent speed
            Vector3 normalizedDirection = direction / distance;

            // Calculate speed scaling: 1 when far, 0 when very close
            float scaleFactor = currentWaypoint.SlowDownToTarget ? Mathf.Clamp01(distance / slowDistance) : 1.0f;
            float scaledSpeed = moveSpeed * scaleFactor;

            _mainRigidbody.velocity = normalizedDirection * scaledSpeed;
            
            // Restore the SeaTruck rigidbody state
            RestoreSeaTruckRigidBodyState();
        }

        /// <summary>
        /// Implement the RotateUpdate interface method, using the Rigidbody to move the Source transform
        /// </summary>
        private void RotateUpdate()
        {
            // Set the SeaTruck rigidbody to allow physics movement
            CacheSeaTruckRigidBodyState();
            SetSeaTruckRigidBodyForMove();
            
            // Direction vector from player to target
            Vector3 toTarget = (currentWaypoint.Position - transform.position).normalized;

            // Current forward direction
            Vector3 forward = transform.forward;

            // Cross product gives the axis of rotation from forward → toTarget
            Vector3 rotationAxis = Vector3.Cross(forward, toTarget);

            // Angle between forward and target
            float angle = Vector3.SignedAngle(forward, toTarget, rotationAxis);

            // Apply torque proportional to angle
            _mainRigidbody.AddTorque(rotationAxis.normalized * angle * rotateSpeed);
            
            // Restore the SeaTruck rigidbody state
            RestoreSeaTruckRigidBodyState();
        }
        
        /// <summary>
        /// True if we've arrived
        /// </summary>
        private bool HasArrivedAtCurrentWaypoint()
        {
            return isAtCurrentTarget;
        }
        
        /// <summary>
        /// Determines whether the source is now facing the target
        /// </summary>
        private void CheckRotationComplete()
        {
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