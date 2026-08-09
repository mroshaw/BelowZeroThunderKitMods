using System;
using System.Collections.Generic;

using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    /// <summary>
    /// Stores a sparse, authorable network of long-range navigation points.
    /// </summary>
    [CreateAssetMenu(fileName = "StrategicNavigationGraph", menuName = "Daft Apple Games/SeaTruck Recall/Strategic Navigation Graph")]
    public sealed class StrategicNavigationGraph : ScriptableObject
    {
        [SerializeField] private bool connectionsBidirectional = true;
        [SerializeField] private List<Node> nodes = new List<Node>();

        public bool ConnectionsBidirectional => connectionsBidirectional;
        public IReadOnlyList<Node> Nodes => nodes;

        /// <summary>
        /// Replaces the graph contents with newly baked navigation data.
        /// </summary>
        public void ReplaceBakedData(List<Node> bakedNodes, bool bidirectional)
        {
            nodes = bakedNodes ?? new List<Node>();
            connectionsBidirectional = bidirectional;
        }

        /// <summary>
        /// Describes a strategic navigation point and its outgoing connections.
        /// </summary>
        [Serializable]
        public sealed class Node
        {
            [SerializeField] private string nodeName = "Navigation Point";
            [SerializeField] private Vector3 position;
            [SerializeField] private List<int> connections = new List<int>();

            public string NodeName => nodeName;
            public Vector3 Position => position;
            public IReadOnlyList<int> Connections => connections;

            /// <summary>
            /// Creates a strategic graph node from baked world-space data.
            /// </summary>
            public Node(string name, Vector3 worldPosition, List<int> connectedNodes)
            {
                nodeName = name;
                position = worldPosition;
                connections = connectedNodes ?? new List<int>();
            }
        }
    }
}
