using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace DaftAppleGames.SubnauticaPets.Editor
{
    public class SceneLoaderWindow : EditorWindow
    {
        private SceneAsset _modelSceneAsset;
        
        [MenuItem("Tools/Scene Loader")]
        public static void ShowWindow()
        {
            EditorWindow editorWindow = GetWindow(typeof(SceneLoaderWindow));
            editorWindow.titleContent = new GUIContent("Scene Loader");
            editorWindow.minSize = new Vector2(200, 60);
            editorWindow.maxSize = new Vector2(200, 60);
            editorWindow.Show();
        }
        
        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            AddButton(root, "Model Scene", OpenModelScene);
            AddButton(root, "UI Scene", OpenUiScene);
            AddButton(root, "AI Scene", OpenAiScene);
        }

        private void AddButton(VisualElement root, string buttonText, Action method)
        {
            Button button = new Button
            {
                name = buttonText,
                text = buttonText
            };
            button.clicked += method;
            root.Add(button);
        }
        
        private void OpenModelScene()
        {
            OpenScene("Assets/SubnauticaPets_BZ/Scenes/Models Scene.unity");
        }
        
        private void OpenUiScene()
        {
            OpenScene("Assets/SubnauticaPets_BZ/Scenes/UI Scene.unity");
        }
   
        
        private void OpenAiScene()
        {
            OpenScene("Assets/SubnauticaPets_BZ/Scenes/AI Scene.unity");
        }
        
        private void OpenScene(string sceneAssetPath)
        {
            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene(sceneAssetPath, OpenSceneMode.Single);
        }
    }
}