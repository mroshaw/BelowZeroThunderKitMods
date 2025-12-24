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
        [SerializeField] private bool logToConsole = true;

        // Bound text to display in the Editor
        [SerializeField] private string logText;
        [SerializeField] private string titleText;
        [SerializeField] private string instructionText;
        
        // Logging instance
        private EditorLog _log;
        protected VisualElement CustomEditorRootVisualElement;

        private Button _clearLogButton;
        private VisualElement _customEditorContainer;
        private TextField _logTextField;
        private ScrollView _logTextScrollView;
        private SerializedObject _serializedObject;

        private bool _hasInternalLogging;

        protected virtual string ToolTitle => "Title";
        protected virtual string IntroText => "Instructions.";
        protected virtual string WelcomeLogText => "Welcome log text.";

        public virtual void CreateGUI()
        {
            _log = new EditorLog(logToConsole, detailedLogging);

            if (rootVisualElement == null)
            {
                Debug.LogError("No rootVisualElement found!");
                return;
            }
            
            baseVisualTree.CloneTree(rootVisualElement);

            // Confider the logger
            _logTextScrollView = rootVisualElement.Q<ScrollView>("LogScrollView");
            _logTextField =  rootVisualElement.Q<TextField>("LogText");
            
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
            CreateCustomGUI();

            // Configure logging
            _log.LogChangedEvent.RemoveListener(LogChangedHandler);
            _log.LogChangedEvent.AddListener(LogChangedHandler);

            Toggle logToConsoleToggle = rootVisualElement.Q<Toggle>("LogToConsoleToggle");
            logToConsoleToggle?.RegisterValueChangedCallback(evt => LogToConsoleToggled(evt.newValue));
            logToConsole = logToConsoleToggle == null || logToConsoleToggle.value;

            Toggle detailedLoggingToggle = rootVisualElement.Q<Toggle>("DetailedLoggingToggle");
            detailedLoggingToggle?.RegisterValueChangedCallback(evt => DetailedLoggingToggled(evt.newValue));
            detailedLogging = detailedLoggingToggle == null || detailedLoggingToggle.value;
            
            Button clearLogButton = rootVisualElement.Q<Button>("ClearLogButton");
            clearLogButton.clicked -= ClearLog;
            clearLogButton.clicked += ClearLog;

            ClearLog();
            LogInfo(WelcomeLogText);
            
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


        /// <summary>
        /// Handle change to the Log To Console checkbox
        /// </summary>
        private void LogToConsoleToggled(bool value)
        {
            _log.LogToConsole = value;
        }

        /// <summary>
        /// Handle change to the Detailed Logging checkbox
        /// </summary>
        private void DetailedLoggingToggled(bool value)
        {
            _log.DetailedLogging = value;
        }

        private void LogChangedHandler(EditorLog changedLog)
        {
            logText = changedLog.GetLogAsString();
            // Force the scrollview to scroll
            _logTextScrollView.schedule.Execute(ScrollLogToBottom).StartingIn(60);
        }

        /// <summary>
        /// Protected method for classes to write to a debug message to the log
        /// </summary>
        protected void LogDebug(string logMessage)
        {
            _log.LogDebug(logMessage);
        }

        /// <summary>
        /// Protected method for classes to write to an info message to the log
        /// </summary>
        protected void LogInfo(string logMessage)
        {
            _log.LogInfo(logMessage);
        }

        /// <summary>
        /// Protected method for classes to write to a warning message to the log
        /// </summary>
        protected void LogWarning(string logMessage)
        {
            _log.LogWarning(logMessage);
        }
        
        /// <summary>
        /// Protected method for classes to write to an error message to the log
        /// </summary>
        protected void LogError(string logMessage)
        {
            _log.LogError(logMessage);
        }
        
        /// <summary>
        /// Handle the Clear Log button
        /// </summary>
        private void ClearLog()
        {
            _log.Clear();
        }
        
        /// <summary>
        /// Force the Log ScrollView to always show the latest entries 
        /// </summary>
        private void ScrollLogToBottom()
        {
            if (_logTextScrollView != null)
            {
                // Get the total scrollable height
                float scrollHeight = _logTextScrollView.contentContainer.layout.height - _logTextScrollView.contentViewport.layout.height;

                // Clamp to avoid negative values if content is smaller than viewport
                scrollHeight = Mathf.Max(0, scrollHeight);
                Vector2 newScrollOffset = new Vector2(_logTextScrollView.scrollOffset.x, scrollHeight);
                
                // Set scroll offset
                _logTextScrollView.scrollOffset = newScrollOffset;
            }
        }
    }
}