using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    /// <summary>
    /// Moves a SeaTruck by directly controlling its rigidbody velocity and torque.
    /// </summary>
    internal sealed class PhysicsMovement : IMovement
    {
        private const float MaximumAngularSpeed = 0.5f;
        private const float AngularSpeedPerDegree = 0.025f;

        private readonly Transform sourceTransform;
        private readonly Rigidbody rigidbody;

        /// <summary>
        /// Creates a physics movement strategy for a SeaTruck.
        /// </summary>
        internal PhysicsMovement(Transform sourceTransform, Rigidbody rigidbody)
        {
            this.sourceTransform = sourceTransform;
            this.rigidbody = rigidbody;
        }

        /// <summary>
        /// Gets the name used to identify this movement strategy in diagnostics.
        /// </summary>
        public string Name => "Physics";

        /// <summary>
        /// Gets whether the strategy must continuously maintain angular velocity after alignment.
        /// </summary>
        public bool MaintainsRotation => true;

        /// <summary>
        /// Gets whether connected rigidbodies must be isolated while this strategy is active.
        /// </summary>
        public bool RequiresRigidBodyIsolation => true;

        /// <summary>
        /// Moves the SeaTruck towards a world-space target.
        /// </summary>
        public void Move(Vector3 targetPosition, float targetSpeed)
        {
            Vector3 direction = targetPosition - rigidbody.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                rigidbody.velocity = Vector3.zero;
                return;
            }

            rigidbody.velocity = direction.normalized * targetSpeed;
        }

        /// <summary>
        /// Rotates the SeaTruck towards a world-space target.
        /// </summary>
        public void Rotate(Vector3 targetPosition, float rotationScale)
        {
            Vector3 targetDirection = targetPosition - sourceTransform.position;
            if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                rigidbody.angularVelocity = Vector3.zero;
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized, Vector3.up);
            Rotate(targetRotation, rotationScale);
        }

        /// <summary>
        /// Rotates the SeaTruck towards an exact world-space orientation.
        /// </summary>
        public void Rotate(Quaternion targetRotation, float rotationScale)
        {
            Quaternion rotationError = targetRotation * Quaternion.Inverse(rigidbody.rotation);
            float errorAngle;
            Vector3 errorAxis;
            rotationError.ToAngleAxis(out errorAngle, out errorAxis);
            if (errorAngle > 180.0f)
            {
                errorAngle -= 360.0f;
            }

            if (Mathf.Abs(errorAngle) <= 0.01f || errorAxis.sqrMagnitude <= Mathf.Epsilon)
            {
                rigidbody.angularVelocity = Vector3.zero;
                return;
            }

            Vector3 targetAngularVelocity = errorAxis.normalized * errorAngle * AngularSpeedPerDegree;
            targetAngularVelocity = Vector3.ClampMagnitude(targetAngularVelocity, MaximumAngularSpeed);
            float smoothing = Mathf.Clamp01(rotationScale * Time.fixedDeltaTime);
            rigidbody.angularVelocity = Vector3.Lerp(rigidbody.angularVelocity, targetAngularVelocity, smoothing);
        }

        /// <summary>
        /// Stops motion produced by this strategy.
        /// </summary>
        public void Stop()
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
    }
}
