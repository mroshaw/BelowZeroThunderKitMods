using UnityEngine;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ.Integration
{
    // The native HUD also runs while walking inside a submarine. Only control visibility of our child.
    [DefaultExecutionOrder(-100)]
    internal class VehicleFrameworkCustomHudController : MonoBehaviour
    {
        private GameObject indicators;

        internal void Configure(GameObject indicatorChild)
        {
            indicators = indicatorChild;
            UpdateIndicators();
        }

        private void Update()
        {
            UpdateIndicators();
        }

        private void OnDestroy()
        {
            // Release bound vehicle delegates when the native HUD/session is destroyed.
            VehicleMotion.ClearCustomVehicle();
        }

        private void UpdateIndicators()
        {
            if (!indicators)
            {
                return;
            }

            bool isControlling = VehicleFrameworkIntegration.IsMonitorMode &&
                                 VehicleMotion.TryGetCustomVehicle(out _, out _);
            bool hasFeatures = ConfigFile.IsSpeedometerEnabled(EnhancedVehicle.CustomVehicle) ||
                               ConfigFile.IsHsiEnabled(EnhancedVehicle.CustomVehicle) ||
                               ConfigFile.IsTimeAndWeatherEnabled(EnhancedVehicle.CustomVehicle) ||
                               ConfigFile.AreReversingBeepsEnabled(EnhancedVehicle.CustomVehicle) ||
                               ConfigFile.GetReversingVoice(EnhancedVehicle.CustomVehicle) != ReversingVoice.None;
            bool show = isControlling && hasFeatures;
            if (indicators.activeSelf != show)
            {
                indicators.SetActive(show);
            }
        }
    }
}
