using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;

namespace DaftAppleGames.BiggerAquariums
{
    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("Bigger Aquariums")]
    internal class ModConfigFile : ConfigFile
    {
        [Toggle("Detailed Logging", Tooltip="Only check this if you have a problem and need to see the debug output of the mod in the Player.log file"), OnChange(nameof(DetailedLoggingChangedHandler))]
        public bool DetailedLogging = false;

        /// <summary>
        /// Set the Detailed Logging on the Mod Logger
        /// </summary>
        private void DetailedLoggingChangedHandler(ToggleChangedEventArgs newArgs)
        {
            BiggerAquariumsPlugin.ModDebugLog.SetDetailedLoggingState(newArgs.Value);
        }
    }
}