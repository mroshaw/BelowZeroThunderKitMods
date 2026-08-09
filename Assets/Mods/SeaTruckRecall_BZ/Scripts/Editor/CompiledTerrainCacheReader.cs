using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Editor
{
    /// <summary>
    /// Performs bounded clearance queries directly against Below Zero compact terrain octrees.
    /// </summary>
    internal sealed class CompiledTerrainCacheReader : IDisposable
    {
        private const int MaximumCachedBatches = 96;

        private readonly string compiledOctreesPath;
        private readonly Vector3 worldOrigin;
        private readonly Dictionary<Vector3Int, CompiledTerrainBatch> loadedBatches =
            new Dictionary<Vector3Int, CompiledTerrainBatch>();
        private readonly Dictionary<Vector3Int, LinkedListNode<Vector3Int>> cacheNodes =
            new Dictionary<Vector3Int, LinkedListNode<Vector3Int>>();
        private readonly LinkedList<Vector3Int> cacheOrder = new LinkedList<Vector3Int>();
        private readonly HashSet<Vector3Int> missingBatches = new HashSet<Vector3Int>();

        internal CompiledTerrainCacheMetadata Metadata { get; }
        internal int LoadedBatchCount { get; private set; }
        internal int MissingBatchCount => missingBatches.Count;

        /// <summary>
        /// Opens a compiled Below Zero terrain cache for spatial queries.
        /// </summary>
        internal CompiledTerrainCacheReader(string cacheRoot, Vector3 terrainWorldOrigin)
        {
            if (string.IsNullOrEmpty(cacheRoot))
            {
                throw new ArgumentException("A terrain cache root is required.", nameof(cacheRoot));
            }
            Metadata = CompiledTerrainCacheMetadata.Read(cacheRoot);
            compiledOctreesPath = Path.Combine(cacheRoot, "CompiledOctreesCache");
            if (!Directory.Exists(compiledOctreesPath))
            {
                throw new DirectoryNotFoundException(
                    $"Compiled octree directory was not found: '{compiledOctreesPath}'.");
            }
            worldOrigin = terrainWorldOrigin;
        }

        /// <summary>
        /// Returns true when a clearance volume around a world position contains no solid terrain.
        /// </summary>
        internal bool IsPositionClear(Vector3 worldPosition, float clearance)
        {
            Vector3Int minimumTree;
            Vector3Int maximumTree;
            Vector3 clearanceVector = Vector3.one * clearance;
            GetOverlappingTrees(worldPosition - clearanceVector, worldPosition + clearanceVector,
                out minimumTree, out maximumTree);
            for (int x = minimumTree.x; x <= maximumTree.x; x++)
            {
                for (int y = minimumTree.y; y <= maximumTree.y; y++)
                {
                    for (int z = minimumTree.z; z <= maximumTree.z; z++)
                    {
                        Vector3Int treeIndex = new Vector3Int(x, y, z);
                        byte[] treeData = GetTreeData(treeIndex);
                        if (treeData == null || treeData.Length == 0)
                        {
                            continue;
                        }
                        Vector3 treeMinimum = worldOrigin + Vector3.Scale((Vector3)treeIndex,
                            Vector3.one * Metadata.TreeSize);
                        if (SolidNodeIntersectsPoint(treeData, 0, treeMinimum, Metadata.TreeSize, worldPosition,
                                clearance))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true when a swept clearance volume between two positions contains no solid terrain.
        /// </summary>
        internal bool IsConnectionClear(Vector3 start, Vector3 end, float clearance)
        {
            Vector3 clearanceVector = Vector3.one * clearance;
            Vector3 queryMinimum = new Vector3(Math.Min(start.x, end.x), Math.Min(start.y, end.y),
                                       Math.Min(start.z, end.z)) - clearanceVector;
            Vector3 queryMaximum = new Vector3(Math.Max(start.x, end.x), Math.Max(start.y, end.y),
                                       Math.Max(start.z, end.z)) + clearanceVector;
            Vector3Int minimumTree;
            Vector3Int maximumTree;
            GetOverlappingTrees(queryMinimum, queryMaximum, out minimumTree, out maximumTree);
            for (int x = minimumTree.x; x <= maximumTree.x; x++)
            {
                for (int y = minimumTree.y; y <= maximumTree.y; y++)
                {
                    for (int z = minimumTree.z; z <= maximumTree.z; z++)
                    {
                        Vector3Int treeIndex = new Vector3Int(x, y, z);
                        byte[] treeData = GetTreeData(treeIndex);
                        if (treeData == null || treeData.Length == 0)
                        {
                            continue;
                        }
                        Vector3 treeMinimum = worldOrigin + Vector3.Scale((Vector3)treeIndex,
                            Vector3.one * Metadata.TreeSize);
                        if (SolidNodeIntersectsSegment(treeData, 0, treeMinimum, Metadata.TreeSize, start, end,
                                clearance))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Releases cached batch data.
        /// </summary>
        public void Dispose()
        {
            loadedBatches.Clear();
            cacheNodes.Clear();
            cacheOrder.Clear();
            missingBatches.Clear();
        }

        private byte[] GetTreeData(Vector3Int treeIndex)
        {
            Vector3Int batchIndex = new Vector3Int(treeIndex.x / Metadata.TreesPerBatch.x,
                treeIndex.y / Metadata.TreesPerBatch.y, treeIndex.z / Metadata.TreesPerBatch.z);
            CompiledTerrainBatch batch = GetBatch(batchIndex);
            if (batch == null)
            {
                return null;
            }
            Vector3Int localTree = new Vector3Int(treeIndex.x % Metadata.TreesPerBatch.x,
                treeIndex.y % Metadata.TreesPerBatch.y, treeIndex.z % Metadata.TreesPerBatch.z);
            return batch.GetTree(localTree);
        }

        private CompiledTerrainBatch GetBatch(Vector3Int batchIndex)
        {
            CompiledTerrainBatch batch;
            if (loadedBatches.TryGetValue(batchIndex, out batch))
            {
                TouchBatch(batchIndex);
                return batch;
            }
            if (missingBatches.Contains(batchIndex))
            {
                return null;
            }

            string fileName = $"compiled-batch-{batchIndex.x}-{batchIndex.y}-{batchIndex.z}.optoctrees";
            string batchPath = Path.Combine(compiledOctreesPath, fileName);
            if (!File.Exists(batchPath))
            {
                missingBatches.Add(batchIndex);
                return null;
            }

            batch = CompiledTerrainBatch.Load(batchPath, batchIndex, Metadata);
            LoadedBatchCount++;
            loadedBatches.Add(batchIndex, batch);
            LinkedListNode<Vector3Int> cacheNode = cacheOrder.AddFirst(batchIndex);
            cacheNodes.Add(batchIndex, cacheNode);
            TrimBatchCache();
            return batch;
        }

        private void TouchBatch(Vector3Int batchIndex)
        {
            LinkedListNode<Vector3Int> cacheNode = cacheNodes[batchIndex];
            cacheOrder.Remove(cacheNode);
            cacheOrder.AddFirst(cacheNode);
        }

        private void TrimBatchCache()
        {
            while (loadedBatches.Count > MaximumCachedBatches)
            {
                LinkedListNode<Vector3Int> oldestNode = cacheOrder.Last;
                Vector3Int oldestBatch = oldestNode.Value;
                cacheOrder.RemoveLast();
                cacheNodes.Remove(oldestBatch);
                loadedBatches.Remove(oldestBatch);
            }
        }

        private void GetOverlappingTrees(Vector3 worldMinimum, Vector3 worldMaximum,
            out Vector3Int minimumTree,
            out Vector3Int maximumTree)
        {
            Vector3 localMinimum = worldMinimum - worldOrigin;
            Vector3 localMaximum = worldMaximum - worldOrigin;
            minimumTree = new Vector3Int(
                ClampTreeIndex((int)Math.Floor(localMinimum.x / Metadata.TreeSize), Metadata.TreeCount.x),
                ClampTreeIndex((int)Math.Floor(localMinimum.y / Metadata.TreeSize), Metadata.TreeCount.y),
                ClampTreeIndex((int)Math.Floor(localMinimum.z / Metadata.TreeSize), Metadata.TreeCount.z));
            maximumTree = new Vector3Int(
                ClampTreeIndex((int)Math.Floor(localMaximum.x / Metadata.TreeSize), Metadata.TreeCount.x),
                ClampTreeIndex((int)Math.Floor(localMaximum.y / Metadata.TreeSize), Metadata.TreeCount.y),
                ClampTreeIndex((int)Math.Floor(localMaximum.z / Metadata.TreeSize), Metadata.TreeCount.z));
        }

        private static bool SolidNodeIntersectsPoint(byte[] treeData, int nodeId, Vector3 nodeMinimum,
            int nodeSize, Vector3 point, float clearance)
        {
            Vector3 expandedMinimum = nodeMinimum - Vector3.one * clearance;
            Vector3 expandedMaximum = nodeMinimum + Vector3.one * (nodeSize + clearance);
            if (!PointInsideBounds(point, expandedMinimum, expandedMaximum))
            {
                return false;
            }

            int firstChild = GetFirstChild(treeData, nodeId);
            if (firstChild == 0)
            {
                return IsSolid(treeData, nodeId);
            }

            int childSize = nodeSize / 2;
            for (int child = 0; child < 8; child++)
            {
                Vector3 childMinimum = nodeMinimum + new Vector3(
                    (child & 4) == 0 ? 0.0f : childSize,
                    (child & 2) == 0 ? 0.0f : childSize,
                    (child & 1) == 0 ? 0.0f : childSize);
                if (SolidNodeIntersectsPoint(treeData, firstChild + child, childMinimum, childSize, point,
                        clearance))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool SolidNodeIntersectsSegment(byte[] treeData, int nodeId, Vector3 nodeMinimum,
            int nodeSize, Vector3 start, Vector3 end, float clearance)
        {
            Vector3 expandedMinimum = nodeMinimum - Vector3.one * clearance;
            Vector3 expandedMaximum = nodeMinimum + Vector3.one * (nodeSize + clearance);
            if (!SegmentIntersectsBounds(start, end, expandedMinimum, expandedMaximum))
            {
                return false;
            }

            int firstChild = GetFirstChild(treeData, nodeId);
            if (firstChild == 0)
            {
                return IsSolid(treeData, nodeId);
            }

            int childSize = nodeSize / 2;
            for (int child = 0; child < 8; child++)
            {
                Vector3 childMinimum = nodeMinimum + new Vector3(
                    (child & 4) == 0 ? 0.0f : childSize,
                    (child & 2) == 0 ? 0.0f : childSize,
                    (child & 1) == 0 ? 0.0f : childSize);
                if (SolidNodeIntersectsSegment(treeData, firstChild + child, childMinimum, childSize, start, end,
                        clearance))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool SegmentIntersectsBounds(Vector3 start, Vector3 end, Vector3 boundsMinimum,
            Vector3 boundsMaximum)
        {
            Vector3 direction = end - start;
            float minimumTime = 0.0f;
            float maximumTime = 1.0f;
            return IntersectsAxis(start.x, direction.x, boundsMinimum.x, boundsMaximum.x, ref minimumTime,
                       ref maximumTime) &&
                   IntersectsAxis(start.y, direction.y, boundsMinimum.y, boundsMaximum.y, ref minimumTime,
                       ref maximumTime) &&
                   IntersectsAxis(start.z, direction.z, boundsMinimum.z, boundsMaximum.z, ref minimumTime,
                       ref maximumTime);
        }

        private static bool IntersectsAxis(float origin, float direction, float minimum, float maximum,
            ref float minimumTime, ref float maximumTime)
        {
            if (Math.Abs(direction) < 0.0001f)
            {
                return origin >= minimum && origin <= maximum;
            }
            float inverseDirection = 1.0f / direction;
            float firstTime = (minimum - origin) * inverseDirection;
            float secondTime = (maximum - origin) * inverseDirection;
            if (firstTime > secondTime)
            {
                float temporary = firstTime;
                firstTime = secondTime;
                secondTime = temporary;
            }
            minimumTime = Math.Max(minimumTime, firstTime);
            maximumTime = Math.Min(maximumTime, secondTime);
            return minimumTime <= maximumTime;
        }

        private static bool PointInsideBounds(Vector3 point, Vector3 minimum, Vector3 maximum)
        {
            return point.x >= minimum.x && point.x <= maximum.x &&
                   point.y >= minimum.y && point.y <= maximum.y &&
                   point.z >= minimum.z && point.z <= maximum.z;
        }

        private static int ClampTreeIndex(int index, int count)
        {
            if (index < 0)
            {
                return 0;
            }
            return index >= count ? count - 1 : index;
        }

        private static bool IsSolid(byte[] treeData, int nodeId)
        {
            int byteIndex = nodeId * 4;
            byte type = treeData[byteIndex];
            byte density = treeData[byteIndex + 1];
            return density == 0 ? type > 0 : density >= 126;
        }

        private static int GetFirstChild(byte[] treeData, int nodeId)
        {
            int byteIndex = nodeId * 4;
            return treeData[byteIndex + 2] | treeData[byteIndex + 3] << 8;
        }
    }
}
