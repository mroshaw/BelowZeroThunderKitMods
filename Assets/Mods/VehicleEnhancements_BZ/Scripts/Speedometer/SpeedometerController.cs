using TMPro;
using Sirenix.OdinInspector;
using UnityEngine;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ.Speedometer
{
    internal class SpeedometerController : MonoBehaviour
    {
        [SerializeField, Required] private GameObject speedometerRoot;

        [SerializeField, Required] private RectTransform needle;

        [SerializeField, Required] private TextMeshProUGUI speedText;

        [SerializeField, Required] private RectTransform verticalNeedle;

        [SerializeField, Required] private TextMeshProUGUI verticalSpeedText;
        
        [SerializeField] private float minimumNeedleAngle = 180.0f;

        [SerializeField] private float maximumNeedleAngle = -80.0f;

        [SerializeField, MinValue(0.1f)] private float maximumDisplayedSpeed = 20.0f;

        [SerializeField, MinValue(0.1f)] private float maximumDisplayedVerticalSpeed = 10.0f;

        [SerializeField] private float minimumVerticalNeedleAngle = 180.0f;

        [SerializeField] private float maximumVerticalNeedleAngle = 0.0f;

        [SerializeField, MinValue(0.0f)] private float needleSmoothTime = 0.12f;

        private float smoothedSpeed;
        private float speedVelocity;
        private float smoothedVerticalSpeed;
        private float verticalSpeedVelocity;
        private int lastDisplayedSpeedTenths = int.MinValue;
        private int lastDisplayedVerticalSpeedTenths = int.MinValue;
        private EnhancedVehicle vehicle = EnhancedVehicle.Seatruck;

        internal void Configure(EnhancedVehicle selectedVehicle)
        {
            vehicle = selectedVehicle;
        }

        private void Awake()
        {
            if (!speedometerRoot || !needle || !speedText || !verticalNeedle || !verticalSpeedText)
            {
                ModDebugLog.LogError("Could not find the SeaTruck speedometer objects.");
                enabled = false;
                return;
            }

            speedometerRoot.SetActive(ConfigFile.IsSpeedometerEnabled(vehicle));
            SetDisplay(0.0f, 0.0f);
            SetVerticalDisplay(0.0f, 0.0f);
        }

        private void Update()
        {
            bool showSpeedometer = ConfigFile.IsSpeedometerEnabled(vehicle);
            if (speedometerRoot.activeSelf != showSpeedometer)
            {
                speedometerRoot.SetActive(showSpeedometer);
            }

            if (!showSpeedometer)
            {
                return;
            }

            GetSignedSpeeds(out float forwardSpeed, out float verticalSpeed);
            float absoluteForwardSpeed = Mathf.Abs(forwardSpeed);
            smoothedSpeed = Mathf.SmoothDamp(
                smoothedSpeed,
                absoluteForwardSpeed,
                ref speedVelocity,
                needleSmoothTime);
            SetDisplay(forwardSpeed, smoothedSpeed);
            smoothedVerticalSpeed = Mathf.SmoothDamp(
                smoothedVerticalSpeed,
                verticalSpeed,
                ref verticalSpeedVelocity,
                needleSmoothTime);
            SetVerticalDisplay(verticalSpeed, smoothedVerticalSpeed);
        }

        private void GetSignedSpeeds(out float forwardSpeed, out float verticalSpeed)
        {
            forwardSpeed = 0.0f;
            verticalSpeed = 0.0f;
            if (!VehicleMotion.TryGet(vehicle, out Transform vehicleTransform, out Vector3 velocity))
            {
                return;
            }

            forwardSpeed = Vector3.Dot(velocity, vehicleTransform.forward);
            verticalSpeed = velocity.y;
        }

        private void SetDisplay(float textSpeed, float needleSpeed)
        {
            int speedTenths = Mathf.RoundToInt(textSpeed * 10.0f);
            if (speedTenths != lastDisplayedSpeedTenths)
            {
                lastDisplayedSpeedTenths = speedTenths;
                int absoluteSpeedTenths = Mathf.Abs(speedTenths);
                int wholeSpeed = absoluteSpeedTenths / 10;
                int decimalPart = absoluteSpeedTenths % 10;
                if (speedTenths < 0)
                {
                    speedText.SetText("-{0:00}.{1:0}<color=#FFFFFF00>-</color>", wholeSpeed, decimalPart);
                }
                else
                {
                    speedText.SetText(
                        "<color=#FFFFFF00>-</color>{0:00}.{1:0}<color=#FFFFFF00>-</color>",
                        wholeSpeed,
                        decimalPart);
                }
            }

            float normalizedSpeed = Mathf.Clamp01(needleSpeed / maximumDisplayedSpeed);
            float needleAngle = Mathf.Lerp(
                minimumNeedleAngle,
                maximumNeedleAngle,
                normalizedSpeed);
            needle.localRotation = Quaternion.Euler(0.0f, 0.0f, needleAngle);
        }

        private void SetVerticalDisplay(float textSpeed, float needleSpeed)
        {
            int speedTenths = Mathf.RoundToInt(textSpeed * 10.0f);
            if (speedTenths != lastDisplayedVerticalSpeedTenths)
            {
                lastDisplayedVerticalSpeedTenths = speedTenths;
                int absoluteSpeedTenths = Mathf.Abs(speedTenths);
                int wholeSpeed = absoluteSpeedTenths / 10;
                int decimalPart = absoluteSpeedTenths % 10;
                if (speedTenths > 0)
                {
                    verticalSpeedText.SetText("+{0:00}.{1:0}", wholeSpeed, decimalPart);
                }
                else if (speedTenths < 0)
                {
                    verticalSpeedText.SetText("-{0:00}.{1:0}", wholeSpeed, decimalPart);
                }
                else
                {
                    verticalSpeedText.SetText("<color=#FFFFFF00>+</color>{0:00}.{1:0}", wholeSpeed, decimalPart);
                }
            }

            float normalizedSpeed = Mathf.InverseLerp(
                -maximumDisplayedVerticalSpeed,
                maximumDisplayedVerticalSpeed,
                needleSpeed);
            float needleAngle = Mathf.Lerp(
                minimumVerticalNeedleAngle,
                maximumVerticalNeedleAngle,
                normalizedSpeed);
            verticalNeedle.localRotation = Quaternion.Euler(0.0f, 0.0f, needleAngle);
        }
    }
}
