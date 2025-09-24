using DaftAppleGames.SeaTruckRecall_BZ.DockRecaller;
using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;

namespace DaftAppleGames.SeaTruckRecall_BZ
{    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("Sea Truck Recall")]
    public class ModConfigFile : ConfigFile
    {
        /// <summary>
        /// Recall Config
        /// </summary>
        [Choice("Recall Method (Restart Required)", Tooltip="Smooth movement will attempt to drive the Seatruck to the dock. Instance will immediately move it and engage the dock.")]
        public RecallMoveMethod RecallMoveMethod = RecallMoveMethod.Smooth;

        /// <summary>
        /// Speed and movement
        /// </summary>
        [Slider("Maximum Range", Step = 10, Min = 100, Max = 2000, DefaultValue = 200), OnChange(nameof(RangeChangeHandler))]
        public int MaximumRange = 1000;

        [Toggle("NavGrid Debug", Tooltip="If checked, nodes of the NavGrid will be spawned as spheres in game. This will SEVERELY impact performance, and is for debugging only!")]
        public bool EnableNavGridDebug = false;
        
        /// <summary>
        /// Handlers for config changes
        /// </summary>
        private void RangeChangeHandler(SliderChangedEventArgs newRangeArgs)
        {
            AllSeaTruckDockRecallers.UpdateAllDockRange(newRangeArgs.Value);
        }
        
        [Toggle("Detailed Logging", Tooltip="Only check this if you have a problem and need to see the debug output of the mod in the Player.log file"), OnChange(nameof(DetailedLoggingChangedHandler))]
        public bool DetailedLogging = false;

        /// <summary>
        /// Set the Detailed Logging on the Mod Logger
        /// </summary>
        private void DetailedLoggingChangedHandler(ToggleChangedEventArgs newArgs)
        {
            SeaTruckDockRecallPlugin.ModDebugLog.SetDetailedLoggingState(newArgs.Value);
        }
    }
}