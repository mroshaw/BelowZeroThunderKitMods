using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using static DaftAppleGames.SeaTruckSpeedMod_BZ.SeaTruckSpeedPluginBz;

namespace DaftAppleGames.SeaTruckSpeedMod_BZ.Config
{
    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("SeaTruck Speed Modifier")]
    internal class ModConfigFile : ConfigFile
    {
        /// <summary>
        /// Drag Modifier
        /// </summary>
        [Slider("Speed Multiplier", Step = 0.1f, Format = "{0:F2}", Min = 1.0f, Max = 10.0f, DefaultValue = 2.0f), OnChange(nameof(SpeedSliderChangedHandler))]
        public float DragModifier = 2.0f;

        /// <summary>
        /// Additional speed-relative power drain
        /// </summary>
        [Slider("Power Drain", Tooltip = "Adds a modest amount of power consumption based on the Seatruck's actual speed. Set to 0 to disable.", Step = 0.05f, Format = "{0:F2}", Min = 0.0f, Max = 1.0f, DefaultValue = 0.2f)]
        public float PowerDrain = 0.2f;

        /// <summary>
        /// Enable detailed logging
        /// </summary>
        [Toggle("Detailed logging", Tooltip = "Use this to produce a detailed log when reporting bugs. Logs are written to %LOCALAPPDATA%low\\Unknown Worlds\\Below Zero\\Player.log"), OnChange(nameof(OnLoggingChanged))]
        public bool DetailedLogging = false;


        /// <summary>
        /// Handle Drag slider changes
        /// </summary>
        private void SpeedSliderChangedHandler(SliderChangedEventArgs newDragArgs)
        {
            SeaTruckHistory.UpdateAllDrag(newDragArgs.Value);
        }

        /// <summary>
        /// Handle toggling of detailed logging
        /// </summary>
        private void OnLoggingChanged(ToggleChangedEventArgs eventArgs)
        {
            ModDebugLog.SetDetailedLoggingState(eventArgs.Value);
        }
    }
}
