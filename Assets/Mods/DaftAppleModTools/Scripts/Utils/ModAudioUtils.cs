using FMOD;
using Nautilus.Extensions;
using Nautilus.Handlers;
using Nautilus.Utility;
using UnityEngine;

namespace DaftAppleGames.ModUtils
{
    /// <summary>
    /// Wrapper around Unity and Nautilus audio utils
    /// </summary>
    public class ModAudioUtils
    {
        /// <summary>
        /// Configures the given FMOD CustomEmitter
        /// </summary>
        public static void ConfigureEmitter(FMOD_CustomEmitter emitter, string audioClipName, string busPath, float volume, ModAssetBundleUtils assetBundleUtils, ModLog modLog)
        {
            modLog.LogDebug($"Configuring FMOD emitter: {emitter.gameObject.name}");
            RegisterSound(audioClipName, busPath, assetBundleUtils, modLog, 0.1f, 15.0f, 0);
            FMODAsset newAsset = AudioUtils.GetFmodAsset(audioClipName);
            emitter.SetAsset(newAsset);
            SetEmitterVolume(emitter, volume, modLog);
            modLog.LogDebug($"Configured emitter done!");
        }

        /// <summary>
        /// Registers a new AudioClip
        /// </summary>
        public static void RegisterSound(string clipName, string bus, ModAssetBundleUtils assetBundleUtils, ModLog modLog, float minDistance = 10f,
            float maxDistance = 200f, float fadeDuration = 0)
        {
            modLog.LogDebug($"Registering new sound clip: {clipName}");
            var sound = AudioUtils.CreateSound(assetBundleUtils.GetObjectFromAssetBundle<AudioClip>(clipName) as AudioClip,
                maxDistance >= 0 ? AudioUtils.StandardSoundModes_3D : AudioUtils.StandardSoundModes_2D);
            if (maxDistance >= 0)
                sound.set3DMinMaxDistance(minDistance, maxDistance);

            if (fadeDuration > 0)
            {
                sound.AddFadeOut(fadeDuration);
            }
            CustomSoundHandler.RegisterCustomSound(clipName, sound, bus);
            modLog.LogDebug($"Register clip done!");
        }

        /// <summary>
        /// Sets the volume on the given FMOD emitter
        /// </summary>
        public static void SetEmitterVolume(FMOD_CustomEmitter emitter, float volume, ModLog modLog)
        {
            modLog.LogDebug($"Setting volume on emitter: {emitter.gameObject.name} to {volume}");
            if (!emitter.evt.hasHandle())
            {
                emitter.CacheEventInstance();
            }

            if (!emitter.evt.hasHandle())
            {
                modLog.LogDebug($"FMOD Emitter has no handle!");
                return;
            }

            RESULT result = emitter.evt.getVolume(out float currentVolume, out float finalVolume);
            result = emitter.evt.setVolume(volume);
            modLog.LogDebug($"Result of SetVolume is: {result.ToString()}");
        }
    }
}