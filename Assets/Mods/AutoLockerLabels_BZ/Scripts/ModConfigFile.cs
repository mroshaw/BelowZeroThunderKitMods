using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using static DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabelsPlugin;

namespace DaftAppleGames.AutoLockerLabels_BZ
{
    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("Auto Locker Labels")]
    public class ModConfigFile : ConfigFile
    {
        /// <summary>
        /// Threshold for an item to override the locker label
        /// </summary>
        [Slider(
            "Dominant item ratio",
            Tooltip = "If the percentage of a single item in a locker exceeds this threshold, then the locker will be labelled according to that single item. e.g. by default, if 60% of all items are 'Copper', the label wil be 'COPPER'",
            Step = 1, Format = "{0}", Min = 1, Max = 100, DefaultValue = 60)]
        public int DominantItemRatio = 60;
        
        /// <summary>
        /// Enable detailed logging
        /// </summary>
        [Toggle("Detailed logging", Tooltip="Use this to produce a detailed log when reporting bugs. Logs are written to %LOCALAPPDATA%low\\Unknown Worlds\\Subnautica\\Player.log"), OnChange(nameof(OnLoggingChanged))]
        public bool DetailedLogging = false;
        
        /// <summary>
        /// Handle toggling of detailed logging
        /// </summary>
        private void OnLoggingChanged(ToggleChangedEventArgs eventArgs)
        {
            ModDebugLog.SetDetailedLoggingState(eventArgs.Value);
        }
    }
}
