using DaftAppleGames.ModTools.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DaftAppleGames.StartupCommand.StartupCommandPlugin;

namespace DaftAppleGames.StartupCommand
{
    /// <summary>
    /// Simple class to manage the About dialog from the Mod menu
    /// </summary>
    public class ScriptConfigDialog : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private Button runNowSaveButton;
        [SerializeField] private Button runNowNoSaveButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;
        
        [SerializeField] private TMP_InputField startupScriptInputText;
        
        private const string MainMenuOptionsPanelPath = "Panel/Options";
        private const string InGameMenuOptionsPanelPath = "Options";

        private CanvasGroup _parentCanvasGroup;
        private bool _isShowing;
        
        /// <summary>
        /// Set the version number and hide until ready to show
        /// </summary>
        private void Awake()
        {
            ModDebugLog.LogDebug("ScriptConfigCanvas awake!");
            versionText.text = $"v{VersionString}";
            _parentCanvasGroup = transform.parent.GetComponent<CanvasGroup>();
            Hide();
        }

        /// <summary>
        /// Reparents the Panel to the main menu options
        /// </summary>
        internal void Reparent()
        {
            ModDebugLog.LogDebug("Reparenting...");
            GameObject optionsPanel = GetMainPanel();
            if (!optionsPanel)
            {
                ModDebugLog.LogDebug("Could not find Main Menu options panel!");
                Hide();
                return;
            }

            // Parent our new panel
            ModDebugLog.LogDebug($"Setting ScriptConfigCanvasParent to: {optionsPanel}");
            mainPanel.transform.SetParent(optionsPanel.transform);
            mainPanel.transform.LocalZero();
        }

        /// <summary>
        /// Show the UI
        /// </summary>
        internal void Show()
        {
            if (_isShowing)
            {
                return;
            }
            
            LoadConfig();
            // Allow Run Now only from in game
            runNowSaveButton.interactable = InGameMenu();
            runNowNoSaveButton.interactable = InGameMenu();
            
            mainPanel.SetActive(true);
            
            // Disable parent options UI
            if (_parentCanvasGroup)
            {
                _parentCanvasGroup.interactable = false;
                _parentCanvasGroup.blocksRaycasts = false;
            }
            _isShowing = true;
        }

        /// <summary>
        /// Hide the UI
        /// </summary>
        private void Hide()
        {
            mainPanel.SetActive(false);
            
            // Re-enable parent options UI
            if (_parentCanvasGroup)
            {
                _parentCanvasGroup.interactable = true;
                _parentCanvasGroup.blocksRaycasts = true;
            }
            _isShowing = false;
        }

        /// <summary>
        /// Loads settings from the Config file
        /// </summary>
        private void LoadConfig()
        {
            StartupCommandPlugin.ScriptConfigFile.Load();
            startupScriptInputText.text = StartupCommandPlugin.ScriptConfigFile.StartupScript;
        }

        /// <summary>
        /// Updates the config
        /// </summary>
        private void UpdateConfig()
        {
            StartupCommandPlugin.ScriptConfigFile.StartupScript = startupScriptInputText.text;
        }
        
        /// <summary>
        /// Saves settings to the Config file
        /// </summary>
        private void SaveConfig()
        {
            StartupCommandPlugin.ScriptConfigFile.Save();
        }

        /// <summary>
        /// Handle the Save button click
        /// </summary>
        public void SaveButtonHandler()
        {
            UpdateConfig();
            SaveConfig();
            Hide();
        }

        /// <summary>
        /// Handle the Cancel button click
        /// </summary>
        public void CancelButtonHandler()
        {
            Hide();
        }

        /// <summary>
        /// Handle the Run Now button click
        /// </summary>
        public void RunNowButtonHandler(bool runAndSave)
        {
            if (runAndSave)
            {
                SaveConfig();
            }
            
            StartupCommands.RunCommands(true);
            Hide();
        }

        /// <summary>
        /// Return the main Panel from the Main Menu
        /// </summary>
        private GameObject GetMainPanel()
        {
            // Try Main Menu first
            GameObject optionsPanel = GetMainMenuOptionsPanel();
            if (optionsPanel)
            {
                return optionsPanel;
            }

            // Try InGame Menu next
            optionsPanel = GetInGameMenuOptionsPanel();
            if (optionsPanel)
            {
                return optionsPanel;
            }

            ModDebugLog.LogError("Could not find Options panel!");
            return null;
        }

        /// <summary>
        /// Try to find the options panel in MainMenu
        /// </summary>
        private GameObject GetMainMenuOptionsPanel()
        {
            IngameMenu menuUi = FindObjectOfType<IngameMenu>();
            if (!menuUi)
            {
                return null;
            }

            Transform panelTransform = menuUi.transform.Find(InGameMenuOptionsPanelPath);

            if (!panelTransform)
            {
                return null;
            }

            return panelTransform.gameObject;
        }

        /// <summary>
        /// Try to find options panel in InGame menu
        /// </summary>
        /// <returns></returns>
        private GameObject GetInGameMenuOptionsPanel()
        {
            uGUI_MainMenu menuUi = FindObjectOfType<uGUI_MainMenu>();
            if (!menuUi)
            {
                return null;
            }

            Transform panelTransform = menuUi.transform.Find(MainMenuOptionsPanelPath);

            if (!panelTransform)
            {
                return null;
            }

            return panelTransform.gameObject;
        }

        /// <summary>
        /// Check if we're in the game menu (not the main menu)
        /// </summary>
        /// <returns></returns>
        private bool InGameMenu()
        {
            IngameMenu menuUi = FindObjectOfType<IngameMenu>();
            return menuUi != null;
        }
    }
}