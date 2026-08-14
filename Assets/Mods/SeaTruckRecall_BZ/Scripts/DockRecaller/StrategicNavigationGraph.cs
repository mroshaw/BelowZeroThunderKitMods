using System.Collections.Generic;

using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    /// <summary>
    /// Stores a flattened network of long-range navigation points.
    /// </summary>
    [CreateAssetMenu(fileName = "StrategicNavigationGraph", menuName = "Daft Apple Games/SeaTruck Recall/Strategic Navigation Graph")]
    public sealed class StrategicNavigationGraph : ScriptableObject
    {
        [SerializeField] private bool connectionsBidirectional;
        [SerializeField] private Vector3[] nodePositions = new Vector3[0];
        [SerializeField] private int[] connectionOffsets = new int[0];
        [SerializeField] private int[] connectionCounts = new int[0];
        [SerializeField] private int[] connectedNodeIndices = new int[0];

        public bool ConnectionsBidirectional => connectionsBidirectional;
        public int NodeCount => nodePositions.Length;
        public int StoredConnectionCount => connectedNodeIndices.Length;

        /// <summary>
        /// Returns the world-space position of a baked node.
        /// </summary>
        public Vector3 GetNodePosition(int nodeIndex) => nodePositions[nodeIndex];

        /// <summary>
        /// Returns the number of outgoing connections stored for a baked node.
        /// </summary>
        public int GetConnectionCount(int nodeIndex) => connectionCounts[nodeIndex];

        /// <summary>
        /// Returns one connected node index from a baked node's flattened connection range.
        /// </summary>
        public int GetConnectedNode(int nodeIndex, int connectionIndex) =>
            connectedNodeIndices[connectionOffsets[nodeIndex] + connectionIndex];

        /// <summary>
        /// Replaces the graph contents with newly baked navigation data.
        /// </summary>
        public void ReplaceBakedData(List<Node> bakedNodes, bool bidirectional)
        {
            int nodeCount = bakedNodes == null ? 0 : bakedNodes.Count;
            nodePositions = new Vector3[nodeCount];
            connectionOffsets = new int[nodeCount];
            connectionCounts = new int[nodeCount];
            connectionsBidirectional = bidirectional;

            int totalConnections = 0;
            if (bakedNodes != null)
            {
                foreach (Node node in bakedNodes)
                {
                    totalConnections += node.Connections.Count;
                }
            }
            connectedNodeIndices = new int[totalConnections];

            int connectionOffset = 0;
            for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
            {
                Node node = bakedNodes[nodeIndex];
                nodePositions[nodeIndex] = node.Position;
                connectionOffsets[nodeIndex] = connectionOffset;
                connectionCounts[nodeIndex] = node.Connections.Count;
                foreach (int connectedNode in node.Connections)
                {
                    connectedNodeIndices[connectionOffset++] = connectedNode;
                }
            }
        }

        /// <summary>
        /// Describes a strategic navigation point and its outgoing connections.
        /// </summary>
        /// <summary>
        /// Holds temporary graph data while an Editor baker constructs the flattened asset.
        /// </summary>
        public sealed class Node
        {
            public Vector3 Position { get; }
            public IReadOnlyList<int> Connections { get; }

            /// <summary>
            /// Creates a strategic graph node from baked world-space data.
            /// </summary>
            public Node(string name, Vector3 worldPosition, List<int> connectedNodes)
            {
                Position = worldPosition;
                Connections = connectedNodes ?? new List<int>();
            }
        }
    }
}
