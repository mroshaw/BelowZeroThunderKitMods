using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DaftAppleGames.Editor
{
    public abstract class BaseEditorWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset baseVisualTree;
        [SerializeField] private VisualTreeAsset customEditorVisualTree;
        [SerializeField] private bool detailedLogging;
        [SerializeField] private bool logToConsole;

        // Bound text to display in the Editor
        [SerializeField] private string logText;
        [SerializeField] private string titleText;
        [SerializeField] private string instructionText;

        private bool _isPopupOpen = false;

        // Logging instance
        protected EditorLog Log;
        protected VisualElement CustomEditorRootVisualElement;

        private Button _clearLogButton;
        private VisualElement _customEditorContainer;
        private ScrollView _logTextScrollView;
        private SerializedObject _serializedObject;

        private bool _hasInternalLogging;

        protected virtual string ToolTitle => "Title";
        protected virtual string IntroText => "Instructions.";
        protected virtual string WelcomeLogText => "Welcome log text.";

        public virtual void CreateGUI()
        {
            Log = new EditorLog(logToConsole, detailedLogging);

            if (rootVisualElement == null)
            {
                Debug.LogError("No rootVisualElement found!");
                return;
            }

            baseVisualTree.CloneTree(rootVisualElement);

            // Setup the custom editor content in the container placeholder
            _customEditorContainer = rootVisualElement.Q<VisualElement>("CustomEditorContainer");
            if (customEditorVisualTree)
            {
                CustomEditorRootVisualElement = customEditorVisualTree.CloneTree();
                _customEditorContainer.Add(CustomEditorRootVisualElement);
            }

            // Set window titles
            titleText = ToolTitle;
            instructionText = IntroText;
            logText = WelcomeLogText;

            CreateCustomGUI();

            // Configure logging
            Log.LogChangedEvent.RemoveListener(LogChangedHandler);
            Log.LogChangedEvent.AddListener(LogChangedHandler);

            Toggle logToConsoleToggle = rootVisualElement.Q<Toggle>("LogToConsoleToggle");
            logToConsoleToggle?.RegisterValueChangedCallback(evt => LogToConsoleToggled(evt.newValue));
            logToConsole = logToConsoleToggle == null || logToConsoleToggle.value;

            Button clearLogButton = rootVisualElement.Q<Button>("ClearLogButton");
            clearLogButton.clicked -= ClearLog;
            clearLogButton.clicked += ClearLog;

            // Bind the UI to serialized properties
            BindUI();
        }

        protected abstract void CreateCustomGUI();

        private void BindUI()
        {
            // Bind to UI
            _serializedObject = new SerializedObject(this);
            rootVisualElement.Bind(_serializedObject);
        }


        private void LogToConsoleToggled(bool value)
        {
            Log.LogToConsole = value;
        }

        private void DetailedLoggingToggled(bool value)
        {
            Log.DetailedLogging = value;
        }

        private void LogChangedHandler(EditorLog changedLog)
        {
            logText = changedLog.GetLogAsString();
            ScrollLogToBottom();
        }

        protected void LogDebug(string logMessage)
        {
            Log.AddToLog(logMessage);
        }
        
        private void ClearLog()
        {
            Log.Clear();
        }

        private void ScrollLogToBottom()
        {
            if (_logTextScrollView != null)
            {
                _logTextScrollView.scrollOffset = _logTextScrollView.contentContainer.layout.max -
                                                  _logTextScrollView.contentViewport.layout.size;
                // _logTextScrollView.scrollOffset = new Vector2(0, float.MaxValue); // Force scroll to bottom
            }
        }
    }
}