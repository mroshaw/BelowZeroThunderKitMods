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
        [SerializeField] private List<GameObject> sceneObjects = new List<GameObject>();
        [SerializeField] private List<GameObject> assets = new List<GameObject>();

        [SerializeField] private int numFrames = 60;
        
        private List<string> _assetPaths = new List<string>();

        private Button _findMissingScriptsInSceneButton;
        private Button _findMissingScriptsInAssetsButton;
        private Button _deleteMissingScriptsInSceneButton;
        private Button _deleteMissingScriptsInAssetsButton;

        private ListView _objectsListView;
        private ListView _assetsListView;

        private EditorCoroutine _coroutineHandle;
        
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

        /// <summary>
        /// Kill any running coroutines if window is closed
        /// </summary>
        private void OnDestroy()
        {
            if (_coroutineHandle != null)
            {
                EditorCoroutineUtility.StopCoroutine(_coroutineHandle);
            }
        }
        
        /// <summary>
        /// Create the Missing Script editor window components
        /// </summary>
        protected override void CreateCustomGUI()
        {
            // Register buttons
            InitButton("FindMissingScriptsInSceneButton", FindInSceneButtonClicked,
                out _findMissingScriptsInSceneButton);
            InitButton("FindMissingScriptsInAssetsButton", FindInAssetsButtonClicked,
                out _findMissingScriptsInAssetsButton);
            InitButton("DeleteInSceneButton", DeleteInSceneButtonClicked, out _deleteMissingScriptsInSceneButton);
            InitButton("DeleteInAssetsButton", DeleteInAssetsButtonClicked, out _deleteMissingScriptsInAssetsButton);

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
        /// Handle the Find in Scene button click
        /// </summary>
        private void FindInSceneButtonClicked()
        {
            SetButonState(false);
            if (_coroutineHandle != null)
            {
                EditorCoroutineUtility.StopCoroutine(_coroutineHandle);
            }
            _coroutineHandle = EditorCoroutineUtility.StartCoroutine(FindInSceneAsync(), this);
        }

        /// <summary>
        /// Handle the Find in Assets button click
        /// </summary>
        private void FindInAssetsButtonClicked()
        {
            SetButonState(false);
            if (_coroutineHandle != null)
            {
                EditorCoroutineUtility.StopCoroutine(_coroutineHandle);
            }
            _coroutineHandle = EditorCoroutineUtility.StartCoroutine(FindInAssetsAsync(), this);
        }

        /// <summary>
        /// Handle the Delete in Scenes button click
        /// </summary>
        private void DeleteInSceneButtonClicked()
        {
            SetButonState(false);
            if (_coroutineHandle != null)
            {
                EditorCoroutineUtility.StopCoroutine(_coroutineHandle);
            }
            _coroutineHandle = EditorCoroutineUtility.StartCoroutine(DeleteInScenesAsync(), this);
        }

        /// <summary>
        /// Handle the Delete in Assets button click
        /// </summary>
        private void DeleteInAssetsButtonClicked()
        {
            SetButonState(false);
            if (_coroutineHandle != null)
            {
                EditorCoroutineUtility.StopCoroutine(_coroutineHandle);
            }
            _coroutineHandle = EditorCoroutineUtility.StartCoroutine(DeleteInAssetsAsync(), this);
        }
        
        /// <summary>
        /// Search through entire game object structure for missing scripts
        /// </summary>
        private IEnumerator FindMissingScriptsInGameObjectAndChildrenAsync(GameObject parentGameObject, GameObject rootGameObject,
            string assetPath, bool delete)
        {
            LogDebug($"Processing: {parentGameObject} on parent: {parentGameObject.name}");
            Component[] components = parentGameObject.GetComponents<Component>();
            bool hasMissingScript = components.Any(c => c == null);
            if (hasMissingScript)
            {
                if (delete)
                {
                    LogInfo($"Deleting missing script on: {parentGameObject.name}, root object: {rootGameObject.name}");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(parentGameObject);
                }
                else
                {
                    LogInfo($"Found missing script on: {parentGameObject.name}, root object: {rootGameObject.name}");
                    
                    // This is a scene object
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        sceneObjects.Add(parentGameObject);
                    }
                    // This is an asset
                    else
                    {
                        // Only add the root gameobject
                        if (!assets.Contains(rootGameObject))
                        {
                            assets.Add(rootGameObject);
                            _assetPaths.Add(assetPath);
                        }
                    }
                }
            }

            foreach (Transform child in parentGameObject.transform) // Recursively check children
            {
                // Recurse
                yield return FindMissingScriptsInGameObjectAndChildrenAsync(child.gameObject, rootGameObject, assetPath, delete);
            }
        }

        /// <summary>
        /// Find all objects in open scenes that contain missing scripts
        /// </summary>
        private IEnumerator FindInSceneAsync()
        {
            LogInfo("Looking for missing scripts in open scenes...");
            sceneObjects.Clear();
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject parentGameObject in allObjects)
            {
                if (parentGameObject.transform.parent == null) // Only start with root objects
                {
                    yield return
                        FindMissingScriptsInGameObjectAndChildrenAsync(parentGameObject, parentGameObject, null, false);
                }
                // Pause a frame
                yield return null;
            }
            OnSceneProcessingComplete();
        }

        /// <summary>
        /// Delete missing scripts from those GameObjects that were identified
        /// </summary>
        private IEnumerator DeleteInScenesAsync()
        {
            LogInfo("Deleting missing scripts...");
            foreach (GameObject parentGameObject in sceneObjects.ToArray())
            {
                Undo.RecordObject(parentGameObject, $"Delete Missing Script from {parentGameObject.name}");
                yield return
                    FindMissingScriptsInGameObjectAndChildrenAsync(parentGameObject, parentGameObject, null, false);
                // Yield every numFrames
                if (Time.frameCount % numFrames == 0)
                {
                    yield return null;
                }
            }
            OnSceneProcessingComplete();
        }

        /// <summary>
        /// Find Assets in the project that contain missing scripts
        /// </summary>
        private IEnumerator FindInAssetsAsync()
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

                yield return FindMissingScriptsInGameObjectAndChildrenAsync(assetRoot, assetRoot, assetPath, false);
                // Pause a frame
                yield return null;
            }

            OnAssetProcessingComplete();
        }

        /// <summary>
        /// Deletes empty scripts from all assets in the list
        /// </summary>
        private IEnumerator DeleteInAssetsAsync()
        {
            foreach (string assetPath in _assetPaths)
            {
                GameObject assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                Undo.RecordObject(assetRoot, $"Delete Missing Script from {assetRoot}");
                LogInfo($"Deleting missing scripts from asset: {assetPath}");
                yield return FindMissingScriptsInGameObjectAndChildrenAsync(assetRoot, assetRoot, assetPath, true);
                SaveAsset(assetRoot);
                // Pause a frame
                yield return null;
            }

            OnAssetProcessingComplete();
        }

        /// <summary>
        /// Update the Scene Objects list
        /// </summary>
        private void OnSceneProcessingComplete()
        {
            LogInfo($"Processed {sceneObjects.Count} missing script(s) in open scenes.");
            _objectsListView.Refresh();
            EditorSceneManager.SaveOpenScenes();
            SetButonState(true);
        }

        /// <summary>
        /// Update the Assets list
        /// </summary>
        private void OnAssetProcessingComplete()
        {
            LogInfo($"Processed {assets.Count} missing script(s) in assets.");
            _assetsListView.Refresh();
            EditorSceneManager.SaveOpenScenes();
            SetButonState(true);
        }

        /// <summary>
        /// Marks the asset as dirty and saves to disk
        /// </summary>
        private void SaveAsset(GameObject assetRoot)
        {
            LogInfo($"Saving {assetRoot} asset changes...");
            EditorUtility.SetDirty(assetRoot);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
        }

        /// <summary>
        /// Sets the button state
        /// </summary>
        private void SetButonState(bool state)
        {
            _findMissingScriptsInSceneButton.SetEnabled(state);
            _findMissingScriptsInAssetsButton.SetEnabled(state);
            _deleteMissingScriptsInSceneButton.SetEnabled(state);
            _deleteMissingScriptsInAssetsButton.SetEnabled(state);
        }
    }
}