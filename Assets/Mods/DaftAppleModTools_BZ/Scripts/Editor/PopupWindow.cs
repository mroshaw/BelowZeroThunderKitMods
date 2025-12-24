#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#else
using DaftAppleGames.Attributes;
#endif
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DaftAppleGames.Editor
{
#if ODIN_INSPECTOR
    public class PopupWindow : OdinEditorWindow
#else
    public class PopupWindow : EditorWindow
#endif
    {
        [SerializeField] private VisualTreeAsset baseVisualTree;

        private Label _titleLabel;
        private Label _contentLabel;
        private Button _okButton;

        public static void Show(string windowTitleText, string titleText, string contentText, Action okButtonCallBack = null)
        {
            PopupWindow popupWindow = CreateInstance<PopupWindow>();
            popupWindow.titleContent = new GUIContent(windowTitleText);
            popupWindow.Init(titleText, contentText, okButtonCallBack);

            // Set the window's position
            popupWindow.ShowUtility();
            popupWindow.CenterOnMainWindow();
        }

        private void CenterOnMainWindow()
        {
            /*
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            Rect popup = position;
            popup.width = 500;
            popup.height = 150;
            popup.x = main.x + (main.width - popup.width) * 0.5f;
            popup.y = main.y + (main.height - popup.height) * 0.5f;
            position = popup;
            */
        }

        public static void ShowWindow()
        {
            GetWindow(typeof(PopupWindow));
        }

        private void Init(string titleText, string messageText, Action okButtonCallBack)
        {
            // Load UXML and USS
            baseVisualTree.CloneTree(rootVisualElement);

            // Get references
            _titleLabel = rootVisualElement.Q<Label>("TitleTextLabel");
            _contentLabel = rootVisualElement.Q<Label>("ContentTextLabel");
            _okButton = rootVisualElement.Q<Button>("OkayButton");

            // Set values
            _titleLabel.text = titleText;
            _contentLabel.text = messageText;

            _okButton.clicked += () =>
            {
                Close();
                okButtonCallBack?.Invoke();
            };
        }
    }
}