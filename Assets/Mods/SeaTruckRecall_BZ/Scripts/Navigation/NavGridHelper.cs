using System;
using UnityEngine;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.Navigation
{
    internal class NavGridHelper : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float maxRange = 100.0f;
        [SerializeField] private int navGridCellExtends = 5;
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
        
        /// <summary>
        /// Refresh the internal grid
        /// </summary>
        internal void RefreshNavGrid(Action<GenerateStatus> gridCompleteCallBack)
        {
#if !UNITY_EDITOR
            navGridDebug = SeaTruckDockRecallPlugin.ConfigFile.EnableNavGridDebug;
#endif
            StartCoroutine(_navGrid.GenerateNavGridAsync(transform.position, navGridCellExtends, maxRange, navGridIncludeLayerMask,
                gridCompleteCallBack, navGridDebug, navGridDebugContainer, visualiserPrefab));
        }

        /// <summary>
        /// Generates a path in the current grid and returns it via the callback
        /// </summary>
        internal void GenerateNavPath(Vector3 startPos, Vector3 endPos, Action<GenerateStatus, NavPath> pathGenCompleteCallBack)
        {
#if !UNITY_EDITOR
            pathDebug = SeaTruckDockRecallPlugin.ConfigFile.EnableNavGridDebug;
#endif
            StartCoroutine(_navGrid.GeneratePathAsync(startPos, endPos, pathGenCompleteCallBack, pathDebug, navGridDebugContainer));
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