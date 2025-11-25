using System.ComponentModel;
using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using UnityEngine;
using static DaftAppleGames.StartupCommand.StartupCommandPlugin;

namespace DaftAppleGames.StartupCommand
{    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("Startup Commands")]
    public class ModConfigFile : ConfigFile
    {
        private const string CommandConfigUiPrefabAssetName = "CommandConfigUi.prefab";
        private static GameObject _commandConfigUi;
        private static ScriptConfigDialog _scriptConfigDialog;
        
        /// <summary>
        /// Display a dialogue to allow configuration of the command scripts
        /// </summary>
        [Button("Configure Commands")]
        public void ConfigureButton(ButtonClickedEventArgs e)
        {
            InitConfigUi();
            _scriptConfigDialog.Show();
        }

        [Slider("Start Delay",
            Tooltip = "The number of seconds after the Player is initialised before the commands are executed.", Step = 0.1f, Format = "{0:F1}", Min = 0.0f, Max = 60.0f, DefaultValue = 5.0f)]
        public float StartDelay = 5.0f;

        [Slider("Delay Between Commands",
            Tooltip = "The number of seconds to wait in between executing each command.", Step = 0.1f, Format = "{0:F1}", Min = 0.0f, Max = 60.0f, DefaultValue = 2.0f)]
        public float CommandDelay = 2.0f;
        
        [Toggle("Disable Alerts", Tooltip = "You can check this to suppress the alerts that are displayed when commands are executed. Details will still be written to the Player log")]
        public bool DisableAlerts = false;
            
        /// <summary>
        /// Initialise the About UI if needs be
        /// </summary>
        private void InitConfigUi()
        {
            if (!_scriptConfigDialog)
            {
                ModDebugLog.LogDebug("Config dialog not yet initialized. Initialising...");
                _commandConfigUi = ModAssetUtils.GetPrefabInstanceFromAssetBundle(CommandConfigUiPrefabAssetName, false);
                _scriptConfigDialog = _commandConfigUi.GetComponentInChildren<ScriptConfigDialog>();
                _scriptConfigDialog.Reparent();
            }
        }
        
        [Toggle("Detailed Logging", Tooltip="Only enable this if you have a problem and need to see the debug output of the mod in the Player.log file."), OnChange(nameof(DetailedLoggingChangedHandler))]
        public bool DetailedLogging = false;
        
        /// <summary>
        /// Set the Detailed Logging on the Mod Logger
        /// </summary>
        private void DetailedLoggingChangedHandler(ToggleChangedEventArgs newArgs)
        {
            ModDebugLog.SetDetailedLoggingState(newArgs.Value);
        }
    }
}