using FMOD.Studio;
using UnityEngine;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    /// <summary>
    /// Plays the SeaTruck engine sound while the autopilot is moving the vehicle.
    /// </summary>
    internal class SeaTruckAutoPilotAudio : MonoBehaviour
    {
        private const float FullRpmSpeed = 5.0f;
        private const float FullTurnAngularSpeed = 1.0f;

        private SeaTruckAutoPilot autoPilot;
        private SeaTruckMotor motor;
        private Rigidbody mainRigidbody;
        private FMOD_CustomLoopingEmitter autoPilotEngineSound;
        private PARAMETER_ID velocityParameterIndex;
        private PARAMETER_ID depthParameterIndex;
        private PARAMETER_ID rpmParameterIndex;
        private PARAMETER_ID damagedParameterIndex;
        private PARAMETER_ID turnParameterIndex;
        private PARAMETER_ID upgradeParameterIndex;
        private bool isConfigured;
        private bool shouldPlay;

        private void Awake()
        {
            autoPilot = GetComponent<SeaTruckAutoPilot>();
            motor = GetComponent<SeaTruckMotor>();
            mainRigidbody = GetComponent<Rigidbody>();
            ConfigureEmitter();
        }

        private void OnEnable()
        {
            if (autoPilot)
            {
                autoPilot.onStateChanged.AddListener(AutoPilotStateChangedHandler);
            }
        }

        private void OnDisable()
        {
            if (autoPilot)
            {
                autoPilot.onStateChanged.RemoveListener(AutoPilotStateChangedHandler);
            }

            StopEngineSound();
        }

        private void Update()
        {
            if (!isConfigured)
            {
                return;
            }

            if (!shouldPlay || !IsPowered())
            {
                StopEngineSound();
                return;
            }

            if (!autoPilotEngineSound.playing)
            {
                StartEngineSound();
                return;
            }

            UpdateEngineSoundParameters();
        }

        private void ConfigureEmitter()
        {
            if (!motor || !motor.engineSound || !motor.engineSound.asset || !mainRigidbody)
            {
                ModDebugLog.LogWarning("SeaTruckAutoPilotAudio could not find the vanilla SeaTruck engine sound configuration.");
                return;
            }

            GameObject emitterObject = new GameObject("AutoPilotEngineSound");
            emitterObject.transform.SetParent(motor.engineSound.transform, false);

            autoPilotEngineSound = emitterObject.AddComponent<FMOD_CustomLoopingEmitter>();
            autoPilotEngineSound.followParent = true;
            autoPilotEngineSound.playOnAwake = false;
            autoPilotEngineSound.restartOnPlay = false;
            autoPilotEngineSound.SetAsset(motor.engineSound.asset);

            velocityParameterIndex = autoPilotEngineSound.GetParameterIndex("velocity");
            depthParameterIndex = autoPilotEngineSound.GetParameterIndex("depth");
            rpmParameterIndex = autoPilotEngineSound.GetParameterIndex("rpm");
            damagedParameterIndex = autoPilotEngineSound.GetParameterIndex("seatruck_damage");
            turnParameterIndex = autoPilotEngineSound.GetParameterIndex("turn");
            upgradeParameterIndex = autoPilotEngineSound.GetParameterIndex("seatruck_upgrade");
            isConfigured = true;
        }

        private void AutoPilotStateChangedHandler(AutoPilotState oldState, AutoPilotState newState)
        {
            shouldPlay = newState == AutoPilotState.Moving;
            if (shouldPlay)
            {
                StartEngineSound();
                return;
            }

            StopEngineSound();
        }

        private void StartEngineSound()
        {
            if (!isConfigured || !IsPowered())
            {
                return;
            }

            UpdateEngineSoundParameters();
            autoPilotEngineSound.Play();
            ModDebugLog.LogDebug("SeaTruck autopilot engine sound started.");
        }

        private void StopEngineSound()
        {
            if (!autoPilotEngineSound || !autoPilotEngineSound.playing)
            {
                return;
            }

            autoPilotEngineSound.Stop();
            ModDebugLog.LogDebug("SeaTruck autopilot engine sound stopped.");
        }

        private void UpdateEngineSoundParameters()
        {
            float speed = mainRigidbody.velocity.magnitude;
            Vector3 localAngularVelocity = transform.InverseTransformDirection(mainRigidbody.angularVelocity);
            float rpm = 0.5f + Mathf.Clamp01(speed / FullRpmSpeed) * 0.5f;
            float turn = Mathf.Clamp(localAngularVelocity.y / FullTurnAngularSpeed, -1.0f, 1.0f);
            float upgrade = (motor.powerEfficiencyFactor < 1.0f ? 1.0f : 0.0f) +
                            (motor.horsePowerUpgrade ? 2.0f : 0.0f);

            autoPilotEngineSound.SetParameterValue(velocityParameterIndex, speed);
            autoPilotEngineSound.SetParameterValue(depthParameterIndex, transform.position.y);
            autoPilotEngineSound.SetParameterValue(rpmParameterIndex, rpm);
            autoPilotEngineSound.SetParameterValue(turnParameterIndex, turn);
            autoPilotEngineSound.SetParameterValue(upgradeParameterIndex, upgrade);

            if (motor.liveMixin)
            {
                autoPilotEngineSound.SetParameterValue(damagedParameterIndex, 1.0f - motor.liveMixin.GetHealthFraction());
            }
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
