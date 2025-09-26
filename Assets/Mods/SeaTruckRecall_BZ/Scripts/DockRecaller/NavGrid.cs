using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;
using Object = UnityEngine.Object;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    internal enum GenerateStatus
    {
        Idle,
        Generating,
        Success,
        Failed
    }

    /// <summary>
    /// Implements a dynamic 3D grid of cubes eminating from a "Source" position to a "Target" position.
    /// The grid extends forward all the way to the target, and "numExtends" side-to-side and vertically.
    /// Grid "NavCells" have a position, the center of the cube, and a boolean that is true if any colliders
    /// are present within that cube.
    ///
    /// The GetPath method returns true and a path (list of NavCells) based on a simple A* pathfinding algorithm.
    /// </summary>
    internal class NavGrid
    {
        // Internal NavGrid 3D array
        private NavCell[,,] _navGrid;

        private GenerateStatus _gridStatus = GenerateStatus.Idle;
        private GenerateStatus _pathStatus = GenerateStatus.Idle;

        private const int YieldFrameCount = 5;
        
        private bool IsBusy => _gridStatus == GenerateStatus.Generating || _pathStatus == GenerateStatus.Generating;
        internal bool IsPathingReady => _gridStatus == GenerateStatus.Success && _pathStatus == GenerateStatus.Success;
        internal bool HasPathingFailed => _gridStatus == GenerateStatus.Failed || _pathStatus == GenerateStatus.Failed;
        internal bool IsGridReady => _gridStatus == GenerateStatus.Success;

        internal readonly GridStatusChangedEvent OnGridStatusChanged = new GridStatusChangedEvent();
        internal readonly PathingStatusChangedEvent OnPathingStatusChanged = new PathingStatusChangedEvent();

        // Cached RayCastHits for collider checks
        private readonly Collider[] _colliderHitCache = new Collider[100];
        private readonly RaycastHit[] _hitCache =  new RaycastHit[10];

        internal NavGrid()
        {
            SetGridStatus(GenerateStatus.Idle);
            SetPathingStatus(GenerateStatus.Idle);
        }

        private void SetGridStatus(GenerateStatus newStatus)
        {
            if (_gridStatus == newStatus)
            {
                return;
            }
            _gridStatus = newStatus;
            OnGridStatusChanged?.Invoke(newStatus);
        }

        private void SetPathingStatus(GenerateStatus newStatus, NavPath navPath = null)
        {
            if (_pathStatus == newStatus)
            {
                return;
            }
            _pathStatus = newStatus;
            OnPathingStatusChanged?.Invoke(newStatus, navPath);
        }

        // Only used in debugging. Keeps a list of cells so we can tweak the visualisers
        private Dictionary<NavCell, CellVisualiser> _debugCellVisualisers = new Dictionary<NavCell, CellVisualiser>();
        
        private Transform GetGridDebugContainer(Transform parentContainer)
        {
            Transform container = parentContainer.Find("DEBUG");
            return container;
        }

        private Transform ResetGridDebugContainer(Transform debugContainer)
        {
            return ResetDebugContainer(debugContainer, "GRID");
        }

        private Transform ResetDebugContainer(Transform parentContainer, string containerName)
        {
            Transform container = parentContainer.Find(containerName);
            if (container)
            {
                Object.Destroy(container.gameObject);
            }
            GameObject newContainer = new GameObject(containerName);
            newContainer.transform.SetParent(parentContainer, true);
            return newContainer.transform;
        }

        internal IEnumerator GenerateNavGridAsync(Vector3 sourcePosition, float range, float distanceBetweenCells, LayerMask colliderLayerMask,
            Action<GenerateStatus> gridCompleteAction = null,
           bool debug = false,  Transform debugContainer = null, CellVisualiser debugVisualiser = null)
        {
            if (IsBusy)
            {
                ModDebugLog.LogDebug("NavGrid is busy!");
                gridCompleteAction?.Invoke(GenerateStatus.Failed);
                yield break;
            }

            int totalCells = 0;
            int totalBlockedCells = 0;
            int totalClearCells = 0;

            _debugCellVisualisers = new Dictionary<NavCell, CellVisualiser>();
            Transform gridDebugContainer = debug ? ResetGridDebugContainer(debugContainer) : null;

            float genStartTime = Time.fixedTime;
            ModDebugLog.LogDebug($"Started Grid Generation: {genStartTime}");
            ModDebugLog.LogDebug($"Ocean Level is: {Ocean.GetOceanLevel()}");
            ModDebugLog.LogDebug($"Grid Center is: {sourcePosition}");
            SetGridStatus(GenerateStatus.Generating);
            
            Vector3 direction = (sourcePosition).normalized;

            int numCellExtents =  (int) (Math.Ceiling(range / distanceBetweenCells)) / 2;
            int cellsInRow = (numCellExtents * 2) + 1;
            
            ModDebugLog.LogDebug($"Range: {range}");
            ModDebugLog.LogDebug($"Distance between cells: {distanceBetweenCells}");
            ModDebugLog.LogDebug($"Number of cell extents: {numCellExtents}");
            ModDebugLog.LogDebug($"NavGrid dimensions: x:{cellsInRow}, y:{cellsInRow}, z:{cellsInRow}. Total cells: {Math.Pow(cellsInRow,3)}");

            _navGrid = new NavCell[cellsInRow, cellsInRow, cellsInRow];

            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, direction).normalized;

            int iterations = 0;
            
            for (int x = -numCellExtents; x < numCellExtents; x++)
            {
                for (int y = -numCellExtents; y <= numCellExtents; y++)
                {
                    for (int z = -numCellExtents; z <= numCellExtents; z++)
                    {
                        iterations++;
                        Vector3 cellPosition = sourcePosition + (direction * (x * distanceBetweenCells)) + (up * (y * distanceBetweenCells)) + (right * (z * distanceBetweenCells));

                        // If above sea level, mark as invalid
                        bool isTraversable = true;
#if !UNITY_EDITOR
                        isTraversable = !(cellPosition.y > Ocean.GetOceanLevel() - 2.0f);

                        // If below the terrain, mark as invalid
                        if (isTraversable)
                        {
                            isTraversable = Physics.RaycastNonAlloc(cellPosition, Vector3.down, _hitCache) != 0;
                        }
#endif
                        int numColliderHits = Physics.OverlapBoxNonAlloc(cellPosition, Vector3.one * (distanceBetweenCells * 0.5f), _colliderHitCache, Quaternion.identity, colliderLayerMask, QueryTriggerInteraction.Ignore);

                        // Check if the cell contains any colliders
                        if (isTraversable)
                        {
                            isTraversable = !(HasValidColliders(numColliderHits, _colliderHitCache));
                        }

                        int cellXIndex = x + numCellExtents;
                        int cellYIndex = y + numCellExtents;
                        int cellZIndex = z + numCellExtents;

                        string cellName = $"(X:{cellXIndex}, Y:{cellYIndex}, Z:{cellZIndex})";

                        _navGrid[cellXIndex, cellYIndex, cellZIndex] = new NavCell { Position = cellPosition, IsTraversable = isTraversable, Name = cellName };

                        totalCells++;

                        if (isTraversable)
                        {
                            totalClearCells++;
                        }
                        else
                        {
                            totalBlockedCells++;
                        }

                        if (debug)
                        {
                            GameObject newCellVisualiser = GameObject.Instantiate(debugVisualiser.gameObject, gridDebugContainer, true);
                            newCellVisualiser.name = $"Cell Visualiser: {cellName}";
                            newCellVisualiser.SetActive(true);
                            CellVisualiser cellVis = newCellVisualiser.GetComponent<CellVisualiser>();
                            cellVis.CreateOrUpdate(_navGrid[cellXIndex, cellYIndex, cellZIndex], CellType.NavCell, gridDebugContainer);
                            _debugCellVisualisers.Add(_navGrid[cellXIndex, cellYIndex, cellZIndex], cellVis);
                        }
                    }

                    // Yield every n frames for performance
                    if (iterations % YieldFrameCount == 0)
                    {
                        yield return null;
                    }
                }
            }

            float genEndTime = Time.fixedTime;
            ModDebugLog.LogDebug($"Finished Grid Generation: {Time.time}. Time taken: {genEndTime - genStartTime}");
            ModDebugLog.LogDebug($"Number of iterations: {iterations}");
            ModDebugLog.LogDebug($"Cells created: {totalCells}, Blocked cells: {totalBlockedCells}, Clear cells: {totalClearCells}");
            SetGridStatus(GenerateStatus.Success);
            gridCompleteAction?.Invoke(GenerateStatus.Success);
        }

        private static bool HasValidColliders(int numColliders, Collider[] allColliders)
        {
            for (int curColliderIndex = 0; curColliderIndex < numColliders; curColliderIndex++)
            {
                // LogDebug($"Found collider: {allColliders[curColliderIndex].name} on layer named: {LayerMask.LayerToName(allColliders[curColliderIndex].gameObject.layer)}");
                if (allColliders[curColliderIndex].gameObject.transform.parent && allColliders[curColliderIndex].gameObject.transform.parent.GetComponentInChildren<Creature>())
                {
                    // We want to ignore these
                    // ModDebugLog.LogDebug("NavGrid: found Creature collider, ignoring...");
                    continue;
                }

                return true;
            }
            return false;
        }

        internal IEnumerator GeneratePathAsync(Vector3 startPos, Vector3 endPos,
            Action<GenerateStatus, NavPath> pathCompleteAction = null,
            bool debug = false, Transform debugContainer = null)
        {

            if (IsBusy)
            {
                ModDebugLog.LogDebug("NavGrid is busy!");
                pathCompleteAction?.Invoke(GenerateStatus.Failed, null);
                yield break;
            }

            if (_gridStatus != GenerateStatus.Success)
            {
                ModDebugLog.LogDebug("NavGrid grid is not ready for pathing!");
                pathCompleteAction?.Invoke(GenerateStatus.Failed, null);
                yield break;
            }

            Transform pathDebugContainer = debug ? GetGridDebugContainer(debugContainer) : null;

            float genTime = Time.time;
            ModDebugLog.LogDebug($"Started Path Generation: {genTime}");
            SetPathingStatus(GenerateStatus.Generating);

            HashSet<NavCell> openSet = new HashSet<NavCell>();
            HashSet<NavCell> closedSet = new HashSet<NavCell>();
            Dictionary<NavCell, NavCell> cameFrom = new Dictionary<NavCell, NavCell>();
            Dictionary<NavCell, float> gScore = new Dictionary<NavCell, float>();
            Dictionary<NavCell, float> fScore = new Dictionary<NavCell, float>();

            NavCell startCell = FindClosestWalkableCell(startPos);
            NavCell targetCell = FindClosestWalkableCell(endPos);

            openSet.Add(startCell);
            gScore[startCell] = 0;
            fScore[startCell] = Heuristic(startCell.Position, targetCell.Position);

            int iterations = 0;
            while (openSet.Count > 0)
            {
                iterations++;
                NavCell current = GetLowestFScore(openSet, fScore);

                if (current.Position == targetCell.Position)
                {
                    // Reached our destination, so pathing is complete
                    NavPath navPath = ReconstructPath(cameFrom, current);

                    if (debug)
                    {
                        UpdatePathVisualisers(pathDebugContainer, navPath);
                    }

                    SetPathingStatus(GenerateStatus.Success);
                    pathCompleteAction?.Invoke(GenerateStatus.Success, navPath);
                    ModDebugLog.LogDebug($"Finished Path Generation: {Time.time}. Time taken: {Time.time - genTime}.");
                    ModDebugLog.LogDebug($"Number of iterations: {iterations}");
                    ModDebugLog.LogDebug($"Number of path cells: {navPath.Count}");
                    yield break;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (NavCell neighbor in GetNeighbors(_navGrid, current))
                {
                    if (closedSet.Contains(neighbor) || !neighbor.IsTraversable)
                    {
                        continue;
                    }

                    float tentativeGScore = gScore[current] + Vector3.Distance(current.Position, neighbor.Position);

                    if (!openSet.Contains(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor.Position, targetCell.Position);
                        openSet.Add(neighbor);
                    }
                }

                // Yield every n frames for performance
                if (iterations % YieldFrameCount == 0)
                {
                    yield return null;
                }
            }

            // No path found
            ModDebugLog.LogDebug($"Finished Path Generation: {Time.time}. Time taken: {Time.time - genTime}.");
            ModDebugLog.LogDebug($"Number of iterations: {iterations}");
            ModDebugLog.LogDebug("No path found!");

            SetPathingStatus(GenerateStatus.Failed);
            pathCompleteAction?.Invoke(GenerateStatus.Failed, null);
        }

        private void UpdatePathVisualisers(Transform pathDebugContainer, NavPath navPath)
        {
            for (int curCell = 0; curCell < navPath.Count; curCell++)
            {
                // Start
                if (curCell == 0)
                {
                    _debugCellVisualisers[navPath[curCell]].CreateOrUpdate(navPath[curCell], CellType.Start, null);
                }
                else if (curCell == navPath.Count - 1)
                {
                    _debugCellVisualisers[navPath[curCell]].CreateOrUpdate(navPath[curCell], CellType.End, null);
                }
                else
                {
                    _debugCellVisualisers[navPath[curCell]].CreateOrUpdate(navPath[curCell], CellType.Route, null);
                }
            }
        }

        private NavCell FindClosestWalkableCell(Vector3 position)
        {
            NavCell closest = _navGrid[0, 0, 0];
            float minDistance = float.MaxValue;
            bool foundWalkable = false;

            foreach (NavCell cell in _navGrid)
            {
                if (!cell.IsTraversable || string.IsNullOrEmpty(cell.Name))
                {
                    continue; // Skip blocked cells
                }

                float dist = Vector3.Distance(position, cell.Position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = cell;
                    foundWalkable = true;
                }
            }

            if (foundWalkable)
            {
                return closest;
            }

            ModDebugLog.LogDebug("Couldn't find closest walkable cell!");
            return _navGrid[0, 0, 0]; // Fallback to first cell if no walkable found
        }


        private static float Heuristic(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        private static NavCell GetLowestFScore(HashSet<NavCell> openSet, Dictionary<NavCell, float> fScore)
        {
            NavCell best = default;
            float minScore = float.MaxValue;

            foreach (var cell in openSet)
            {
                if (fScore[cell] < minScore)
                {
                    minScore = fScore[cell];
                    best = cell;
                }
            }

            return best;
        }

        private static List<NavCell> GetNeighbors(NavCell[,,] grid, NavCell cell)
        {
            List<NavCell> neighbors = new List<NavCell>();

            int gridX = grid.GetLength(0);
            int gridY = grid.GetLength(1);
            int gridZ = grid.GetLength(2);

            Vector3Int cellIndex = GetCellIndex(grid, cell);
            if (cellIndex == new Vector3Int(-1, -1, -1)) {
                return neighbors; // Cell not found in grid
}

            int[][] directions =
            {
                new[] { 1, 0, 0 }, new[] { -1, 0, 0 }, // Forward, Backward
                new[] { 0, 1, 0 }, new[] { 0, -1, 0 }, // Up, Down
                new[] { 0, 0, 1 }, new[] { 0, 0, -1 } // Left, Right
            };

            foreach (int[] dir in directions)
            {
                int newX = cellIndex.x + dir[0];
                int newY = cellIndex.y + dir[1];
                int newZ = cellIndex.z + dir[2];

                if (newX >= 0 && newX < gridX &&
                    newY >= 0 && newY < gridY &&
                    newZ >= 0 && newZ < gridZ)
                {
                    NavCell neighbor = grid[newX, newY, newZ];
                    if (neighbor.IsTraversable) // Only add walkable cells
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }

            return neighbors;
        }


        private static Vector3Int GetCellIndex(NavCell[,,] grid, NavCell cell)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    for (int z = 0; z < grid.GetLength(2); z++)
                    {
                        if (grid[x, y, z].Position == cell.Position)
                        {
                            return new Vector3Int(x, y, z);
                        }
                    }
                }
            }

            return new Vector3Int(-1, -1, -1); // Not found
        }

        private static NavPath ReconstructPath(Dictionary<NavCell, NavCell> cameFrom, NavCell current)
        {
            NavPath path = new NavPath();
            while (cameFrom.ContainsKey(current))
            {
                path.Add(current);
                current = cameFrom[current];
            }

            path.Reverse();
            return path;
        }
        
        /// <summary>
        /// Event to publish Grid generation status
        /// </summary>
        [Serializable]
        internal class GridStatusChangedEvent : UnityEvent<GenerateStatus>
        {
        }

        /// <summary>
        /// Event to publish Path generation status
        /// </summary>
        [Serializable]
        internal class PathingStatusChangedEvent : UnityEvent<GenerateStatus, NavPath>
        {
        }
    }
}