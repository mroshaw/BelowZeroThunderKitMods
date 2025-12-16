using TMPro;
using UnityEngine;
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

        [SerializeField] private TMP_InputField startupScriptInputText;
        
        private const string MainMenuOptionsPanelPath = "Panel/Options";
        private const string InGameMenuOptionsPanelPath = "Options";
        
        /// <summary>
        /// Set the version number and hide until ready to show
        /// </summary>
        private void Awake()
        {
            ModDebugLog.LogDebug("ScriptConfigCanvas awake!");
            versionText.text = $"v{VersionString}";
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
            mainPanel.transform.localPosition = Vector3.zero;
            mainPanel.transform.localRotation = Quaternion.identity;
            mainPanel.transform.localScale = Vector3.one;
            
            RectTransform mainPanelRectTransform = mainPanel.GetComponent<RectTransform>();
            mainPanelRectTransform.sizeDelta = new Vector2(-760.0f, -470.0f);
        }
        
        /// <summary>
        /// Show the UI
        /// </summary>
        internal void Show()
        {
            LoadConfig();
            mainPanel.SetActive(true);
        }

        /// <summary>
        /// Hide the UI
        /// </summary>
        private void Hide()
        {
            mainPanel.SetActive(false);
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
        /// Saves settings to the Conig file
        /// </summary>
        private void SaveConfig()
        {
            StartupCommandPlugin.ScriptConfigFile.StartupScript = startupScriptInputText.text;
            StartupCommandPlugin.ScriptConfigFile.Save();
        }
        
        /// <summary>
        /// Handle the Save button click
        /// </summary>
        public void SaveButtonHandler()
        {
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
        /// Return the main Panel from the Main Menu
        /// </summary>
        /// <returns></returns>
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
    }
}