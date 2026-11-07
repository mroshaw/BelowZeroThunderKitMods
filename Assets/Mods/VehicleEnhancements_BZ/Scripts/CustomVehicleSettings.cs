namespace DaftAppleGames.VehicleEnhancements_BZ
{
    /// <summary>
    /// Settings shared by all instances of a registered custom vehicle type.
    /// </summary>
    internal class CustomVehicleSettings
    {
        public bool EnableSpeedometer { get; set; } = true;
        public bool EnableHsi { get; set; } = true;
        public bool EnableTime { get; set; } = true;
        public ReversingVoice ReversingVoice { get; set; } = ReversingVoice.ThisVehicleIsReversing;
        public bool EnableReversingBeeps { get; set; } = true;
        public HudLocation SpeedometerLocation { get; set; } = HudLocation.Right;
        public float SpeedometerHorizontalAdjust { get; set; } = 0.0f;
        public float SpeedometerVerticalAdjust { get; set; } = 110.0f;
        public float SpeedometerUiScale { get; set; } = 1.0f;
        public HudLocation HsiLocation { get; set; } = HudLocation.Right;
        public float HsiHorizontalAdjust { get; set; } = -290.0f;
        public float HsiVerticalAdjust { get; set; } = 110.0f;
        public float HsiUiScale { get; set; } = 1.0f;
        public HudLocation TimeLocation { get; set; } = HudLocation.Left;
        public float TimeHorizontalAdjust { get; set; } = -250.0f;
        public float TimeVerticalAdjust { get; set; } = 78.7f;
        public float TimeUiScale { get; set; } = 1.0f;

        internal void CopyLayoutFrom(CustomVehicleSettings source)
        {
            SpeedometerLocation = source.SpeedometerLocation;
            SpeedometerHorizontalAdjust = source.SpeedometerHorizontalAdjust;
            SpeedometerVerticalAdjust = source.SpeedometerVerticalAdjust;
            SpeedometerUiScale = source.SpeedometerUiScale;
            HsiLocation = source.HsiLocation;
            HsiHorizontalAdjust = source.HsiHorizontalAdjust;
            HsiVerticalAdjust = source.HsiVerticalAdjust;
            HsiUiScale = source.HsiUiScale;
            TimeLocation = source.TimeLocation;
            TimeHorizontalAdjust = source.TimeHorizontalAdjust;
            TimeVerticalAdjust = source.TimeVerticalAdjust;
            TimeUiScale = source.TimeUiScale;
        }
    }
}
