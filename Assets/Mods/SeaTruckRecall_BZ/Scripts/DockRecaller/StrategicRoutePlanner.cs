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
            Vector3 destination, List<Vector3> route)
        {
            route.Clear();
            if (!graph || graph.Nodes.Count == 0)
            {
                return false;
            }

            int startNode = FindNearestNode(graph.Nodes, startPosition);
            int destinationNode = FindNearestNode(graph.Nodes, destination);
            Dictionary<int, int> cameFrom = new Dictionary<int, int>();
            Dictionary<int, float> costs = new Dictionary<int, float>();
            NavPriorityQueue<int> frontier = new NavPriorityQueue<int>();
            frontier.Enqueue(startNode, 0.0f);
            costs[startNode] = 0.0f;

            bool foundDestination = false;
            while (frontier.Count > 0)
            {
                int current = frontier.Dequeue();
                if (current == destinationNode)
                {
                    foundDestination = true;
                    break;
                }

                AddConnectedNodes(graph, current, destinationNode, frontier, cameFrom, costs);
            }

            if (!foundDestination)
            {
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
                    return false;
                }
                reversedRoute.Add(routeNode);
            }

            for (int index = reversedRoute.Count - 1; index >= 0; index--)
            {
                route.Add(graph.Nodes[reversedRoute[index]].Position);
            }
            route.Add(destination);
            return true;
        }

        private static int FindNearestNode(IReadOnlyList<StrategicNavigationGraph.Node> nodes, Vector3 position)
        {
            int nearestIndex = 0;
            float nearestDistance = (nodes[0].Position - position).sqrMagnitude;
            for (int index = 1; index < nodes.Count; index++)
            {
                float distance = (nodes[index].Position - position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = index;
                }
            }
            return nearestIndex;
        }

        private static void AddConnectedNodes(StrategicNavigationGraph graph, int current, int destination,
            NavPriorityQueue<int> frontier, Dictionary<int, int> cameFrom, Dictionary<int, float> costs)
        {
            IReadOnlyList<StrategicNavigationGraph.Node> nodes = graph.Nodes;
            IReadOnlyList<int> connections = nodes[current].Connections;
            foreach (int connectedNode in connections)
            {
                TryAddNode(nodes, current, connectedNode, destination, frontier, cameFrom, costs);
            }

            if (!graph.ConnectionsBidirectional)
            {
                return;
            }

            for (int index = 0; index < nodes.Count; index++)
            {
                if (index == current || !ContainsConnection(nodes[index].Connections, current))
                {
                    continue;
                }
                TryAddNode(nodes, current, index, destination, frontier, cameFrom, costs);
            }
        }

        private static void TryAddNode(IReadOnlyList<StrategicNavigationGraph.Node> nodes, int current,
            int connectedNode, int destination, NavPriorityQueue<int> frontier, Dictionary<int, int> cameFrom,
            Dictionary<int, float> costs)
        {
            if (connectedNode < 0 || connectedNode >= nodes.Count || connectedNode == current)
            {
                return;
            }

            float newCost = costs[current] + Vector3.Distance(nodes[current].Position, nodes[connectedNode].Position);
            float existingCost;
            if (costs.TryGetValue(connectedNode, out existingCost) && newCost >= existingCost)
            {
                return;
            }

            costs[connectedNode] = newCost;
            cameFrom[connectedNode] = current;
            float heuristic = Vector3.Distance(nodes[connectedNode].Position, nodes[destination].Position);
            frontier.Enqueue(connectedNode, newCost + heuristic);
        }

        private static bool ContainsConnection(IReadOnlyList<int> connections, int nodeIndex)
        {
            foreach (int connection in connections)
            {
                if (connection == nodeIndex)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
