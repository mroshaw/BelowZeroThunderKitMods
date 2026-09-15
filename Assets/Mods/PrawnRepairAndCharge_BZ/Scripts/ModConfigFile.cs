using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using UnityEngine;
using static DaftAppleGames.PrawnSuitRepairAndCharge_BZ.PrawnSuitRepairAndChargePluginBz;

namespace DaftAppleGames.PrawnSuitRepairAndCharge_BZ
{
    /// <summary>
    /// Nautilus mod config class
    /// </summary>
    [Menu("Prawn Suit Repair And Charge")]
    public class ModConfigFile : ConfigFile
    {
        /// <summary>
        /// Usage option toggles
        /// </summary>
        [Toggle("Enable In MoonPool", Tooltip = "Enable to repair and charge when docking at a Moonpool.")]
        public bool EnableInMoonPool = true;

        [Toggle("Enable In SeaTruck", Tooltip = "Enable to repair and charge when docking at a Seatruck.")]
        public bool EnableInSeaTruck = true;

        /// <summary>
        /// Draws energy from the base when repairing and charging in a Moonpool.
        /// </summary>
        [Toggle("Consume Base Power", Tooltip = "Enable to draw proportionate power from the base when docking with at a MoonPool.")]
        public bool ConsumeBasePower = true;

        [Toggle("Consume SeaTruck Power", Tooltip = "Enable to draw proportionate power from the base when docking with at a Seatruck.")]
        public bool ConsumeSeaTruckPower = true;

        /// <summary>
        /// Docking power costs
        /// </summary>
        [Slider("Charge Power Cost Modifier", Tooltip = "Sets the energy cost per unit of charge added for either docking location.", Step = 0.1f, Format = "{0:F2}", Min = 0.0f, Max = 10.0f, DefaultValue = 0.5f)]
        public float SeaTruckPowerUseChargeModifier = 0.5f;

        [Slider("Repair Power Cost Modifier", Tooltip = "Sets the energy cost per unit of health restored for either docking location.", Step = 0.1f, Format = "{0:F2}", Min = 0.0f, Max = 10.0f, DefaultValue = 0.5f)]
        public float SeaTruckPowerUseRepairModifier = 0.1f;

        /// <summary>
        /// Enables detailed logging for bug reports.
        /// </summary>
        [Toggle("Detailed logging", Tooltip = "Use this to produce a detailed log when reporting bugs. Logs are written to %LOCALAPPDATA%low\\Unknown Worlds\\Below Zero\\Player.log"), OnChange(nameof(OnLoggingChanged))]
        public bool DetailedLogging = false;

        private void OnLoggingChanged(ToggleChangedEventArgs eventArgs)
        {
            ModDebugLog.SetDetailedLoggingState(eventArgs.Value);
        }
    }
}
