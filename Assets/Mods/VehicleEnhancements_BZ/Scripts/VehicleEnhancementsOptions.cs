using System;
using System.Collections.Generic;
using Nautilus.Options;
using TMPro;
using UnityEngine;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ
{
    internal class VehicleEnhancementsOptions : ModOptions
    {
        private const string MenuName = "Vehicle Enhancements BZ";
        private static readonly string[] ReversingVoices =
        {
            "This vehicle is reversing", "This Seatruck is reversing",
            "This Prawn Suit is reversing", "This Snowfox is reversing", "None"
        };

        private readonly ModConfigFile config;
        private readonly SortedDictionary<int, OptionItem> orderedOptions = new SortedDictionary<int, OptionItem>();

        internal VehicleEnhancementsOptions(ModConfigFile config) : base(MenuName)
        {
            this.config = config;
            AddToggle(100, nameof(config.EnableSpeedometer), "Speedometer", config.EnableSpeedometer,
                value => config.EnableSpeedometer = value);
            AddToggle(101, nameof(config.EnableHsi), "HSI", config.EnableHsi,
                value => config.EnableHsi = value);
            AddToggle(102, nameof(config.EnableTimeAndWeather), "Time and Weather", config.EnableTimeAndWeather,
                value => config.EnableTimeAndWeather = value);
            AddVoiceChoice(103, nameof(config.SeatruckReversingVoice), config.SeatruckReversingVoice,
                value => config.SeatruckReversingVoice = value);
            AddToggle(104, nameof(config.EnableSeatruckReversingBeeps), "Reversing Beeps", config.EnableSeatruckReversingBeeps,
                value => config.EnableSeatruckReversingBeeps = value);

            AddPositionSlider(105, nameof(config.SeatruckSpeedometerHorizontalAdjust), "Speedometer: Horizontal Adjust", config.SeatruckSpeedometerHorizontalAdjust,
                false, value => config.SeatruckSpeedometerHorizontalAdjust = value);
            AddPositionSlider(106, nameof(config.SeatruckSpeedometerVerticalAdjust), "Speedometer: Vertical Adjust", config.SeatruckSpeedometerVerticalAdjust,
                true, value => config.SeatruckSpeedometerVerticalAdjust = value);
            AddScaleSlider(107, nameof(config.SeatruckSpeedometerUiScale), "Speedometer: UI Scale", config.SeatruckSpeedometerUiScale,
                value => config.SeatruckSpeedometerUiScale = value);
            AddPositionSlider(108, nameof(config.SeatruckHsiHorizontalAdjust), "HSI: Horizontal Adjust", config.SeatruckHsiHorizontalAdjust,
                false, value => config.SeatruckHsiHorizontalAdjust = value);
            AddPositionSlider(109, nameof(config.SeatruckHsiVerticalAdjust), "HSI: Vertical Adjust", config.SeatruckHsiVerticalAdjust,
                true, value => config.SeatruckHsiVerticalAdjust = value);
            AddScaleSlider(110, nameof(config.SeatruckHsiUiScale), "HSI: UI Scale", config.SeatruckHsiUiScale,
                value => config.SeatruckHsiUiScale = value);
            AddPositionSlider(111, nameof(config.SeatruckTimeHorizontalAdjust), "Time/Weather: Horizontal Adjust", config.SeatruckTimeHorizontalAdjust,
                false, value => config.SeatruckTimeHorizontalAdjust = value);
            AddPositionSlider(112, nameof(config.SeatruckTimeVerticalAdjust), "Time/Weather: Vertical Adjust", config.SeatruckTimeVerticalAdjust,
                true, value => config.SeatruckTimeVerticalAdjust = value);
            AddScaleSlider(113, nameof(config.SeatruckTimeUiScale), "Time/Weather: UI Scale", config.SeatruckTimeUiScale,
                value => config.SeatruckTimeUiScale = value);

            AddToggle(114, nameof(config.EnableReversingCamera), "Reversing Camera", config.EnableReversingCamera,
                value => config.EnableReversingCamera = value);
            AddPositionSlider(115, nameof(config.SeatruckCameraHorizontalAdjust), "Camera: Horizontal Adjust", config.SeatruckCameraHorizontalAdjust,
                false, value => config.SeatruckCameraHorizontalAdjust = value);
            AddPositionSlider(116, nameof(config.SeatruckCameraVerticalAdjust), "Camera: Vertical Adjust", config.SeatruckCameraVerticalAdjust,
                true, value => config.SeatruckCameraVerticalAdjust = value);
            AddScaleSlider(117, nameof(config.SeatruckCameraUiScale), "Camera: UI Scale", config.SeatruckCameraUiScale,
                value => config.SeatruckCameraUiScale = value);
            AddToggle(118, nameof(config.EnableBuildingInside), "Allow Building Inside", config.EnableBuildingInside,
                value => config.EnableBuildingInside = value);

            AddToggle(200, nameof(config.EnablePrawnSpeedometer), "Speedometer", config.EnablePrawnSpeedometer,
                value => config.EnablePrawnSpeedometer = value);
            AddToggle(201, nameof(config.EnablePrawnHsi), "HSI", config.EnablePrawnHsi,
                value => config.EnablePrawnHsi = value);
            AddToggle(202, nameof(config.EnablePrawnTimeAndWeather), "Time and Weather", config.EnablePrawnTimeAndWeather,
                value => config.EnablePrawnTimeAndWeather = value);
            AddVoiceChoice(203, nameof(config.PrawnReversingVoice), config.PrawnReversingVoice,
                value => config.PrawnReversingVoice = value);
            AddToggle(204, nameof(config.EnablePrawnReversingBeeps), "Reversing Beeps", config.EnablePrawnReversingBeeps,
                value => config.EnablePrawnReversingBeeps = value);

            AddPositionSlider(205, nameof(config.PrawnSpeedometerHorizontalAdjust), "Speedometer: Horizontal Adjust", config.PrawnSpeedometerHorizontalAdjust,
                false, value => config.PrawnSpeedometerHorizontalAdjust = value);
            AddPositionSlider(206, nameof(config.PrawnSpeedometerVerticalAdjust), "Speedometer: Vertical Adjust", config.PrawnSpeedometerVerticalAdjust,
                true, value => config.PrawnSpeedometerVerticalAdjust = value);
            AddScaleSlider(207, nameof(config.PrawnSpeedometerUiScale), "Speedometer: UI Scale", config.PrawnSpeedometerUiScale,
                value => config.PrawnSpeedometerUiScale = value);
            AddPositionSlider(208, nameof(config.PrawnHsiHorizontalAdjust), "HSI: Horizontal Adjust", config.PrawnHsiHorizontalAdjust,
                false, value => config.PrawnHsiHorizontalAdjust = value);
            AddPositionSlider(209, nameof(config.PrawnHsiVerticalAdjust), "HSI: Vertical Adjust", config.PrawnHsiVerticalAdjust,
                true, value => config.PrawnHsiVerticalAdjust = value);
            AddScaleSlider(210, nameof(config.PrawnHsiUiScale), "HSI: UI Scale", config.PrawnHsiUiScale,
                value => config.PrawnHsiUiScale = value);
            AddPositionSlider(211, nameof(config.PrawnTimeHorizontalAdjust), "Time/Weather: Horizontal Adjust", config.PrawnTimeHorizontalAdjust,
                false, value => config.PrawnTimeHorizontalAdjust = value);
            AddPositionSlider(212, nameof(config.PrawnTimeVerticalAdjust), "Time/Weather: Vertical Adjust", config.PrawnTimeVerticalAdjust,
                true, value => config.PrawnTimeVerticalAdjust = value);
            AddScaleSlider(213, nameof(config.PrawnTimeUiScale), "Time/Weather: UI Scale", config.PrawnTimeUiScale,
                value => config.PrawnTimeUiScale = value);

            AddToggle(300, nameof(config.EnableSnowfoxSpeedometer), "Speedometer", config.EnableSnowfoxSpeedometer,
                value => config.EnableSnowfoxSpeedometer = value);
            AddToggle(301, nameof(config.EnableSnowfoxHsi), "HSI", config.EnableSnowfoxHsi,
                value => config.EnableSnowfoxHsi = value);
            AddToggle(302, nameof(config.EnableSnowfoxTimeAndWeather), "Time and Weather", config.EnableSnowfoxTimeAndWeather,
                value => config.EnableSnowfoxTimeAndWeather = value);
            AddVoiceChoice(303, nameof(config.SnowfoxReversingVoice), config.SnowfoxReversingVoice,
                value => config.SnowfoxReversingVoice = value);
            AddToggle(304, nameof(config.EnableSnowfoxReversingBeeps), "Reversing Beeps", config.EnableSnowfoxReversingBeeps,
                value => config.EnableSnowfoxReversingBeeps = value);

            AddPositionSlider(305, nameof(config.SnowfoxSpeedometerHorizontalAdjust), "Speedometer: Horizontal Adjust", config.SnowfoxSpeedometerHorizontalAdjust,
                false, value => config.SnowfoxSpeedometerHorizontalAdjust = value);
            AddPositionSlider(306, nameof(config.SnowfoxSpeedometerVerticalAdjust), "Speedometer: Vertical Adjust", config.SnowfoxSpeedometerVerticalAdjust,
                true, value => config.SnowfoxSpeedometerVerticalAdjust = value);
            AddScaleSlider(307, nameof(config.SnowfoxSpeedometerUiScale), "Speedometer: UI Scale", config.SnowfoxSpeedometerUiScale,
                value => config.SnowfoxSpeedometerUiScale = value);
            AddPositionSlider(308, nameof(config.SnowfoxHsiHorizontalAdjust), "HSI: Horizontal Adjust", config.SnowfoxHsiHorizontalAdjust,
                false, value => config.SnowfoxHsiHorizontalAdjust = value);
            AddPositionSlider(309, nameof(config.SnowfoxHsiVerticalAdjust), "HSI: Vertical Adjust", config.SnowfoxHsiVerticalAdjust,
                true, value => config.SnowfoxHsiVerticalAdjust = value);
            AddScaleSlider(310, nameof(config.SnowfoxHsiUiScale), "HSI: UI Scale", config.SnowfoxHsiUiScale,
                value => config.SnowfoxHsiUiScale = value);
            AddPositionSlider(311, nameof(config.SnowfoxTimeHorizontalAdjust), "Time/Weather: Horizontal Adjust", config.SnowfoxTimeHorizontalAdjust,
                false, value => config.SnowfoxTimeHorizontalAdjust = value);
            AddPositionSlider(312, nameof(config.SnowfoxTimeVerticalAdjust), "Time/Weather: Vertical Adjust", config.SnowfoxTimeVerticalAdjust,
                true, value => config.SnowfoxTimeVerticalAdjust = value);
            AddScaleSlider(313, nameof(config.SnowfoxTimeUiScale), "Time/Weather: UI Scale", config.SnowfoxTimeUiScale,
                value => config.SnowfoxTimeUiScale = value);

            AddOption(500, ModSliderOption.Create(nameof(config.ReversingAudioVolume), "Reversing Audio Volume",
                0.0f, 1.0f, config.ReversingAudioVolume, 0.1f, "{0:F2}", 0.05f),
                value => config.ReversingAudioVolume = value);
            AddToggle(501, nameof(config.DetailedLogging), "Detailed Logging", config.DetailedLogging, value =>
            {
                config.DetailedLogging = value;
                ModDebugLog.SetDetailedLoggingState(value);
            });
        }

        /// <summary>
        /// Builds vehicle settings beneath standard subheadings in one mod section.
        /// </summary>
        public override void BuildModOptions(uGUI_TabbedControlsPanel panel, int modsTabIndex, IReadOnlyCollection<OptionItem> options)
        {
            panel.AddHeading(modsTabIndex, MenuName);
            int previousSection = -1;
            foreach (KeyValuePair<int, OptionItem> entry in orderedOptions)
            {
                int section = entry.Key / 100;
                if (section != previousSection)
                {
                    // Stock game headings stay inside the single collapsible Nautilus mod section.
                    GameObject heading = panel.AddItem(modsTabIndex, panel.headingPrefab, GetSectionName(section));
                    heading.GetComponentInChildren<TextMeshProUGUI>().fontStyle |= FontStyles.Bold;
                    previousSection = section;
                }

                entry.Value.AddToPanel(panel, modsTabIndex);
            }
        }

        private static string GetSectionName(int section)
        {
            switch (section)
            {
                // "Seatruck" is the game's uppercase HUD label; "SeaTruck" is the vehicle name.
                case 1: return "SeaTruck";
                case 2: return "Prawn Suit";
                case 3: return "Snowfox";
                default: return "General";
            }
        }

        private void AddToggle(int order, string id, string label, bool value, Action<bool> setValue)
        {
            AddOption(order, ModToggleOption.Create(id, label, value), setValue);
        }

        private void AddVoiceChoice(int order, string id, ReversingVoice voice, Action<ReversingVoice> setValue)
        {
            ModChoiceOption<string> option = ModChoiceOption<string>.Create(id, "Reversing Voice", ReversingVoices,
                Mathf.Clamp((int)voice, 0, ReversingVoices.Length - 1));
            option.OnChanged += (sender, args) =>
            {
                setValue((ReversingVoice)args.Index);
                config.Save();
            };
            AddItem(option);
            orderedOptions.Add(order, option);
        }

        private void AddPositionSlider(int order, string id, string label, float value, bool vertical, Action<float> setValue)
        {
            string tooltip = vertical
                ? "Offset from the default position in UI units. Positive moves up."
                : "Offset from the default position in UI units. Positive moves right.";
            AddOption(order, ModSliderOption.Create(id, label, ModConfigFile.MinimumHudAdjust, ModConfigFile.MaximumHudAdjust,
                value, 0.0f, "{0:F0}", 1.0f, tooltip), setValue);
        }

        private void AddScaleSlider(int order, string id, string label, float value, Action<float> setValue)
        {
            AddOption(order, ModSliderOption.Create(id, label, ModConfigFile.MinimumHudScale, ModConfigFile.MaximumHudScale,
                value, 1.0f, "{0:F2}x", 0.05f, "Multiplier on the default size. Combines with any scaling already applied by the game."), setValue);
        }

        private void AddOption<T, TEventArgs>(int order, ModOption<T, TEventArgs> option, Action<T> setValue)
            where TEventArgs : ConfigOptionEventArgs<T>
        {
            option.OnChanged += (sender, args) =>
            {
                setValue(args.Value);
                config.Save();
            };
            AddItem(option);
            orderedOptions.Add(order, option);
        }
    }
}
