using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Navigation
{
    internal class MotorTest : MonoBehaviour
    {
        [SerializeField] private Transform currentTarget;
        #if UNITY_EDITOR
        private DummyMotor _motor;
#else
        private SeaTruckMotor _motor;
#endif
        /// <summary>
        /// Initialise the component
        /// </summary>
        private void Awake()
        {
#if UNITY_EDITOR
            _motor = GetComponent<DummyMotor>();
#else
            _motor = GetComponent<SeaTruckMotor>();
#endif
        }

        private void FixedUpdate()
        {
            if (!currentTarget)
            {
                return;
            }
            
            MoveMotor(currentTarget.position);
        }
        
        private void Update()
        {
            if (!currentTarget)
            {
                return;
            }
            
            RotateMotor(currentTarget.position);
        }

        private void MoveMotor(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            
            direction = direction.normalized;

            float num = 1f / Mathf.Max(1f, _motor.GetWeight() * 0.35f) * _motor.acceleration;

            _motor.useRigidbody.AddForce(num * direction, ForceMode.Acceleration);
            _motor.StabilizeRoll();

        }
        
        private void RotateMotor(Vector3 targetPosition)
        {
            _motor.UpdateDrag();

            Vector2 vector = GetRotateNormal(transform, targetPosition);
            vector.x = Mathf.Clamp(vector.x, -30f, 30f);
            vector.y = Mathf.Clamp(vector.y, -30f, 30f);
            Int2 @int;
            if (vector.x > 0f)
            {
                @int.x = 1;
            }
            else if (vector.x < 0f)
            {
                @int.x = -1;
            }
            else
            {
                @int.x = 0;
            }

            if (vector.y > 0f)
            {
                @int.y = -1;
            }
            else if (vector.y < 0f)
            {
                @int.y = 1;
            }
            else
            {
                @int.y = 0;
            }

            float num = 1f / Mathf.Max(1f, _motor.GetWeight() * 0.8f) * _motor.steeringMultiplier;
            _motor.useRigidbody.AddTorque(base.transform.up * vector.x * num, ForceMode.VelocityChange);
            _motor.useRigidbody.AddTorque(base.transform.right * -vector.y * num, ForceMode.VelocityChange);
            _motor.useRigidbody.AddTorque(base.transform.forward * -vector.x * num * 0.02f,
                ForceMode.VelocityChange);
            
#if !UNITY_EDITOR
            if (_motor.engineSound)
            {
                _motor.engineSound.Play();
                _motor.engineSound.SetParameterValue(_motor.velocityParamIndex, _motor.useRigidbody.velocity.magnitude);
                _motor.engineSound.SetParameterValue(_motor.depthParamIndex, base.transform.position.y);
                _motor.engineSound.SetParameterValue(_motor.rpmParamIndex,
                        (GameInput.GetMoveDirection().z + 1f) * 0.5f);
                _motor.engineSound.SetParameterValue(_motor.turnParamIndex,
                        Mathf.Clamp(GameInput.GetLookDelta().x * 0.3f, -1f, 1f));
                _motor.engineSound.SetParameterValue(_motor.upgradeParamIndex,
                        (float)(((_motor.powerEfficiencyFactor < 1f) ? 1 : 0) + (_motor.horsePowerUpgrade ? 2 : 0)));
                    if (_motor.liveMixin)
                    {
                        _motor.engineSound.SetParameterValue(_motor.damagedParamIndex,
                            1f - _motor.liveMixin.GetHealthFraction());
                    }
            }
#endif
        }
        
        public static Vector2 GetRotateNormal(Transform source, Vector3 targetPosition)
        {

            Vector3 toTarget = (targetPosition - source.position).normalized;

            // --- Horizontal (XZ plane) ---
            Vector3 forwardXZ = new Vector3(source.forward.x, 0f, source.forward.z).normalized;
            Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z).normalized;

            float dotX = Vector3.Dot(forwardXZ, toTargetXZ);
            float xFactor = (1f - dotX) * 0.5f;

            // --- Vertical (Y plane) ---
            Vector3 forwardY = new Vector3(0f, source.forward.y, source.forward.z).normalized;
            Vector3 toTargetY = new Vector3(0f, toTarget.y, toTarget.z).normalized;

            float dotY = Vector3.Dot(forwardY, toTargetY);
            float yFactor = (1f - dotY) * 0.5f;

            return new Vector2(xFactor, yFactor);
        }
    }
}