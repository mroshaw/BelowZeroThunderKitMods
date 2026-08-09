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

        private const int YieldIterationCount = 256;
        
        private bool IsBusy => _gridStatus == GenerateStatus.Generating || _pathStatus == GenerateStatus.Generating;
        internal bool IsPathingReady => _gridStatus == GenerateStatus.Success && _pathStatus == GenerateStatus.Success;
        internal bool HasPathingFailed => _gridStatus == GenerateStatus.Failed || _pathStatus == GenerateStatus.Failed;
        internal bool IsGridReady => _gridStatus == GenerateStatus.Success;

        internal readonly GridStatusChangedEvent OnGridStatusChanged = new GridStatusChangedEvent();
        internal readonly PathingStatusChangedEvent OnPathingStatusChanged = new PathingStatusChangedEvent();

        // Cached RayCastHits for collider checks
        private readonly Collider[] _colliderHitCache = new Collider[100];
        private readonly RaycastHit[] _hitCache =  new RaycastHit[10];
        private Vector3 _gridOrigin;
        private float _cellSize;
        private int _operationVersion;

        internal NavGrid()
        {
            SetGridStatus(GenerateStatus.Idle);
            SetPathingStatus(GenerateStatus.Idle);
        }

        /// <summary>
        /// Cancels a path calculation that is yielding across frames.
        /// </summary>
        internal void CancelPathGeneration()
        {
            _operationVersion++;
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

        internal IEnumerator GenerateNavGridAsync(Vector3 sourcePosition, float range, float distanceBetweenCells,
            float vehicleClearance, LayerMask colliderLayerMask,
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
            
            int numCellExtents =  (int) (Math.Ceiling(range / distanceBetweenCells)) / 2;
            int cellsInRow = (numCellExtents * 2) + 1;
            
            ModDebugLog.LogDebug($"Range: {range}");
            ModDebugLog.LogDebug($"Distance between cells: {distanceBetweenCells}");
            ModDebugLog.LogDebug($"Number of cell extents: {numCellExtents}");
            ModDebugLog.LogDebug($"NavGrid dimensions: x:{cellsInRow}, y:{cellsInRow}, z:{cellsInRow}. Total cells: {Math.Pow(cellsInRow,3)}");

            _navGrid = new NavCell[cellsInRow, cellsInRow, cellsInRow];
            _cellSize = distanceBetweenCells;
            _gridOrigin = sourcePosition - Vector3.one * (numCellExtents * distanceBetweenCells);
            int operationVersion = ++_operationVersion;
            Vector3 overlapHalfExtents = Vector3.one * Mathf.Max(distanceBetweenCells * 0.45f, vehicleClearance);

            int iterations = 0;
            
            for (int x = 0; x < cellsInRow; x++)
            {
                for (int y = 0; y < cellsInRow; y++)
                {
                    for (int z = 0; z < cellsInRow; z++)
                    {
                        if (operationVersion != _operationVersion)
                        {
                            yield break;
                        }
                        iterations++;
                        Vector3 cellPosition = _gridOrigin + new Vector3(x * distanceBetweenCells,
                            y * distanceBetweenCells, z * distanceBetweenCells);

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
                        int numColliderHits = Physics.OverlapBoxNonAlloc(cellPosition, overlapHalfExtents,
                            _colliderHitCache, Quaternion.identity, colliderLayerMask, QueryTriggerInteraction.Ignore);

                        // Check if the cell contains any colliders
                        if (isTraversable)
                        {
                            isTraversable = !(HasValidColliders(numColliderHits, _colliderHitCache));
                        }

                        int cellXIndex = x;
                        int cellYIndex = y;
                        int cellZIndex = z;

                        string cellName = $"(X:{cellXIndex}, Y:{cellYIndex}, Z:{cellZIndex})";

                        _navGrid[cellXIndex, cellYIndex, cellZIndex] = new NavCell
                        {
                            Index = new Vector3Int(cellXIndex, cellYIndex, cellZIndex),
                            Position = cellPosition,
                            IsTraversable = isTraversable,
                            Name = cellName
                        };

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
                    if (iterations % YieldIterationCount == 0)
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

            float genTime = Time.time;
            ModDebugLog.LogDebug($"Started Path Generation: {genTime}");
            SetPathingStatus(GenerateStatus.Generating);

            NavCell startCell;
            NavCell targetCell;
            if (!TryFindClosestWalkableCell(startPos, out startCell) ||
                !TryFindClosestWalkableCell(endPos, out targetCell))
            {
                ModDebugLog.LogDebug("Path start or destination is outside the navigation grid.");
                SetPathingStatus(GenerateStatus.Failed);
                pathCompleteAction?.Invoke(GenerateStatus.Failed, null);
                yield break;
            }

            int operationVersion = ++_operationVersion;
            NavPriorityQueue<Vector3Int> openQueue = new NavPriorityQueue<Vector3Int>();
            HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
            Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            Dictionary<Vector3Int, float> gScore = new Dictionary<Vector3Int, float>();

            gScore[startCell.Index] = 0.0f;
            openQueue.Enqueue(startCell.Index, Heuristic(startCell.Position, targetCell.Position));

            int iterations = 0;
            while (openQueue.Count > 0)
            {
                if (operationVersion != _operationVersion)
                {
                    yield break;
                }

                iterations++;
                Vector3Int currentIndex = openQueue.Dequeue();
                if (closedSet.Contains(currentIndex))
                {
                    continue;
                }
                NavCell current = GetCell(currentIndex);

                if (currentIndex == targetCell.Index)
                {
                    NavPath fullPath = ReconstructPath(cameFrom, currentIndex);
                    NavPath navPath = SimplifyPath(fullPath);

                    if (debug)
                    {
                        UpdatePathVisualisers(null, navPath);
                    }

                    SetPathingStatus(GenerateStatus.Success);
                    pathCompleteAction?.Invoke(GenerateStatus.Success, navPath);
                    ModDebugLog.LogDebug($"Finished Path Generation: {Time.time}. Time taken: {Time.time - genTime}.");
                    ModDebugLog.LogDebug($"Number of iterations: {iterations}");
                    ModDebugLog.LogDebug($"Path cells reduced from {fullPath.Count} to {navPath.Count}");
                    yield break;
                }

                closedSet.Add(currentIndex);
                for (int xOffset = -1; xOffset <= 1; xOffset++)
                {
                    for (int yOffset = -1; yOffset <= 1; yOffset++)
                    {
                        for (int zOffset = -1; zOffset <= 1; zOffset++)
                        {
                            if (xOffset == 0 && yOffset == 0 && zOffset == 0)
                            {
                                continue;
                            }

                            Vector3Int neighborIndex = currentIndex + new Vector3Int(xOffset, yOffset, zOffset);
                            if (!IsInBounds(neighborIndex) || closedSet.Contains(neighborIndex))
                            {
                                continue;
                            }

                            NavCell neighbor = GetCell(neighborIndex);
                            if (!neighbor.IsTraversable ||
                                !CanTraverseDiagonal(currentIndex, xOffset, yOffset, zOffset))
                            {
                                continue;
                            }

                            float tentativeGScore = gScore[currentIndex] +
                                                    Vector3.Distance(current.Position, neighbor.Position);
                            float existingGScore;
                            if (gScore.TryGetValue(neighborIndex, out existingGScore) &&
                                tentativeGScore >= existingGScore)
                            {
                                continue;
                            }

                            cameFrom[neighborIndex] = currentIndex;
                            gScore[neighborIndex] = tentativeGScore;
                            openQueue.Enqueue(neighborIndex,
                                tentativeGScore + Heuristic(neighbor.Position, targetCell.Position));
                        }
                    }
                }

                if (iterations % YieldIterationCount == 0)
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

        private bool TryFindClosestWalkableCell(Vector3 position, out NavCell closest)
        {
            closest = default(NavCell);
            Vector3Int positionIndex = PositionToIndex(position);
            if (!IsInBounds(positionIndex))
            {
                return false;
            }

            float minDistance = float.MaxValue;
            bool foundWalkable = false;

            foreach (NavCell cell in _navGrid)
            {
                if (!cell.IsTraversable || string.IsNullOrEmpty(cell.Name))
                {
                    continue; // Skip blocked cells
                }

                float dist = (position - cell.Position).sqrMagnitude;
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = cell;
                    foundWalkable = true;
                }
            }

            if (foundWalkable)
            {
                return true;
            }

            ModDebugLog.LogDebug("Couldn't find closest walkable cell!");
            return false;
        }


        private static float Heuristic(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        private bool CanTraverseDiagonal(Vector3Int currentIndex, int xOffset, int yOffset, int zOffset)
        {
            if (xOffset != 0 && !GetCell(currentIndex + new Vector3Int(xOffset, 0, 0)).IsTraversable)
            {
                return false;
            }
            if (yOffset != 0 && !GetCell(currentIndex + new Vector3Int(0, yOffset, 0)).IsTraversable)
            {
                return false;
            }
            if (zOffset != 0 && !GetCell(currentIndex + new Vector3Int(0, 0, zOffset)).IsTraversable)
            {
                return false;
            }
            return true;
        }

        private NavPath SimplifyPath(NavPath fullPath)
        {
            if (fullPath.Count <= 1)
            {
                return fullPath;
            }

            NavPath simplifiedPath = new NavPath();
            int anchorIndex = 0;
            while (anchorIndex < fullPath.Count - 1)
            {
                int nextIndex = fullPath.Count - 1;
                while (nextIndex > anchorIndex + 1 &&
                       !HasGridLineOfSight(fullPath[anchorIndex], fullPath[nextIndex]))
                {
                    nextIndex--;
                }
                simplifiedPath.Add(fullPath[nextIndex]);
                anchorIndex = nextIndex;
            }
            return simplifiedPath;
        }

        private bool HasGridLineOfSight(NavCell start, NavCell end)
        {
            float distance = Vector3.Distance(start.Position, end.Position);
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(distance / (_cellSize * 0.5f)));
            Vector3Int previousIndex = start.Index;
            for (int sample = 1; sample < sampleCount; sample++)
            {
                Vector3 samplePosition = Vector3.Lerp(start.Position, end.Position, sample / (float)sampleCount);
                Vector3Int sampleIndex = PositionToIndex(samplePosition);
                if (!IsInBounds(sampleIndex) || !GetCell(sampleIndex).IsTraversable)
                {
                    return false;
                }

                Vector3Int offset = sampleIndex - previousIndex;
                if (offset != Vector3Int.zero &&
                    !CanTraverseDiagonal(previousIndex, offset.x, offset.y, offset.z))
                {
                    return false;
                }
                previousIndex = sampleIndex;
            }
            return true;
        }

        private NavPath ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int currentIndex)
        {
            NavPath path = new NavPath { GetCell(currentIndex) };
            Vector3Int previousIndex;
            while (cameFrom.TryGetValue(currentIndex, out previousIndex))
            {
                currentIndex = previousIndex;
                path.Add(GetCell(currentIndex));
            }
            path.Reverse();
            return path;
        }

        private Vector3Int PositionToIndex(Vector3 position)
        {
            Vector3 localPosition = (position - _gridOrigin) / _cellSize;
            return new Vector3Int(Mathf.RoundToInt(localPosition.x), Mathf.RoundToInt(localPosition.y),
                Mathf.RoundToInt(localPosition.z));
        }

        private bool IsInBounds(Vector3Int index)
        {
            return index.x >= 0 && index.x < _navGrid.GetLength(0) &&
                   index.y >= 0 && index.y < _navGrid.GetLength(1) &&
                   index.z >= 0 && index.z < _navGrid.GetLength(2);
        }

        private NavCell GetCell(Vector3Int index)
        {
            return _navGrid[index.x, index.y, index.z];
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
