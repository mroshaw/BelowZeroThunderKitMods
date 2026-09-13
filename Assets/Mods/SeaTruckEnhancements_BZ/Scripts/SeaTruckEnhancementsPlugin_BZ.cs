using System.Reflection;
using BepInEx;
using HarmonyLib;
using DaftAppleGames.ModTools;
using Nautilus.Handlers;
using Nautilus.Utility;
using UnityEngine;

namespace DaftAppleGames.SeaTruckEnhancements_BZ
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class SeaTruckEnhancementsPlugin_BZ : BaseUnityPlugin
    {
        // Plugin properties
        private const string MyGuid = "com.mroshaw.seatruckenhancements";
        private const string PluginName = "Sea Truck Enhancements BZ";
        private const string VersionString = "1.0.0";
        private const string AssetBundleName = "enhancedseatruckassetbundle";
        private const string SeaTruckCameraFrameSpriteName = "SeaTruckCameraFrame.png";
        private const string ReversingBeepsAudioClipName = "ReversingBeeps.wav";
        private const string ThisSeaTruckIsReversingAudioClipName = "ThisSeaTruckIsReversing.wav";

        internal static ModAssetBundleUtils ModAssetUtils;
        internal static Sprite SeaTruckCameraFrameSprite;
        internal static FMODAsset ReversingBeepsFmodAsset;
        internal static FMODAsset ThisSeaTruckIsReversingFmodAsset;

        // Config file / Log initialisation
#if !UNITY_EDITOR
        internal static ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
        internal static ModLog ModDebugLog;
        internal static bool DetailedLoggingEnabled => ConfigFile.DetailedLogging;
#else
        internal static readonly ModConfigFile ConfigFile;
        internal static ModLog ModDebugLog = new ModLog(null, true);
        internal static bool DetailedLoggingEnabled => true;
#endif

        private static readonly Harmony Harmony = new Harmony(MyGuid);

        /// <summary>
        /// Configure the mod
        /// </summary>
        private void Awake()
        {
            // Initialise Logger
            ModDebugLog = new ModLog(Logger, DetailedLoggingEnabled);

            // Initialise AssetBundle
            ModAssetUtils =
                new ModAssetBundleUtils(AssetBundleName, Assembly.GetExecutingAssembly(), true, ModDebugLog);

            SeaTruckCameraFrameSprite =
                ModAssetUtils.GetObjectFromAssetBundle<Sprite>(SeaTruckCameraFrameSpriteName, false) as Sprite;
            if (!SeaTruckCameraFrameSprite)
            {
                ModDebugLog.LogError($"Could not load camera frame sprite '{SeaTruckCameraFrameSpriteName}'.");
            }

            RegisterCustomSounds();

            // Patch in our MOD
            Harmony.PatchAll();
            ModDebugLog.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }

        private static void RegisterCustomSounds()
        {
            ReversingBeepsFmodAsset = RegisterCustomSound(ReversingBeepsAudioClipName);
            ThisSeaTruckIsReversingFmodAsset = RegisterCustomSound(ThisSeaTruckIsReversingAudioClipName);

            if (!ReversingBeepsFmodAsset || !ThisSeaTruckIsReversingFmodAsset)
            {
                ModDebugLog.LogError("Could not register one or more SeaTruck reversing sounds.");
            }
        }

        private static FMODAsset RegisterCustomSound(string clipName)
        {
            AudioClip audioClip = ModAssetUtils.GetObjectFromAssetBundle<AudioClip>(clipName, false) as AudioClip;
            if (!audioClip)
            {
                ModDebugLog.LogError($"Could not load reversing audio clip '{clipName}'.");
                return null;
            }

            ModAudioUtils.RegisterSound(
                clipName,
                AudioUtils.BusPaths.PlayerSFXs,
                ModAssetUtils,
                ModDebugLog,
                minDistance: 2.0f,
                maxDistance: 30.0f,
                loop: true);
            return AudioUtils.GetFmodAsset(clipName);
        }
    }
}
