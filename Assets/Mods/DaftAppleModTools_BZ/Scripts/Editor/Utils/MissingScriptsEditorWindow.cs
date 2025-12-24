using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DaftAppleGames.Editor
{
    public class MissingScriptsEditorWindow : BaseEditorWindow
    {

        [SerializeField] List<GameObject> sceneObjects = new List<GameObject>();
        [SerializeField] List<GameObject> assets = new List<GameObject>();
        private List<string> _assetPaths = new List<string>();
        
        private Button _findMissingScriptsInSceneButton;
        private Button _findMissingScriptsInAssetsButton;
        
        private ListView _objectsListView;
        private ListView _assetsListView;
        
        protected override string ToolTitle => "Missing Scripts";
        protected override string IntroText => "This tool will help you find and remove missing scripts from the current scene or all asset files.";
        protected override string WelcomeLogText => "Welcome to the Missing Scripts tool!";
        
        [MenuItem("Tools/Missing Scripts Tool")]
        public static void ShowWindow()
        {
            MissingScriptsEditorWindow editorWindow = GetWindow<MissingScriptsEditorWindow>();
            editorWindow.titleContent = new GUIContent("Missing Scripts");
        }

        protected override void CreateCustomGUI()
        {
            // Register buttons
            InitButton("FindMissingScriptsInSceneButton", FindInScene,  out _findMissingScriptsInSceneButton);
            InitButton("FindMissingScriptsInAssetsButton", FindInAssets,  out _findMissingScriptsInAssetsButton);
            InitButton("DeleteInSceneButton", DeleteInScenes,  out _findMissingScriptsInSceneButton);
            InitButton("DeleteInAssetsButton", DeleteInAssets,  out _findMissingScriptsInSceneButton);
            
            // Get lists
            _objectsListView = rootVisualElement.Q<ListView>("SceneObjectsListView");
            // _objectsListView.SetEnabled(false);
            _objectsListView.Refresh();
            
            _assetsListView = rootVisualElement.Q<ListView>("AssetsListView");
            // _assetsListView.SetEnabled(false);
            _assetsListView.Refresh();
        }

        /// <summary>
        /// Find and configure an Editor window button
        /// </summary>
        private void InitButton(string uiElementName, Action clickAction, out Button button)
        {
            button = rootVisualElement.Q<Button>(uiElementName);
            if (button != null)
            {
                button.clicked -= clickAction;
                button.clicked += clickAction;
            }
            else
            {
                Debug.LogError($"Could not find {uiElementName}!");
            }
        }
        
        /// <summary>
        /// Find all objects in open scenes that contain missing scripts
        /// </summary>
        private void FindInScene()
        {
            LogDebug("Looking for missing scripts in open scenes...");
            sceneObjects.Clear();
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject go in allObjects)
            {
                if (go.transform.parent == null) // Only start with root objects
                {
                    FindMissingScriptsInGameObjectAndChildren(go);
                }
            }

            LogDebug($"Found {sceneObjects.Count} missing script(s) in open scenes.");
            _objectsListView.Refresh();
        }

        /// <summary>
        /// Search through entire game object structure for missing scripts
        /// </summary>
        private void FindMissingScriptsInGameObjectAndChildren(GameObject parentGameObject)
        {
            Component[] components = parentGameObject.GetComponents<Component>();
            bool hasMissingScript = components.Any(c => c == null);
            if (hasMissingScript)
            {
                LogDebug($"Found missing script on: {parentGameObject.name}");
                sceneObjects.Add(parentGameObject);
            }

            foreach (Transform child in parentGameObject.transform) // Recursively check children
            {
                FindMissingScriptsInGameObjectAndChildren(child.gameObject);
            }
        }
        
        /// <summary>
        /// Delete missing scripts from those GameObjects that were identified
        /// </summary>
        private void DeleteInScenes()
        {
            Log.AddToLog(LogLevel.Info, "Deleting missing scripts...");
            foreach (GameObject go in sceneObjects)
            {
                Undo.RecordObject(go, $"Delete Missing Script from {go.name}");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                Log.AddToLog($"Deleted missing script from: {go.name}");
                if (!go.hideFlags.HasFlag(HideFlags.HideInHierarchy))
                {
                    continue;
                }

                Log.AddToLog($"Revealing hidden GameObject: {go.name}");
                go.hideFlags &= ~HideFlags.HideInHierarchy;
            }

            Log.AddToLog(LogLevel.Info, "Done deleting missing scripts!.");
        }
        
        /// <summary>
        /// Find Assets in the project that contain missing scripts
        /// </summary>
        private void FindInAssets()
        {
            assets.Clear();
            _assetPaths.Clear();
            
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

                Log.AddToLog(LogLevel.Info, $"Found missing script on: {assetRoot.name}");

                assets.Add(assetRoot);
                _assetPaths.Add(assetPath);
            }

            Log.AddToLog(LogLevel.Info, $"Found {assets.Count} missing script(s) in assets.");
            
            _assetsListView.Refresh();
        }

        /// <summary>
        /// Deletes empty scripts from all assets in the list
        /// </summary>
        private void DeleteInAssets()
        {
            foreach (string assetPath in _assetPaths)
            {
                GameObject assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                
                Component[] components = assetRoot.GetComponents<Component>();
                foreach (Component component in components.ToArray())
                {
                    if (component == null)
                    {
                        Undo.RecordObject(assetRoot, $"Delete Missing Script from {assetRoot}");
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(assetRoot);
                    }
                }
                EditorUtility.SetDirty(assetRoot);
                AssetDatabase.SaveAssets();
            }
        }
    }
}