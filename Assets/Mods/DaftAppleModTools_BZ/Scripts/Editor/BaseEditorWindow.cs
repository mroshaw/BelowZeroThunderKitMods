using System.Globalization;
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

        private const int MaxLogCharacters = 10000;
        
        // Bound text to display in the Editor
        [SerializeField] private string logText;
        [SerializeField] private string titleText;
        [SerializeField] private string instructionText;
        [SerializeField] private float processProgress;

        protected VisualElement CustomEditorRootVisualElement;
        
        // Logging instance
        private EditorLog _log;
        private bool _hasInternalLogging;
        private TextField _logTextField;
        private ScrollView _logTextScrollView;
        private Button _clearLogButton;

        // Process management
        private Button _cancelProcessButton;
        private Slider _progressSlider;
        private Label _progressLabel;
        
        private VisualElement _customEditorContainer;

        // Bound Serialized data
        private SerializedObject _serializedObject;


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
            
            // Setup the Progress functions
            _cancelProcessButton =  rootVisualElement.Q<Button>("CancelProcessButton");
            _cancelProcessButton.clicked -= CancelProcessClicked;
            _cancelProcessButton.clicked += CancelProcessClicked;

            _progressSlider =  rootVisualElement.Q<Slider>("ProgressSlider");
            _progressSlider.SetEnabled(false);
            _progressLabel =  rootVisualElement.Q<Label>("ProgressLabel");
            
            ResetProcessProgress();
            
            // Create the embedded custom UI
            CreateCustomGUI();
            
            // Bind the UI to serialized properties
            BindUI();
        }

        protected abstract void CreateCustomGUI();

        protected abstract void CancelProcess();

        /// <summary>
        /// Call before starting a process to enable the cancel button and reset the progress
        /// </summary>
        protected void StartProcess()
        {
            _cancelProcessButton.SetEnabled(true);
        }

        /// <summary>
        /// Call after processing has finished, to disable cancel button
        /// </summary>
        protected void EndProcess()
        {
            _cancelProcessButton.SetEnabled(false);
        }
        
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
        /// Used to set the process progress
        /// </summary>
        protected void SetProcessProgress(float newValue)
        {
            processProgress = newValue;
            _progressLabel.text = $"{ Mathf.RoundToInt(newValue).ToString()}%";
            _progressSlider.value = newValue;
        }

        /// <summary>
        /// Resets progress to zero
        /// </summary>
        protected void ResetProcessProgress()
        {
            SetProcessProgress(0);
            _cancelProcessButton.SetEnabled(false);
        }

        /// <summary>
        /// Handle the Cancel Progress button click
        /// </summary>
        private void CancelProcessClicked()
        {
            CancelProcess();
            EndProcess();
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
            
            // Truncate the log text is it's too big for the control
            if (logText.Length > MaxLogCharacters)
            {
                logText = logText.Substring(logText.Length - MaxLogCharacters);
            }

            // Update the log text control
            _logTextField.value = logText;
            
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