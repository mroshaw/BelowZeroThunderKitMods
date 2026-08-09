using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    // Recaller Status
    internal enum DockRecallState
    {
        None,
        Initialising,
        NoTrucksFound,
        FindingPath,
        PathingError,
        Ready,
        Recalling,
        Stuck,
        Aborted,
        Docking,
        Docked
    }

    /// <summary>
    /// MonoBehaviour class to attach to a SeaTruckDock
    /// that implements the recall behaviour
    /// </summary>
    internal class SeaTruckDockRecaller : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool createGridOnStart = true;
        [SerializeField] private bool instantNav = false;
        [SerializeField] private int maximumReplanAttempts = 3;
        
        [Header("Debug")]
        [SerializeField] private SeaTruckAutoPilot currentAutoPilot;
        
        [Header("Events")]
        // Event publishing latest recall state and distance
        [SerializeField] internal AutoPilotChangedEvent onAutoPilotChanged = new AutoPilotChangedEvent();
        [SerializeField]internal DockStateChangedEvent onDockingStateChanged = new DockStateChangedEvent();
        
        private List<Waypoint> _instantNavWaypoints = new List<Waypoint>();
        private Waypoint _endOfDockRunway;
        private Waypoint _startOfDockRunway;
        private Waypoint _dockEngagement;
        
        // Useful internal components
        private MoonpoolExpansionManager _dockingManager;

        // Internal tracking and audit
        private DockRecallState _currentRecallState = DockRecallState.None;
        
        private bool _gridReady;
        private NavGridHelper _navGridHelper;
        private int _pathRequestVersion;
        private int _replanAttemptCount;
        private bool _replanGridGenerating;

        private void OnEnable()
        {
            if (!_navGridHelper)
            {
                _navGridHelper = GetComponent<NavGridHelper>();
            }
            AllSeaTruckDockRecallers.AddInstance(this);
        }

        private void OnDisable()
        {
            AllSeaTruckDockRecallers.RemoveInstance(this);
        }

        private void Awake()
        {
            _navGridHelper = GetComponent<NavGridHelper>();
            _dockingManager = GetComponent<MoonpoolExpansionManager>();
            SetDockedAutoPilot();
        }
        
        private void Start()
        {
            // Init useful local components
#if !UNITY_EDITOR
            _dockingManager = gameObject.transform.parent.GetComponent<MoonpoolExpansionManager>();
#endif

            // Set up the docking waypoints
            CreateWaypoints();

            if (createGridOnStart)
            {
                GenerateNavGrid();
            }
        }

        /// <summary>
        /// Check to see if something is docked when the component first starts
        /// </summary>
        private void SetDockedAutoPilot()
        {
            if (_dockingManager && _dockingManager.dockedHead && !currentAutoPilot)
            {
                SeaTruckAutoPilot autoPilot = _dockingManager.dockedHead.GetComponent<SeaTruckAutoPilot>();
                if (autoPilot)
                {
                    SetAutoPilot(autoPilot);
                }
            }
        }
        
        /// <summary>
        /// Sets the SeaTruck linked to this recaller
        /// </summary>
        private void SetAutoPilot(SeaTruckAutoPilot newAutoPilot)
        {
            SeaTruckAutoPilot oldAutoPilot = currentAutoPilot;
            currentAutoPilot = newAutoPilot;
            
            ModDebugLog.LogDebug($"AutoPilot changed from: {oldAutoPilot} to {newAutoPilot}");
            
            if (oldAutoPilot)
            {
                oldAutoPilot.onStateChanged?.RemoveListener(AutoPilotStateChangeHandler);
            }

            if (newAutoPilot)
            {
                newAutoPilot.onStateChanged?.AddListener(AutoPilotStateChangeHandler);
            }
            
            onAutoPilotChanged?.Invoke(oldAutoPilot, newAutoPilot);
        }
        
        private IEnumerator GenerateNavGridAsync(Vector3 centerPosition)
        {
            SetDockState(DockRecallState.Initialising);
            yield return _navGridHelper.RefreshNavGridAsync(centerPosition, GridReadyHandler);
        }
        
        /// <summary>
        /// Generate the NavGrid centered around this dock
        /// </summary>
        internal void GenerateNavGrid()
        {
            SetDockState(DockRecallState.Initialising);
            _navGridHelper.RefreshNavGrid(_startOfDockRunway.Position, GridReadyHandler);
        }

        private void GridReadyHandler(GenerateStatus gridStatus)
        {
            _gridReady = gridStatus == GenerateStatus.Success;
            if (_gridReady)
            {
                SetDockState(DockRecallState.Ready);
            }
        }
        
        private void AutoPilotStateChangeHandler(AutoPilotState oldState, AutoPilotState newState)
        {
            switch (newState)
            {
                case AutoPilotState.Ready:
                    SetDockState(DockRecallState.Ready);
                    break;
                case AutoPilotState.Moving:
                    SetDockState(DockRecallState.Recalling);
                    break;
                case AutoPilotState.Replanning:
                    SetDockState(DockRecallState.FindingPath);
                    break;
                case AutoPilotState.Aborted:
                    if (_currentRecallState != DockRecallState.PathingError &&
                        _currentRecallState != DockRecallState.Stuck)
                    {
                        SetDockState(DockRecallState.Aborted);
                    }
                    SetAutoPilot(null);
                    SetDockState(_replanGridGenerating
                        ? DockRecallState.Initialising
                        : DockRecallState.Ready);
                    break;
                case AutoPilotState.Stuck:
                    BeginReplan();
                    break;
                case AutoPilotState.Arrived:
                    break;
                case AutoPilotState.Docking:
                    SetDockState(DockRecallState.Docking);
                    break;
                case AutoPilotState.Docked:
                    SetDockState(DockRecallState.Docked);
                    break;
            }
        }
        
        /// <summary>
        /// Release the currently docked seatruck. Does a check to see if we have a Seatruck in the
        /// dock that isn't registered (e.g. from a game save load)
        /// </summary>
        public void ReleaseCurrentlyDocked()
        {
            if (_dockingManager.dockedHead && !currentAutoPilot)
            {
                SetDockedAutoPilot();
            }

            if (currentAutoPilot)
            {
                currentAutoPilot.ReleaseFromDock();
                SetAutoPilot(null);
            }
            SetDockState(DockRecallState.Ready);
        }

        /// <summary>
        /// Public method to cancel in-progress Recall
        /// </summary>
        internal void AbortRecall()
        {
            ModDebugLog.LogDebug("Aborting Recall...");
            _pathRequestVersion++;
            _navGridHelper.CancelPathGeneration();
            currentAutoPilot.AbortNavigation();
        }

        /// <summary>
        /// Public method to recall the closest SeaTruck
        /// </summary>
        public void RecallClosestSeaTruck()
        {
            if (!IsDockReady())
            {
                ModDebugLog.LogDebug("Dock is already occupied or busy!");
                return;
            }
            ModDebugLog.LogDebug("Finding closest SeaTruck...");
            SeaTruckAutoPilot closestAutoPilot = AllSeaTruckAutoPilots.GetClosestAutoPilot(transform.position,
                _navGridHelper.GridRadius);
            if (closestAutoPilot == null)
            {
                // Couldn't find a closest SeaTruck
                ModDebugLog.LogDebug("No SeaTrucks found!");
                SetDockState(DockRecallState.NoTrucksFound);
                SetDockState(DockRecallState.Ready);
                return;
            }

            // Recall the SeaTruck
            SetAutoPilot(closestAutoPilot);
            _replanAttemptCount = 0;
            
#if UNITY_EDITOR
            if (instantNav)
#else
            if (ConfigFile.RecallMoveMethod == RecallMoveMethod.Teleport)
#endif
            {
                InstantNav();
                return;
            }
            
            // Generate a path for the SeaTruck
            SetDockState(DockRecallState.FindingPath);
            int requestVersion = ++_pathRequestVersion;
            SeaTruckAutoPilot requestedAutoPilot = currentAutoPilot;
            _navGridHelper.GenerateNavPath(closestAutoPilot.transform.position, _startOfDockRunway.Position,
                (pathStatus, navPath) => PathReadyHandler(requestVersion, requestedAutoPilot, pathStatus, navPath));
        }

        /// <summary>
        /// Teleports the SeaTruck to the end of the docking tube, then navigates to the dock point
        /// </summary>
        private void InstantNav()
        {
            if (currentAutoPilot)
            {
                currentAutoPilot.transform.position = _startOfDockRunway.Position;
                currentAutoPilot.transform.LookAt(_endOfDockRunway.Position);
                currentAutoPilot.StartNavigation(_instantNavWaypoints);
            }
        }
        
        private void PathReadyHandler(int requestVersion, SeaTruckAutoPilot requestedAutoPilot,
            GenerateStatus pathStatus, NavPath navPath)
        {
            if (requestVersion != _pathRequestVersion || requestedAutoPilot != currentAutoPilot)
            {
                ModDebugLog.LogDebug("Ignoring stale navigation path result.");
                return;
            }

            if (pathStatus == GenerateStatus.Success)
            {
                List<Waypoint> waypoints = navPath.GetWayPointsFromNavPath();
                
                // Add the two runway points
                // waypoints.Add(_startOfDockRunway);
                waypoints.Add(_endOfDockRunway);
                waypoints.Add(_dockEngagement);
                if (!currentAutoPilot.StartNavigation(waypoints))
                {
                    FailRecall(DockRecallState.PathingError);
                }
            }
            else
            {
                FailRecall(DockRecallState.PathingError);
            }
        }

        private void BeginReplan()
        {
            if (!currentAutoPilot)
            {
                return;
            }

            if (_replanAttemptCount >= maximumReplanAttempts)
            {
                ModDebugLog.LogDebug("Maximum route replanning attempts reached.");
                FailRecall(DockRecallState.Stuck);
                return;
            }

            _replanAttemptCount++;
            ModDebugLog.LogDebug($"Refreshing navigation grid for replan attempt {_replanAttemptCount} of {maximumReplanAttempts}.");
            currentAutoPilot.PrepareForReplan();
            SetDockState(DockRecallState.FindingPath);
            _replanGridGenerating = true;

            int requestVersion = ++_pathRequestVersion;
            SeaTruckAutoPilot requestedAutoPilot = currentAutoPilot;
            _navGridHelper.RefreshNavGrid(_startOfDockRunway.Position,
                gridStatus => ReplanGridReadyHandler(requestVersion, requestedAutoPilot, gridStatus));
        }

        private void ReplanGridReadyHandler(int requestVersion, SeaTruckAutoPilot requestedAutoPilot,
            GenerateStatus gridStatus)
        {
            _replanGridGenerating = false;
            if (requestVersion != _pathRequestVersion || requestedAutoPilot != currentAutoPilot)
            {
                if (!currentAutoPilot)
                {
                    _gridReady = gridStatus == GenerateStatus.Success;
                    SetDockState(_gridReady ? DockRecallState.Ready : DockRecallState.PathingError);
                }
                return;
            }

            if (gridStatus != GenerateStatus.Success)
            {
                FailRecall(DockRecallState.PathingError);
                return;
            }

            _navGridHelper.GenerateNavPath(currentAutoPilot.transform.position, _startOfDockRunway.Position,
                (pathStatus, navPath) => PathReadyHandler(requestVersion, requestedAutoPilot, pathStatus, navPath));
        }

        private void FailRecall(DockRecallState failureState)
        {
            SetDockState(failureState);
            if (currentAutoPilot)
            {
                currentAutoPilot.AbortNavigation();
            }
            else
            {
                SetDockState(DockRecallState.Ready);
            }
        }
        
        /// <summary>
        /// Set up the docking waypoints for this dock
        /// </summary>
        private void CreateWaypoints()
        {
            Transform dockingTriggerTransform = _dockingManager && _dockingManager.bay
                ? _dockingManager.bay.transform
                : gameObject.transform;

            _startOfDockRunway = CreateDockWaypoint(gameObject.transform.position + (new Vector3(0, 0.1f, 0)) + (-gameObject.transform.right * 45.0f), "Docking Runway Start", Color.red, true);
            _endOfDockRunway = CreateDockWaypoint(gameObject.transform.position + (new Vector3(0, 0.1f, 0)) + (-gameObject.transform.right * 30.0f), "Docking Runway End", Color.yellow, true);
            _dockEngagement = CreateDockWaypoint(dockingTriggerTransform.position, "Dock Engagement", Color.green, false);
            
            _instantNavWaypoints.Add(_endOfDockRunway);
            _instantNavWaypoints.Add(_dockEngagement);
        }
        
        /// <summary>
        /// Create a dock waypoint
        /// </summary>
        private Waypoint CreateDockWaypoint(Vector3 position, string waypointName, Color debugColor,
            bool monitorObstacles)
        {
            Waypoint newWaypoint = new Waypoint(position, Quaternion.identity, true, true, waypointName,
                monitorObstacles);

            // if (_navGridHelper.NavGridDebug)
            if (true)
            {
                GameObject newWaypointGo = new GameObject(waypointName)
                {
                    transform =
                    {
                        position = position
                    }
                };
                
                /*
                GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.name = waypointName;
                debugSphere.transform.SetParent(_navGridHelper.NavGridDebugContainer);
                debugSphere.transform.position = newWaypointGo.transform.position;
                debugSphere.transform.rotation = newWaypointGo.transform.rotation;
                debugSphere.GetComponent<Renderer>().material.color = debugColor;
                Destroy(debugSphere.GetComponent<Collider>());
                */
            }

            return newWaypoint;
        }
        
        /// <summary>
        /// Returns true if recaller is available
        /// otherwise false
        /// </summary>
        public bool IsDockReady()
        {
            // Allow us to test in the Unity Editor
            if (!_dockingManager)
            {
                return true;
            }
            return _gridReady && !_dockingManager.IsOccupied() && currentAutoPilot == null;
        }
        
        private void SetDockState(DockRecallState newRecallState)
        {
            if (newRecallState == _currentRecallState)
            {
                return;
            }

            ModDebugLog.LogDebug($"SeaTruckRecaller.SetDockState: state changed from {_currentRecallState} to {newRecallState}.");
            _currentRecallState = newRecallState;
            onDockingStateChanged?.Invoke(newRecallState);
        }
        
        // Unity Events to publish DockRecallStatus changes
        [Serializable]
        internal class AutoPilotChangedEvent : UnityEvent<SeaTruckAutoPilot, SeaTruckAutoPilot>
        {
        }
        
        [Serializable]
        internal class DockStateChangedEvent : UnityEvent<DockRecallState>
        {
        }
    }
}
