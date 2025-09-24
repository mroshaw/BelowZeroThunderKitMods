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
        /// Recaller NavGrid properties
        /// </summary>
        [Slider("Maximum Range", Tooltip = "Determines the range of the recaller. If you increase the range, consider increasing the cell extents or navigation accuracy will be reduced. A high MaxRange and a high NumExtents may cause performance issues.", Step = 10, Min = 50, Max = 1000, DefaultValue = 100)]
        public int MaximumRange = 100;

        [Slider("Cell Extents", Tooltip = "Determines the number of NavCells that the NavGrid extends in each axis direction from the center of the dock. The distance between each cell will be MaxRange/NumExtents. The larger the number, the more accurate the navigation but with an equivalent increase in path finding time and processing.", Step = 1, Min = 5, Max = 50, DefaultValue = 5)]
        public int CellExtents = 5;
        
        /// <summary>
        /// Apply Range and Extents to all existing dock recallers
        /// </summary>
        [Button("Regenerate Nav Grid", Tooltip="Uses the values specified above and recreates the NavGrid on all current Dock Recallers. Depending on your settings, this could take a while. Only click this once - it may not look like it, but the recallers are being updated in the background.")]
        public void RegenerateNavGrid(ButtonClickedEventArgs e)
        {
            AllSeaTruckDockRecallers.RegenerateAllNavGrids();
        }
        
        /// <summary>
        /// Debug stuff
        /// </summary>
        [Toggle("NavGrid Debug", Tooltip="If checked, nodes of the NavGrid will be spawned as spheres in game. This will SEVERELY impact performance, and is for debugging only!")]
        public bool EnableNavGridDebug = false;
        
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