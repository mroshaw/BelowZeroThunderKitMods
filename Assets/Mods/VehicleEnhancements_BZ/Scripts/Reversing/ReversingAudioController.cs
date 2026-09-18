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
        private FMOD_CustomEmitter reversingVoiceEmitter;

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
        private DSP reversingVoiceLowPass;
        private Channel reversingBeepsChannel;
        private Channel reversingVoiceChannel;
        private bool reversingBeepsLowPassAttached;
        private bool reversingVoiceLowPassAttached;
        private float appliedBeepsVolume = -1.0f;
        private float appliedVoiceVolume = -1.0f;
        private ReversingVoice configuredVoice = ReversingVoice.None;

        internal void Configure(EnhancedVehicle selectedVehicle)
        {
            vehicle = selectedVehicle;
        }

        private void Awake()
        {
            if (!reversingBeepsEmitter || !reversingVoiceEmitter)
            {
                ModDebugLog.LogError("Could not find the vehicle reversing audio emitters.");
                enabled = false;
                return;
            }

            if (!ReversingBeepsFmodAsset)
            {
                ModDebugLog.LogError("The vehicle reversing FMOD assets are not available.");
                enabled = false;
                return;
            }

            ConfigureEmitter(reversingBeepsEmitter, ReversingBeepsFmodAsset);
            UpdateVoiceSelection();
            if (vehicle != EnhancedVehicle.Snowfox)
            {
                CreateLowPassDsp(ref reversingBeepsLowPass);
                CreateLowPassDsp(ref reversingVoiceLowPass);
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
            UpdateVoiceSelection();

            bool isReversing = Vector3.Dot(velocity, vehicleTransform.forward) < -reversingSpeedThreshold;
            bool playBeeps = isReversing && ConfigFile.AreReversingBeepsEnabled(vehicle);
            bool playVoice = isReversing && configuredVoice != ReversingVoice.None &&
                             GetReversingVoiceAsset(configuredVoice);
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
                reversingVoiceEmitter,
                playVoice,
                volume,
                ref appliedVoiceVolume,
                ref reversingVoiceLowPass,
                ref reversingVoiceChannel,
                ref reversingVoiceLowPassAttached);
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
                ref reversingVoiceLowPass,
                ref reversingVoiceChannel,
                ref reversingVoiceLowPassAttached);
        }

        private static void ConfigureEmitter(FMOD_CustomEmitter emitter, FMODAsset soundAsset)
        {
            emitter.followParent = true;
            emitter.playOnAwake = false;
            emitter.restartOnPlay = false;
            ModAudioUtils.ConfigureEmitter(emitter, soundAsset, ModDebugLog);
        }

        private void UpdateVoiceSelection()
        {
            ReversingVoice selectedVoice = ConfigFile.GetReversingVoice(vehicle);
            if (configuredVoice == selectedVoice)
            {
                return;
            }

            SetEmitterPlaying(
                reversingVoiceEmitter,
                false,
                0.0f,
                ref appliedVoiceVolume,
                ref reversingVoiceLowPass,
                ref reversingVoiceChannel,
                ref reversingVoiceLowPassAttached);

            configuredVoice = selectedVoice;
            if (selectedVoice == ReversingVoice.None)
            {
                return;
            }

            FMODAsset voiceAsset = GetReversingVoiceAsset(selectedVoice);
            if (!voiceAsset)
            {
                ModDebugLog.LogError($"The selected reversing voice '{selectedVoice}' is unavailable.");
                return;
            }

            ConfigureEmitter(reversingVoiceEmitter, voiceAsset);
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
            SetEmitterTransform(reversingVoiceEmitter.transform, vehicleTransform);
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

            if (reversingVoiceEmitter)
            {
                SetHudEmitterTransform(reversingVoiceEmitter.transform);
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

            if (reversingVoiceEmitter)
            {
                SetEmitterPlaying(
                    reversingVoiceEmitter,
                    false,
                    0.0f,
                    ref appliedVoiceVolume,
                    ref reversingVoiceLowPass,
                    ref reversingVoiceChannel,
                    ref reversingVoiceLowPassAttached);
            }
        }
    }
}
