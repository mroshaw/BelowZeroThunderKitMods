using UnityEngine;
using UnityEditor;

namespace DaftAppleGames.Editor.Utils
{
    /// <summary>
    /// This forces the Editor to select the parent game object, rather than the
    /// LOD mesh game object, when selecting a multi-LOD object in the scene view
    /// Can be enabled/disabled
    /// </summary>
    [InitializeOnLoad]
    public class AlwaysSelectParentInHierarchy
    {
        private static bool _isEnabled;

        static AlwaysSelectParentInHierarchy()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            if (!_isEnabled)
            {
                return;
            }

            if (!MouseIsOverAnySceneView())
            {
                {
                    return;
                }
            }

            GameObject[] selectedObjects = Selection.gameObjects;

            for (int goIndex = 0; goIndex < selectedObjects.Length; goIndex++)
            {
                GameObject currGameObject = selectedObjects[goIndex];
                if (currGameObject == null)
                {
                    continue;
                }

                Transform parentWithLOD = currGameObject.transform;

                while (parentWithLOD != null)
                {
                    if (parentWithLOD.GetComponent<LODGroup>() != null)
                    {
                        GameObject newSelection = parentWithLOD.gameObject;
                        int index = goIndex;
                        EditorApplication.delayCall += () =>
                        {
                            selectedObjects[index] = newSelection;
                            Selection.objects = selectedObjects;
                        };
                        break;
                    }

                    parentWithLOD = parentWithLOD.parent;
                }
            }
        }

        /// <summary>
        /// Check to see if user is selecting something in the scene view
        /// </summary>
        private static bool MouseIsOverAnySceneView()
        {
            foreach (SceneView view in SceneView.sceneViews)
            {
                if (view == EditorWindow.mouseOverWindow)
                {
                    return true;
                }
            }

            return false;
        }

        [MenuItem("Tools/Toggle LOD Parent Selection")]
        public static void ToggleFunctionality()
        {
            _isEnabled = !_isEnabled;
            Debug.Log("LOD Parent Selection is " + (_isEnabled ? "enabled" : "disabled"));
        }
    }
}