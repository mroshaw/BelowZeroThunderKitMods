using Nautilus.Json;

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
    internal class ModConfigFile : ConfigFile
    {
        internal const float MinimumHudAdjust = -400.0f;
        internal const float MaximumHudAdjust = 400.0f;
        internal const float MinimumHudScale = 0.1f;
        internal const float MaximumHudScale = 3.0f;

        /// <summary>
        /// Enables the Seatruck speedometer.
        /// </summary>
        public bool EnableSpeedometer = true;

        /// <summary>
        /// Enables the Seatruck pitch and roll indicator.
        /// </summary>
        public bool EnableHsi = true;

        /// <summary>
        /// Enables time and weather in the Seatruck HUD.
        /// </summary>
        public bool EnableTimeAndWeather = true;

        /// <summary>
        /// Enables the Prawn Suit speedometer.
        /// </summary>
        public bool EnablePrawnSpeedometer = true;

        /// <summary>
        /// Enables the Prawn Suit pitch and roll indicator.
        /// </summary>
        public bool EnablePrawnHsi = true;

        /// <summary>
        /// Enables time and weather in the Prawn Suit HUD.
        /// </summary>
        public bool EnablePrawnTimeAndWeather = true;

        /// <summary>
        /// Selects the Prawn Suit reversing voice.
        /// </summary>
        public ReversingVoice PrawnReversingVoice = ReversingVoice.ThisPrawnSuitIsReversing;

        /// <summary>
        /// Enables Prawn Suit reversing beeps.
        /// </summary>
        public bool EnablePrawnReversingBeeps = true;

        /// <summary>
        /// Enables the Snowfox speedometer.
        /// </summary>
        public bool EnableSnowfoxSpeedometer = true;

        /// <summary>
        /// Enables the Snowfox pitch and roll indicator.
        /// </summary>
        public bool EnableSnowfoxHsi = true;

        /// <summary>
        /// Enables time and weather in the Snowfox HUD.
        /// </summary>
        public bool EnableSnowfoxTimeAndWeather = true;

        /// <summary>
        /// Selects the Snowfox reversing voice.
        /// </summary>
        public ReversingVoice SnowfoxReversingVoice = ReversingVoice.ThisSnowfoxIsReversing;

        /// <summary>
        /// Enables Snowfox reversing beeps.
        /// </summary>
        public bool EnableSnowfoxReversingBeeps = true;

        /// <summary>
        /// Enables the Seatruck reversing camera.
        /// </summary>
        public bool EnableReversingCamera = true;

        /// <summary>
        /// Allows building inside Seatruck segments.
        /// </summary>
        public bool EnableBuildingInside = true;

        /// <summary>
        /// Selects the Seatruck reversing voice.
        /// </summary>
        public ReversingVoice SeatruckReversingVoice = ReversingVoice.ThisSeatruckIsReversing;

        /// <summary>
        /// Enables Seatruck reversing beeps.
        /// </summary>
        public bool EnableSeatruckReversingBeeps = true;

        /// <summary>
        /// Sets the reversing audio volume for all vehicles.
        /// </summary>
        public float ReversingAudioVolume = 0.1f;

        public float SeatruckSpeedometerHorizontalAdjust { get; set; } = 0.0f;
        public float SeatruckSpeedometerVerticalAdjust { get; set; } = 0.0f;
        public float SeatruckSpeedometerUiScale { get; set; } = 1.0f;

        public float SeatruckHsiHorizontalAdjust { get; set; } = 0.0f;
        public float SeatruckHsiVerticalAdjust { get; set; } = 0.0f;
        public float SeatruckHsiUiScale { get; set; } = 1.0f;

        public float SeatruckTimeHorizontalAdjust { get; set; } = 0.0f;
        public float SeatruckTimeVerticalAdjust { get; set; } = 0.0f;
        public float SeatruckTimeUiScale { get; set; } = 1.0f;

        public float PrawnSpeedometerHorizontalAdjust { get; set; } = 0.0f;
        public float PrawnSpeedometerVerticalAdjust { get; set; } = 0.0f;
        public float PrawnSpeedometerUiScale { get; set; } = 1.0f;

        public float PrawnHsiHorizontalAdjust { get; set; } = 0.0f;
        public float PrawnHsiVerticalAdjust { get; set; } = 0.0f;
        public float PrawnHsiUiScale { get; set; } = 1.0f;

        public float PrawnTimeHorizontalAdjust { get; set; } = 0.0f;
        public float PrawnTimeVerticalAdjust { get; set; } = 0.0f;
        public float PrawnTimeUiScale { get; set; } = 1.0f;

        public float SnowfoxSpeedometerHorizontalAdjust { get; set; } = 0.0f;
        public float SnowfoxSpeedometerVerticalAdjust { get; set; } = 0.0f;
        public float SnowfoxSpeedometerUiScale { get; set; } = 1.0f;

        public float SnowfoxHsiHorizontalAdjust { get; set; } = 0.0f;
        public float SnowfoxHsiVerticalAdjust { get; set; } = 0.0f;
        public float SnowfoxHsiUiScale { get; set; } = 1.0f;

        public float SnowfoxTimeHorizontalAdjust { get; set; } = 0.0f;
        public float SnowfoxTimeVerticalAdjust { get; set; } = 0.0f;
        public float SnowfoxTimeUiScale { get; set; } = 1.0f;

        public float SeatruckCameraHorizontalAdjust { get; set; } = 0.0f;
        public float SeatruckCameraVerticalAdjust { get; set; } = 0.0f;
        public float SeatruckCameraUiScale { get; set; } = 1.0f;

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
        public bool DetailedLogging = false;
    }
}
