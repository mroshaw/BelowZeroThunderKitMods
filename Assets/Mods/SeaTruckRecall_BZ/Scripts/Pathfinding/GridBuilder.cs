using UnityEngine;

namespace DaftAppleGames.SeatruckRecall_BZ.Navigation
{
    internal class GridBuilder : MonoBehaviour
    {
        [SerializeField] private float maxRange = 100.0f;
        [SerializeField] private float navGridCellSize = 10f;
        [SerializeField] private int navGridCellExtends = 5;
        [SerializeField] private LayerMask navGridIgnoreLayerMask;
        [SerializeField] private bool navGridDebug = true;
        [SerializeField] private Transform navGridDebugContainer;
        
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
        internal void RefreshNavGrid()
        {
            float distanceBetweenCells = maxRange /  navGridCellExtends;
            StartCoroutine(_navGrid.GenerateNavGridAsync(transform.position, navGridCellSize, navGridCellExtends, distanceBetweenCells, navGridIgnoreLayerMask,
                null, navGridDebug, navGridDebugContainer));
        }
        
        /// <summary>
        /// Pass the grid status change up the chain
        /// </summary>
        /// <param name="gridStatus"></param>
        private void GridStatusChangedHandler(GenerateStatus gridStatus)
        {
            OnGridStatusChanged.Invoke(gridStatus);
        }
    }
}