using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    /// <summary>
    /// Moves a SeaTruck using thrust and steering equivalent to the vanilla player input model.
    /// </summary>
    internal sealed class InputMovement : IMovement
    {
        private const float BrakingMultiplier = 1.5f;
        private const float DefaultNavigationRotationScale = 15.0f;
        private const float MaximumAngularSpeed = 0.75f;
        private const float AngularSpeedPerDegree = 0.025f;

        private readonly Transform sourceTransform;
        private readonly Rigidbody rigidbody;
        private readonly SeaTruckMotor motor;
        private readonly SeaTruckSegment segment;

        /// <summary>
        /// Creates an input-model movement strategy for a SeaTruck.
        /// </summary>
        internal InputMovement(Transform sourceTransform, Rigidbody rigidbody, SeaTruckMotor motor)
        {
            this.sourceTransform = sourceTransform;
            this.rigidbody = rigidbody;
            this.motor = motor;
            segment = motor.truckSegment;
        }

        /// <summary>
        /// Gets the name used to identify this movement strategy in diagnostics.
        /// </summary>
        public string Name => "Input";

        /// <summary>
        /// Gets whether the strategy must continuously maintain angular velocity after alignment.
        /// </summary>
        public bool MaintainsRotation => true;

        /// <summary>
        /// Gets whether connected rigidbodies must be isolated while this strategy is active.
        /// </summary>
        public bool RequiresRigidBodyIsolation => false;

        /// <summary>
        /// Moves the SeaTruck towards a world-space target.
        /// </summary>
        public void Move(Vector3 targetPosition, float targetSpeed)
        {
            if (!IsPowered() || motor.IsBusyAnimating())
            {
                return;
            }

            Vector3 direction = targetPosition - rigidbody.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            float acceleration = motor.acceleration / Mathf.Max(1.0f, GetWeight() * 0.35f);
            Vector3 desiredVelocity = direction.normalized * targetSpeed;
            Vector3 velocityError = desiredVelocity - rigidbody.velocity;
            float maximumAcceleration = acceleration * BrakingMultiplier;
            Vector3 inputAcceleration = Vector3.ClampMagnitude(velocityError / Time.fixedDeltaTime,
                maximumAcceleration);
            rigidbody.AddForce(inputAcceleration, ForceMode.Acceleration);

            if (motor.relay && inputAcceleration.sqrMagnitude > Mathf.Epsilon)
            {
                motor.relay.ConsumeEnergy(Time.fixedDeltaTime * motor.powerEfficiencyFactor * 0.12f, out float consumed);
            }
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

            float steeringScale = Mathf.Clamp01(rotationScale / DefaultNavigationRotationScale);
            Vector3 desiredAngularVelocity = errorAxis.normalized * errorAngle * AngularSpeedPerDegree;
            desiredAngularVelocity = Vector3.ClampMagnitude(desiredAngularVelocity, MaximumAngularSpeed);
            Vector3 angularVelocityCorrection =
                (desiredAngularVelocity - rigidbody.angularVelocity) * steeringScale;

            rigidbody.AddTorque(angularVelocityCorrection, ForceMode.VelocityChange);
        }

        /// <summary>
        /// Stops motion produced by this strategy.
        /// </summary>
        public void Stop()
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        private float GetWeight()
        {
            return segment.GetWeight() + segment.GetAttachedWeight() * (motor.horsePowerUpgrade ? 0.65f : 0.8f);
        }

        private bool IsPowered()
        {
            if (!motor.requiresPower)
            {
                return true;
            }

            return motor.relay && motor.relay.IsPowered();
        }
    }
}
