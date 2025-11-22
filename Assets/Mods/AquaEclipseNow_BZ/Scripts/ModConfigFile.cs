using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;

namespace DaftAppleGames.AquaEclipseNowPlugin
{    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("Aqua Eclipse Now")]
    public class ModConfigFile : ConfigFile
    {
        [Slider("Time Before Eclipse", Tooltip="The eclipse now command will forward game time to this many seconds before the next eclipse.", Step = 1, Format = "{0}", Min = 0, Max = 120, DefaultValue = 15)]
        public int TimeBeforeEclipse = 15;
        
        [Slider("Time Skip Duration", Tooltip="The time taken to smoothly for the current time to the time of the next eclipse.", Step = 1, Format = "{0}", Min = 1, Max = 10, DefaultValue = 3)]
        public int TimeSkipDuration = 3;
        
        [Slider("Simulation Iterations", Tooltip="How many times the simulation will be run to try to find the next eclipse, before it gives up.", Step = 1000, Format = "{0}", Min = 5000, Max = 1000000, DefaultValue = 200000)]
        public int SimulationIterations = 200000;

        [Slider("Simulation Time Step", Tooltip="The step forward in time that the simulation takes each iteration.", Step = 0.1f, Format = "{0:F2}", Min = 0.1f, Max = 10.0f, DefaultValue = 0.2f)]
        public float SimulationTimeStep = 0.2f;

        [Slider("Simulation Threshold", Tooltip="If a simulation iteration finds an eclipse ratio that exceeds this value, the time will be used for the eclipse.", Step = 0.001f, Format = "{0:F3}", Min = 0.5f, Max = 1.0f, DefaultValue = 0.99f)]
        public float SimulationThreshold = 0.99f;

        [Toggle("Detailed Logging", Tooltip="Only enable this if you have a problem and need to see the debug output of the mod in the Player.log file."), OnChange(nameof(DetailedLoggingChangedHandler))]
        public bool DetailedLogging = false;
        
        /// <summary>
        /// Set the Detailed Logging on the Mod Logger
        /// </summary>
        private void DetailedLoggingChangedHandler(ToggleChangedEventArgs newArgs)
        {
            AquaEclipseNowPlugin.ModDebugLog.SetDetailedLoggingState(newArgs.Value);
        }
    }
}