using TMPro;
using Sirenix.OdinInspector;
using UnityEngine;
using static DaftAppleGames.SeaTruckEnhancements_BZ.SeaTruckEnhancementsPlugin_BZ;

namespace DaftAppleGames.SeaTruckEnhancements_BZ.UI
{
    internal class SpeedometerController : MonoBehaviour
    {
        [SerializeField, Required]
        private RectTransform needle;

        [SerializeField, Required]
        private TextMeshProUGUI speedText;

        [SerializeField]
        private float minimumNeedleAngle = 180.0f;

        [SerializeField]
        private float maximumNeedleAngle = -80.0f;

        [SerializeField, MinValue(0.1f)]
        private float maximumDisplayedSpeed = 20.0f;

        [SerializeField, MinValue(0.0f)]
        private float needleSmoothTime = 0.12f;

        private float smoothedSpeed;
        private float speedVelocity;
        private int lastDisplayedSpeedTenths = int.MinValue;

        private void Awake()
        {
            if (!needle || !speedText)
            {
                ModDebugLog.LogError("Could not find the SeaTruck speedometer Needle or SpeedText object.");
                enabled = false;
                return;
            }

            SetDisplay(0.0f, 0.0f);
        }

        private void Update()
        {
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
            int speedTenths = Mathf.RoundToInt(textSpeed * 10.0f);
            if (speedTenths != lastDisplayedSpeedTenths)
            {
                lastDisplayedSpeedTenths = speedTenths;
                int absoluteSpeedTenths = Mathf.Abs(speedTenths);
                int wholeSpeed = absoluteSpeedTenths / 10;
                int tenthsDigit = absoluteSpeedTenths % 10;
                if (speedTenths < 0)
                {
                    speedText.SetText("-{0}.{1} m/s", wholeSpeed, tenthsDigit);
                }
                else
                {
                    speedText.SetText("{0}.{1} m/s", wholeSpeed, tenthsDigit);
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
