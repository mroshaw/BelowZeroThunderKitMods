using TMPro;
using Sirenix.OdinInspector;
using UnityEngine;
using static DaftAppleGames.SeaTruckEnhancements_BZ.SeaTruckEnhancementsPlugin_BZ;

namespace DaftAppleGames.SeaTruckEnhancements_BZ.Speedometer
{
    internal class SpeedometerController : MonoBehaviour
    {
        [SerializeField, Required] private GameObject speedometerRoot;

        [SerializeField, Required] private RectTransform needle;

        [SerializeField, Required] private TextMeshProUGUI speedText;
        
        [SerializeField] private float minimumNeedleAngle = 180.0f;

        [SerializeField] private float maximumNeedleAngle = -80.0f;

        [SerializeField, MinValue(0.1f)] private float maximumDisplayedSpeed = 20.0f;

        [SerializeField, MinValue(0.0f)] private float needleSmoothTime = 0.12f;

        private float smoothedSpeed;
        private float speedVelocity;
        private int lastDisplayedSpeedHundredths = int.MinValue;

        private void Awake()
        {
            if (!speedometerRoot || !needle || !speedText)
            {
                ModDebugLog.LogError("Could not find the SeaTruck speedometer objects.");
                enabled = false;
                return;
            }

            speedometerRoot.SetActive(ConfigFile.EnableSpeedometer);
            SetDisplay(0.0f, 0.0f);
        }

        private void Update()
        {
            bool showSpeedometer = ConfigFile.EnableSpeedometer;
            if (speedometerRoot.activeSelf != showSpeedometer)
            {
                speedometerRoot.SetActive(showSpeedometer);
            }

            if (!showSpeedometer)
            {
                return;
            }

            float forwardSpeed = GetSignedForwardSpeed();
            float absoluteForwardSpeed = Mathf.Abs(forwardSpeed);
            smoothedSpeed = Mathf.SmoothDamp(
                smoothedSpeed,
                absoluteForwardSpeed,
                ref speedVelocity,
                needleSmoothTime);
            SetDisplay(forwardSpeed, smoothedSpeed);
        }

        private float GetSignedForwardSpeed()
        {
            Player player = Player.main;
            if (!player)
            {
                return 0.0f;
            }

            SeaTruckMotor seaTruckMotor = player.GetComponentInParent<SeaTruckMotor>();
            if (!seaTruckMotor || !seaTruckMotor.useRigidbody)
            {
                return 0.0f;
            }

            float forwardSpeed = Vector3.Dot(
                seaTruckMotor.useRigidbody.velocity,
                seaTruckMotor.transform.forward);
            return forwardSpeed;
        }

        private void SetDisplay(float textSpeed, float needleSpeed)
        {
            int speedHundredths = Mathf.RoundToInt(textSpeed * 100.0f);
            if (speedHundredths != lastDisplayedSpeedHundredths)
            {
                lastDisplayedSpeedHundredths = speedHundredths;
                int absoluteSpeedHundredths = Mathf.Abs(speedHundredths);
                int wholeSpeed = absoluteSpeedHundredths / 100;
                int decimalPart = absoluteSpeedHundredths % 100;
                if (speedHundredths < 0)
                {
                    speedText.SetText("-{0:00}.{1:00}<color=#FFFFFF00>-</color>", wholeSpeed, decimalPart);
                }
                else
                {
                    speedText.SetText(
                        "<color=#FFFFFF00>-</color>{0:00}.{1:00}<color=#FFFFFF00>-</color>",
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
    }
}
