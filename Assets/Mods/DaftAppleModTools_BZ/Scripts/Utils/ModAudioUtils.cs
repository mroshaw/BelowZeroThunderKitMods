using System;
using System.Reflection;
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
        public static void ConfigureEmitter(FMOD_CustomEmitter emitter, FMODAsset fmodAsset, ModLog modLog)
        {
            modLog.LogDebug($"Configuring FMOD emitter: {emitter.gameObject.name}");
            emitter.SetAsset(fmodAsset);
            modLog.LogDebug($"Configured emitter done!");
        }

        /// <summary>
        /// Registers a new AudioClip
        /// </summary>
        public static void RegisterSound(string clipName, string bus, ModAssetBundleUtils assetBundleUtils, ModLog modLog, float minDistance = 10f,
            float maxDistance = 200f, float fadeDuration = 0, bool loop = false)
        {
            modLog.LogDebug($"Registering new sound clip: {clipName}");
            MODE fmodMode = maxDistance >= 0 ? AudioUtils.StandardSoundModes_3D : AudioUtils.StandardSoundModes_2D;
            fmodMode = loop ? fmodMode | MODE.LOOP_NORMAL : fmodMode;
            
            Sound sound = AudioUtils.CreateSound(assetBundleUtils.GetObjectFromAssetBundle<AudioClip>(clipName) as AudioClip, fmodMode);
            if (maxDistance >= 0)
                sound.set3DMinMaxDistance(minDistance, maxDistance);

            if (fadeDuration > 0)
            {
                sound.AddFadeOut(fadeDuration);
            }

            if (loop)
            {
                sound.getLength(out uint soundLength, TIMEUNIT.PCM);
                modLog.LogDebug($"Sound length: {soundLength}");
                sound.setLoopPoints(0, TIMEUNIT.PCM, soundLength, TIMEUNIT.PCMFRACTION);
            }

            sound.getMode(out MODE soundMode);
            sound.getLoopPoints(out uint loopStart, TIMEUNIT.PCMFRACTION, out uint loopEnd,
                TIMEUNIT.PCMFRACTION);
            modLog.LogDebug($"Sound mode after loop: {soundMode}. Loop start: {loopStart}, Loop end: {loopEnd}");
            
            CustomSoundHandler.RegisterCustomSound(clipName, sound, bus);
            modLog.LogDebug($"Register clip done!");
        }

        /// <summary>
        /// Sets the volume on the given FMOD emitter
        /// </summary>
        public static void SetEmitterVolume(FMOD_CustomEmitter emitter, float volume, ModLog modLog)
        {
            modLog.LogDebug($"Setting volume on emitter: {emitter.gameObject.name} to {volume}");
            emitter.evt.getDescription(out FMOD.Studio.EventDescription eventDescription);
            eventDescription.createInstance(out FMOD.Studio.EventInstance instance);
            instance.setVolume(volume);
            instance.start();
        }
        
        public static void DumpEmitterFields(FMOD_CustomEmitter emitter, ModLog modLog)
        {
            var t = emitter.GetType();
            modLog.LogDebug($"Emitter type: {t.FullName}");
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                try
                {
                    object val = f.GetValue(emitter);
                    if (val == null) { modLog.LogDebug($"{f.Name} = null"); continue; }
                    // Useful types to look for: string (path), Guid, FMODAsset classes etc.
                    modLog.LogDebug($"{f.Name} ({f.FieldType.Name}) = {val}");
                }
                catch (Exception e)
                {
                    modLog.LogDebug($"Couldn't read field {f.Name}: {e.Message}");
                }
            }

            foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                try
                {
                    object val = p.GetValue(emitter, null);
                    if (val == null) { modLog.LogDebug($"{p.Name} = null"); continue; }
                    modLog.LogDebug($"PROP {p.Name} ({p.PropertyType.Name}) = {val}");
                }
                catch { /* ignore property getters that throw */ }
            }
        }
        
    }
}