using static DaftAppleGames.SeatruckRecall_BZ.SeaTruckDockRecallPlugin;
using System.Collections.Generic;
using DaftAppleGames.SeatruckRecall_BZ.AutoPilot;
using DaftAppleGames.SeatruckRecall_BZ.DockRecaller;
using TMPro;
using UnityEngine;

namespace DaftAppleGames.SeatruckRecall_BZ.Navigation
{
    public class TestUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text gridStatusText;
        [SerializeField] private TMP_Text pathingStatusText;
        [SerializeField] private TMP_Text waypointStatusText;
        [SerializeField] private TMP_Text autopilotStatusText;
        [SerializeField] private TMP_Text dockRecallStatusText;

        [SerializeField] TMP_InputField forceText;

        [SerializeField] private GameObject seaTruck;
        [SerializeField] private SeaTruckDockRecaller seaTruckDockRecaller;

        private List<Waypoint> _waypoints;
        private WaypointNavigation _navSystem;
        private SeaTruckAutoPilot _autoPilot;
        private PathFinder _pathFinder;
        
        private void OnEnable()
        {
            if (!_pathFinder)
            {
                _pathFinder = seaTruck.GetComponent<PathFinder>();
            }

            if (!_autoPilot)
            {
                _autoPilot = seaTruck.GetComponent<SeaTruckAutoPilot>();
            }

            seaTruckDockRecaller.OnDockingStateChanged.AddListener(DockingStateChangedHandler);
            _pathFinder.OnWaypointStatusChanged.AddListener(WaypointStatusChangedHandler);
            _autoPilot.OnAutopilotStateChanged.AddListener(AutoPilotStateChangedHandler);
        }

        private void OnDisable()
        {
            if (!_pathFinder)
            {
                _pathFinder = seaTruck.GetComponent<PathFinder>();
            }

            if (!_autoPilot)
            {
                _autoPilot = seaTruck.GetComponent<SeaTruckAutoPilot>();
            }

            _pathFinder.OnWaypointStatusChanged.RemoveListener(WaypointStatusChangedHandler);
            _autoPilot.OnAutopilotStateChanged.AddListener(AutoPilotStateChangedHandler);
            seaTruckDockRecaller.OnDockingStateChanged.RemoveListener(DockingStateChangedHandler);

        }

        private void Awake()
        {
            _autoPilot = seaTruck.GetComponent<SeaTruckAutoPilot>();
            AllAutoPilots.GetAllActiveAutoPilots();
        }
        
        public void StartNav()
        {
            // _navSystem.StartWaypointNavigation(_waypoints);
            // _autoPilot.BeginNavigation(_waypoints);
            seaTruckDockRecaller.RecallClosestSeatruck();
        }

        public void RefreshGrid()
        {
            seaTruckDockRecaller.GenerateNavGrid();
        }

        public void RefreshPath()
        {
            // _pathFinder.SetPath();
        }

        private void SetWaypoints(GenerateStatus waypointStatus, List<Waypoint> waypoints)
        {
            if (waypointStatus != GenerateStatus.Success)
            {
                LogError("Failed to generate waypoints!");
                return;
            }

            _waypoints = waypoints;
        }

        private void WaypointsReady(GenerateStatus waypointStatus, List<Waypoint> waypoints)
        {
            if (waypointStatus != GenerateStatus.Success)
            {
                LogError("Failed to generate waypoints!");
                return;
            }
            _navSystem.StartWaypointNavigation(waypoints);
        }
        private void GridStatusChangedHandler(GenerateStatus status)
        {
            gridStatusText.text = status.ToString();
        }

        private void PathingStatusChangedHandler(GenerateStatus status)
        {
            pathingStatusText.text = status.ToString();
        }

        private void WaypointStatusChangedHandler(GenerateStatus status)
        {
            waypointStatusText.text = status.ToString();
        }

        private void AutoPilotStateChangedHandler(AutoPilotState state)
        {
            autopilotStatusText.text = state.ToString();
        }

        private void DockingStateChangedHandler(DockRecallState state)
        {
            dockRecallStatusText.text = state.ToString();
        }

        public void ApplyForce()
        {
            float forceToApply = float.Parse(forceText.text);
            Rigidbody rigidBody = _navSystem.GetComponent<Rigidbody>();

            Vector3 forwardForce = rigidBody.transform.forward * forceToApply;

            rigidBody.AddForce(forwardForce);
        }

        public void ConfigureRigidBodies()
        {
            RigidbodyNavMovement rbNav = _navSystem as RigidbodyNavMovement;
            rbNav.ConfigureRigidBodies();
        }

        public void RestoreRigidBodies()
        {
            RigidbodyNavMovement rbNav = _navSystem as RigidbodyNavMovement;
            rbNav.RestoreRigidBodies();
        }
    }
}