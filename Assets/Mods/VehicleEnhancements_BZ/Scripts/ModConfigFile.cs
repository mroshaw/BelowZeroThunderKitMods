using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ
{
    internal enum ReversingAudio
    {
        None,
        Beeps,
        ThisSeaTruckIsReversing,
        Both
    }

    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("Vehicle Enhancements")] internal class ModConfigFile : ConfigFile
    {
        /// <summary>
        /// Speedometer
        /// </summary>
        [Toggle("Seatruck: Speedometer", Order = 10, Tooltip = "Shows the Seatruck speedometer")]
        public bool EnableSpeedometer = true;

        /// <summary>
        /// Inclinometer
        /// </summary>
        [Toggle("Seatruck: HSI", Order = 11, Tooltip = "Shows the Seatruck pitch and roll indicator")]
        public bool EnableHsi = true;

        /// <summary>
        /// Time and weather indicator
        /// </summary>
        [Toggle("Seatruck: Time and Weather", Order = 12, Tooltip = "Shows time and weather in the Seatruck HUD")]
        public bool EnableTimeAndWeather = true;

        [Toggle("Prawn Suit: Speedometer", Order = 20, Tooltip = "Shows the Prawn Suit speedometer")]
        public bool EnablePrawnSpeedometer = true;

        [Toggle("Prawn Suit: HSI", Order = 21, Tooltip = "Shows the Prawn Suit pitch and roll indicator")]
        public bool EnablePrawnHsi = true;

        [Toggle("Prawn Suit: Time and Weather", Order = 22, Tooltip = "Shows time and weather in the Prawn Suit HUD")]
        public bool EnablePrawnTimeAndWeather = true;

        [Toggle("Snowfox: Speedometer", Order = 30, Tooltip = "Shows the Snowfox speedometer")]
        public bool EnableSnowfoxSpeedometer = true;

        [Toggle("Snowfox: HSI", Order = 31, Tooltip = "Shows the Snowfox pitch and roll indicator")]
        public bool EnableSnowfoxHsi = true;

        [Toggle("Snowfox: Time and Weather", Order = 32, Tooltip = "Shows time and weather in the Snowfox HUD")]
        public bool EnableSnowfoxTimeAndWeather = true;

        /// <summary>
        /// Reversing Camera
        /// </summary>
        [Toggle("Seatruck: Reversing Camera", Order = 13, Tooltip = "Enables the Seatruck reversing camera")]
        public bool EnableReversingCamera = true;

        /// <summary>
        /// Building inside the Vehicles
        /// </summary>
        [Toggle("Allow Building Inside Vehicles", Order = 40,
            Tooltip = "Allows wall-mounted and floor-based habitat builder objects to be constructed inside vehicles.")]
        public bool EnableBuildingInside = true;

        /// <summary>
        /// Reversing Audio
        /// </summary>
        [Choice("Seatruck: Reversing Audio", "None", "Beeps", "Voice", "Both", Order = 14,
            Tooltip = "Set the audio that will play when the vehicle is reversing.")]
        public ReversingAudio ReversingAudio = ReversingAudio.Both;

        /// <summary>
        /// Reversing audio volume
        /// </summary>
        [Slider(
            "Seatruck: Reversing Audio Volume",
            Order = 15,
            Tooltip = "Sets the volume of the reversing audio.",
            Step = 0.05f,
            Format = "{0:F2}",
            Min = 0.0f,
            Max = 1.0f,
            DefaultValue = 0.1f)]
        public float ReversingAudioVolume = 0.1f;

        internal bool IsSpeedometerEnabled(EnhancedVehicle vehicle)
        {
            switch (vehicle)
            {
                case EnhancedVehicle.PrawnSuit:
                    return EnablePrawnSpeedometer;
                case EnhancedVehicle.Snowfox:
                    return EnableSnowfoxSpeedometer;
                default:
                    return EnableSpeedometer;
            }
        }

        internal bool IsHsiEnabled(EnhancedVehicle vehicle)
        {
            switch (vehicle)
            {
                case EnhancedVehicle.PrawnSuit:
                    return EnablePrawnHsi;
                case EnhancedVehicle.Snowfox:
                    return EnableSnowfoxHsi;
                default:
                    return EnableHsi;
            }
        }

        internal bool IsTimeAndWeatherEnabled(EnhancedVehicle vehicle)
        {
            switch (vehicle)
            {
                case EnhancedVehicle.PrawnSuit:
                    return EnablePrawnTimeAndWeather;
                case EnhancedVehicle.Snowfox:
                    return EnableSnowfoxTimeAndWeather;
                default:
                    return EnableTimeAndWeather;
            }
        }

        /// <summary>
        /// Enable detailed logging
        /// </summary>
        [Toggle("Detailed logging", Order = 50,
             Tooltip =
                 "Use this to produce a detailed log when reporting bugs. Logs are written to %LOCALAPPDATA%low\\Unknown Worlds\\Below Zero\\Player.log"),
         OnChange(nameof(OnLoggingChanged))]
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
