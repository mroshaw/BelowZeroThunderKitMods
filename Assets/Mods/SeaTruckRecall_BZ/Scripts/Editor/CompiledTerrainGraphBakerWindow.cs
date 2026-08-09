using System;
using System.Collections.Generic;
using System.IO;

using DaftAppleGames.SeaTruckRecall_BZ.DockRecaller;
using UnityEditor;
using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Editor
{
    /// <summary>
    /// Bakes a full strategic graph directly from Below Zero compiled terrain caches.
    /// </summary>
    public sealed class CompiledTerrainGraphBakerWindow : EditorWindow
    {
        private StrategicNavigationGraph targetGraph;
        private string cacheRoot = string.Empty;
        private Vector3 terrainWorldOrigin = new Vector3(-2048.0f, -3040.0f, -2048.0f);
        private Bounds bakeBounds = new Bounds(new Vector3(0.0f, -500.0f, 0.0f),
            new Vector3(3800.0f, 1000.0f, 3800.0f));
        private float nodeSpacing = 75.0f;
        private float clearanceRadius = 8.0f;
        private bool keepLargestConnectedRegion = true;
        private string cacheSummary = "Cache has not been validated in this session.";
        private string resultSummary = "No compiled-terrain graph has been baked in this session.";

        /// <summary>
        /// Opens the production compiled-terrain graph baker.
        /// </summary>
        [MenuItem("Tools/Daft Apple Games/SeaTruck Recall/Compiled Terrain Graph Baker")]
        public static void ShowWindow()
        {
            CompiledTerrainGraphBakerWindow window = GetWindow<CompiledTerrainGraphBakerWindow>();
            window.titleContent = new GUIContent("BZ Terrain Graph");
            window.minSize = new Vector2(520.0f, 580.0f);
            window.Show();
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(cacheRoot))
            {
                cacheRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "GameFiles~",
                    "ExportedProject", "Assets", "StreamingAssets", "SNUnmanagedData", "Expansion"));
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Compiled Below Zero Terrain Graph", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Production baker: reads Expansion/index.txt and CompiledOctreesCache directly. " +
                "It does not require a loaded terrain scene and does not instantiate game terrain.",
                MessageType.Info);

            targetGraph = (StrategicNavigationGraph)EditorGUILayout.ObjectField("Target Graph", targetGraph,
                typeof(StrategicNavigationGraph), false);
            if (GUILayout.Button("Create New Graph Asset"))
            {
                CreateGraphAsset();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Terrain Cache", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            cacheRoot = EditorGUILayout.TextField("Expansion Cache Root", cacheRoot);
            if (GUILayout.Button("Browse", GUILayout.Width(70.0f)))
            {
                BrowseForCacheRoot();
            }
            EditorGUILayout.EndHorizontal();
            terrainWorldOrigin = EditorGUILayout.Vector3Field("Voxeland World Origin", terrainWorldOrigin);
            if (GUILayout.Button("Validate Terrain Cache"))
            {
                ValidateCacheAndDisplayResult();
            }
            EditorGUILayout.HelpBox(cacheSummary, MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Strategic Graph", EditorStyles.boldLabel);
            bakeBounds = EditorGUILayout.BoundsField("Navigation Bounds", bakeBounds);
            nodeSpacing = EditorGUILayout.FloatField("Node Spacing", nodeSpacing);
            clearanceRadius = EditorGUILayout.FloatField("SeaTruck Clearance", clearanceRadius);
            keepLargestConnectedRegion = EditorGUILayout.Toggle("Keep Largest Water Region",
                keepLargestConnectedRegion);
            StrategicGraphBuilder previewBuilder = new StrategicGraphBuilder(bakeBounds,
                Mathf.Max(nodeSpacing, 0.01f), keepLargestConnectedRegion);
            EditorGUILayout.LabelField("Candidate Samples", previewBuilder.SampleCount.ToString("N0"));
            EditorGUILayout.HelpBox(
                "The default origin is taken from Below Zero's Main scene. Navigation bounds intentionally stop " +
                "inside the 4096m world edges and at sea level to avoid routing through unused outer volume.",
                MessageType.None);

            EditorGUI.BeginDisabledGroup(targetGraph == null);
            if (GUILayout.Button("Bake Compiled Terrain Graph", GUILayout.Height(36.0f)))
            {
                BakeGraph();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(resultSummary, MessageType.None);
        }

        private void BrowseForCacheRoot()
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Expansion terrain cache", cacheRoot,
                string.Empty);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                cacheRoot = selectedPath;
            }
        }

        private void CreateGraphAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Compiled Terrain Navigation Graph",
                "BelowZeroStrategicNavigationGraph", "asset", "Choose where to save the baked graph asset.");
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

        private void ValidateCacheAndDisplayResult()
        {
            try
            {
                CompiledTerrainCacheMetadata metadata = CompiledTerrainCacheMetadata.Read(cacheRoot);
                string octreePath = Path.Combine(cacheRoot, "CompiledOctreesCache");
                int fileCount = Directory.Exists(octreePath)
                    ? Directory.GetFiles(octreePath, "*.optoctrees", SearchOption.TopDirectoryOnly).Length
                    : 0;
                cacheSummary = $"Index v{metadata.IndexVersion}; world {metadata.WorldSize}; " +
                               $"{metadata.TreeCount} trees at {metadata.TreeSize}m; " +
                               $"{metadata.TreesPerBatch} trees per batch; {fileCount:N0} compiled batch files.";
            }
            catch (Exception exception)
            {
                cacheSummary = $"Cache validation failed: {exception.Message}";
            }
            Repaint();
        }

        private void BakeGraph()
        {
            string validationError;
            if (!ValidateBakeSettings(out validationError))
            {
                EditorUtility.DisplayDialog("Cannot Bake Compiled Terrain Graph", validationError, "OK");
                return;
            }

            try
            {
                using (CompiledTerrainCacheReader cacheReader =
                       new CompiledTerrainCacheReader(cacheRoot, terrainWorldOrigin))
                {
                    Bounds terrainBounds = new Bounds(terrainWorldOrigin +
                                                       (Vector3)cacheReader.Metadata.WorldSize * 0.5f,
                        cacheReader.Metadata.WorldSize);
                    if (!terrainBounds.Contains(bakeBounds.min) || !terrainBounds.Contains(bakeBounds.max))
                    {
                        throw new InvalidOperationException(
                            $"Navigation bounds must remain inside terrain world bounds {terrainBounds}.");
                    }

                    StrategicGraphBuilder builder = new StrategicGraphBuilder(bakeBounds, nodeSpacing,
                        keepLargestConnectedRegion);
                    List<StrategicNavigationGraph.Node> bakedNodes;
                    int blockedSamples;
                    int blockedConnections;
                    int discardedNodes;
                    bool completed = builder.TryBuild(
                        position => cacheReader.IsPositionClear(position, clearanceRadius),
                        (start, end) => cacheReader.IsConnectionClear(start, end, clearanceRadius),
                        out bakedNodes, out blockedSamples, out blockedConnections, out discardedNodes);
                    if (!completed)
                    {
                        resultSummary = "Bake cancelled. The existing graph asset was not changed.";
                        return;
                    }
                    if (bakedNodes.Count == 0)
                    {
                        resultSummary = "Bake produced no connected navigation nodes. The existing graph was not changed.";
                        EditorUtility.DisplayDialog("Empty Compiled Terrain Graph", resultSummary, "OK");
                        return;
                    }

                    Undo.RecordObject(targetGraph, "Bake compiled Below Zero terrain graph");
                    targetGraph.ReplaceBakedData(bakedNodes, false);
                    EditorUtility.SetDirty(targetGraph);
                    AssetDatabase.SaveAssets();
                    resultSummary = $"Baked {bakedNodes.Count:N0} nodes; {blockedSamples:N0} obstructed samples; " +
                                    $"{blockedConnections:N0} obstructed connections; " +
                                    $"{discardedNodes:N0} disconnected nodes discarded; " +
                                    $"{cacheReader.LoadedBatchCount:N0} batch loads; " +
                                    $"{cacheReader.MissingBatchCount:N0} absent empty batches.";
                    Selection.activeObject = targetGraph;
                }
            }
            catch (Exception exception)
            {
                resultSummary = $"Bake failed: {exception.Message}";
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Compiled Terrain Bake Failed", resultSummary, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private bool ValidateBakeSettings(out string validationError)
        {
            if (!targetGraph)
            {
                validationError = "Select or create a target graph asset.";
                return false;
            }
            if (string.IsNullOrEmpty(cacheRoot) || !File.Exists(Path.Combine(cacheRoot, "index.txt")))
            {
                validationError = "Select an Expansion cache root containing index.txt.";
                return false;
            }
            if (nodeSpacing <= 0.0f || clearanceRadius <= 0.0f)
            {
                validationError = "Node spacing and SeaTruck clearance must both be greater than zero.";
                return false;
            }
            if (bakeBounds.size.x <= 0.0f || bakeBounds.size.y <= 0.0f || bakeBounds.size.z <= 0.0f)
            {
                validationError = "Every navigation-bounds dimension must be greater than zero.";
                return false;
            }

            StrategicGraphBuilder builder = new StrategicGraphBuilder(bakeBounds, nodeSpacing,
                keepLargestConnectedRegion);
            if (builder.SampleCount > StrategicGraphBuilder.MaximumSamples)
            {
                validationError = $"This bake would test {builder.SampleCount:N0} positions. Increase spacing or " +
                                  $"reduce the bounds to stay below {StrategicGraphBuilder.MaximumSamples:N0}.";
                return false;
            }
            validationError = string.Empty;
            return true;
        }
    }
}
