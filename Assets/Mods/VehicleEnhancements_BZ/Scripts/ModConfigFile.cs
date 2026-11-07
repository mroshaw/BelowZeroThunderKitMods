using System.Collections.Generic;
using Nautilus.Json;

namespace DaftAppleGames.VehicleEnhancements_BZ
{
    internal enum HudLocation
    {
        Left,
        Center,
        Right
    }

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
        internal const float MinimumHudAdjust = -1500.0f;
        internal const float MaximumHudAdjust = 1500.0f;
        internal const float MinimumHudScale = 0.1f;
        internal const float MaximumHudScale = 4.0f;

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

        public HudLocation SeatruckSpeedometerLocation { get; set; } = HudLocation.Right;
        public float SeatruckSpeedometerHorizontalAdjust { get; set; } = -30.0f;
        public float SeatruckSpeedometerVerticalAdjust { get; set; } = 110.0f;
        public float SeatruckSpeedometerUiScale { get; set; } = 1.0f;

        public HudLocation SeatruckHsiLocation { get; set; } = HudLocation.Right;
        public float SeatruckHsiHorizontalAdjust { get; set; } = -300.0f;
        public float SeatruckHsiVerticalAdjust { get; set; } = 110.0f;
        public float SeatruckHsiUiScale { get; set; } = 1.0f;

        public HudLocation SeatruckTimeLocation { get; set; } = HudLocation.Left;
        public float SeatruckTimeHorizontalAdjust { get; set; } = -215.0f;
        public float SeatruckTimeVerticalAdjust { get; set; } = 78.7f;
        public float SeatruckTimeUiScale { get; set; } = 1.0f;

        public HudLocation PrawnSpeedometerLocation { get; set; } = HudLocation.Right;
        public float PrawnSpeedometerHorizontalAdjust { get; set; } = -30.0f;
        public float PrawnSpeedometerVerticalAdjust { get; set; } = 130.0f;
        public float PrawnSpeedometerUiScale { get; set; } = 1.0f;

        public HudLocation PrawnHsiLocation { get; set; } = HudLocation.Right;
        public float PrawnHsiHorizontalAdjust { get; set; } = -300.0f;
        public float PrawnHsiVerticalAdjust { get; set; } = 110.0f;
        public float PrawnHsiUiScale { get; set; } = 1.0f;

        public HudLocation PrawnTimeLocation { get; set; } = HudLocation.Left;
        public float PrawnTimeHorizontalAdjust { get; set; } = -215.0f;
        public float PrawnTimeVerticalAdjust { get; set; } = 78.7f;
        public float PrawnTimeUiScale { get; set; } = 1.0f;

        public HudLocation SnowfoxSpeedometerLocation { get; set; } = HudLocation.Right;
        public float SnowfoxSpeedometerHorizontalAdjust { get; set; } = -30.0f;
        public float SnowfoxSpeedometerVerticalAdjust { get; set; } = 10.0f;
        public float SnowfoxSpeedometerUiScale { get; set; } = 1.0f;

        public HudLocation SnowfoxHsiLocation { get; set; } = HudLocation.Right;
        public float SnowfoxHsiHorizontalAdjust { get; set; } = -300.0f;
        public float SnowfoxHsiVerticalAdjust { get; set; } = 110.0f;
        public float SnowfoxHsiUiScale { get; set; } = 1.0f;

        public HudLocation SnowfoxTimeLocation { get; set; } = HudLocation.Left;
        public float SnowfoxTimeHorizontalAdjust { get; set; } = -215.0f;
        public float SnowfoxTimeVerticalAdjust { get; set; } = 78.7f;
        public float SnowfoxTimeUiScale { get; set; } = 1.0f;

        public HudLocation SeatruckCameraLocation { get; set; } = HudLocation.Right;
        public float SeatruckCameraHorizontalAdjust { get; set; } = -333.0f;
        public float SeatruckCameraVerticalAdjust { get; set; } = 67.0f;
        public float SeatruckCameraUiScale { get; set; } = 1.0f;

        public Dictionary<string, CustomVehicleSettings> CustomVehicleProfiles { get; set; } =
            new Dictionary<string, CustomVehicleSettings>();

        private static readonly CustomVehicleSettings defaultCustomVehicleSettings = new CustomVehicleSettings();
        private CustomVehicleSettings currentCustomVehicleSettings;

        internal CustomVehicleSettings CurrentCustomVehicleSettings => currentCustomVehicleSettings ?? defaultCustomVehicleSettings;

        internal void SetCustomVehicleType(string identifier)
        {
            currentCustomVehicleSettings = string.IsNullOrEmpty(identifier) ? null : GetOrCreateCustomVehicleSettings(identifier);
        }

        internal CustomVehicleSettings GetOrCreateCustomVehicleSettings(string identifier)
        {
            if (!CustomVehicleProfiles.TryGetValue(identifier, out CustomVehicleSettings settings))
            {
                settings = new CustomVehicleSettings();
                CustomVehicleProfiles.Add(identifier, settings);
            }

            return settings;
        }

        internal ReversingVoice GetReversingVoice(EnhancedVehicle vehicle)
        {
            switch (vehicle)
            {
                case EnhancedVehicle.PrawnSuit:
                    return PrawnReversingVoice;
                case EnhancedVehicle.Snowfox:
                    return SnowfoxReversingVoice;
                case EnhancedVehicle.CustomVehicle:
                    return CurrentCustomVehicleSettings.ReversingVoice;
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
                case EnhancedVehicle.CustomVehicle:
                    return CurrentCustomVehicleSettings.EnableReversingBeeps;
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
                case EnhancedVehicle.CustomVehicle:
                    return CurrentCustomVehicleSettings.EnableSpeedometer;
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
                case EnhancedVehicle.CustomVehicle:
                    return CurrentCustomVehicleSettings.EnableHsi;
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
                case EnhancedVehicle.CustomVehicle:
                    return CurrentCustomVehicleSettings.EnableTime;
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
