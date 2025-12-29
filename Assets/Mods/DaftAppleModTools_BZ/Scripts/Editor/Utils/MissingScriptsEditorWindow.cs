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

        // Coroutine will yield after this many frames
        private const int NumFramesBeforeYield = 60;
        
        private int _currFrameCount = 0;
        
        private List<string> _assetPaths = new List<string>();

        private Button _findMissingScriptsInSceneButton;
        private Button _findMissingScriptsInAssetsButton;
        private Button _deleteMissingScriptsInSceneButton;
        private Button _deleteMissingScriptsInAssetsButton;

        private ListView _objectsListView;
        private ListView _assetsListView;

        private EditorCoroutine _coroutineHandle;

        private int _totalObjectsToProcess;
        private int _currentObjectProcessing;
        
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
            
            SetReadyState();
        }

        /// <summary>
        /// Cancel the in progress subroutine
        /// </summary>
        protected override void CancelProcess()
        {
            LogInfo("Process cancelled!");
            SetReadyState();
        }

        /// <summary>
        /// Stops an active coroutine
        /// </summary>
        private void CancelInProgressSubroutine()
        {
            if (_coroutineHandle != null)
            {
                EditorCoroutineUtility.StopCoroutine(_coroutineHandle);
                ResetProcessProgress();
            }
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
            SetInProgressState();
            _coroutineHandle = EditorCoroutineUtility.StartCoroutine(FindInSceneAsync(), this);
        }

        /// <summary>
        /// Handle the Find in Assets button click
        /// </summary>
        private void FindInAssetsButtonClicked()
        {
           SetInProgressState();
            _coroutineHandle = EditorCoroutineUtility.StartCoroutine(FindInAssetsAsync(), this);
        }

        /// <summary>
        /// Handle the Delete in Scenes button click
        /// </summary>
        private void DeleteInSceneButtonClicked()
        {
            SetInProgressState();
            _coroutineHandle = EditorCoroutineUtility.StartCoroutine(DeleteInScenesAsync(), this);
        }

        /// <summary>
        /// Handle the Delete in Assets button click
        /// </summary>
        private void DeleteInAssetsButtonClicked()
        {
            SetInProgressState();
            _coroutineHandle = EditorCoroutineUtility.StartCoroutine(DeleteInAssetsAsync(), this);
        }

        /// <summary>
        /// Sets the state when the process is started
        /// </summary>
        private void SetInProgressState()
        {
            SetButonState(false);
            CancelInProgressSubroutine();
            StartProcess();
        }

        /// <summary>
        /// Sets the state when the process is not started/ended
        /// </summary>
        private void SetReadyState()
        {
            SetButonState(true);
            CancelInProgressSubroutine();
            EndProcess();
        }
        
        /// <summary>
        /// Resets the process progress
        /// </summary>
        private void ResetProgress()
        {
            _totalObjectsToProcess = 0;
            _currentObjectProcessing = 0;
            UpdateProgress();
        }
        
        /// <summary>
        /// Updates the current process progress
        /// </summary>
        private void UpdateProgress()
        {
            float processPercentage = _totalObjectsToProcess > 0 ? (float)_currentObjectProcessing / _totalObjectsToProcess * 100 : 0;
            LogDebug($"Progressing: {_currentObjectProcessing} of {_totalObjectsToProcess}. Percentage: {processPercentage}%");
            SetProcessProgress(_totalObjectsToProcess > 0 ? processPercentage : 0);
        }
        
        /// <summary>
        /// Search through entire game object structure for missing scripts
        /// </summary>
        private IEnumerator FindMissingScriptsInGameObjectAndChildrenAsync(GameObject parentGameObject, GameObject rootGameObject,
            string assetPath, bool delete)
        {

            _currFrameCount++;
            
            // Pause a frame
            if (_currFrameCount % NumFramesBeforeYield == 0)
            {
                UpdateProgress();
                _currFrameCount = 0;
                yield return null;
            }
            
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
            ResetProgress();
            
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            _totalObjectsToProcess = allObjects.Length;
            
            foreach (GameObject parentGameObject in allObjects)
            {
                _currentObjectProcessing++;
                if (parentGameObject.transform.parent == null) // Only start with root objects
                {
                    yield return
                        FindMissingScriptsInGameObjectAndChildrenAsync(parentGameObject, parentGameObject, null, false);
                }
            }
            OnSceneProcessingComplete();
        }

        /// <summary>
        /// Delete missing scripts from those GameObjects that were identified
        /// </summary>
        private IEnumerator DeleteInScenesAsync()
        {
            LogInfo("Deleting missing scripts in open scenes...");
            ResetProgress();
            _totalObjectsToProcess = sceneObjects.Count;
            
            foreach (GameObject parentGameObject in sceneObjects.ToArray())
            {
                _currentObjectProcessing++;
                Undo.RecordObject(parentGameObject, $"Delete Missing Script from {parentGameObject.name}");
                yield return
                    FindMissingScriptsInGameObjectAndChildrenAsync(parentGameObject, parentGameObject, null, false);
            }
            OnSceneProcessingComplete();
        }

        /// <summary>
        /// Find Assets in the project that contain missing scripts
        /// </summary>
        private IEnumerator FindInAssetsAsync()
        {
            LogInfo("Looking for missing scripts in assets...");
            assets.Clear();
            _assetPaths.Clear();
            ResetProgress();
            string[] allAssets = AssetDatabase.GetAllAssetPaths();
            _totalObjectsToProcess = allAssets.Length;
            
            foreach (string assetPath in allAssets)
            {
                _currentObjectProcessing++;
                
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
            }

            OnAssetProcessingComplete();
        }

        /// <summary>
        /// Deletes empty scripts from all assets in the list
        /// </summary>
        private IEnumerator DeleteInAssetsAsync()
        {
            LogInfo("Deleting missing scripts in assets...");
            ResetProgress();
            _totalObjectsToProcess = _assetPaths.Count;
            
            foreach (string assetPath in _assetPaths)
            {
                _currentObjectProcessing++;
                GameObject assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                Undo.RecordObject(assetRoot, $"Delete Missing Script from {assetRoot}");
                LogInfo($"Deleting missing scripts from asset: {assetPath}");
                yield return FindMissingScriptsInGameObjectAndChildrenAsync(assetRoot, assetRoot, assetPath, true);
                SaveAsset(assetRoot);
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
            SetReadyState();
        }

        /// <summary>
        /// Update the Assets list
        /// </summary>
        private void OnAssetProcessingComplete()
        {
            LogInfo($"Processed {assets.Count} missing script(s) in assets.");
            _assetsListView.Refresh();
            EditorSceneManager.SaveOpenScenes();
            SetReadyState();
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