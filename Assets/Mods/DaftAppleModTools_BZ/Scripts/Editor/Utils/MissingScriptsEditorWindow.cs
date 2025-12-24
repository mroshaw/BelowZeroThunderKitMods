using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.EditorCoroutines.Editor;

namespace DaftAppleGames.Editor
{
    public class MissingScriptsEditorWindow : BaseEditorWindow
    {
        [SerializeField] List<GameObject> sceneObjects = new List<GameObject>();
        [SerializeField] List<GameObject> assets = new List<GameObject>();
        private List<string> _assetPaths = new List<string>();

        private Button _findMissingScriptsInSceneButton;
        private Button _findMissingScriptsInAssetsButton;
        private Button _deleteMissingScriptsInSceneButton;
        private Button _deleteMissingScriptsInAssetsButton;

        private ListView _objectsListView;
        private ListView _assetsListView;

        protected override string ToolTitle => "Missing Scripts";
        protected override string IntroText =>
            "This tool will help you find and remove missing scripts from the current scene or all asset files.";
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
            InitButton("FindMissingScriptsInSceneButton", FindInScene, out _findMissingScriptsInSceneButton);
            InitButton("FindMissingScriptsInAssetsButton", FindInAssets, out _findMissingScriptsInAssetsButton);
            InitButton("DeleteInSceneButton", DeleteInScenes, out _deleteMissingScriptsInSceneButton);
            InitButton("DeleteInAssetsButton", DeleteInAssets, out _deleteMissingScriptsInAssetsButton);

            // Configure Objects List
            _objectsListView = rootVisualElement.Q<ListView>("SceneObjectsListView");
            ConfigureListView(_objectsListView, sceneObjects, true);
            _objectsListView.Refresh();

            // Configure Assets List
            _assetsListView = rootVisualElement.Q<ListView>("AssetsListView");
            ConfigureListView(_assetsListView, assets, false);
            _assetsListView.Refresh();
        }

        /// <summary>
        /// Consistently configure a List View control
        /// </summary>
        private void ConfigureListView(ListView listView, List<GameObject> objectList, bool allowSceneObjects)
        {
            // Disable the collection count
            listView.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                // Remove the collection size field if present
                var intField = listView.Q<IntegerField>();
                if (intField != null)
                {
                    intField.RemoveFromHierarchy();
                    Debug.Log("Removed count from list view");
                }
            });

            // Custom view to show only GameObject, as read-only
            listView.makeItem = () =>
            {
                var field = new ObjectField
                {
                    objectType = typeof(GameObject),
                    allowSceneObjects = allowSceneObjects,
                    style =
                    {
                        flexGrow = 1
                    }
                };

                return field;
            };

            // Hide empty entries
            listView.bindItem = (element, index) =>
            {
                var field = (ObjectField)element;

                // Hide empty entries
                if (objectList.Count == 0 || index >= objectList.Count || index < 0)
                {
                    field.style.display = DisplayStyle.None;
                }
                else
                {
                    var go = objectList[index];
                    field.SetValueWithoutNotify(go);
                    field.style.display = DisplayStyle.Flex;
                }
            };
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
            LogInfo("Looking for missing scripts in open scenes...");
            sceneObjects.Clear();
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject go in allObjects)
            {
                if (go.transform.parent == null) // Only start with root objects
                {
                    FindMissingScriptsInGameObjectAndChildren(go, false);
                }
            }

            LogInfo($"Found {sceneObjects.Count} missing script(s) in open scenes.");
            _objectsListView.Refresh();
        }

        /// <summary>
        /// Async method to allow the Editor UI to respond
        /// </summary>
        private void FindMissingScriptsInGameObjectAndChildren(GameObject parentGameObject, bool delete)
        {
            EditorCoroutineUtility.StartCoroutine(
                FindMissingScriptsInGameObjectAndChildrenAsync(parentGameObject, delete), this);
        }

        /// <summary>
        /// Search through entire game object structure for missing scripts
        /// </summary>
        private IEnumerator FindMissingScriptsInGameObjectAndChildrenAsync(GameObject parentGameObject, bool delete)
        {
            Component[] components = parentGameObject.GetComponents<Component>();
            bool hasMissingScript = components.Any(c => c == null);
            if (hasMissingScript)
            {
                if (delete)
                {
                    int numDeleted = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(parentGameObject);
                    LogInfo($"Deleted {numDeleted} missing scripts on: {parentGameObject.name}");
                }
                else
                {
                    LogInfo($"Found missing script on: {parentGameObject.name}");
                    sceneObjects.Add(parentGameObject);
                }
            }

            foreach (Transform child in parentGameObject.transform) // Recursively check children
            {
                // Pause a frame
                yield return null;

                // Recurse
                yield return FindMissingScriptsInGameObjectAndChildrenAsync(child.gameObject, delete);
            }
        }

        /// <summary>
        /// Delete missing scripts from those GameObjects that were identified
        /// </summary>
        private void DeleteInScenes()
        {
            LogInfo("Deleting missing scripts...");
            foreach (GameObject go in sceneObjects)
            {
                Undo.RecordObject(go, $"Delete Missing Script from {go.name}");
                FindMissingScriptsInGameObjectAndChildren(go, true);
                LogInfo($"Deleted missing script from: {go.name}");
            }

            EditorSceneManager.SaveOpenScenes();
            LogInfo("Done deleting missing scripts!.");
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

                LogInfo($"Found missing script on: {assetRoot.name}");

                assets.Add(assetRoot);
                _assetPaths.Add(assetPath);
            }

            LogInfo($"Found {assets.Count} missing script(s) in assets.");

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

                Undo.RecordObject(assetRoot, $"Delete Missing Script from {assetRoot}");
                LogInfo($"Deleting missing scripts from asset: {assetPath}");
                FindMissingScriptsInGameObjectAndChildren(assetRoot, true);

                EditorUtility.SetDirty(assetRoot);
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveOpenScenes();

                LogInfo("Done deleting missing scripts!.");
            }
        }
    }
}