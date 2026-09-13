using UnityEngine;

namespace DaftAppleGames.SeaTruckSpeedMod_BZ
{
    /// <summary>
    /// Removes a registered Seatruck from the mod history when its GameObject is destroyed.
    /// </summary>
    internal sealed class SeaTruckHistoryTracker : MonoBehaviour
    {
        private SeaTruckMotor seaTruck;

        /// <summary>
        /// Associates this tracker with its Seatruck motor.
        /// </summary>
        internal void Initialize(SeaTruckMotor seaTruckMotor)
        {
            seaTruck = seaTruckMotor;
        }

        private void OnDestroy()
        {
            SeaTruckHistory.RemoveSeaTruck(seaTruck);
        }
    }
}
