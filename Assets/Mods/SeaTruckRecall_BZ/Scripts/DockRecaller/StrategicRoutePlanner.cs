using System.Collections.Generic;

using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    /// <summary>
    /// Calculates routes across a sparse strategic navigation graph.
    /// </summary>
    internal static class StrategicRoutePlanner
    {
        /// <summary>
        /// Attempts to calculate a strategic route between two world positions.
        /// </summary>
        internal static bool TryCalculateRoute(StrategicNavigationGraph graph, Vector3 startPosition,
            Vector3 destination, List<Vector3> route, out string failureReason)
        {
            route.Clear();
            failureReason = string.Empty;
            if (!graph)
            {
                failureReason = "the graph asset reference is missing at runtime";
                return false;
            }
            if (graph.NodeCount == 0)
            {
                failureReason = $"graph '{graph.name}' contains no nodes";
                return false;
            }

            int startNode = FindNearestNode(graph, startPosition);
            int destinationNode = FindNearestNode(graph, destination);
            List<int>[] expandedAdjacency = graph.ConnectionsBidirectional
                ? BuildBidirectionalAdjacency(graph)
                : null;
            Dictionary<int, int> cameFrom = new Dictionary<int, int>();
            Dictionary<int, float> costs = new Dictionary<int, float>();
            NavPriorityQueue<int> frontier = new NavPriorityQueue<int>();
            frontier.Enqueue(startNode, 0.0f);
            costs[startNode] = 0.0f;

            bool foundDestination = false;
            int visitedNodeCount = 0;
            while (frontier.Count > 0)
            {
                int current = frontier.Dequeue();
                visitedNodeCount++;
                if (current == destinationNode)
                {
                    foundDestination = true;
                    break;
                }

                AddConnectedNodes(graph, expandedAdjacency, current, destinationNode, frontier, cameFrom, costs);
            }

            if (!foundDestination)
            {
                failureReason = $"graph '{graph.name}' could not connect nearest start node {startNode} " +
                                $"({graph.GetNodePosition(startNode)}, " +
                                $"{GetConnectionCount(graph, expandedAdjacency, startNode)} connections) " +
                                $"to nearest destination node {destinationNode} " +
                                $"({graph.GetNodePosition(destinationNode)}, " +
                                $"{GetConnectionCount(graph, expandedAdjacency, destinationNode)} connections); " +
                                $"visited {visitedNodeCount} " +
                                $"of {graph.NodeCount} nodes";
                return false;
            }

            List<int> reversedRoute = new List<int>();
            int routeNode = destinationNode;
            reversedRoute.Add(routeNode);
            while (routeNode != startNode)
            {
                if (!cameFrom.TryGetValue(routeNode, out routeNode))
                {
                    route.Clear();
                    failureReason = $"route reconstruction failed before reaching start node {startNode}";
                    return false;
                }
                reversedRoute.Add(routeNode);
            }

            for (int index = reversedRoute.Count - 1; index >= 0; index--)
            {
                route.Add(graph.GetNodePosition(reversedRoute[index]));
            }
            route.Add(destination);
            return true;
        }

        private static int FindNearestNode(StrategicNavigationGraph graph, Vector3 position)
        {
            int nearestIndex = 0;
            float nearestDistance = (graph.GetNodePosition(0) - position).sqrMagnitude;
            for (int index = 1; index < graph.NodeCount; index++)
            {
                float distance = (graph.GetNodePosition(index) - position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = index;
                }
            }
            return nearestIndex;
        }

        private static List<int>[] BuildBidirectionalAdjacency(StrategicNavigationGraph graph)
        {
            List<int>[] expandedAdjacency = new List<int>[graph.NodeCount];
            for (int index = 0; index < graph.NodeCount; index++)
            {
                expandedAdjacency[index] = new List<int>();
            }

            for (int index = 0; index < graph.NodeCount; index++)
            {
                int connectionCount = graph.GetConnectionCount(index);
                for (int connectionIndex = 0; connectionIndex < connectionCount; connectionIndex++)
                {
                    int connection = graph.GetConnectedNode(index, connectionIndex);
                    if (connection < 0 || connection >= graph.NodeCount || connection == index)
                    {
                        continue;
                    }
                    AddUnique(expandedAdjacency[index], connection);
                    AddUnique(expandedAdjacency[connection], index);
                }
            }
            return expandedAdjacency;
        }

        private static void AddConnectedNodes(StrategicNavigationGraph graph,
            List<int>[] expandedAdjacency, int current, int destination,
            NavPriorityQueue<int> frontier, Dictionary<int, int> cameFrom, Dictionary<int, float> costs)
        {
            if (expandedAdjacency != null)
            {
                foreach (int connectedNode in expandedAdjacency[current])
                {
                    TryAddNode(graph, current, connectedNode, destination, frontier, cameFrom, costs);
                }
                return;
            }

            int connectionCount = graph.GetConnectionCount(current);
            for (int connectionIndex = 0; connectionIndex < connectionCount; connectionIndex++)
            {
                int connectedNode = graph.GetConnectedNode(current, connectionIndex);
                TryAddNode(graph, current, connectedNode, destination, frontier, cameFrom, costs);
            }
        }

        private static int GetConnectionCount(StrategicNavigationGraph graph, List<int>[] expandedAdjacency,
            int nodeIndex)
        {
            return expandedAdjacency == null
                ? graph.GetConnectionCount(nodeIndex)
                : expandedAdjacency[nodeIndex].Count;
        }

        private static void TryAddNode(StrategicNavigationGraph graph, int current,
            int connectedNode, int destination, NavPriorityQueue<int> frontier, Dictionary<int, int> cameFrom,
            Dictionary<int, float> costs)
        {
            if (connectedNode < 0 || connectedNode >= graph.NodeCount || connectedNode == current)
            {
                return;
            }

            float newCost = costs[current] + Vector3.Distance(graph.GetNodePosition(current),
                graph.GetNodePosition(connectedNode));
            float existingCost;
            if (costs.TryGetValue(connectedNode, out existingCost) && newCost >= existingCost)
            {
                return;
            }

            costs[connectedNode] = newCost;
            cameFrom[connectedNode] = current;
            float heuristic = Vector3.Distance(graph.GetNodePosition(connectedNode),
                graph.GetNodePosition(destination));
            frontier.Enqueue(connectedNode, newCost + heuristic);
        }

        private static void AddUnique(List<int> connections, int nodeIndex)
        {
            foreach (int connection in connections)
            {
                if (connection == nodeIndex)
                {
                    return;
                }
            }
            connections.Add(nodeIndex);
        }
    }
}
