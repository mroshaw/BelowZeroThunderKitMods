using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    /// <summary>
    /// Defines how navigation translates and rotates a SeaTruck.
    /// </summary>
    internal interface IMovement
    {
        /// <summary>
        /// Gets the name used to identify this movement strategy in diagnostics.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets whether the strategy must continuously maintain angular velocity after alignment.
        /// </summary>
        bool MaintainsRotation { get; }

        /// <summary>
        /// Gets whether connected rigidbodies must be isolated while this strategy is active.
        /// </summary>
        bool RequiresRigidBodyIsolation { get; }

        /// <summary>
        /// Moves the SeaTruck towards a world-space target.
        /// </summary>
        void Move(Vector3 targetPosition, float targetSpeed);

        /// <summary>
        /// Rotates the SeaTruck towards a world-space target.
        /// </summary>
        void Rotate(Vector3 targetPosition, float rotationScale);

        /// <summary>
        /// Rotates the SeaTruck towards an exact world-space orientation.
        /// </summary>
        void Rotate(Quaternion targetRotation, float rotationScale);

        /// <summary>
        /// Stops motion produced by this strategy.
        /// </summary>
        void Stop();
    }
}
