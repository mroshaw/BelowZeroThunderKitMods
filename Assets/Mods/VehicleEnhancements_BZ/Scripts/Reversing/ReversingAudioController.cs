using DaftAppleGames.ModTools;
using FMOD;
using FMODUnity;
using Nautilus.Handlers;
using Sirenix.OdinInspector;
using UnityEngine;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ.Reversing
{
    internal class ReversingAudioController : MonoBehaviour
    {
        [SerializeField, Required]
        private FMOD_CustomEmitter reversingBeepsEmitter;

        [SerializeField, Required]
        private FMOD_CustomEmitter thisSeaTruckIsReversingEmitter;

        [SerializeField, MinValue(0.0f)]
        private float reversingSpeedThreshold = 0.1f;

        [SerializeField]
        private Vector3 audioSourceOffset = new Vector3(0.0f, 0.0f, -3.0f);

        [SerializeField, MinValue(10.0f)]
        private float cabinLowPassCutoff = 1200.0f;

        [SerializeField, Range(1.0f, 10.0f)]
        private float cabinLowPassResonance = 1.0f;

        private EnhancedVehicle vehicle = EnhancedVehicle.Seatruck;
        private DSP reversingBeepsLowPass;
        private DSP thisSeaTruckIsReversingLowPass;
        private Channel reversingBeepsChannel;
        private Channel thisSeaTruckIsReversingChannel;
        private bool reversingBeepsLowPassAttached;
        private bool thisSeaTruckIsReversingLowPassAttached;
        private float appliedBeepsVolume = -1.0f;
        private float appliedVoiceVolume = -1.0f;

        internal void Configure(EnhancedVehicle selectedVehicle)
        {
            vehicle = selectedVehicle;
        }

        private void Awake()
        {
            if (!reversingBeepsEmitter || !thisSeaTruckIsReversingEmitter)
            {
                ModDebugLog.LogError("Could not find the vehicle reversing audio emitters.");
                enabled = false;
                return;
            }

            if (!ReversingBeepsFmodAsset || !ThisSeaTruckIsReversingFmodAsset)
            {
                ModDebugLog.LogError("The vehicle reversing FMOD assets are not available.");
                enabled = false;
                return;
            }

            ConfigureEmitter(reversingBeepsEmitter, ReversingBeepsFmodAsset);
            ConfigureEmitter(thisSeaTruckIsReversingEmitter, ThisSeaTruckIsReversingFmodAsset);
            if (vehicle != EnhancedVehicle.Snowfox)
            {
                CreateLowPassDsp(ref reversingBeepsLowPass);
                CreateLowPassDsp(ref thisSeaTruckIsReversingLowPass);
            }
        }

        private void Update()
        {
            if (!TryGetPilotedVehicleMotion(out Transform vehicleTransform, out Vector3 velocity))
            {
                StopEmitters();
                ReturnEmittersToHud();
                return;
            }

            PositionEmittersAtVehicle(vehicleTransform);

            bool isReversing = Vector3.Dot(velocity, vehicleTransform.forward) < -reversingSpeedThreshold;
            ReversingAudio selectedAudio = ConfigFile.GetReversingAudio(vehicle);

            bool playBeeps = isReversing &&
                             (selectedAudio == ReversingAudio.Beeps || selectedAudio == ReversingAudio.Both);
            bool playVoice = isReversing &&
                            (selectedAudio == ReversingAudio.ThisSeaTruckIsReversing ||
                             selectedAudio == ReversingAudio.Both);
            float volume = Mathf.Clamp01(ConfigFile.ReversingAudioVolume);

            SetEmitterPlaying(
                reversingBeepsEmitter,
                playBeeps,
                volume,
                ref appliedBeepsVolume,
                ref reversingBeepsLowPass,
                ref reversingBeepsChannel,
                ref reversingBeepsLowPassAttached);
            SetEmitterPlaying(
                thisSeaTruckIsReversingEmitter,
                playVoice,
                volume,
                ref appliedVoiceVolume,
                ref thisSeaTruckIsReversingLowPass,
                ref thisSeaTruckIsReversingChannel,
                ref thisSeaTruckIsReversingLowPassAttached);
        }

        private void OnDisable()
        {
            StopEmitters();
            ReturnEmittersToHud();
        }

        private void OnDestroy()
        {
            ReleaseLowPassDsp(
                ref reversingBeepsLowPass,
                ref reversingBeepsChannel,
                ref reversingBeepsLowPassAttached);
            ReleaseLowPassDsp(
                ref thisSeaTruckIsReversingLowPass,
                ref thisSeaTruckIsReversingChannel,
                ref thisSeaTruckIsReversingLowPassAttached);
        }

        private static void ConfigureEmitter(FMOD_CustomEmitter emitter, FMODAsset soundAsset)
        {
            emitter.followParent = true;
            emitter.playOnAwake = false;
            emitter.restartOnPlay = false;
            ModAudioUtils.ConfigureEmitter(emitter, soundAsset, ModDebugLog);
        }

        private bool TryGetPilotedVehicleMotion(out Transform vehicleTransform, out Vector3 velocity)
        {
            vehicleTransform = null;
            velocity = Vector3.zero;
            if (!VehicleMotion.TryGet(vehicle, out vehicleTransform, out velocity))
            {
                return false;
            }

            if (vehicle == EnhancedVehicle.Seatruck)
            {
                SeaTruckMotor seaTruckMotor = vehicleTransform.GetComponent<SeaTruckMotor>();
                return seaTruckMotor && seaTruckMotor.IsPiloted();
            }

            return true;
        }

        private void PositionEmittersAtVehicle(Transform vehicleTransform)
        {
            SetEmitterTransform(reversingBeepsEmitter.transform, vehicleTransform);
            SetEmitterTransform(thisSeaTruckIsReversingEmitter.transform, vehicleTransform);
        }

        private void SetEmitterTransform(Transform emitterTransform, Transform vehicleTransform)
        {
            emitterTransform.position = vehicleTransform.TransformPoint(audioSourceOffset);
            emitterTransform.rotation = vehicleTransform.rotation;
        }

        private void ReturnEmittersToHud()
        {
            if (reversingBeepsEmitter)
            {
                SetHudEmitterTransform(reversingBeepsEmitter.transform);
            }

            if (thisSeaTruckIsReversingEmitter)
            {
                SetHudEmitterTransform(thisSeaTruckIsReversingEmitter.transform);
            }
        }

        private void SetHudEmitterTransform(Transform emitterTransform)
        {
            emitterTransform.localPosition = Vector3.zero;
            emitterTransform.localRotation = Quaternion.identity;
        }

        private static void SetEmitterPlaying(
            FMOD_CustomEmitter emitter,
            bool shouldPlay,
            float volume,
            ref float appliedVolume,
            ref DSP lowPass,
            ref Channel soundChannel,
            ref bool lowPassAttached)
        {
            if (!shouldPlay)
            {
                DetachLowPassDsp(ref lowPass, ref soundChannel, ref lowPassAttached);

                if (emitter.playing)
                {
                    emitter.Stop();
                }

                appliedVolume = -1.0f;
                return;
            }

            if (!emitter.playing)
            {
                emitter.Play();
            }

            if (!soundChannel.hasHandle())
            {
                CustomSoundHandler.TryGetCustomSoundChannel(emitter.GetInstanceID(), out soundChannel);
            }

            if (!lowPassAttached && lowPass.hasHandle() && soundChannel.hasHandle())
            {
                if (soundChannel.addDSP(-3, lowPass) == RESULT.OK)
                {
                    lowPassAttached = true;
                }
            }

            if (!Mathf.Approximately(appliedVolume, volume) &&
                soundChannel.hasHandle())
            {
                if (soundChannel.setVolume(volume) == RESULT.OK)
                {
                    appliedVolume = volume;
                }
            }
        }

        private void CreateLowPassDsp(ref DSP lowPass)
        {
            RESULT result = RuntimeManager.CoreSystem.createDSPByType(DSP_TYPE.LOWPASS, out lowPass);
            if (result != RESULT.OK || !lowPass.hasHandle())
            {
                ModDebugLog.LogError($"Could not create the vehicle cabin low-pass DSP: {result}.");
                return;
            }

            lowPass.setParameterFloat((int)DSP_LOWPASS.CUTOFF, cabinLowPassCutoff);
            lowPass.setParameterFloat((int)DSP_LOWPASS.RESONANCE, cabinLowPassResonance);
        }

        private static void ReleaseLowPassDsp(
            ref DSP lowPass,
            ref Channel soundChannel,
            ref bool lowPassAttached)
        {
            DetachLowPassDsp(ref lowPass, ref soundChannel, ref lowPassAttached);

            if (lowPass.hasHandle())
            {
                lowPass.release();
                lowPass.clearHandle();
            }
        }

        private static void DetachLowPassDsp(
            ref DSP lowPass,
            ref Channel soundChannel,
            ref bool lowPassAttached)
        {
            if (lowPassAttached && soundChannel.hasHandle() && lowPass.hasHandle())
            {
                soundChannel.removeDSP(lowPass);
            }

            soundChannel.clearHandle();
            lowPassAttached = false;
        }

        private void StopEmitters()
        {
            if (reversingBeepsEmitter)
            {
                SetEmitterPlaying(
                    reversingBeepsEmitter,
                    false,
                    0.0f,
                    ref appliedBeepsVolume,
                    ref reversingBeepsLowPass,
                    ref reversingBeepsChannel,
                    ref reversingBeepsLowPassAttached);
            }

            if (thisSeaTruckIsReversingEmitter)
            {
                SetEmitterPlaying(
                    thisSeaTruckIsReversingEmitter,
                    false,
                    0.0f,
                    ref appliedVoiceVolume,
                    ref thisSeaTruckIsReversingLowPass,
                    ref thisSeaTruckIsReversingChannel,
                    ref thisSeaTruckIsReversingLowPassAttached);
            }
        }
    }
}
