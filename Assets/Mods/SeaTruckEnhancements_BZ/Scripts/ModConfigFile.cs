using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using static DaftAppleGames.SeaTruckEnhancements_BZ.SeaTruckEnhancementsPlugin_BZ;

namespace DaftAppleGames.SeaTruckEnhancements_BZ
{

    internal enum ReversingAudio { None, Beeps, ThisSeaTruckIsReversing, Both }
    
    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("SeaTruck Enhancements")]
    internal class ModConfigFile : ConfigFile
    {
        /// <summary>
        /// Speedometer
        /// </summary>
        [Toggle("Enable Speedomoeter", Tooltip = "Enables the onscreen Seatruck speedometer"), OnChange(nameof(OnSpeedometerChanged))]
        public bool EnableSpeedometer = true;
        
        /// <summary>
        /// Reversing Camera
        /// </summary>
        [Toggle("Enable Reversing Cam", Tooltip = "Enables the Seatruck reversing camera"), OnChange(nameof(OnReversingCamChanged))]
        public bool EnableReversingCamera = true;
        
        /// <summary>
        /// Reversing Audio
        /// </summary>
        [Choice("Reversing Audio", "None", "Beeps", "This SeaTruck is reversing", "Both", Tooltip="Set the audio that will play when the Seatruck is reversing.")]
        public ReversingAudio ReversingAudio = ReversingAudio.Both;
        
        /// <summary>
        /// Enable detailed logging
        /// </summary>
        [Toggle("Detailed logging", Tooltip = "Use this to produce a detailed log when reporting bugs. Logs are written to %LOCALAPPDATA%low\\Unknown Worlds\\Below Zero\\Player.log"), OnChange(nameof(OnLoggingChanged))]
        public bool DetailedLogging = false;

        /// <summary>
        /// Handle toggling of detailed logging
        /// </summary>
        private void OnSpeedometerChanged(ToggleChangedEventArgs eventArgs)
        {
        }

        /// <summary>
        /// Handle toggling of detailed logging
        /// </summary>
        private void OnReversingCamChanged(ToggleChangedEventArgs eventArgs)
        {
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
