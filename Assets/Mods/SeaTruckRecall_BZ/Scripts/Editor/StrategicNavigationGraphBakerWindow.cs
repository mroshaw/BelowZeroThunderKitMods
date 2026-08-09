using System.Collections.Generic;

using DaftAppleGames.SeaTruckRecall_BZ.DockRecaller;
using UnityEditor;
using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Editor
{
    /// <summary>
    /// Bakes a sparse strategic navigation graph from loaded collider geometry.
    /// </summary>
    public sealed class StrategicNavigationGraphBakerWindow : EditorWindow
    {
        private const int QueryBufferSize = 256;
        private const int MaximumSamples = 250000;

        private readonly Collider[] overlapBuffer = new Collider[QueryBufferSize];
        private readonly RaycastHit[] castBuffer = new RaycastHit[QueryBufferSize];
        private readonly List<Collider> eligibleColliders = new List<Collider>();

        private StrategicNavigationGraph targetGraph;
        private Transform terrainRoot;
        private Bounds bakeBounds = new Bounds(new Vector3(0.0f, -250.0f, 0.0f),
            new Vector3(3000.0f, 500.0f, 3000.0f));
        private LayerMask obstacleLayers = -5;
        private float nodeSpacing = 75.0f;
        private float clearanceRadius = 8.0f;
        private bool requireStaticColliders;
        private bool keepLargestConnectedRegion = true;
        private string resultSummary = "No graph has been baked in this session.";

        /// <summary>
        /// Opens the strategic terrain graph baker.
        /// </summary>
        [MenuItem("Tools/Daft Apple Games/SeaTruck Recall/Loaded Collider Graph Baker (Validation)")]
        public static void ShowWindow()
        {
            StrategicNavigationGraphBakerWindow window = GetWindow<StrategicNavigationGraphBakerWindow>();
            window.titleContent = new GUIContent("SeaTruck Graph Baker");
            window.minSize = new Vector2(440.0f, 470.0f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Loaded Collider Validation Graph", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Validation baker: samples collider geometry currently loaded in the active Unity scene. " +
                "Use the Compiled Terrain Graph Baker for the production whole-map graph.",
                MessageType.Info);

            targetGraph = (StrategicNavigationGraph)EditorGUILayout.ObjectField("Target Graph", targetGraph,
                typeof(StrategicNavigationGraph), false);
            if (GUILayout.Button("Create New Graph Asset"))
            {
                CreateGraphAsset();
            }

            EditorGUILayout.Space();
            terrainRoot = (Transform)EditorGUILayout.ObjectField("Terrain Root (Optional)", terrainRoot,
                typeof(Transform), true);
            requireStaticColliders = EditorGUILayout.Toggle("Require Static Colliders", requireStaticColliders);
            obstacleLayers = DrawLayerMaskField("Obstacle Layers", obstacleLayers);
            bakeBounds = EditorGUILayout.BoundsField("World Bounds", bakeBounds);
            nodeSpacing = EditorGUILayout.FloatField("Node Spacing", nodeSpacing);
            clearanceRadius = EditorGUILayout.FloatField("SeaTruck Clearance", clearanceRadius);
            keepLargestConnectedRegion = EditorGUILayout.Toggle("Keep Largest Water Region",
                keepLargestConnectedRegion);

            int sampleCount = CalculateSampleCount();
            EditorGUILayout.LabelField("Candidate Samples", sampleCount.ToString("N0"));
            EditorGUILayout.HelpBox(
                "Smaller spacing produces better strategic coverage but increases bake time, asset size, " +
                "and route-search cost. Local obstacle avoidance still handles detail between these nodes.",
                MessageType.None);

            EditorGUI.BeginDisabledGroup(targetGraph == null);
            if (GUILayout.Button("Bake Loaded Terrain Graph", GUILayout.Height(34.0f)))
            {
                BakeGraph();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(resultSummary, MessageType.None);
        }

        private void CreateGraphAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Strategic Navigation Graph",
                "StrategicNavigationGraph", "asset", "Choose where to save the baked graph asset.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            StrategicNavigationGraph newGraph = CreateInstance<StrategicNavigationGraph>();
            AssetDatabase.CreateAsset(newGraph, path);
            AssetDatabase.SaveAssets();
            targetGraph = newGraph;
            Selection.activeObject = newGraph;
        }

        private void BakeGraph()
        {
            string validationMessage;
            if (!ValidateSettings(out validationMessage))
            {
                EditorUtility.DisplayDialog("Cannot Bake Strategic Graph", validationMessage, "OK");
                return;
            }

            CollectEligibleColliders();
            if (eligibleColliders.Count == 0)
            {
                EditorUtility.DisplayDialog("No Terrain Colliders",
                    "No eligible colliders were found in the loaded scene. Check the terrain root, layer mask, " +
                    "static-collider option, and ensure the required terrain chunks are loaded.", "OK");
                return;
            }

            Physics.SyncTransforms();
            List<StrategicNavigationGraph.Node> bakedNodes;
            int blockedSamples;
            int saturatedQueries;
            int discardedNodes;
            bool completed = TryBakeNodes(out bakedNodes, out blockedSamples, out saturatedQueries,
                out discardedNodes);
            EditorUtility.ClearProgressBar();
            if (!completed)
            {
                resultSummary = "Bake cancelled. The existing graph asset was not changed.";
                Repaint();
                return;
            }
            if (bakedNodes.Count == 0)
            {
                resultSummary = "Bake produced no connected navigation nodes. The existing graph was not changed.";
                EditorUtility.DisplayDialog("Empty Strategic Graph", resultSummary, "OK");
                Repaint();
                return;
            }

            Undo.RecordObject(targetGraph, "Bake strategic terrain graph");
            targetGraph.ReplaceBakedData(bakedNodes, false);
            EditorUtility.SetDirty(targetGraph);
            AssetDatabase.SaveAssets();
            resultSummary = $"Baked {bakedNodes.Count:N0} nodes from {eligibleColliders.Count:N0} colliders; " +
                            $"{blockedSamples:N0} samples were obstructed. " +
                            $"{discardedNodes:N0} disconnected nodes were discarded. " +
                            $"{saturatedQueries:N0} physics queries exceeded the {QueryBufferSize}-hit buffer.";
            Selection.activeObject = targetGraph;
            Repaint();
        }

        private bool TryBakeNodes(out List<StrategicNavigationGraph.Node> bakedNodes, out int blockedSamples,
            out int saturatedQueries, out int discardedNodes)
        {
            bakedNodes = new List<StrategicNavigationGraph.Node>();
            blockedSamples = 0;
            saturatedQueries = 0;
            discardedNodes = 0;
            Dictionary<Vector3Int, int> nodeIndices = new Dictionary<Vector3Int, int>();
            List<Vector3> nodePositions = new List<Vector3>();

            Vector3Int dimensions = CalculateGridDimensions();
            Vector3 origin = CalculateGridOrigin(dimensions);
            int processedSamples = 0;
            int totalSamples = dimensions.x * dimensions.y * dimensions.z;
            for (int z = 0; z < dimensions.z; z++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    for (int x = 0; x < dimensions.x; x++)
                    {
                        if ((processedSamples & 255) == 0 && EditorUtility.DisplayCancelableProgressBar(
                                "Baking SeaTruck strategic graph", "Sampling navigable space...",
                                (float)processedSamples / totalSamples))
                        {
                            return false;
                        }

                        Vector3 position = origin + new Vector3(x * nodeSpacing, y * nodeSpacing, z * nodeSpacing);
                        bool saturated;
                        if (IsPositionBlocked(position, out saturated))
                        {
                            blockedSamples++;
                        }
                        else
                        {
                            Vector3Int gridIndex = new Vector3Int(x, y, z);
                            nodeIndices.Add(gridIndex, nodePositions.Count);
                            nodePositions.Add(position);
                        }
                        if (saturated)
                        {
                            saturatedQueries++;
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
                if ((processedNodes & 127) == 0 && EditorUtility.DisplayCancelableProgressBar(
                        "Baking SeaTruck strategic graph", "Checking node connections...",
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

                            bool saturated;
                            if (!IsConnectionBlocked(nodePositions[nodeEntry.Value], nodePositions[neighbourNode],
                                    out saturated))
                            {
                                connections[nodeEntry.Value].Add(neighbourNode);
                                connections[neighbourNode].Add(nodeEntry.Value);
                            }
                            if (saturated)
                            {
                                saturatedQueries++;
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
                int newIndex = remappedIndices[index];
                bakedNodes.Add(new StrategicNavigationGraph.Node($"Terrain Node {newIndex}", nodePositions[index],
                    remappedConnections));
            }
        }

        private static void RetainLargestConnectedRegion(List<int>[] connections, bool[] retainedNodes)
        {
            bool[] visited = new bool[connections.Length];
            List<int> largestRegion = new List<int>();
            Queue<int> frontier = new Queue<int>();
            List<int> currentRegion = new List<int>();
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

        private bool IsPositionBlocked(Vector3 position, out bool saturated)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(position, clearanceRadius, overlapBuffer, obstacleLayers,
                QueryTriggerInteraction.Ignore);
            saturated = hitCount == overlapBuffer.Length;
            for (int index = 0; index < hitCount; index++)
            {
                if (IsEligibleCollider(overlapBuffer[index]))
                {
                    return true;
                }
            }
            return saturated;
        }

        private bool IsConnectionBlocked(Vector3 start, Vector3 end, out bool saturated)
        {
            Vector3 offset = end - start;
            float distance = offset.magnitude;
            int hitCount = Physics.SphereCastNonAlloc(start, clearanceRadius, offset.normalized, castBuffer, distance,
                obstacleLayers, QueryTriggerInteraction.Ignore);
            saturated = hitCount == castBuffer.Length;
            for (int index = 0; index < hitCount; index++)
            {
                if (IsEligibleCollider(castBuffer[index].collider))
                {
                    return true;
                }
            }
            return saturated;
        }

        private void CollectEligibleColliders()
        {
            eligibleColliders.Clear();
            Collider[] sceneColliders = FindObjectsOfType<Collider>();
            foreach (Collider sceneCollider in sceneColliders)
            {
                if (IsEligibleCollider(sceneCollider) && sceneCollider.bounds.Intersects(bakeBounds))
                {
                    eligibleColliders.Add(sceneCollider);
                }
            }
        }

        private bool IsEligibleCollider(Collider candidate)
        {
            if (!candidate || !candidate.enabled || !candidate.gameObject.activeInHierarchy || candidate.isTrigger)
            {
                return false;
            }
            if ((obstacleLayers.value & (1 << candidate.gameObject.layer)) == 0)
            {
                return false;
            }
            if (requireStaticColliders && !candidate.gameObject.isStatic)
            {
                return false;
            }
            return !terrainRoot || candidate.transform == terrainRoot || candidate.transform.IsChildOf(terrainRoot);
        }

        private bool ValidateSettings(out string validationMessage)
        {
            if (!targetGraph)
            {
                validationMessage = "Select or create a target graph asset.";
                return false;
            }
            if (nodeSpacing <= 0.0f || clearanceRadius <= 0.0f)
            {
                validationMessage = "Node spacing and SeaTruck clearance must both be greater than zero.";
                return false;
            }
            if (bakeBounds.size.x <= 0.0f || bakeBounds.size.y <= 0.0f || bakeBounds.size.z <= 0.0f)
            {
                validationMessage = "Every world-bounds dimension must be greater than zero.";
                return false;
            }

            int sampleCount = CalculateSampleCount();
            if (sampleCount > MaximumSamples)
            {
                validationMessage = $"This bake would test {sampleCount:N0} positions. Increase node spacing or " +
                                    $"reduce the bounds to stay below the {MaximumSamples:N0}-sample safety limit.";
                return false;
            }
            validationMessage = string.Empty;
            return true;
        }

        private int CalculateSampleCount()
        {
            Vector3Int dimensions = CalculateGridDimensions();
            long count = (long)dimensions.x * dimensions.y * dimensions.z;
            return count > int.MaxValue ? int.MaxValue : (int)count;
        }

        private Vector3Int CalculateGridDimensions()
        {
            float safeSpacing = Mathf.Max(nodeSpacing, 0.01f);
            return new Vector3Int(Mathf.FloorToInt(bakeBounds.size.x / safeSpacing) + 1,
                Mathf.FloorToInt(bakeBounds.size.y / safeSpacing) + 1,
                Mathf.FloorToInt(bakeBounds.size.z / safeSpacing) + 1);
        }

        private Vector3 CalculateGridOrigin(Vector3Int dimensions)
        {
            Vector3 sampledSize = new Vector3((dimensions.x - 1) * nodeSpacing,
                (dimensions.y - 1) * nodeSpacing, (dimensions.z - 1) * nodeSpacing);
            return bakeBounds.center - sampledSize * 0.5f;
        }

        private static LayerMask DrawLayerMaskField(string label, LayerMask layerMask)
        {
            string[] layerNames = UnityEditorInternal.InternalEditorUtility.layers;
            int compactMask = 0;
            for (int index = 0; index < layerNames.Length; index++)
            {
                int layer = LayerMask.NameToLayer(layerNames[index]);
                if ((layerMask.value & (1 << layer)) != 0)
                {
                    compactMask |= 1 << index;
                }
            }

            compactMask = EditorGUILayout.MaskField(label, compactMask, layerNames);
            int expandedMask = 0;
            for (int index = 0; index < layerNames.Length; index++)
            {
                if ((compactMask & (1 << index)) != 0)
                {
                    expandedMask |= 1 << LayerMask.NameToLayer(layerNames[index]);
                }
            }
            layerMask.value = expandedMask;
            return layerMask;
        }
    }
}
