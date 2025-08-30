using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Utils
{
    /// <summary>
    /// Static LogUtils class for common functions
    /// </summary>
    internal static class ModUtils
    {
        /// <summary>
        /// Outputs all distinct tag names for objects currently in the scene
        /// </summary>
        public static void DumpTagNamesFromScene()
        {
            var tags = new HashSet<string>();
            foreach (var go in GameObject.FindObjectsOfType<GameObject>())
            {
                tags.Add(go.tag);
            }

            foreach (var tag in tags)
            {
                Debug.Log($"Tag (in use): {tag}");
            }
        }
        
        /// <summary>
        /// Outputs all of the 32 tags in the game
        /// Tries to use InternalEditorUtility, if it hasn't been stripped
        /// </summary>
        public static void DumpTagNames()
        {
            // UnityEngineInternal.InternalEditorUtility is not accessible,
            // but we can reflect it in the player build.
            var unityEditorAssembly = typeof(GameObject).Assembly;
            var internalEditorUtility = unityEditorAssembly.GetType("UnityEditorInternal.InternalEditorUtility");
            if (internalEditorUtility == null)
            {
                Debug.Log("InternalEditorUtility not found.");
                return;
            }

            var tagsProp = internalEditorUtility.GetProperty("tags", BindingFlags.Static | BindingFlags.Public);
            if (tagsProp == null)
            {
                Debug.Log("Tags property not found.");
                return;
            }

            string[] tags = (string[])tagsProp.GetValue(null, null);
            foreach (var tag in tags)
            {
                Debug.Log($"Tag: {tag}");
            }
        }

        /// <summary>
        /// Outputs all of the 32 layers in the game
        /// </summary>
        public static void DumpLayerNames()
        {
            // List all layers
            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                {
                    LogUtils.LogInfo($"Layer {i}: {name}");
                }
            }
        }

        public static void DumpLayerCollisionMatrixAsExcel()
        {
            StringBuilder sb = new StringBuilder();

            // Collect valid layers
            string[] layerNames = new string[32];
            var validLayers = new System.Collections.Generic.List<int>();

            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                {
                    layerNames[i] = name;
                    validLayers.Add(i);
                }
            }

            // Header row
            sb.Append("Layer\t");
            foreach (int j in validLayers)
            {
                sb.Append(layerNames[j]).Append("\t");
            }
            sb.AppendLine();

            // Rows
            foreach (int i in validLayers)
            {
                sb.Append(layerNames[i]).Append("\t");
                foreach (int j in validLayers)
                {
                    bool ignored = Physics.GetIgnoreLayerCollision(i, j);
                    // Use 1 for collides, 0 for ignored (or swap if you prefer)
                    sb.Append(ignored ? "0" : "1").Append("\t");
                }
                sb.AppendLine();
            }

            LogUtils.LogInfo(sb.ToString());
        }
        
        /// <summary>
        /// Outputs all of the Physics Layer relationships, effectively
        /// showing the content of Project Settings > Physics > Layer Collision Matrix
        /// </summary>
        public static void DumpLayerCollisionMatrix()
        {
            for (int i = 0; i < 32; i++)
            {
                string layerNameI = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerNameI))
                    continue; // skip unused layers

                for (int j = i; j < 32; j++) // symmetric, so start from i
                {
                    string layerNameJ = LayerMask.LayerToName(j);
                    if (string.IsNullOrEmpty(layerNameJ))
                        continue;

                    bool ignored = Physics.GetIgnoreLayerCollision(i, j);

                    LogUtils.LogInfo($"[{i}:{layerNameI}] vs [{j}:{layerNameJ}] => " +
                                     (ignored ? "IGNORED" : "COLLIDES"));
                }
            }
        }
    }
}