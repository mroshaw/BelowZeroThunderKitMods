using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DaftAppleGames.Editor
{
    public static class MissingScriptsEditor
    {

        [MenuItem("Tools/Missing Scripts/List Missing Scripts (Scene)")]
        private static void ListInScene()
        {
            FindMissingScriptsInOpenScenes(false);
        }
        
        [MenuItem("Tools/Missing Scripts/Delete Missing Scripts (Scene)")]
        private static void FindAndDeleteInScene()
        {
            FindMissingScriptsInOpenScenes(true);
        }
        
        [MenuItem("Tools/Missing Scripts/List Missing Scripts (Assets)")]
        private static void ListInAssets()
        {
            FindMissingScriptsInAssets(false);
        }
        
        [MenuItem("Tools/Missing Scripts/Delete Missing Scripts (Assets)")]
        private static void FindAndDeleteInAssets()
        {
            FindMissingScriptsInAssets(true);
        }

        /// <summary>
        ///     Find game objects in open scenes for missing scripts
        /// </summary>
        private static void FindMissingScriptsInOpenScenes(bool deleteScripts)
        {
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (GameObject go in allObjects)
            {
                if (go.transform.parent == null) // Only start with root objects
                {
                    FindMissingScriptsInGameObjectAndChildren(go, deleteScripts);
                }
            }
        }

        /// <summary>
        ///     Search through entire game object structure for missing scripts
        /// </summary>
        private static void FindMissingScriptsInGameObjectAndChildren(GameObject parentGameObject, bool deleteScripts)
        {
            Component[] components = parentGameObject.GetComponents<Component>();
            bool hasMissingScript = components.Any(c => c == null);
            if (hasMissingScript)
            {
                Debug.Log($"Found missing script on: {parentGameObject.name}");
                if (deleteScripts)
                {
                    DeleteMissingScripts(parentGameObject, false);
                }
            }

            foreach (Transform child in parentGameObject.transform) // Recursively check children
            {
                FindMissingScriptsInGameObjectAndChildren(child.gameObject, deleteScripts);
            }
        }

        /// <summary>
        ///     Search assets for objects with missing scripts
        /// </summary>
        private static void FindMissingScriptsInAssets(bool deleteScripts)
        {
            string[] allAssets = AssetDatabase.GetAllAssetPaths();
            foreach (string assetPath in allAssets)
            {
                if (assetPath.StartsWith("Packages/"))
                {
                    continue;
                }

                if (Path.GetExtension(assetPath) != ".prefab")
                {
                    continue;
                }

                GameObject assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (assetRoot == null)
                {
                    continue;
                }

                Component[] components = assetRoot.GetComponentsInChildren<Component>(true);
                bool hasMissingScript = components.Any(c => c == null);
                if (!hasMissingScript)
                {
                    continue;
                }

                Debug.Log($"Found missing script on: {assetRoot.name}");
                if (deleteScripts)
                {
                    DeleteMissingScripts(assetRoot, true);
                }
            }
        }

        private static void DeleteMissingScripts(GameObject gameObjectWithMissingScripts, bool isAsset)
        {
            int numDeleted = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObjectWithMissingScripts);
            if (isAsset)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(gameObjectWithMissingScripts);
            }
            Debug.Log($"Deleted {numDeleted} missing script from: {gameObjectWithMissingScripts.name}");
            
            if (!gameObjectWithMissingScripts.hideFlags.HasFlag(HideFlags.HideInHierarchy))
            {
                return;
            }

            Debug.Log($"Revealing hidden GameObject: {gameObjectWithMissingScripts.name}");
            gameObjectWithMissingScripts.hideFlags &= ~HideFlags.HideInHierarchy;
        }
    }
}