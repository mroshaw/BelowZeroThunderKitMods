using System;
using System.Collections.Generic;

using DaftAppleGames.SeaTruckRecall_BZ.DockRecaller;
using UnityEditor;
using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Editor
{
    /// <summary>
    /// Builds strategic graph data from supplied terrain-clearance queries.
    /// </summary>
    internal sealed class StrategicGraphBuilder
    {
        internal const int MaximumSamples = 250000;

        private readonly Bounds bounds;
        private readonly float nodeSpacing;
        private readonly bool keepLargestConnectedRegion;

        /// <summary>
        /// Creates a strategic graph builder for a bounded, regularly spaced lattice.
        /// </summary>
        internal StrategicGraphBuilder(Bounds graphBounds, float spacing, bool keepLargestRegion)
        {
            bounds = graphBounds;
            nodeSpacing = spacing;
            keepLargestConnectedRegion = keepLargestRegion;
        }

        internal int SampleCount
        {
            get
            {
                Vector3Int dimensions = CalculateDimensions();
                long count = (long)dimensions.x * dimensions.y * dimensions.z;
                return count > int.MaxValue ? int.MaxValue : (int)count;
            }
        }

        /// <summary>
        /// Builds connected navigation nodes using the supplied clearance predicates.
        /// </summary>
        internal bool TryBuild(Func<Vector3, bool> isPositionClear,
            Func<Vector3, Vector3, bool> isConnectionClear,
            out List<StrategicNavigationGraph.Node> bakedNodes, out int blockedSamples,
            out int blockedConnections, out int discardedNodes)
        {
            bakedNodes = new List<StrategicNavigationGraph.Node>();
            blockedSamples = 0;
            blockedConnections = 0;
            discardedNodes = 0;
            Dictionary<Vector3Int, int> nodeIndices = new Dictionary<Vector3Int, int>();
            List<Vector3> nodePositions = new List<Vector3>();
            Vector3Int dimensions = CalculateDimensions();
            Vector3 origin = CalculateOrigin(dimensions);
            int totalSamples = dimensions.x * dimensions.y * dimensions.z;
            int processedSamples = 0;

            for (int z = 0; z < dimensions.z; z++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    for (int x = 0; x < dimensions.x; x++)
                    {
                        if ((processedSamples & 127) == 0 && EditorUtility.DisplayCancelableProgressBar(
                                "Baking Below Zero terrain graph", "Sampling compiled terrain octrees...",
                                (float)processedSamples / totalSamples))
                        {
                            return false;
                        }

                        Vector3 position = origin + new Vector3(x * nodeSpacing, y * nodeSpacing,
                            z * nodeSpacing);
                        if (isPositionClear(position))
                        {
                            Vector3Int gridIndex = new Vector3Int(x, y, z);
                            nodeIndices.Add(gridIndex, nodePositions.Count);
                            nodePositions.Add(position);
                        }
                        else
                        {
                            blockedSamples++;
                        }
                        processedSamples++;
                    }
                }
            }

            List<int>[] connections = new List<int>[nodePositions.Count];
            for (int index = 0; index < connections.Length; index++)
            {
                connections[index] = new List<int>(26);
            }

            int processedNodes = 0;
            foreach (KeyValuePair<Vector3Int, int> nodeEntry in nodeIndices)
            {
                if ((processedNodes & 63) == 0 && EditorUtility.DisplayCancelableProgressBar(
                        "Baking Below Zero terrain graph", "Testing swept strategic connections...",
                        nodePositions.Count == 0 ? 1.0f : (float)processedNodes / nodePositions.Count))
                {
                    return false;
                }

                for (int zOffset = -1; zOffset <= 1; zOffset++)
                {
                    for (int yOffset = -1; yOffset <= 1; yOffset++)
                    {
                        for (int xOffset = -1; xOffset <= 1; xOffset++)
                        {
                            if (xOffset == 0 && yOffset == 0 && zOffset == 0)
                            {
                                continue;
                            }
                            Vector3Int neighbourIndex = nodeEntry.Key +
                                                        new Vector3Int(xOffset, yOffset, zOffset);
                            int neighbourNode;
                            if (!nodeIndices.TryGetValue(neighbourIndex, out neighbourNode) ||
                                neighbourNode <= nodeEntry.Value)
                            {
                                continue;
                            }

                            if (isConnectionClear(nodePositions[nodeEntry.Value], nodePositions[neighbourNode]))
                            {
                                connections[nodeEntry.Value].Add(neighbourNode);
                                connections[neighbourNode].Add(nodeEntry.Value);
                            }
                            else
                            {
                                blockedConnections++;
                            }
                        }
                    }
                }
                processedNodes++;
            }

            BuildBakedNodes(nodePositions, connections, bakedNodes, out discardedNodes);
            return true;
        }

        private void BuildBakedNodes(List<Vector3> nodePositions, List<int>[] connections,
            List<StrategicNavigationGraph.Node> bakedNodes, out int discardedNodes)
        {
            bool[] retainedNodes = new bool[nodePositions.Count];
            if (keepLargestConnectedRegion)
            {
                RetainLargestConnectedRegion(connections, retainedNodes);
            }
            else
            {
                for (int index = 0; index < retainedNodes.Length; index++)
                {
                    retainedNodes[index] = true;
                }
            }

            int[] remappedIndices = new int[nodePositions.Count];
            int retainedCount = 0;
            for (int index = 0; index < nodePositions.Count; index++)
            {
                remappedIndices[index] = -1;
                if (retainedNodes[index])
                {
                    remappedIndices[index] = retainedCount;
                    retainedCount++;
                }
            }
            discardedNodes = nodePositions.Count - retainedCount;

            for (int index = 0; index < nodePositions.Count; index++)
            {
                if (!retainedNodes[index])
                {
                    continue;
                }
                List<int> remappedConnections = new List<int>(connections[index].Count);
                foreach (int connection in connections[index])
                {
                    if (retainedNodes[connection])
                    {
                        remappedConnections.Add(remappedIndices[connection]);
                    }
                }
                bakedNodes.Add(new StrategicNavigationGraph.Node(string.Empty, nodePositions[index],
                    remappedConnections));
            }
        }

        private static void RetainLargestConnectedRegion(List<int>[] connections, bool[] retainedNodes)
        {
            bool[] visited = new bool[connections.Length];
            List<int> largestRegion = new List<int>();
            List<int> currentRegion = new List<int>();
            Queue<int> frontier = new Queue<int>();
            for (int startIndex = 0; startIndex < connections.Length; startIndex++)
            {
                if (visited[startIndex])
                {
                    continue;
                }
                currentRegion.Clear();
                frontier.Enqueue(startIndex);
                visited[startIndex] = true;
                while (frontier.Count > 0)
                {
                    int current = frontier.Dequeue();
                    currentRegion.Add(current);
                    foreach (int connection in connections[current])
                    {
                        if (visited[connection])
                        {
                            continue;
                        }
                        visited[connection] = true;
                        frontier.Enqueue(connection);
                    }
                }
                if (currentRegion.Count > largestRegion.Count)
                {
                    largestRegion.Clear();
                    largestRegion.AddRange(currentRegion);
                }
            }
            foreach (int retainedNode in largestRegion)
            {
                retainedNodes[retainedNode] = true;
            }
        }

        private Vector3Int CalculateDimensions()
        {
            float safeSpacing = Mathf.Max(nodeSpacing, 0.01f);
            return new Vector3Int(Mathf.FloorToInt(bounds.size.x / safeSpacing) + 1,
                Mathf.FloorToInt(bounds.size.y / safeSpacing) + 1,
                Mathf.FloorToInt(bounds.size.z / safeSpacing) + 1);
        }

        private Vector3 CalculateOrigin(Vector3Int dimensions)
        {
            Vector3 sampledSize = new Vector3((dimensions.x - 1) * nodeSpacing,
                (dimensions.y - 1) * nodeSpacing, (dimensions.z - 1) * nodeSpacing);
            return bounds.center - sampledSize * 0.5f;
        }
    }
}
