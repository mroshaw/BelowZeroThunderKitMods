using System;
using System.Collections;
using UnityEngine;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    internal class NavGridHelper : MonoBehaviour
    {
        [Header("Settings")] [SerializeField]
        internal Transform gridCenterPosition;
        [SerializeField] internal float maxRange = 100.0f;
        [SerializeField] internal float localPlanningRadius = 60.0f;
        [SerializeField] internal float distanceBetweenCells = 5.0f;
        [SerializeField] internal float vehicleClearance = 3.0f;
        [SerializeField] private LayerMask navGridIncludeLayerMask;
        
        [Header("Debug")]
        [SerializeField] private bool navGridDebug = true;
        [SerializeField] private bool pathDebug = true;
        [SerializeField] private Transform navGridDebugContainer;
        [SerializeField] private CellVisualiser visualiserPrefab;
        
        [Header("Events")]
        [SerializeField] internal NavGrid.GridStatusChangedEvent onGridStatusChanged = new NavGrid.GridStatusChangedEvent();
        [SerializeField] internal NavGrid.PathingStatusChangedEvent onPathingStatusChanged = new NavGrid.PathingStatusChangedEvent();
        
        private NavGrid _navGrid;
        internal NavGrid NavGrid => _navGrid;
        internal float RecallRange => maxRange;
        internal float LocalPlanningRadius => localPlanningRadius;
        internal float LocalPlanningDistance => Mathf.Max(distanceBetweenCells,
            localPlanningRadius - Mathf.Max(distanceBetweenCells * 2.0f, vehicleClearance * 2.0f));
        
        internal bool NavGridDebug =>
#if UNITY_EDITOR
            navGridDebug;
#else
            ConfigFile.EnableNavGridDebug;
#endif

        internal Transform NavGridDebugContainer => navGridDebugContainer;
        
        private void Awake()
        {
            _navGrid = new NavGrid();
            _navGrid.OnGridStatusChanged.AddListener(GridStatusChangedHandler);
            _navGrid.OnPathingStatusChanged.AddListener(PathingStatusChangedHandler);
        }
        
        internal IEnumerator RefreshNavGridAsync(Vector3 centerPosition, Action<GenerateStatus> gridCompleteCallBack)
        {
#if !UNITY_EDITOR
            navGridDebug = SeaTruckDockRecallPlugin.ConfigFile.EnableNavGridDebug;
            maxRange = SeaTruckDockRecallPlugin.ConfigFile.MaximumRange;
            localPlanningRadius = SeaTruckDockRecallPlugin.ConfigFile.LocalPlanningRadius;
            distanceBetweenCells = SeaTruckDockRecallPlugin.ConfigFile.DistanceBetweenCells;
#endif
#if UNITY_EDITOR
            centerPosition = gridCenterPosition.position;
#endif
            
            yield return StartCoroutine(_navGrid.GenerateNavGridAsync(centerPosition, localPlanningRadius * 2.0f,
                distanceBetweenCells,
                vehicleClearance, navGridIncludeLayerMask, null,
                gridCompleteCallBack, navGridDebug, navGridDebugContainer, visualiserPrefab));
        }
        
        /// <summary>
        /// Refresh the internal grid
        /// </summary>
        internal void RefreshNavGrid(Vector3 centerPosition, Action<GenerateStatus> gridCompleteCallBack,
            GameObject ignoredEntity = null)
        {
#if !UNITY_EDITOR
            navGridDebug = SeaTruckDockRecallPlugin.ConfigFile.EnableNavGridDebug;
            maxRange = SeaTruckDockRecallPlugin.ConfigFile.MaximumRange;
            localPlanningRadius = SeaTruckDockRecallPlugin.ConfigFile.LocalPlanningRadius;
            distanceBetweenCells = SeaTruckDockRecallPlugin.ConfigFile.DistanceBetweenCells;
#endif
#if UNITY_EDITOR
            centerPosition = gridCenterPosition.position;
#endif
            StartCoroutine(_navGrid.GenerateNavGridAsync(centerPosition, localPlanningRadius * 2.0f,
                distanceBetweenCells,
                vehicleClearance, navGridIncludeLayerMask, ignoredEntity,
                gridCompleteCallBack, navGridDebug, navGridDebugContainer, visualiserPrefab));
        }

        /// <summary>
        /// Generates a path in the current grid and returns it via the callback
        /// </summary>
        internal void GenerateNavPath(Vector3 startPos, Vector3 endPos, Action<GenerateStatus, NavPath> pathGenCompleteCallBack)
        {
#if UNITY_EDITOR
            endPos = gridCenterPosition.position;
#endif
#if !UNITY_EDITOR
            pathDebug = SeaTruckDockRecallPlugin.ConfigFile.EnableNavGridDebug;
#endif
            StartCoroutine(_navGrid.GeneratePathAsync(startPos, endPos, pathGenCompleteCallBack, pathDebug, navGridDebugContainer));
        }

        /// <summary>
        /// Cancels the active path calculation, if one is in progress.
        /// </summary>
        internal void CancelPathGeneration()
        {
            _navGrid.CancelPathGeneration();
        }
        
        /// <summary>
        /// Pass the grid status change up the chain
        /// </summary>
        private void GridStatusChangedHandler(GenerateStatus gridStatus)
        {
            onGridStatusChanged?.Invoke(gridStatus);
        }

        /// <summary>
        /// Pass the pathing status change up the chain
        /// </summary>
        private void PathingStatusChangedHandler(GenerateStatus pathingStatus, NavPath navPath)
        {
            onPathingStatusChanged?.Invoke(pathingStatus, navPath);
        }
    }
}
