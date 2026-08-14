using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    // AutoPilot states
    internal enum AutoPilotState
    {
        Initialising,
        Ready,
        Moving,
        Replanning,
        Aborted,
        Arrived,
        Stuck,
        Parking,
        Docking,
        Docked
    };


    /// <summary>
    /// MonoBehavior implementing SeaTruck specific AutoPilot behavior
    /// the game.
    /// </summary>
    internal class SeaTruckAutoPilot : MonoBehaviour
    {
        [Header("Stuck Check")]
        [SerializeField] private float stuckCheckTimeThreshold = 10.0f;
        [SerializeField] private float stuckCheckPositionThreshold = 0.1f;
        [SerializeField] private float stuckCheckRotationThreshold = 5.0f;
        [SerializeField] private float stuckCheckProgressThreshold = 0.5f;
        
        [Header("Waypoints")]
        [SerializeField] private List<Waypoint> currentWaypoints;

        [Header("Debug")]
        [SerializeField] private AutoPilotState currentAutoPilotState;
        [SerializeField] private NavState currentNavState;
        
        [Header("Events")]
        [SerializeField] internal StateChangedEvent onStateChanged = new StateChangedEvent();
        [SerializeField] internal WaypointChangedEvent onWaypointChanged = new WaypointChangedEvent();
        
        // Used by the MoonPoolExpansion patches to detect a docking autopilot
        internal bool IsBusy => currentAutoPilotState != AutoPilotState.Ready && currentAutoPilotState != AutoPilotState.Initialising;

        internal bool IsNavigating => currentAutoPilotState == AutoPilotState.Moving;

        internal bool IsRecalling => currentAutoPilotState != AutoPilotState.Initialising &&
                                     currentAutoPilotState != AutoPilotState.Ready &&
                                     currentAutoPilotState != AutoPilotState.Aborted &&
                                     currentAutoPilotState != AutoPilotState.Docked;
        
        private Vector3 _finalDestination;
        
        // If the autopilot is active and doesn't "move" for this amount of time and distance, it's considered "stuck"
        private float _currStuckCheckTimer;
        private Vector3 _lastPosition =  Vector3.zero;
        private Quaternion _lastRotation = Quaternion.identity;
        private float _lastDistanceToWaypoint;
        
        // Component references
        private SeaTruckNavMovement _seaTruckNavMovement;
        private Waypoint _currentWaypoint;
        private Rigidbody _rigidBody;
        private Dockable _dockable;
        
        private List<Waypoint> _recallWaypoints;
        private List<Waypoint> _instantNavWaypoints;
        private int _currentWaypointIndex;
        private int _totalWaypoints;

        /// <summary>
        /// Init component references and subscribe to events
        /// </summary>
        protected virtual void OnEnable()
        {
            // Subscribe to Waypoint changed event
            _seaTruckNavMovement.onNavStateChanged.AddListener(NavStateChangedHandler);
            _seaTruckNavMovement.onWaypointSet.AddListener(NavWaypointChangedHandler);
            _seaTruckNavMovement.onDestinationReached.AddListener(NavCompleteHandler);
            
            AllSeaTruckAutoPilots.AddInstance(this);
        }

        /// <summary>
        /// Unsubscribe from events
        /// </summary>
        protected virtual void OnDisable()
        {
            // Subscribe to Waypoint changed event
            _seaTruckNavMovement.onNavStateChanged.RemoveListener(NavStateChangedHandler);
            _seaTruckNavMovement.onWaypointSet.RemoveListener(NavWaypointChangedHandler);
            _seaTruckNavMovement.onDestinationReached.RemoveListener(NavCompleteHandler);
            
            AllSeaTruckAutoPilots.RemoveInstance(this);
        }

        private void Awake()
        {
            _seaTruckNavMovement = GetComponent<SeaTruckNavMovement>();
            _rigidBody = GetComponent<Rigidbody>();
            _dockable = GetComponent<Dockable>();
        }
        
        private void Start()
        {
            // Set default state
            SetAutopilotState(AutoPilotState.Ready);
        }

        /// <summary>
        /// Check to see if the SeaTruck is stuck, and trigger the event
        /// if that happens. The Recaller will handle the Abort.
        /// </summary>
        private void Update()
        {
            if (currentAutoPilotState != AutoPilotState.Moving)
            {
                return;
            }

            // If we get stuck, change status and notify listeners
            if (IsStuckCheck())
            {
                ModDebugLog.LogDebug("AutoPilot has stopped moving. Requesting a new route.");
                _seaTruckNavMovement.BlockNavigation();
            }
        }
        
        /// <summary>
        /// Set by recaller when parking mechanism is engaged
        /// </summary>
        internal void BeginParking()
        {
            _rigidBody.velocity = Vector3.zero;
            _rigidBody.angularVelocity = Vector3.zero;
            SetAutopilotState(AutoPilotState.Parking);
        }

        internal void BeginDocking()
        {
            // If we're coming from Ready state, it means we're already docked (e.g. loading a save game)
            // ModDebugLog.LogDebug($"BeginDocking called with currentAutoPilotState: {currentAutoPilotState}");
            if (currentAutoPilotState == AutoPilotState.Docking || currentAutoPilotState == AutoPilotState.Docked)
            {
                return;
            }
            
            if (currentAutoPilotState == AutoPilotState.Ready || currentAutoPilotState == AutoPilotState.Initialising)
            {
                SetAutopilotState(AutoPilotState.Docked);
                return;
            }
            StopNavigation();
            SetAutopilotState(AutoPilotState.Docking);
        }
        
        /// <summary>
        /// Set by recaller when docked
        /// </summary>
        internal void DockingComplete()
        {
            StopNavigation();
            SetAutopilotState(AutoPilotState.Docked);
        }

        /// <summary>
        /// Set by recaller when released
        /// </summary>
        internal void ReleaseFromDock()
        {
            ModDebugLog.LogDebug("SeaTruck has been released from the dock");
            SetAutopilotState(AutoPilotState.Ready);
        }
        
        /// <summary>
        /// Check to see if the SeaTruck is stuck in the environment.
        /// </summary>
        private bool IsStuckCheck()
        {
            _currStuckCheckTimer += Time.deltaTime;

            if (_currStuckCheckTimer < stuckCheckTimeThreshold)
            {
                return false;
            }

            // Reset timer
            _currStuckCheckTimer = 0.0f;

            // If this is the first check, cache the current transform
            if (_lastPosition.Equals(Vector3.zero) || _lastRotation.Equals(Quaternion.identity))
            {
                _lastPosition = transform.position;
                _lastRotation = transform.rotation;
                _lastDistanceToWaypoint = _currentWaypoint == null
                    ? 0.0f
                    : Vector3.Distance(transform.position, _currentWaypoint.Position);
                ModDebugLog.LogDebug($"Stuck-check baseline: position {transform.position}, rotation " +
                                     $"{transform.eulerAngles}, waypoint '{GetCurrentWaypointName()}', distance " +
                                     $"{_lastDistanceToWaypoint:F2}.");
                return false;
            }

            // Check how far we've travelled and rotated since the last stuck check
            // If we haven't moved since the last check, then we're stuck
            float positionChange = Vector3.Distance(_lastPosition, transform.position);
            float rotationChange = Quaternion.Angle(_lastRotation, transform.rotation);
            float currentDistanceToWaypoint = _currentWaypoint == null
                ? 0.0f
                : Vector3.Distance(transform.position, _currentWaypoint.Position);
            float waypointProgress = _lastDistanceToWaypoint - currentDistanceToWaypoint;
            ModDebugLog.LogDebug($"Stuck check: position {transform.position}, waypoint " +
                                 $"'{GetCurrentWaypointName()}', distance {currentDistanceToWaypoint:F2}, " +
                                 $"progress {waypointProgress:F2}, translation {positionChange:F2}, rotation " +
                                 $"{rotationChange:F1} degrees, velocity {_rigidBody.velocity}, angular velocity " +
                                 $"{_rigidBody.angularVelocity}.");
            if (waypointProgress < stuckCheckProgressThreshold &&
                (positionChange < stuckCheckPositionThreshold ||
                 rotationChange < stuckCheckRotationThreshold))
            {
                return true;
            }

            // Reset position
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
            _lastDistanceToWaypoint = currentDistanceToWaypoint;
            return false;
        }

        private string GetCurrentWaypointName()
        {
            return _currentWaypoint == null ? "none" : _currentWaypoint.Name;
        }

        internal bool IsAvailable()
        {
            return currentAutoPilotState == AutoPilotState.Ready;
        }

        /// <summary>
        /// Begin navigating to the list of waypoints given
        /// </summary>
        internal bool StartNavigation(List<Waypoint> waypoints)
        {
            // Abort, if already being recalled
            if (currentAutoPilotState != AutoPilotState.Ready &&
                currentAutoPilotState != AutoPilotState.Replanning)
            {
                // Already being recalled or is already docked
                ModDebugLog.LogDebug($"AutoPilot BeginNavigation: autopilot is not ready. State is: {currentAutoPilotState}");
                return false;
            }

#if !UNITY_EDITOR
            // Reset dockable time
            _dockable.timeUndocked = 0.0f;
#endif
            _totalWaypoints =  waypoints.Count;
            _currentWaypointIndex = 0;
            _currStuckCheckTimer = 0.0f;
            _lastPosition = Vector3.zero;
            _lastRotation = Quaternion.identity;
            _lastDistanceToWaypoint = 0.0f;
            
            // Used to calculate remaining distance
            _finalDestination = waypoints[waypoints.Count - 1].Position;

            // Start navigation
            ModDebugLog.LogDebug("AutoPilot engaged!");
            return _seaTruckNavMovement.StartNavigation(waypoints);
        }

        /// <summary>
        /// Reserves this SeaTruck while its first navigation segment is being calculated.
        /// </summary>
        internal void BeginPlanning()
        {
            if (currentAutoPilotState == AutoPilotState.Ready)
            {
                SetAutopilotState(AutoPilotState.Replanning);
            }
        }

        /// <summary>
        /// Resets the movement controller so a replacement route can be installed.
        /// </summary>
        internal void PrepareForReplan()
        {
            _seaTruckNavMovement.PauseNavigation();
            SetAutopilotState(AutoPilotState.Replanning);
        }
        
        internal void StopNavigation()
        {
            _seaTruckNavMovement.StopNavigation();
        }
        
        /// <summary>
        /// Public method to abort navigation
        /// </summary>
        internal void AbortNavigation()
        {
            StopNavigation();
            SetAutopilotState(AutoPilotState.Aborted);
            SetAutopilotState(AutoPilotState.Ready);
        }

        internal void SetStuck()
        {
            _seaTruckNavMovement.BlockNavigation();
        }
        
        /// <summary>
        /// Handler the nav component arriving at the final destination
        /// </summary>
        private void NavCompleteHandler(Waypoint waypoint)
        {
            ModDebugLog.LogDebug("AutoPilot nav complete!");
        }
        
        /// <summary>
        /// Used to set the AutoPilot state, and inform listeners
        /// </summary>
        private void SetAutopilotState(AutoPilotState newState)
        {
            if (currentAutoPilotState == newState)
            {
                return;
            }
            ModDebugLog.LogDebug($"AutoPilotState changed from: {currentAutoPilotState} to {newState}");
            AutoPilotState oldState = currentAutoPilotState;
            currentAutoPilotState = newState;
            onStateChanged?.Invoke(oldState, newState);
        }

        /// <summary>
        /// Listen for waypoint changes from the NavMethod and pass it up
        /// </summary>
        private void NavWaypointChangedHandler(Waypoint newWaypoint)
        {
            if (_currentWaypoint == newWaypoint)
            {
                return;
            }

            _currentWaypoint = newWaypoint;
            _currentWaypointIndex++;
            float distanceToTarget = Vector3.Distance(transform.position, _finalDestination);
            _currStuckCheckTimer = 0.0f;
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
            _lastDistanceToWaypoint = Vector3.Distance(transform.position, newWaypoint.Position);
            ModDebugLog.LogDebug($"AutoPilot NavWaypointChanged: {newWaypoint}");
            onWaypointChanged?.Invoke(newWaypoint, _currentWaypointIndex, _totalWaypoints, distanceToTarget);
        }

        /// <summary>
        /// Handle NavState change event
        /// </summary>
        private void NavStateChangedHandler(NavState navState)
        {
            currentNavState = navState;

            // Handle the various SeaTruck states
            switch (currentNavState)
            {
                case NavState.Moving:
                    SetAutopilotState(AutoPilotState.Moving);
                    break;
                case NavState.Arrived:
                    SetAutopilotState(AutoPilotState.Arrived);
                    break;
                case NavState.Blocked:
                    SetAutopilotState(AutoPilotState.Stuck);
                    break;
                default:
                    return;
            }
        }
        
        // Unity Event to publish AutoPilot state changes
        [Serializable]
        internal class StateChangedEvent : UnityEvent<AutoPilotState, AutoPilotState>
        {
        }
    }
}
