using UnityEngine;

namespace DaftAppleGames.VehicleEnhancements_BZ.Hsi
{
    /// <summary>
    /// Calculates SeaTruck pitch and roll relative to the world horizon.
    /// </summary>
    internal static class HsiCalculator
    {
        /// <summary>
        /// Returns nose-up pitch and right-side-down roll in signed degrees.
        /// </summary>
        internal static void Calculate(Vector3 forward, Vector3 up, out float pitch, out float roll)
        {
            forward.Normalize();
            up.Normalize();

            pitch = Mathf.Asin(Mathf.Clamp(forward.y, -1.0f, 1.0f)) * Mathf.Rad2Deg;

            Vector3 levelUp = Vector3.ProjectOnPlane(Vector3.up, forward);
            if (levelUp.sqrMagnitude < 0.000001f)
            {
                // Roll has no horizon reference when the nose points straight up or down.
                roll = 0.0f;
                return;
            }

            roll = -Vector3.SignedAngle(levelUp, up, forward);
        }
    }
}
