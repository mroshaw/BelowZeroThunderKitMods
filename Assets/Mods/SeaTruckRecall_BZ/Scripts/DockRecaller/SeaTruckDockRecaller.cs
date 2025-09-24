using System;
using System.Collections;
using System.Collections.Generic;
using DaftAppleGames.SeaTruckRecall_BZ.Navigation;

using UnityEngine;
using UnityEngine.Events;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    // Recaller Status
    internal enum DockRecallState
    {
        Initialising,
        PathingError,
        Ready,
        Recalling,
        Stuck,
        Aborted,
        Parking,
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
        [SerializeField] private float maxRange = 500.0f;
        [SerializeField] private bool createGridOnStart = true;
        [SerializeField] private Vector3 parkingDockConnection;

        [Header("Parking")]
        [SerializeField] private float parkingTimeout = 20.0f;
        [SerializeField] private float parkingMoveSpeed = 0.08f;
        [SerializeField] private float parkingRotateSpeed = 0.3f;
        
        [Header("Debug")]
        [SerializeField] private SeaTruckAutoPilot currentAutoPilot;
        
        [Header("Events")]
        // Event publishing latest recall state and distance
        [SerializeField] internal AutoPilotChangedEvent onAutoPilotChanged = new AutoPilotChangedEvent();
        [SerializeField]internal DockStateChangedEvent onDockingStateChanged = new DockStateChangedEvent();
        
        private Waypoint _endOfDockRunway;
        private Waypoint _startOfDockRunway;
        private Waypoint _dockEngagement;
        
        // Useful internal components
        private MoonpoolExpansionManager _dockingManager;

        // Internal tracking and audit
        private DockRecallState _currentRecallState = DockRecallState.Initialising;
        
        private bool _gridReady;
        private NavGridHelper _navGridHelper;

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
        }
        
        private void Start()
        {
            // Init useful local components
#if !UNITY_EDITOR
            _dockingManager = gameObject.transform.parent.GetComponent<MoonpoolExpansionManager>();
#endif
            // Set the initial dock status
            SetCurrentDockedStatus();

            // Set the parking position
            parkingDockConnection = gameObject.transform.position + (-gameObject.transform.right * 2.0f);

            // Set up the docking waypoints
            CreateWaypoints();

            if (createGridOnStart)
            {
                GenerateNavGrid();
            }
        }

        /// <summary>
        /// Sets the SeaTruck linked to this recaller
        /// </summary>
        private void SetAutoPilot(SeaTruckAutoPilot newAutoPilot)
        {
            SeaTruckAutoPilot oldAutoPilot = currentAutoPilot;
            currentAutoPilot = newAutoPilot;
            if (oldAutoPilot)
            {
                newAutoPilot.onStateChanged.RemoveListener(AutoPilotStateChangeHandler);
            }

            if (newAutoPilot)
            {
                newAutoPilot.onStateChanged.AddListener(AutoPilotStateChangeHandler);
            }
            
            onAutoPilotChanged?.Invoke(oldAutoPilot, newAutoPilot);
        }
        
        internal void SetMaxRange(float newRange)
        {
            maxRange = newRange;
        }
        
        /// <summary>
        /// Generate the NavGrid centered around this dock
        /// </summary>
        private void GenerateNavGrid()
        {
            SetDockState(DockRecallState.Initialising);
            _navGridHelper.RefreshNavGrid(GridReadyHandler);
        }

        private void GridReadyHandler(GenerateStatus gridStatus)
        {
            _gridReady = gridStatus == GenerateStatus.Success;
            if (_gridReady)
            {
                SetDockState(DockRecallState.Ready);
            }
        }
        
        private void AutoPilotStateChangeHandler(AutoPilotState newState)
        {
            switch (newState)
            {
                case AutoPilotState.Ready:
                    SetDockState(DockRecallState.Ready);
                    break;
                case AutoPilotState.Aborted:
                    SetDockState(DockRecallState.Ready);
                    break;
                case AutoPilotState.Blocked:
                    SetDockState(DockRecallState.Ready);
                    break;
                case AutoPilotState.Arrived:
                    currentAutoPilot.BeginParking();
                    break;
                case AutoPilotState.Parking:
                    SetDockState(DockRecallState.Parking);
                    // StartCoroutine(ParkSeaTruckAsync());
                    break;
                case AutoPilotState.Docking:
                    SetDockState(DockRecallState.Docking);
                    break;
                case AutoPilotState.Docked:
                    SetDockState(DockRecallState.Docked);
                    break;
            }
        }
        
        public void ReleaseCurrentlyDocked()
        {
            if (currentAutoPilot == null)
            {
                ModDebugLog.LogDebug("ReleaseCurrentlyDocked called but there is no SeaTruck docked.");
                return;
            }
            ReleaseCurrentSeaTruck();
            SetDockState(DockRecallState.Ready);
        }

        /// <summary>
        /// Public method to cancel in-progress Recall
        /// </summary>
        internal void AbortRecall()
        {
            ModDebugLog.LogDebug("Aborting Recall...");
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
            SeaTruckAutoPilot closestAutoPilot = AllSeaTruckAutoPilots.GetClosestAutoPilot(transform.position, maxRange);
            if (closestAutoPilot == null)
            {
                // Couldn't find a closest SeaTruck
                ModDebugLog.LogDebug("No SeaTrucks found!");
                return;
            }

            // Recall the SeaTruck
            SetAutoPilot(closestAutoPilot);
            
            // Generate a path for the SeaTruck
            _navGridHelper.GenerateNavPath(closestAutoPilot.transform.position, _startOfDockRunway.Position, PathReadyHandler);
        }

        private void PathReadyHandler(GenerateStatus pathStatus, NavPath navPath)
        {
            if (pathStatus == GenerateStatus.Success)
            {
                List<Waypoint> waypoints = navPath.GetWayPointsFromNavPath();
                
                // Add the two runway points
                // waypoints.Add(_startOfDockRunway);
                waypoints.Add(_endOfDockRunway);
                waypoints.Add(_dockEngagement);
                currentAutoPilot.StartNavigation(transform.position, waypoints);
            }
            else
            {
                SetDockState(DockRecallState.PathingError);
                currentAutoPilot.AbortNavigation();
                SetDockState(DockRecallState.Ready);
            }
        }
        
        /// <summary>
        /// Set up the docking waypoints for this dock
        /// </summary>
        private void CreateWaypoints()
        {
            _startOfDockRunway = CreateDockWaypoint(gameObject.transform.position + (new Vector3(0, 0.1f, 0)) + (-gameObject.transform.right * 45.0f), "Docking Runway Start", Color.red);
            _endOfDockRunway = CreateDockWaypoint(gameObject.transform.position + (new Vector3(0, 0.1f, 0)) + (-gameObject.transform.right * 30.0f), "Docking Runway End", Color.yellow);
            _dockEngagement = CreateDockWaypoint(gameObject.transform.position + (new Vector3(0, 0.8f, 0)) + (-gameObject.transform.right * 10.0f), "Dock Engagement", Color.green);
        }
        
        /// <summary>
        /// Create a dock waypoint
        /// </summary>
        private Waypoint CreateDockWaypoint(Vector3 position, string waypointName, Color debugColor)
        {
            Waypoint newWaypoint = new Waypoint(position, Quaternion.identity, true, true, waypointName);

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
                
                GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.name = waypointName;
                debugSphere.transform.SetParent(_navGridHelper.NavGridDebugContainer);
                debugSphere.transform.position = newWaypointGo.transform.position;
                debugSphere.transform.rotation = newWaypointGo.transform.rotation;
                debugSphere.GetComponent<Renderer>().material.color = debugColor;
                Destroy(debugSphere.GetComponent<Collider>());
            }

            return newWaypoint;
        }
        
        private void ReleaseCurrentSeaTruck()
        {
            currentAutoPilot.ReleaseFromDock();
            SetAutoPilot(null);
        }
        
        /// <summary>
        /// Sets the appropriate docked status
        /// </summary>
        private void SetCurrentDockedStatus()
        {
            SetDockState(IsDockReady() ? DockRecallState.Ready : DockRecallState.Docked);
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
            return !_dockingManager.IsOccupied() && currentAutoPilot == null;
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
        private IEnumerator ParkSeaTruckAsync()
        {
            ModDebugLog.LogDebug("Parking SeaTruck...");
            
            float dockTime = 0.0f;

            currentAutoPilot.transform.LookAt(parkingDockConnection);
            
            if (currentAutoPilot == null)
            {
                ModDebugLog.LogDebug("Parking cancelled - SeaTruck not set");
                yield break;
            }

            Vector3 dirToTarget = parkingDockConnection - currentAutoPilot.transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(dirToTarget);

            while (_currentRecallState != DockRecallState.Docked && _currentRecallState != DockRecallState.Docking)
            {
                // Rotate
                if (Quaternion.Angle(transform.rotation, targetRotation) >= 1f)
                {
                    currentAutoPilot.transform.rotation = Quaternion.Slerp(currentAutoPilot.transform.rotation, targetRotation, Time.deltaTime * parkingRotateSpeed);
                }

                // Move
                if (Vector3.Distance(parkingDockConnection, currentAutoPilot.transform.position) >= 0.01f)
                {
                    currentAutoPilot.transform.position = Vector3.Lerp(currentAutoPilot.transform.position, parkingDockConnection, Time.deltaTime * parkingMoveSpeed);    
                }

                dockTime += Time.deltaTime;
                
                if (dockTime > parkingTimeout)
                {
                    ModDebugLog.LogDebug("Parking timed out!");
                    currentAutoPilot.transform.position = parkingDockConnection;
                    _dockingManager.JostleSeatruck(currentAutoPilot.GetComponent<SeaTruckSegment>());
                    yield break;
                }

                yield return null;
            }
            ModDebugLog.LogDebug("Parking is complete!");
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