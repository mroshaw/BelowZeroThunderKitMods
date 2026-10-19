using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using static DaftAppleGames.BoosterTankSpeedMod_BZ.BoosterTankSpeedPluginBz;

namespace DaftAppleGames.BoosterTankSpeedMod_BZ
{
    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("Booster Tank Speed")]
    internal class ModConfigFile : ConfigFile
    {
        /// <summary>
        /// Speed Boost multiplier
        /// </summary>
        [Slider("Speed Multiplier", Step = 0.1f, Format = "{0:F2}", Min = 1.0f, Max = 10.0f, DefaultValue = 1.5f), OnChange(nameof(SpeedSliderChangedHandler))]
        public float SpeedBoostMultiplier = 1.5f;

        /// <summary>
        /// Oxygen Consumption multiplier
        /// </summary>
        [Slider("Oxygen Consumption Multiplier", Step = 0.1f, Format = "{0:F2}", Min = 0.0f, Max = 5.0f, DefaultValue = 0.5f), OnChange(nameof(OxygenSliderChangedHandler))]
        public float OxygenConsumptionMultiplier = 0.5f;

        /// <summary>
        /// Enable detailed logging.
        /// </summary>
        [Toggle("Detailed Logging", Tooltip = "Use this to produce a detailed log when reporting bugs. Logs are written to %USERPROFILE%\\AppData\\LocalLow\\Unknown Worlds\\Subnautica Below Zero\\Player.log"), OnChange(nameof(OnLoggingChanged))]
        public bool DetailedLogging = false;

        /// <summary>
        /// Update detailed logging at runtime.
        /// </summary>
        private void OnLoggingChanged(ToggleChangedEventArgs eventArgs)
        {
            ModDebugLog.SetDetailedLoggingState(eventArgs.Value);
        }

        /// <summary>
        /// Handle Speed slider changes
        /// </summary>
        private void SpeedSliderChangedHandler(SliderChangedEventArgs newSpeedArgs)
        {
           BoosterTankHistory.UpdateAllSpeed(newSpeedArgs.Value);
        }

        /// <summary>
        /// Handle Oxygen slider changes
        /// </summary>
        /// <param name="newOxygenArgs"></param>
        private void OxygenSliderChangedHandler(SliderChangedEventArgs newOxygenArgs)
        {
            BoosterTankHistory.UpdateAllOxygen(newOxygenArgs.Value);
        }
    }
}
