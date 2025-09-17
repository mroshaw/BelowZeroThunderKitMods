using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaftAppleGames.SeatruckRecall_BZ.Navigation
{
    internal class NavGridHelper : MonoBehaviour
    {
        [SerializeField] private float maxRange = 100.0f;
        [SerializeField] private int navGridCellExtends = 5;
        [SerializeField] private LayerMask navGridIncludeLayerMask;
        [SerializeField] private bool navGridDebug = true;
        [SerializeField] private Transform navGridDebugContainer;
        [SerializeField] private bool pathDebug = true;
        [SerializeField] private Transform pathDebugContainer;
        
        private NavGrid _navGrid;
        internal NavGrid NavGrid => _navGrid;
        
        internal NavGrid.GridStatusChangedEvent OnGridStatusChanged = new NavGrid.GridStatusChangedEvent();

        private void Awake()
        {
            _navGrid = new NavGrid();
            _navGrid.OnGridStatusChanged.AddListener(GridStatusChangedHandler);
        }
        
        /// <summary>
        /// Refresh the internal grid
        /// </summary>
        internal void RefreshNavGrid(Action<GenerateStatus> gridCompleteCallBack)
        {
            float distanceBetweenCells = maxRange /  navGridCellExtends;
            StartCoroutine(_navGrid.GenerateNavGridAsync(transform.position, navGridCellExtends, distanceBetweenCells, navGridIncludeLayerMask,
                gridCompleteCallBack, navGridDebug, navGridDebugContainer));
        }

        /// <summary>
        /// Generates a path in the current grid and returns it via the callback
        /// </summary>
        internal void GenerateNavPath(Vector3 startPos, Vector3 endPos, Action<GenerateStatus, NavPath> pathGenCompleteCallBack)
        {
            StartCoroutine(_navGrid.GeneratePathAsync(startPos, endPos, pathGenCompleteCallBack, pathDebug, pathDebugContainer));
        }
        
        /// <summary>
        /// Pass the grid status change up the chain
        /// </summary>
        private void GridStatusChangedHandler(GenerateStatus gridStatus)
        {
            OnGridStatusChanged.Invoke(gridStatus);
        }

        /// <summary>
        /// Combine two sets of waypoints
        /// </summary>
        internal static List<Waypoint> CombineWaypoints(List<Waypoint> waypoints1, List<Waypoint> waypoints2)
        {
            List<Waypoint> combinedWaypoints = new List<Waypoint>();
            combinedWaypoints.AddRange(waypoints1);
            combinedWaypoints.AddRange(waypoints2);
            return combinedWaypoints;
        }
        
    }
}