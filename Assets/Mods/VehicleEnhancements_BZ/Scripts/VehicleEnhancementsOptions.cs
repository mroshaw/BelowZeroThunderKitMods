using System;
using System.Collections.Generic;
using Nautilus.Options;
using DaftAppleGames.VehicleEnhancements_BZ.Integration;
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

        private readonly Dictionary<int, string> vehicleHeadings = new Dictionary<int, string>();
        private readonly List<int> generatedOrders = new List<int>();
        private bool applyingCustomVehicleLayout;

        internal VehicleEnhancementsOptions(ModConfigFile config) : base(MenuName)
        {
            this.config = config;
            ModConfigFile defaults = new ModConfigFile();
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

            AddLocationChoice(105, nameof(config.SeatruckSpeedometerLocation), "Speedometer: Location", config.SeatruckSpeedometerLocation,
                value => config.SeatruckSpeedometerLocation = value);
            AddPositionSlider(106, nameof(config.SeatruckSpeedometerHorizontalAdjust), "Speedometer: Horizontal Adjust", config.SeatruckSpeedometerHorizontalAdjust,
                false, value => config.SeatruckSpeedometerHorizontalAdjust = value, defaults.SeatruckSpeedometerHorizontalAdjust);
            AddPositionSlider(107, nameof(config.SeatruckSpeedometerVerticalAdjust), "Speedometer: Vertical Adjust", config.SeatruckSpeedometerVerticalAdjust,
                true, value => config.SeatruckSpeedometerVerticalAdjust = value, defaults.SeatruckSpeedometerVerticalAdjust);
            AddScaleSlider(108, nameof(config.SeatruckSpeedometerUiScale), "Speedometer: UI Scale", config.SeatruckSpeedometerUiScale,
                value => config.SeatruckSpeedometerUiScale = value, defaults.SeatruckSpeedometerUiScale);
            AddLocationChoice(109, nameof(config.SeatruckHsiLocation), "HSI: Location", config.SeatruckHsiLocation,
                value => config.SeatruckHsiLocation = value);
            AddPositionSlider(110, nameof(config.SeatruckHsiHorizontalAdjust), "HSI: Horizontal Adjust", config.SeatruckHsiHorizontalAdjust,
                false, value => config.SeatruckHsiHorizontalAdjust = value, defaults.SeatruckHsiHorizontalAdjust);
            AddPositionSlider(111, nameof(config.SeatruckHsiVerticalAdjust), "HSI: Vertical Adjust", config.SeatruckHsiVerticalAdjust,
                true, value => config.SeatruckHsiVerticalAdjust = value, defaults.SeatruckHsiVerticalAdjust);
            AddScaleSlider(112, nameof(config.SeatruckHsiUiScale), "HSI: UI Scale", config.SeatruckHsiUiScale,
                value => config.SeatruckHsiUiScale = value, defaults.SeatruckHsiUiScale);
            AddLocationChoice(113, nameof(config.SeatruckTimeLocation), "Time/Weather: Location", config.SeatruckTimeLocation,
                value => config.SeatruckTimeLocation = value);
            AddPositionSlider(114, nameof(config.SeatruckTimeHorizontalAdjust), "Time/Weather: Horizontal Adjust", config.SeatruckTimeHorizontalAdjust,
                false, value => config.SeatruckTimeHorizontalAdjust = value, defaults.SeatruckTimeHorizontalAdjust);
            AddPositionSlider(115, nameof(config.SeatruckTimeVerticalAdjust), "Time/Weather: Vertical Adjust", config.SeatruckTimeVerticalAdjust,
                true, value => config.SeatruckTimeVerticalAdjust = value, defaults.SeatruckTimeVerticalAdjust);
            AddScaleSlider(116, nameof(config.SeatruckTimeUiScale), "Time/Weather: UI Scale", config.SeatruckTimeUiScale,
                value => config.SeatruckTimeUiScale = value, defaults.SeatruckTimeUiScale);

            AddToggle(117, nameof(config.EnableReversingCamera), "Reversing Camera", config.EnableReversingCamera,
                value => config.EnableReversingCamera = value);
            AddLocationChoice(118, nameof(config.SeatruckCameraLocation), "Camera: Location", config.SeatruckCameraLocation,
                value => config.SeatruckCameraLocation = value);
            AddPositionSlider(119, nameof(config.SeatruckCameraHorizontalAdjust), "Camera: Horizontal Adjust", config.SeatruckCameraHorizontalAdjust,
                false, value => config.SeatruckCameraHorizontalAdjust = value, defaults.SeatruckCameraHorizontalAdjust);
            AddPositionSlider(120, nameof(config.SeatruckCameraVerticalAdjust), "Camera: Vertical Adjust", config.SeatruckCameraVerticalAdjust,
                true, value => config.SeatruckCameraVerticalAdjust = value, defaults.SeatruckCameraVerticalAdjust);
            AddScaleSlider(121, nameof(config.SeatruckCameraUiScale), "Camera: UI Scale", config.SeatruckCameraUiScale,
                value => config.SeatruckCameraUiScale = value, defaults.SeatruckCameraUiScale);
            AddToggle(122, nameof(config.EnableBuildingInside), "Allow Building Inside", config.EnableBuildingInside,
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

            AddLocationChoice(205, nameof(config.PrawnSpeedometerLocation), "Speedometer: Location", config.PrawnSpeedometerLocation,
                value => config.PrawnSpeedometerLocation = value);
            AddPositionSlider(206, nameof(config.PrawnSpeedometerHorizontalAdjust), "Speedometer: Horizontal Adjust", config.PrawnSpeedometerHorizontalAdjust,
                false, value => config.PrawnSpeedometerHorizontalAdjust = value, defaults.PrawnSpeedometerHorizontalAdjust);
            AddPositionSlider(207, nameof(config.PrawnSpeedometerVerticalAdjust), "Speedometer: Vertical Adjust", config.PrawnSpeedometerVerticalAdjust,
                true, value => config.PrawnSpeedometerVerticalAdjust = value, defaults.PrawnSpeedometerVerticalAdjust);
            AddScaleSlider(208, nameof(config.PrawnSpeedometerUiScale), "Speedometer: UI Scale", config.PrawnSpeedometerUiScale,
                value => config.PrawnSpeedometerUiScale = value, defaults.PrawnSpeedometerUiScale);
            AddLocationChoice(209, nameof(config.PrawnHsiLocation), "HSI: Location", config.PrawnHsiLocation,
                value => config.PrawnHsiLocation = value);
            AddPositionSlider(210, nameof(config.PrawnHsiHorizontalAdjust), "HSI: Horizontal Adjust", config.PrawnHsiHorizontalAdjust,
                false, value => config.PrawnHsiHorizontalAdjust = value, defaults.PrawnHsiHorizontalAdjust);
            AddPositionSlider(211, nameof(config.PrawnHsiVerticalAdjust), "HSI: Vertical Adjust", config.PrawnHsiVerticalAdjust,
                true, value => config.PrawnHsiVerticalAdjust = value, defaults.PrawnHsiVerticalAdjust);
            AddScaleSlider(212, nameof(config.PrawnHsiUiScale), "HSI: UI Scale", config.PrawnHsiUiScale,
                value => config.PrawnHsiUiScale = value, defaults.PrawnHsiUiScale);
            AddLocationChoice(213, nameof(config.PrawnTimeLocation), "Time/Weather: Location", config.PrawnTimeLocation,
                value => config.PrawnTimeLocation = value);
            AddPositionSlider(214, nameof(config.PrawnTimeHorizontalAdjust), "Time/Weather: Horizontal Adjust", config.PrawnTimeHorizontalAdjust,
                false, value => config.PrawnTimeHorizontalAdjust = value, defaults.PrawnTimeHorizontalAdjust);
            AddPositionSlider(215, nameof(config.PrawnTimeVerticalAdjust), "Time/Weather: Vertical Adjust", config.PrawnTimeVerticalAdjust,
                true, value => config.PrawnTimeVerticalAdjust = value, defaults.PrawnTimeVerticalAdjust);
            AddScaleSlider(216, nameof(config.PrawnTimeUiScale), "Time/Weather: UI Scale", config.PrawnTimeUiScale,
                value => config.PrawnTimeUiScale = value, defaults.PrawnTimeUiScale);

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

            AddLocationChoice(305, nameof(config.SnowfoxSpeedometerLocation), "Speedometer: Location", config.SnowfoxSpeedometerLocation,
                value => config.SnowfoxSpeedometerLocation = value);
            AddPositionSlider(306, nameof(config.SnowfoxSpeedometerHorizontalAdjust), "Speedometer: Horizontal Adjust", config.SnowfoxSpeedometerHorizontalAdjust,
                false, value => config.SnowfoxSpeedometerHorizontalAdjust = value, defaults.SnowfoxSpeedometerHorizontalAdjust);
            AddPositionSlider(307, nameof(config.SnowfoxSpeedometerVerticalAdjust), "Speedometer: Vertical Adjust", config.SnowfoxSpeedometerVerticalAdjust,
                true, value => config.SnowfoxSpeedometerVerticalAdjust = value, defaults.SnowfoxSpeedometerVerticalAdjust);
            AddScaleSlider(308, nameof(config.SnowfoxSpeedometerUiScale), "Speedometer: UI Scale", config.SnowfoxSpeedometerUiScale,
                value => config.SnowfoxSpeedometerUiScale = value, defaults.SnowfoxSpeedometerUiScale);
            AddLocationChoice(309, nameof(config.SnowfoxHsiLocation), "HSI: Location", config.SnowfoxHsiLocation,
                value => config.SnowfoxHsiLocation = value);
            AddPositionSlider(310, nameof(config.SnowfoxHsiHorizontalAdjust), "HSI: Horizontal Adjust", config.SnowfoxHsiHorizontalAdjust,
                false, value => config.SnowfoxHsiHorizontalAdjust = value, defaults.SnowfoxHsiHorizontalAdjust);
            AddPositionSlider(311, nameof(config.SnowfoxHsiVerticalAdjust), "HSI: Vertical Adjust", config.SnowfoxHsiVerticalAdjust,
                true, value => config.SnowfoxHsiVerticalAdjust = value, defaults.SnowfoxHsiVerticalAdjust);
            AddScaleSlider(312, nameof(config.SnowfoxHsiUiScale), "HSI: UI Scale", config.SnowfoxHsiUiScale,
                value => config.SnowfoxHsiUiScale = value, defaults.SnowfoxHsiUiScale);
            AddLocationChoice(313, nameof(config.SnowfoxTimeLocation), "Time/Weather: Location", config.SnowfoxTimeLocation,
                value => config.SnowfoxTimeLocation = value);
            AddPositionSlider(314, nameof(config.SnowfoxTimeHorizontalAdjust), "Time/Weather: Horizontal Adjust", config.SnowfoxTimeHorizontalAdjust,
                false, value => config.SnowfoxTimeHorizontalAdjust = value, defaults.SnowfoxTimeHorizontalAdjust);
            AddPositionSlider(315, nameof(config.SnowfoxTimeVerticalAdjust), "Time/Weather: Vertical Adjust", config.SnowfoxTimeVerticalAdjust,
                true, value => config.SnowfoxTimeVerticalAdjust = value, defaults.SnowfoxTimeVerticalAdjust);
            AddScaleSlider(316, nameof(config.SnowfoxTimeUiScale), "Time/Weather: UI Scale", config.SnowfoxTimeUiScale,
                value => config.SnowfoxTimeUiScale = value, defaults.SnowfoxTimeUiScale);

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
            RebuildVehicleOptions();
            panel.AddHeading(modsTabIndex, MenuName);
            List<OptionItem> generalOptions = new List<OptionItem>();
            int previousSection = -1;
            foreach (KeyValuePair<int, OptionItem> entry in orderedOptions)
            {
                int section = entry.Key / 100;
                if (section == 5)
                {
                    generalOptions.Add(entry.Value);
                    continue;
                }

                if (section != previousSection)
                {
                    string name = vehicleHeadings.TryGetValue(section, out string vehicleName) ? vehicleName : GetSectionName(section);
                    AddSectionHeading(panel, modsTabIndex, name);
                    previousSection = section;
                }

                entry.Value.AddToPanel(panel, modsTabIndex);
            }

            AddSectionHeading(panel, modsTabIndex, "General");
            foreach (OptionItem option in generalOptions)
            {
                option.AddToPanel(panel, modsTabIndex);
            }
        }

        private static void AddSectionHeading(uGUI_TabbedControlsPanel panel, int modsTabIndex, string name)
        {
            // Stock game headings stay inside the single collapsible Nautilus mod section.
            GameObject heading = panel.AddItem(modsTabIndex, panel.headingPrefab, name);
            heading.GetComponentInChildren<TextMeshProUGUI>().fontStyle |= FontStyles.Bold;
        }

        private void RebuildVehicleOptions()
        {
            foreach (int order in generatedOrders)
            {
                RemoveItem(orderedOptions[order].Id);
                orderedOptions.Remove(order);
            }

            generatedOrders.Clear();
            vehicleHeadings.Clear();
            if (!VehicleFrameworkIntegration.IsEnabled)
            {
                return;
            }

            int section = 6;
            foreach (KeyValuePair<string, string> vehicle in VehicleFrameworkIntegration.GetRegisteredVehicles())
            {
                vehicleHeadings.Add(section, vehicle.Value);
                AddVehicleProfile(section * 100, vehicle.Key, config.GetOrCreateCustomVehicleSettings(vehicle.Key));
                section++;
            }
        }

        private void AddVehicleProfile(int order, string identifier, CustomVehicleSettings settings)
        {
            CustomVehicleSettings defaults = new CustomVehicleSettings();
            string prefix = "CustomVehicleProfile." + identifier + ".";
            AddToggle(order, prefix + nameof(settings.EnableSpeedometer), "Speedometer", settings.EnableSpeedometer,
                value => settings.EnableSpeedometer = value);
            AddToggle(order + 1, prefix + nameof(settings.EnableHsi), "HSI", settings.EnableHsi,
                value => settings.EnableHsi = value);
            AddToggle(order + 2, prefix + nameof(settings.EnableTime), "Time and Weather", settings.EnableTime,
                value => settings.EnableTime = value);
            AddVoiceChoice(order + 3, prefix + nameof(settings.ReversingVoice), settings.ReversingVoice,
                value => settings.ReversingVoice = value);
            AddToggle(order + 4, prefix + nameof(settings.EnableReversingBeeps), "Reversing Beeps", settings.EnableReversingBeeps,
                value => settings.EnableReversingBeeps = value);
            AddLocationChoice(order + 5, prefix + nameof(settings.SpeedometerLocation), "Speedometer: Location", settings.SpeedometerLocation,
                value => settings.SpeedometerLocation = value);
            AddPositionSlider(order + 6, prefix + nameof(settings.SpeedometerHorizontalAdjust), "Speedometer: Horizontal Adjust",
                settings.SpeedometerHorizontalAdjust, false, value => settings.SpeedometerHorizontalAdjust = value, defaults.SpeedometerHorizontalAdjust);
            AddPositionSlider(order + 7, prefix + nameof(settings.SpeedometerVerticalAdjust), "Speedometer: Vertical Adjust",
                settings.SpeedometerVerticalAdjust, true, value => settings.SpeedometerVerticalAdjust = value, defaults.SpeedometerVerticalAdjust);
            AddScaleSlider(order + 8, prefix + nameof(settings.SpeedometerUiScale), "Speedometer: UI Scale",
                settings.SpeedometerUiScale, value => settings.SpeedometerUiScale = value, defaults.SpeedometerUiScale);
            AddLocationChoice(order + 9, prefix + nameof(settings.HsiLocation), "HSI: Location", settings.HsiLocation,
                value => settings.HsiLocation = value);
            AddPositionSlider(order + 10, prefix + nameof(settings.HsiHorizontalAdjust), "HSI: Horizontal Adjust",
                settings.HsiHorizontalAdjust, false, value => settings.HsiHorizontalAdjust = value, defaults.HsiHorizontalAdjust);
            AddPositionSlider(order + 11, prefix + nameof(settings.HsiVerticalAdjust), "HSI: Vertical Adjust",
                settings.HsiVerticalAdjust, true, value => settings.HsiVerticalAdjust = value, defaults.HsiVerticalAdjust);
            AddScaleSlider(order + 12, prefix + nameof(settings.HsiUiScale), "HSI: UI Scale",
                settings.HsiUiScale, value => settings.HsiUiScale = value, defaults.HsiUiScale);
            AddLocationChoice(order + 13, prefix + nameof(settings.TimeLocation), "Time/Weather: Location", settings.TimeLocation,
                value => settings.TimeLocation = value);
            AddPositionSlider(order + 14, prefix + nameof(settings.TimeHorizontalAdjust), "Time/Weather: Horizontal Adjust",
                settings.TimeHorizontalAdjust, false, value => settings.TimeHorizontalAdjust = value, defaults.TimeHorizontalAdjust);
            AddPositionSlider(order + 15, prefix + nameof(settings.TimeVerticalAdjust), "Time/Weather: Vertical Adjust",
                settings.TimeVerticalAdjust, true, value => settings.TimeVerticalAdjust = value, defaults.TimeVerticalAdjust);
            AddScaleSlider(order + 16, prefix + nameof(settings.TimeUiScale), "Time/Weather: UI Scale",
                settings.TimeUiScale, value => settings.TimeUiScale = value, defaults.TimeUiScale);

            ModButtonOption applyLayout = ModButtonOption.Create(prefix + "ApplyLayoutToAll",
                "Apply layout to all custom", args => ApplyLayoutToAllCustomVehicles(settings),
                "Copy this vehicle's instrument locations, position offsets and UI scales to all other custom vehicles.");
            AddItem(applyLayout);
            orderedOptions.Add(order + 17, applyLayout);

            for (int offset = 0; offset < 18; offset++)
            {
                generatedOrders.Add(order + offset);
            }
        }

        private void ApplyLayoutToAllCustomVehicles(CustomVehicleSettings source)
        {
            foreach (CustomVehicleSettings settings in config.CustomVehicleProfiles.Values)
            {
                settings.CopyLayoutFrom(source);
            }

            // Refresh open controls without saving the config for every slider and choice callback.
            applyingCustomVehicleLayout = true;
            try
            {
                foreach (int section in vehicleHeadings.Keys)
                {
                    int order = section * 100;
                    RefreshLocationChoice(order + 5, source.SpeedometerLocation);
                    RefreshSlider(order + 6, source.SpeedometerHorizontalAdjust);
                    RefreshSlider(order + 7, source.SpeedometerVerticalAdjust);
                    RefreshSlider(order + 8, source.SpeedometerUiScale);
                    RefreshLocationChoice(order + 9, source.HsiLocation);
                    RefreshSlider(order + 10, source.HsiHorizontalAdjust);
                    RefreshSlider(order + 11, source.HsiVerticalAdjust);
                    RefreshSlider(order + 12, source.HsiUiScale);
                    RefreshLocationChoice(order + 13, source.TimeLocation);
                    RefreshSlider(order + 14, source.TimeHorizontalAdjust);
                    RefreshSlider(order + 15, source.TimeVerticalAdjust);
                    RefreshSlider(order + 16, source.TimeUiScale);
                }
            }
            finally
            {
                applyingCustomVehicleLayout = false;
                config.Save();
            }
        }

        private void RefreshLocationChoice(int order, HudLocation location)
        {
            ModChoiceOption<string> option = (ModChoiceOption<string>)orderedOptions[order];
            if (option.Choice)
            {
                option.Choice.value = (int)location;
            }
        }

        private void RefreshSlider(int order, float value)
        {
            ModSliderOption option = (ModSliderOption)orderedOptions[order];
            option.Value = value;
            if (option.OptionGameObject)
            {
                uGUI_SnappingSlider slider = option.OptionGameObject.GetComponentInChildren<uGUI_SnappingSlider>(true);
                if (slider)
                {
                    slider.unsnappedValue = value;
                }
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

        private void AddLocationChoice(int order, string id, string label, HudLocation location, Action<HudLocation> setValue)
        {
            ModChoiceOption<string> option = ModChoiceOption<string>.Create(id, label,
                new[] { "Left", "Center", "Right" }, Mathf.Clamp((int)location, 0, 2));
            option.OnChanged += (sender, args) =>
            {
                setValue((HudLocation)args.Index);
                if (!applyingCustomVehicleLayout)
                {
                    config.Save();
                }
            };
            AddItem(option);
            orderedOptions.Add(order, option);
        }

        private void AddPositionSlider(int order, string id, string label, float value, bool vertical, Action<float> setValue, float defaultValue)
        {
            string tooltip = vertical
                ? "Offset from the selected location in UI units. Positive moves up."
                : "Offset from the selected location in UI units. Positive moves right.";
            AddOption(order, ModSliderOption.Create(id, label, ModConfigFile.MinimumHudAdjust, ModConfigFile.MaximumHudAdjust,
                value, defaultValue, "{0:F1}", 1.0f, tooltip), setValue);
        }

        private void AddScaleSlider(int order, string id, string label, float value, Action<float> setValue, float defaultValue)
        {
            AddOption(order, ModSliderOption.Create(id, label, ModConfigFile.MinimumHudScale, ModConfigFile.MaximumHudScale,
                value, defaultValue, "{0:F2}x", 0.05f, "Scale within the selected location. Inherits any scaling applied by the game."), setValue);
        }

        private void AddOption<T, TEventArgs>(int order, ModOption<T, TEventArgs> option, Action<T> setValue)
            where TEventArgs : ConfigOptionEventArgs<T>
        {
            option.OnChanged += (sender, args) =>
            {
                setValue(args.Value);
                if (!applyingCustomVehicleLayout)
                {
                    config.Save();
                }
            };
            AddItem(option);
            orderedOptions.Add(order, option);
        }
    }
}
