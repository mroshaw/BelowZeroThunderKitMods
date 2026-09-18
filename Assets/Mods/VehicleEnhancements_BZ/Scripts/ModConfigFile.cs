using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ
{
    internal enum ReversingVoice
    {
        ThisVehicleIsReversing,
        ThisSeatruckIsReversing,
        ThisPrawnSuitIsReversing,
        ThisSnowfoxIsReversing,
        None
    }

    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("Vehicle Enhancements")] internal class ModConfigFile : ConfigFile
    {
        /// <summary>
        /// Enables the Seatruck speedometer.
        /// </summary>
        [Toggle("Seatruck: Speedometer", Order = 10, Tooltip = "Shows the Seatruck speedometer")]
        public bool EnableSpeedometer = true;

        /// <summary>
        /// Enables the Seatruck pitch and roll indicator.
        /// </summary>
        [Toggle("Seatruck: HSI", Order = 11, Tooltip = "Shows the Seatruck pitch and roll indicator")]
        public bool EnableHsi = true;

        /// <summary>
        /// Enables time and weather in the Seatruck HUD.
        /// </summary>
        [Toggle("Seatruck: Time and Weather", Order = 12, Tooltip = "Shows time and weather in the Seatruck HUD")]
        public bool EnableTimeAndWeather = true;

        /// <summary>
        /// Enables the Prawn Suit speedometer.
        /// </summary>
        [Toggle("Prawn Suit: Speedometer", Order = 20, Tooltip = "Shows the Prawn Suit speedometer")]
        public bool EnablePrawnSpeedometer = true;

        /// <summary>
        /// Enables the Prawn Suit pitch and roll indicator.
        /// </summary>
        [Toggle("Prawn Suit: HSI", Order = 21, Tooltip = "Shows the Prawn Suit pitch and roll indicator")]
        public bool EnablePrawnHsi = true;

        /// <summary>
        /// Enables time and weather in the Prawn Suit HUD.
        /// </summary>
        [Toggle("Prawn Suit: Time and Weather", Order = 22, Tooltip = "Shows time and weather in the Prawn Suit HUD")]
        public bool EnablePrawnTimeAndWeather = true;

        /// <summary>
        /// Selects the Prawn Suit reversing voice.
        /// </summary>
        [Choice("Prawn Suit: Reversing Voice", "This vehicle is reversing", "This Seatruck is reversing",
            "This Prawn Suit is reversing", "This Snowfox is reversing", "None", Order = 23,
            Tooltip = "Selects the voice that plays when the Prawn Suit reverses.")]
        public ReversingVoice PrawnReversingVoice = ReversingVoice.ThisPrawnSuitIsReversing;

        /// <summary>
        /// Enables Prawn Suit reversing beeps.
        /// </summary>
        [Toggle("Prawn Suit: Reversing Beeps", Order = 24,
            Tooltip = "Plays reversing beeps in the Prawn Suit.")]
        public bool EnablePrawnReversingBeeps = true;

        /// <summary>
        /// Enables the Snowfox speedometer.
        /// </summary>
        [Toggle("Snowfox: Speedometer", Order = 30, Tooltip = "Shows the Snowfox speedometer")]
        public bool EnableSnowfoxSpeedometer = true;

        /// <summary>
        /// Enables the Snowfox pitch and roll indicator.
        /// </summary>
        [Toggle("Snowfox: HSI", Order = 31, Tooltip = "Shows the Snowfox pitch and roll indicator")]
        public bool EnableSnowfoxHsi = true;

        /// <summary>
        /// Enables time and weather in the Snowfox HUD.
        /// </summary>
        [Toggle("Snowfox: Time and Weather", Order = 32, Tooltip = "Shows time and weather in the Snowfox HUD")]
        public bool EnableSnowfoxTimeAndWeather = true;

        /// <summary>
        /// Selects the Snowfox reversing voice.
        /// </summary>
        [Choice("Snowfox: Reversing Voice", "This vehicle is reversing", "This Seatruck is reversing",
            "This Prawn Suit is reversing", "This Snowfox is reversing", "None", Order = 33,
            Tooltip = "Selects the voice that plays when the Snowfox reverses.")]
        public ReversingVoice SnowfoxReversingVoice = ReversingVoice.ThisSnowfoxIsReversing;

        /// <summary>
        /// Enables Snowfox reversing beeps.
        /// </summary>
        [Toggle("Snowfox: Reversing Beeps", Order = 34,
            Tooltip = "Plays reversing beeps on the Snowfox.")]
        public bool EnableSnowfoxReversingBeeps = true;

        /// <summary>
        /// Enables the Seatruck reversing camera.
        /// </summary>
        [Toggle("Seatruck: Reversing Camera", Order = 13, Tooltip = "Enables the Seatruck reversing camera")]
        public bool EnableReversingCamera = true;

        /// <summary>
        /// Allows building inside Seatruck segments.
        /// </summary>
        [Toggle("Seatruck: Allow Building Inside", Order = 16,
            Tooltip = "Allows wall-mounted and floor-based habitat builder objects to be constructed inside the Seatruck.")]
        public bool EnableBuildingInside = true;

        /// <summary>
        /// Selects the Seatruck reversing voice.
        /// </summary>
        [Choice("Seatruck: Reversing Voice", "This vehicle is reversing", "This Seatruck is reversing",
            "This Prawn Suit is reversing", "This Snowfox is reversing", "None", Order = 14,
            Tooltip = "Selects the voice that plays when the Seatruck reverses.")]
        public ReversingVoice SeatruckReversingVoice = ReversingVoice.ThisSeatruckIsReversing;

        /// <summary>
        /// Enables Seatruck reversing beeps.
        /// </summary>
        [Toggle("Seatruck: Reversing Beeps", Order = 15,
            Tooltip = "Plays reversing beeps in the Seatruck.")]
        public bool EnableSeatruckReversingBeeps = true;

        /// <summary>
        /// Sets the reversing audio volume for all vehicles.
        /// </summary>
        [Slider(
            "Reversing Audio Volume",
            Order = 40,
            Tooltip = "Sets the reversing audio volume for all vehicles.",
            Step = 0.05f,
            Format = "{0:F2}",
            Min = 0.0f,
            Max = 1.0f,
            DefaultValue = 0.1f)]
        public float ReversingAudioVolume = 0.1f;

        internal ReversingVoice GetReversingVoice(EnhancedVehicle vehicle)
        {
            switch (vehicle)
            {
                case EnhancedVehicle.PrawnSuit:
                    return PrawnReversingVoice;
                case EnhancedVehicle.Snowfox:
                    return SnowfoxReversingVoice;
                default:
                    return SeatruckReversingVoice;
            }
        }

        internal bool AreReversingBeepsEnabled(EnhancedVehicle vehicle)
        {
            switch (vehicle)
            {
                case EnhancedVehicle.PrawnSuit:
                    return EnablePrawnReversingBeeps;
                case EnhancedVehicle.Snowfox:
                    return EnableSnowfoxReversingBeeps;
                default:
                    return EnableSeatruckReversingBeeps;
            }
        }

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
        /// Enables detailed logging.
        /// </summary>
        [Toggle("Detailed logging", Order = 50,
             Tooltip =
                 "Use this to produce a detailed log when reporting bugs. Logs are written to %USERPROFILE%\\AppData\\LocalLow\\Unknown Worlds\\Subnautica Below Zero\\Player.log"),
         OnChange(nameof(OnLoggingChanged))]
        public bool DetailedLogging = false;

        /// <summary>
        /// Applies the detailed logging setting when it changes.
        /// </summary>
        private void OnLoggingChanged(ToggleChangedEventArgs eventArgs)
        {
            ModDebugLog.SetDetailedLoggingState(eventArgs.Value);
        }
    }
}
