using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DaftAppleGames.SeatruckRecall_BZ.Navigation
{
    internal class PathFinder : MonoBehaviour
    {
        [SerializeField] private bool debugPath;
        [SerializeField] private Transform debugContainer;
        [SerializeField] private Transform targetTransformOverride;

        internal WaypointsStatusChangedEvent OnWaypointStatusChanged = new WaypointsStatusChangedEvent();
        
        // private NavGrid _navGrid;
        
        private GenerateStatus _waypointStatus;
        private List<Waypoint> Waypoints { get; set; }
        
        internal class WaypointsStatusChangedEvent : UnityEvent<GenerateStatus>
        {
        }
        
        private void Start()
        {
            SetWaypointsStatus(GenerateStatus.Idle);
        }

        private void PathingStatusChangedHandler(GenerateStatus pathStatus)
        {
        }
        
        private void SetWaypointsStatus(GenerateStatus newStatus)
        {
            if (_waypointStatus == newStatus)
            {
                return;
            }
            _waypointStatus = newStatus;
            OnWaypointStatusChanged.Invoke(newStatus);
        }

        internal void SetPath(NavGrid navGrid, Vector3 startPosition, Vector3 targetPosition)
        {
            StartCoroutine(SetPathAsync(navGrid, startPosition, targetPosition, debugPath));
        }
        
        private  IEnumerator SetPathAsync(NavGrid navGrid, Vector3 startPosition, Vector3 targetPosition, bool debug = true)
        {
            yield return navGrid.GeneratePathAsync(startPosition, targetPosition, GeneratePathCompleteHandler, debug,
                debugContainer);
        }

        private void GeneratePathCompleteHandler(GenerateStatus pathStatus, NavPath navPath)
        {
            SetWaypointsFromPath(navPath, debugPath);
        }
        
        /// <summary>
        /// Try to establish a path from source to target, return as a list of Waypoints
        /// </summary>
        private void SetWaypointsFromPath(NavPath navPath, bool debug = false)
        {
            Waypoints = new List<Waypoint>();
            int curWaypoint = 1;
            foreach (NavCell navCell in navPath)
            {
                Waypoints.Add(CreateWaypointFromNavCell(navCell, curWaypoint));
                curWaypoint++;
            }
        }

        private Waypoint CreateWaypointFromNavCell(NavCell navCell, int waypointIndex)
        {
            Waypoint newWaypoint = new Waypoint(navCell.Position, Quaternion.identity, false, $"Waypoint: {waypointIndex}");
            return newWaypoint;
        }
    }
}