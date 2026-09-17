using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ.Hsi
{
    internal class HsiController : MonoBehaviour
    {
        [SerializeField, Required] private GameObject inclinometerRoot;

        [SerializeField, Required] private RectTransform rollHinge;

        [SerializeField, Required] private TextMeshProUGUI pitchText;

        [SerializeField, Required] private TextMeshProUGUI rollText;

        [SerializeField, MinValue(0.1f)] private float pitchUnitsPerDegree = 2.6f;

        [SerializeField, MinValue(0.0f)] private float smoothTime = 0.12f;

        private Vector2 hingeInitialPosition;
        private float smoothedPitch;
        private float smoothedRoll;
        private float pitchVelocity;
        private float rollVelocity;
        private int lastDisplayedPitchDegrees = int.MinValue;
        private int lastDisplayedRollDegrees = int.MinValue;
        private EnhancedVehicle vehicle = EnhancedVehicle.Seatruck;

        internal void Configure(EnhancedVehicle selectedVehicle)
        {
            vehicle = selectedVehicle;
        }

        private void Awake()
        {
            if (!inclinometerRoot || !rollHinge || !pitchText || !rollText)
            {
                ModDebugLog.LogError("Could not find the SeaTruck inclinometer objects.");
                enabled = false;
                return;
            }

            hingeInitialPosition = rollHinge.anchoredPosition;
            inclinometerRoot.SetActive(ConfigFile.IsHsiEnabled(vehicle));
            SetDisplay(0.0f, 0.0f);
            SetNumericDisplay(0.0f, 0.0f);
        }

        private void Update()
        {
            bool showInclinometer = ConfigFile.IsHsiEnabled(vehicle);
            if (inclinometerRoot.activeSelf != showInclinometer)
            {
                inclinometerRoot.SetActive(showInclinometer);
            }

            if (!showInclinometer)
            {
                return;
            }

            float pitch = 0.0f;
            float roll = 0.0f;
            if (VehicleMotion.TryGet(vehicle, out Transform vehicleTransform, out Vector3 velocity))
            {
                HsiCalculator.Calculate(
                    vehicleTransform.forward,
                    vehicleTransform.up,
                    out pitch,
                    out roll);
            }

            smoothedPitch = Mathf.SmoothDamp(smoothedPitch, pitch, ref pitchVelocity, smoothTime);
            smoothedRoll = Mathf.SmoothDampAngle(smoothedRoll, roll, ref rollVelocity, smoothTime);
            SetDisplay(smoothedPitch, smoothedRoll);
            SetNumericDisplay(pitch, roll);
        }

        private void SetDisplay(float pitch, float roll)
        {
            rollHinge.anchoredPosition = new Vector2(
                hingeInitialPosition.x,
                hingeInitialPosition.y - pitch * pitchUnitsPerDegree);
            rollHinge.localRotation = Quaternion.Euler(0.0f, 0.0f, roll);
        }

        private void SetNumericDisplay(float pitch, float roll)
        {
            int pitchDegrees = Mathf.RoundToInt(pitch);
            if (pitchDegrees != lastDisplayedPitchDegrees)
            {
                lastDisplayedPitchDegrees = pitchDegrees;
                SetAngleText(pitchText, pitchDegrees);
            }

            int rollDegrees = Mathf.RoundToInt(roll);
            if (rollDegrees != lastDisplayedRollDegrees)
            {
                lastDisplayedRollDegrees = rollDegrees;
                SetAngleText(rollText, rollDegrees);
            }
        }

        private static void SetAngleText(TextMeshProUGUI angleText, int angleDegrees)
        {
            int absoluteDegrees = Mathf.Abs(angleDegrees);
            if (angleDegrees < 0)
            {
                angleText.SetText("-{0}°", absoluteDegrees);
            }
            else
            {
                angleText.SetText("+{0}°", absoluteDegrees);
            }
        }
    }
}
