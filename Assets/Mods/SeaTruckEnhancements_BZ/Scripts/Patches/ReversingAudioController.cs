using DaftAppleGames.ModTools;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
using static DaftAppleGames.SeaTruckEnhancements_BZ.SeaTruckEnhancementsPlugin_BZ;

namespace DaftAppleGames.SeaTruckEnhancements_BZ.Audio
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

        private SeaTruckMotor attachedMotor;
        private DSP reversingBeepsLowPass;
        private DSP thisSeaTruckIsReversingLowPass;
        private ChannelGroup reversingBeepsChannelGroup;
        private ChannelGroup thisSeaTruckIsReversingChannelGroup;
        private bool reversingBeepsLowPassAttached;
        private bool thisSeaTruckIsReversingLowPassAttached;

        private void Awake()
        {
            if (!reversingBeepsEmitter || !thisSeaTruckIsReversingEmitter)
            {
                ModDebugLog.LogError("Could not find the SeaTruck reversing audio emitters.");
                enabled = false;
                return;
            }

            if (!ReversingBeepsFmodAsset || !ThisSeaTruckIsReversingFmodAsset)
            {
                ModDebugLog.LogError("The SeaTruck reversing FMOD assets are not available.");
                enabled = false;
                return;
            }

            ConfigureEmitter(reversingBeepsEmitter, ReversingBeepsFmodAsset);
            ConfigureEmitter(thisSeaTruckIsReversingEmitter, ThisSeaTruckIsReversingFmodAsset);
            CreateLowPassDsp(ref reversingBeepsLowPass);
            CreateLowPassDsp(ref thisSeaTruckIsReversingLowPass);
        }

        private void Update()
        {
            SeaTruckMotor seaTruckMotor = GetPilotedSeaTruckMotor();
            if (!seaTruckMotor)
            {
                StopEmitters();
                ReturnEmittersToHud();
                return;
            }

            AttachEmittersToSeaTruck(seaTruckMotor);

            bool isReversing = GetSignedForwardSpeed(seaTruckMotor) < -reversingSpeedThreshold;
            ReversingAudio selectedAudio = ConfigFile.ReversingAudio;

            bool playBeeps = isReversing &&
                             (selectedAudio == ReversingAudio.Beeps || selectedAudio == ReversingAudio.Both);
            bool playVoice = isReversing &&
                            (selectedAudio == ReversingAudio.ThisSeaTruckIsReversing ||
                             selectedAudio == ReversingAudio.Both);

            SetEmitterPlaying(
                reversingBeepsEmitter,
                playBeeps,
                ref reversingBeepsLowPass,
                ref reversingBeepsChannelGroup,
                ref reversingBeepsLowPassAttached);
            SetEmitterPlaying(
                thisSeaTruckIsReversingEmitter,
                playVoice,
                ref thisSeaTruckIsReversingLowPass,
                ref thisSeaTruckIsReversingChannelGroup,
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
                ref reversingBeepsChannelGroup,
                ref reversingBeepsLowPassAttached);
            ReleaseLowPassDsp(
                ref thisSeaTruckIsReversingLowPass,
                ref thisSeaTruckIsReversingChannelGroup,
                ref thisSeaTruckIsReversingLowPassAttached);
        }

        private static void ConfigureEmitter(FMOD_CustomEmitter emitter, FMODAsset soundAsset)
        {
            emitter.followParent = true;
            emitter.playOnAwake = false;
            emitter.restartOnPlay = false;
            ModAudioUtils.ConfigureEmitter(emitter, soundAsset, ModDebugLog);
        }

        private static SeaTruckMotor GetPilotedSeaTruckMotor()
        {
            Player player = Player.main;
            if (!player)
            {
                return null;
            }

            SeaTruckMotor seaTruckMotor = player.GetComponentInParent<SeaTruckMotor>();
            return seaTruckMotor && seaTruckMotor.IsPiloted() ? seaTruckMotor : null;
        }

        private void AttachEmittersToSeaTruck(SeaTruckMotor seaTruckMotor)
        {
            if (attachedMotor == seaTruckMotor)
            {
                return;
            }

            SetEmitterTransform(reversingBeepsEmitter.transform, seaTruckMotor.transform);
            SetEmitterTransform(thisSeaTruckIsReversingEmitter.transform, seaTruckMotor.transform);
            attachedMotor = seaTruckMotor;
        }

        private void SetEmitterTransform(Transform emitterTransform, Transform parent)
        {
            emitterTransform.SetParent(parent, false);
            emitterTransform.localPosition = audioSourceOffset;
            emitterTransform.localRotation = Quaternion.identity;
            emitterTransform.localScale = Vector3.one;
        }

        private void ReturnEmittersToHud()
        {
            if (!attachedMotor)
            {
                return;
            }

            SetHudEmitterTransform(reversingBeepsEmitter.transform);
            SetHudEmitterTransform(thisSeaTruckIsReversingEmitter.transform);
            attachedMotor = null;
        }

        private void SetHudEmitterTransform(Transform emitterTransform)
        {
            emitterTransform.SetParent(transform, false);
            emitterTransform.localPosition = Vector3.zero;
            emitterTransform.localRotation = Quaternion.identity;
            emitterTransform.localScale = Vector3.one;
        }

        private static float GetSignedForwardSpeed(SeaTruckMotor seaTruckMotor)
        {
            if (!seaTruckMotor.useRigidbody)
            {
                return 0.0f;
            }

            return Vector3.Dot(seaTruckMotor.useRigidbody.velocity, seaTruckMotor.transform.forward);
        }

        private static void SetEmitterPlaying(
            FMOD_CustomEmitter emitter,
            bool shouldPlay,
            ref DSP lowPass,
            ref ChannelGroup channelGroup,
            ref bool lowPassAttached)
        {
            if (!shouldPlay)
            {
                DetachLowPassDsp(ref lowPass, ref channelGroup, ref lowPassAttached);

                if (emitter.playing)
                {
                    emitter.Stop();
                }
                return;
            }

            if (!emitter.playing)
            {
                emitter.Play();
            }

            if (!lowPassAttached && lowPass.hasHandle())
            {
                EventInstance eventInstance = emitter.GetEventInstance();
                if (eventInstance.hasHandle() &&
                    eventInstance.getChannelGroup(out channelGroup) == RESULT.OK &&
                    channelGroup.hasHandle() &&
                    channelGroup.addDSP(-3, lowPass) == RESULT.OK)
                {
                    lowPassAttached = true;
                }
            }
        }

        private void CreateLowPassDsp(ref DSP lowPass)
        {
            RESULT result = RuntimeManager.CoreSystem.createDSPByType(DSP_TYPE.LOWPASS, out lowPass);
            if (result != RESULT.OK || !lowPass.hasHandle())
            {
                ModDebugLog.LogError($"Could not create the SeaTruck cabin low-pass DSP: {result}.");
                return;
            }

            lowPass.setParameterFloat((int)DSP_LOWPASS.CUTOFF, cabinLowPassCutoff);
            lowPass.setParameterFloat((int)DSP_LOWPASS.RESONANCE, cabinLowPassResonance);
        }

        private static void ReleaseLowPassDsp(
            ref DSP lowPass,
            ref ChannelGroup channelGroup,
            ref bool lowPassAttached)
        {
            DetachLowPassDsp(ref lowPass, ref channelGroup, ref lowPassAttached);

            if (lowPass.hasHandle())
            {
                lowPass.release();
                lowPass.clearHandle();
            }
        }

        private static void DetachLowPassDsp(
            ref DSP lowPass,
            ref ChannelGroup channelGroup,
            ref bool lowPassAttached)
        {
            if (lowPassAttached && channelGroup.hasHandle() && lowPass.hasHandle())
            {
                channelGroup.removeDSP(lowPass);
            }

            channelGroup.clearHandle();
            lowPassAttached = false;
        }

        private void StopEmitters()
        {
            if (reversingBeepsEmitter)
            {
                SetEmitterPlaying(
                    reversingBeepsEmitter,
                    false,
                    ref reversingBeepsLowPass,
                    ref reversingBeepsChannelGroup,
                    ref reversingBeepsLowPassAttached);
            }

            if (thisSeaTruckIsReversingEmitter)
            {
                SetEmitterPlaying(
                    thisSeaTruckIsReversingEmitter,
                    false,
                    ref thisSeaTruckIsReversingLowPass,
                    ref thisSeaTruckIsReversingChannelGroup,
                    ref thisSeaTruckIsReversingLowPassAttached);
            }
        }
    }
}
