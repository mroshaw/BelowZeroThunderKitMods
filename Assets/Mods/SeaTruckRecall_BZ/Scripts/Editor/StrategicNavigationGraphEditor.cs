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
            int nodeCount = graph.NodeCount;
            int connectionCount = graph.StoredConnectionCount;

            EditorGUILayout.LabelField("Baked Graph Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Nodes", nodeCount.ToString("N0"));
            EditorGUILayout.LabelField("Stored Connections", connectionCount.ToString("N0"));
            EditorGUILayout.LabelField("Bidirectional", graph.ConnectionsBidirectional ? "Yes" : "No");
            showGraphInScene = EditorGUILayout.Toggle("Show Scene Preview", showGraphInScene);
            if (nodeCount > MaximumVisualizedNodes || connectionCount > MaximumVisualizedConnections)
            {
                EditorGUILayout.HelpBox(
                    "The Scene preview is capped to protect Editor responsiveness. The complete graph remains baked.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
            if (nodeCount <= 200)
            {
                DrawDefaultInspector();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Raw node fields are hidden for large baked graphs to protect Inspector responsiveness.",
                    MessageType.None);
            }
        }

        private void OnSceneGUI()
        {
            if (!showGraphInScene)
            {
                return;
            }

            StrategicNavigationGraph graph = (StrategicNavigationGraph)target;
            Handles.color = new Color(0.1f, 0.85f, 1.0f, 0.45f);
            int visualizedConnections = 0;
            int nodeLimit = Mathf.Min(graph.NodeCount, MaximumVisualizedNodes);
            for (int nodeIndex = 0; nodeIndex < nodeLimit; nodeIndex++)
            {
                int connectionCount = graph.GetConnectionCount(nodeIndex);
                for (int connectionIndex = 0; connectionIndex < connectionCount; connectionIndex++)
                {
                    int connection = graph.GetConnectedNode(nodeIndex, connectionIndex);
                    if (connection < 0 || connection >= nodeLimit || connection < nodeIndex)
                    {
                        continue;
                    }
                    Handles.DrawLine(graph.GetNodePosition(nodeIndex), graph.GetNodePosition(connection));
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
