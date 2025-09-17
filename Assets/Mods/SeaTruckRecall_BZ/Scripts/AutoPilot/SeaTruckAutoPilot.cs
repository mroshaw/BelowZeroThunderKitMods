using System.Collections.Generic;
using DaftAppleGames.SeatruckRecall_BZ.Navigation;
using UnityEngine;
using UnityEngine.Events;
using static DaftAppleGames.SeatruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeatruckRecall_BZ.AutoPilot
{
    // AutoPilot states
    internal enum AutoPilotState
    {
        Ready,
        CalculatingRoute,
        Moving,
        Aborted,
        Arrived,
        Blocked,
        Parking,
        Docked
    };


    /// <summary>
    /// MonoBehavior implementing SeaTruck specific AutoPilot behavior
    /// the game.
    /// </summary>
    internal class SeaTruckAutoPilot : MonoBehaviour
    {
        // Unity Event to publish AutoPilot state changes
        internal class AutopilotStateChangedEvent : UnityEvent<AutoPilotState>
        {
        }
        internal class AutoPilotWaypointChangedEvent : UnityEvent<Waypoint, float>
        {
        }
        
        [Header("Stuck Check")]
        [SerializeField] private float stuckCheckTimeThreshold = 10.0f;
        [SerializeField] private float stuckCheckPositionThreshold = 0.1f;
        [SerializeField] private float stuckCheckRotationThreshold = 5.0f;
        
        [Header("Waypoints")]
        [SerializeField] private List<Waypoint> currentWaypoints;

        // Autopilot state
        private AutoPilotState _currentAutoPilotState;
        private NavState _currentNavState;

        private Vector3 _destination;
        
        // If the autopilot is active and doesn't "move" for this amount of time and distance, it's considered "stuck"
        private float _currStuckCheckTimer = 0.0f;

        private Vector3 _lastPosition =  Vector3.zero;
        private Quaternion _lastRotation = Quaternion.identity;

        // Component references
        private WaypointNavigation _waypointNav;
        private InstantNavigation _instantNav;
        private Waypoint _currentWaypoint;

        // Unity Event publishing Status changes
        internal AutopilotStateChangedEvent OnAutoPilotStateChanged = new AutopilotStateChangedEvent();
        internal WaypointChangedEvent OnNavWaypointChanged = new WaypointChangedEvent();
        internal AutoPilotWaypointChangedEvent OnAutoPilotWaypointChanged = new AutoPilotWaypointChangedEvent();
        
        private List<Waypoint> _recallWaypoints;

        protected virtual void OnEnable()
        {
            InitComponentReferences();
            if (_waypointNav)
            {
                // Subscribe to Waypoint changed event
                _waypointNav.OnNavStateChanged.AddListener(NavStateChangedHandler);
                _waypointNav.OnWaypointChanged.AddListener(NavWaypointChangedHandler);
            }
            AllAutoPilots.AddInstance(this);
        }

        protected virtual void OnDisable()
        {
            AllAutoPilots.RemoveInstance(this);
            if (_waypointNav)
            {
                _waypointNav.OnNavStateChanged.RemoveListener(NavStateChangedHandler);
                _waypointNav.OnWaypointChanged.RemoveListener(NavWaypointChangedHandler);
            }
            InitComponentReferences();
        }

        private void Awake()
        {
            // Include layers for obstacle detection
        }

        private void Start()
        {
            // Set default state
            SetAutopilotState(AutoPilotState.Ready);
        }

        private void Update()
        {
            if (_currentAutoPilotState != AutoPilotState.Moving)
            {
                return;
            }

            // If we get stuck, change status and notify listeners
            if (IsStuckCheck())
            {
                LogDebug("AutoPilot is stuck! Aborting!");
                AbortNavigation();
            }
        }
        
        public void BeginParking()
        {
            SetAutopilotState(AutoPilotState.Parking);
        }

        public void DockingComplete()
        {
            SetAutopilotState(AutoPilotState.Docked);
        }

        public void ReleaseFromDock()
        {
            SetAutopilotState(AutoPilotState.Ready);
        }

        private void InitComponentReferences()
        {
            if (!_waypointNav)
            {
                _waypointNav = GetComponent<WaypointNavigation>();
            }

            if (!_instantNav)
            {
                _instantNav = GetComponent<InstantNavigation>();
            }
        }
        
        private bool IsStuckCheck()
        {
            _currStuckCheckTimer += Time.deltaTime;

            if (_currStuckCheckTimer < stuckCheckTimeThreshold)
            {
                return false;
            }

            // Reset timer
            _currStuckCheckTimer = 0.0f;

            if (_lastPosition.Equals(Vector3.zero) || _lastRotation.Equals(Quaternion.identity))
            {
                _lastPosition = transform.position;
                _lastRotation = transform.rotation;
                return false;
            }

            // Check how far we've travelled and rotated since the last stuck check
            if (Vector3.Distance(_lastPosition, transform.position) < stuckCheckPositionThreshold &&
                Quaternion.Angle(_lastRotation, transform.rotation) < stuckCheckRotationThreshold)
            {
                return true;
            }

            // Reset position
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
            return false;

        }

        internal bool IsAvailable()
        {
            return _currentAutoPilotState == AutoPilotState.Ready;
        }

        /// <summary>
        /// Begin navigating to the list of waypoints given
        /// </summary>
        internal bool StartNavigation(Vector3 destination, List<Waypoint> waypoints)
        {
            // Abort, if already being recalled
            if (_currentAutoPilotState != AutoPilotState.Ready)
            {
                // Already being recalled or is already docked
                LogDebug($"AutoPilot BeginNavigation: autopilot is not ready. State is: {_currentAutoPilotState}");
                return false;
            }
            
            // Used to calculate remaining distance
            _destination = destination;
            
            if (_instantNav)
            {
                SetAutopilotState(AutoPilotState.Moving);
                _instantNav.MoveToDestination(waypoints);
                SetAutopilotState(AutoPilotState.Arrived);
                return true;
            }

            // Setup the Waypoint Nav Component
            _waypointNav.SetWayPoints(waypoints);

            // Start navigation
            LogDebug("AutoPilot engaged!");
            _waypointNav.StartWaypointNavigation();

            return true;
        }

        /// <summary>
        /// Public method to abort navigation
        /// </summary>
        private void AbortNavigation()
        {
            _waypointNav.StopWaypointNavigation();
            SetAutopilotState(AutoPilotState.Aborted);
        }

        /// <summary>
        /// Used to set the AutoPilot state, and inform listeners
        /// </summary>
        private void SetAutopilotState(AutoPilotState newState)
        {
            if (_currentAutoPilotState == newState)
            {
                return;
            }
            _currentAutoPilotState = newState;
            OnAutoPilotStateChanged?.Invoke(newState);
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
            float distanceToTarget = Vector3.Distance(transform.position, _destination);
            OnAutoPilotWaypointChanged?.Invoke(newWaypoint, distanceToTarget);
        }

        /// <summary>
        /// Handle NavState change event
        /// </summary>
        private void NavStateChangedHandler(NavState navState)
        {
            LogDebug($"AutoPilot.NavStateChangeHandler: state changed from {_currentNavState} to {navState}");
            _currentNavState = navState;

            // Handle the various SeaTruck states
            switch (_currentNavState)
            {
                case NavState.Moving:
                    SetAutopilotState(AutoPilotState.Moving);
                    break;

                case NavState.Arrived:
                    SetAutopilotState(AutoPilotState.Arrived);
                    SetAutopilotState(AutoPilotState.Ready);
                    break;
                case NavState.WaypointBlocked:
                case NavState.RouteBlocked:
                    SetAutopilotState(AutoPilotState.Blocked);
                    break;
                default:
                    return;
            }
        }
    }
}