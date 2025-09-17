using System.Collections;
using System.Collections.Generic;
using DaftAppleGames.SeatruckRecall_BZ.AutoPilot;
using DaftAppleGames.SeatruckRecall_BZ.Navigation;

using UnityEngine;
using UnityEngine.Events;
using static DaftAppleGames.SeatruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeatruckRecall_BZ.DockRecaller
{
    // Recaller Status
    internal enum DockRecallState
    {
        Initialising,
        Ready,
        Recalling,
        Stuck,
        Aborted,
        Parking,
        Docked
    }

    internal class AutoPilotChangedEvent : UnityEvent<SeaTruckAutoPilot, SeaTruckAutoPilot>
    {
    }

    // Unity Event to publish DockRecallStatus changes
    internal class DockRecallStateChangedEvent : UnityEvent<DockRecallState>
    {
    }

    /// <summary>
    /// MonoBehaviour class to attach to a SeatruckDock
    /// that implements the recall behaviour
    /// </summary>
    internal class SeaTruckDockRecaller : MonoBehaviour
    {
        // Waypoint names
        private const string MoveToBaseText = "MOVING TO BASE";
        private const string AlignToDockText = "ALIGNING TO DOCK";
        private const string MovingToDockText = "MOVING TO DOCK";
        
        [SerializeField] private SeaTruckAutoPilot currentAutoPilot;
        // Transform within the dock, that the recall will pull the SeaTruck into it's final docking place
        // If not docked within the timeout, abandon
        [SerializeField] private float maxRange = 500.0f;
        [SerializeField] private bool createGridOnStart = true;
        [SerializeField] private Vector3 parkingDockConnection;
        [SerializeField] private List<Waypoint> dockingWaypoints;

        // Useful internal components
        private MoonpoolExpansionManager _dockingManager;

        // Internal tracking and audit
        private DockRecallState _currentRecallState = DockRecallState.Initialising;

        private const float ParkingTimeout = 5.0f;
        private const float ParkingMoveSpeed = 1.0f;
        private const float ParkingRotateSpeed = 1.0f;

        // Event publishing latest recall state and distance
        internal AutoPilotChangedEvent OnAutoPilotChanged = new  AutoPilotChangedEvent();
        internal DockRecallStateChangedEvent OnDockingStateChanged = new DockRecallStateChangedEvent();

        internal UnityEvent OnDocked = new UnityEvent();

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
        
        private void Start()
        {
            // Init useful local components
            _dockingManager = GetComponent<MoonpoolExpansionManager>();

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
            OnAutoPilotChanged?.Invoke(oldAutoPilot, newAutoPilot);
        }
        
        internal void SetMaxRange(float newRange)
        {
            maxRange = newRange;
        }
        
        /// <summary>
        /// Generate the NavGrid centered around this dock
        /// </summary>
        public void GenerateNavGrid()
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
        
        public void CurrentSeaTruckDocked()
        {
            SetDockState(DockRecallState.Docked);
            OnDocked?.Invoke();
        }

        public void ReleaseCurrentlyDocked()
        {
            if (currentAutoPilot == null)
            {
                LogDebug("ReleaseCurrentlyDocked called but there is no SeaTruck docked.");
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
            LogDebug("Aborting Recall...");
            SetDockState(DockRecallState.Aborted);
        }

        /// <summary>
        /// Public method to recall the closest Seatruck
        /// </summary>
        public void RecallClosestSeaTruck()
        {
            if (!IsDockReady())
            {
                LogDebug("Dock is already occupied or busy!");
                return;
            }
            LogDebug("Finding closest Seatruck...");
            SeaTruckAutoPilot closestAutoPilot = AllAutoPilots.GetClosestAutoPilot(transform.position, maxRange);
            if (closestAutoPilot == null)
            {
                // Couldn't find a closest Seatruck
                LogDebug("No Seatrucks found!");
                return;
            }

            // Recall the SeaTruck
            SetAutoPilot(closestAutoPilot);
            
            // Generate a path for the SeaTruck
            _navGridHelper.GenerateNavPath(closestAutoPilot.transform.position, dockingWaypoints[dockingWaypoints.Count - 1].Position, PathReadyHandler);
        }

        private void PathReadyHandler(GenerateStatus pathStatus, NavPath navPath)
        {
            if (pathStatus == GenerateStatus.Success)
            {
                List<Waypoint> waypoints = navPath.GetFinalWaypoints(dockingWaypoints);
                currentAutoPilot.StartNavigation(transform.position, waypoints);
            }
        }
        
        /// <summary>
        /// Set up the docking waypoints for this dock
        /// </summary>
        private void CreateWaypoints()
        {
            dockingWaypoints = new List<Waypoint>();

            // Waypoint above the entrance to the docking tube
            GameObject aboveDockingTubeWaypoint = new GameObject("Top of End of Tube Waypoint")
            {
                transform =
                {
                    position = gameObject.transform.position + (-gameObject.transform.right * 30.0f) + (gameObject.transform.up * 10.0f)
                }
            };
            dockingWaypoints.Add(new Waypoint(aboveDockingTubeWaypoint.transform.position,
                Quaternion.identity,
                false,
                MoveToBaseText));

            // CreateSphere(aboveDockingTubeWaypoint.transform.position, 2.0f, Color.red);
            LogDebug($"Dock tube above end position: {aboveDockingTubeWaypoint.transform.position}");

            // Waypoint at the end of the docking tube.
            GameObject endOfDockTubeWaypoint = new GameObject("End of Tube Waypoint")
            {
                transform =
                {
                    position = gameObject.transform.position + (-gameObject.transform.right * 50.0f)
                }
            };
            dockingWaypoints.Add(new Waypoint(endOfDockTubeWaypoint.transform.position,
                Quaternion.identity,
                true,
                AlignToDockText));

            // CreateSphere(endOfDockTubeWaypoint.transform.position, 1.5f, Color.yellow);
            LogDebug($"Dock tube end position: {endOfDockTubeWaypoint.transform.position}");

            // Waypoint into the docking tube itself
            GameObject dockingWaypoint = new GameObject("Docking Waypoint")
            {
                transform =
                {
                    position = gameObject.transform.position + (-gameObject.transform.right * 15.0f)
                }
            };
            dockingWaypoints.Add(new Waypoint(dockingWaypoint.transform.position,
                Quaternion.identity,
                true,
                MovingToDockText));

            // CreateSphere(dockingWaypoint.transform.position, 1.0f, Color.green);
            LogDebug($"Dock final position: {dockingWaypoint.transform.position}");
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

            LogDebug($"SeaTruckRecaller.SetDockState: state changed from {_currentRecallState} to {newRecallState}.");
            _currentRecallState = newRecallState;
            OnDockingStateChanged?.Invoke(newRecallState);
        }

        /// <summary>
        /// Pulls the SeaTruck towards the dock, forcing it to engage and dock
        /// </summary>
        private void ParkSeaTruck()
        {
            StartCoroutine(ParkSeaTruckAsync());
        }

        private IEnumerator ParkSeaTruckAsync()
        {
            LogDebug("Parking SeaTruck...");

            currentAutoPilot.BeginParking();

            float dockTime = 0.0f;

            if (currentAutoPilot == null)
            {
                LogDebug("Parking cancelled - SeaTruck not set");
                yield break;
            }

            Vector3 dirToTarget = parkingDockConnection - currentAutoPilot.transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(dirToTarget);

            while (_currentRecallState != DockRecallState.Docked)
            {
                // Rotate
                currentAutoPilot.transform.rotation = Quaternion.Slerp(currentAutoPilot.transform.rotation, targetRotation, Time.deltaTime * ParkingRotateSpeed);
                dockTime += Time.deltaTime;

                // Move
                currentAutoPilot.transform.position = Vector3.Lerp(currentAutoPilot.transform.position, parkingDockConnection, Time.deltaTime * ParkingMoveSpeed);

                if (dockTime > ParkingTimeout)
                {
                    LogDebug("Parking timed out!");
                    SetDockState(DockRecallState.Stuck);
                    yield break;
                }

                yield return null;
            }
            currentAutoPilot.DockingComplete();
            LogDebug("Docked state set: Parking Complete!");
        }

        private void CreateSphere(Vector3 spherePosition, float radius, Color color)
        {
            Transform parent = gameObject.transform;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(gameObject.transform, false);
            sphere.transform.position = spherePosition;

            Vector3 inverseScale = new Vector3(
                1f / parent.lossyScale.x,
                1f / parent.lossyScale.y,
                1f / parent.lossyScale.z
            );

            sphere.transform.localScale = inverseScale * radius;
            sphere.GetComponent<Renderer>().material.color = color;
        }
    }
}