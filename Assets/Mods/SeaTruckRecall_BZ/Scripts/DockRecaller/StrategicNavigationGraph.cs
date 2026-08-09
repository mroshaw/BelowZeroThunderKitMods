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

        internal bool ConnectionsBidirectional => connectionsBidirectional;
        internal IReadOnlyList<Node> Nodes => nodes;

        /// <summary>
        /// Describes a strategic navigation point and its outgoing connections.
        /// </summary>
        [Serializable]
        public sealed class Node
        {
            [SerializeField] private string nodeName = "Navigation Point";
            [SerializeField] private Vector3 position;
            [SerializeField] private List<int> connections = new List<int>();

            internal string NodeName => nodeName;
            internal Vector3 Position => position;
            internal IReadOnlyList<int> Connections => connections;
        }
    }
}
