using System.Collections.Generic;

using DaftAppleGames.SeaTruckRecall_BZ.DockRecaller;
using UnityEditor;
using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Editor
{
    /// <summary>
    /// Provides graph statistics and a bounded Scene view preview.
    /// </summary>
    [CustomEditor(typeof(StrategicNavigationGraph))]
    public sealed class StrategicNavigationGraphEditor : UnityEditor.Editor
    {
        private const int MaximumVisualizedNodes = 2000;
        private const int MaximumVisualizedConnections = 5000;

        private bool showGraphInScene = true;

        /// <summary>
        /// Draws the strategic graph inspector and bake statistics.
        /// </summary>
        public override void OnInspectorGUI()
        {
            StrategicNavigationGraph graph = (StrategicNavigationGraph)target;
            IReadOnlyList<StrategicNavigationGraph.Node> nodes = graph.Nodes;
            int connectionCount = 0;
            foreach (StrategicNavigationGraph.Node node in nodes)
            {
                connectionCount += node.Connections.Count;
            }

            EditorGUILayout.LabelField("Baked Graph Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Nodes", nodes.Count.ToString("N0"));
            EditorGUILayout.LabelField("Stored Connections", connectionCount.ToString("N0"));
            EditorGUILayout.LabelField("Bidirectional", graph.ConnectionsBidirectional ? "Yes" : "No");
            showGraphInScene = EditorGUILayout.Toggle("Show Scene Preview", showGraphInScene);
            if (nodes.Count > MaximumVisualizedNodes || connectionCount > MaximumVisualizedConnections)
            {
                EditorGUILayout.HelpBox(
                    "The Scene preview is capped to protect Editor responsiveness. The complete graph remains baked.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }

        private void OnSceneGUI()
        {
            if (!showGraphInScene)
            {
                return;
            }

            StrategicNavigationGraph graph = (StrategicNavigationGraph)target;
            IReadOnlyList<StrategicNavigationGraph.Node> nodes = graph.Nodes;
            Handles.color = new Color(0.1f, 0.85f, 1.0f, 0.45f);
            int visualizedConnections = 0;
            int nodeLimit = Mathf.Min(nodes.Count, MaximumVisualizedNodes);
            for (int nodeIndex = 0; nodeIndex < nodeLimit; nodeIndex++)
            {
                IReadOnlyList<int> connections = nodes[nodeIndex].Connections;
                foreach (int connection in connections)
                {
                    if (connection < 0 || connection >= nodeLimit || connection < nodeIndex)
                    {
                        continue;
                    }
                    Handles.DrawLine(nodes[nodeIndex].Position, nodes[connection].Position);
                    visualizedConnections++;
                    if (visualizedConnections >= MaximumVisualizedConnections)
                    {
                        return;
                    }
                }
            }
        }
    }
}
