using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using DaftAppleGames.ModTools;
using Nautilus.Handlers;
using Nautilus.Utility;
using UnityEngine;

namespace DaftAppleGames.VehicleEnhancements_BZ
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class VehicleEnhancementsPlugin_BZ : BaseUnityPlugin
    {
        // Plugin properties
        private const string MyGuid = "com.mroshaw.vehicleenhancements";
        private const string PluginName = "Vehicle Enhancements BZ";
        private const string VersionString = "2.0.0";
        private const string LegacyAssemblyFileName = "SeaTruckEnhancements_BZ.dll";
        private const string AssetBundleName = "enhancedvehiclesassetbundle";
        private const string ReversingCameraFrameSpriteName = "ReversingCameraFrame.png";
        private const string ReversingBeepsAudioClipName = "ReversingBeeps.wav";
        private const string ThisVehicleIsReversingAudioClipName = "ThisVehicleIsReversing.wav";
        private const string ThisSeatruckIsReversingAudioClipName = "ThisSeatruckIsReversing.wav";
        private const string ThisPrawnSuitIsReversingAudioClipName = "ThisPrawnSuitIsReversing.wav";
        private const string ThisSnowfoxIsReversingAudioClipName = "ThisSnowFoxIsReversing.wav";

        internal static ModAssetBundleUtils ModAssetUtils;
        internal static Sprite SeaTruckCameraFrameSprite;
        internal static FMODAsset ReversingBeepsFmodAsset;
        internal static FMODAsset ThisVehicleIsReversingFmodAsset;
        internal static FMODAsset ThisSeatruckIsReversingFmodAsset;
        internal static FMODAsset ThisPrawnSuitIsReversingFmodAsset;
        internal static FMODAsset ThisSnowfoxIsReversingFmodAsset;

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

#if !UNITY_EDITOR
            string legacyModFolder = FindLegacyModFolder();
            if (legacyModFolder != null)
            {
                ModDebugLog.LogWarning($"Old Seatruck Enhancements BZ installation found at '{legacyModFolder}'. Vehicle Enhancements BZ will remain inactive until the old folder is removed and Below Zero is restarted.");
                StartCoroutine(ShowLegacyModWarning(legacyModFolder));
                return;
            }
#endif

            // Initialise AssetBundle
            ModAssetUtils =
                new ModAssetBundleUtils(AssetBundleName, Assembly.GetExecutingAssembly(), true, ModDebugLog);

            SeaTruckCameraFrameSprite =
                ModAssetUtils.GetObjectFromAssetBundle<Sprite>(ReversingCameraFrameSpriteName, false) as Sprite;
            if (!SeaTruckCameraFrameSprite)
            {
                ModDebugLog.LogError($"Could not load camera frame sprite '{ReversingCameraFrameSpriteName}'.");
            }

            RegisterCustomSounds();

            // Patch in our MOD
            Harmony.PatchAll();
            ModDebugLog.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }

        private static string FindLegacyModFolder()
        {
            try
            {
                foreach (string assemblyPath in Directory.EnumerateFiles(
                             Paths.PluginPath,
                             LegacyAssemblyFileName,
                             SearchOption.AllDirectories))
                {
                    return Path.GetDirectoryName(assemblyPath);
                }
            }
            catch (IOException exception)
            {
                ModDebugLog.LogWarning($"Could not check for the old Seatruck Enhancements BZ installation: {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                ModDebugLog.LogWarning($"Could not check for the old Seatruck Enhancements BZ installation: {exception.Message}");
            }

            return null;
        }

        private static IEnumerator ShowLegacyModWarning(string legacyModFolder)
        {
            while (!uGUI_MainMenu.main || !uGUI.main || !uGUI.main.confirmation)
            {
                yield return null;
            }

            // Let the menu and confirmation panel finish their Start methods before showing the dialog.
            yield return null;

            uGUI_SceneConfirmation confirmation = uGUI.main.confirmation;
            while (confirmation.gameObject.activeSelf)
            {
                yield return null;
            }

            string belowZeroPluginFolder = Paths.PluginPath;
            confirmation.Show(
                $"Old 'Seatruck Enhancements BZ' mod found in your BZ plugins folder!\n" +
                $"This mod has been replaced by 'Vehicle Enhancements BZ'.\nPlease close the game, delete this folder, and restart:\n{legacyModFolder}",
                null,
                uGUI_MainMenu.main);
        }

        private static void RegisterCustomSounds()
        {
            ReversingBeepsFmodAsset = RegisterCustomSound(ReversingBeepsAudioClipName);
            ThisVehicleIsReversingFmodAsset = RegisterCustomSound(ThisVehicleIsReversingAudioClipName);
            ThisSeatruckIsReversingFmodAsset = RegisterCustomSound(ThisSeatruckIsReversingAudioClipName);
            ThisPrawnSuitIsReversingFmodAsset = RegisterCustomSound(ThisPrawnSuitIsReversingAudioClipName);
            ThisSnowfoxIsReversingFmodAsset = RegisterCustomSound(ThisSnowfoxIsReversingAudioClipName);

            if (!ReversingBeepsFmodAsset || !ThisVehicleIsReversingFmodAsset ||
                !ThisSeatruckIsReversingFmodAsset || !ThisPrawnSuitIsReversingFmodAsset ||
                !ThisSnowfoxIsReversingFmodAsset)
            {
                ModDebugLog.LogError("Could not register one or more vehicle reversing sounds.");
            }
        }

        internal static FMODAsset GetReversingVoiceAsset(ReversingVoice voice)
        {
            switch (voice)
            {
                case ReversingVoice.ThisVehicleIsReversing:
                    return ThisVehicleIsReversingFmodAsset;
                case ReversingVoice.ThisSeatruckIsReversing:
                    return ThisSeatruckIsReversingFmodAsset;
                case ReversingVoice.ThisPrawnSuitIsReversing:
                    return ThisPrawnSuitIsReversingFmodAsset;
                case ReversingVoice.ThisSnowfoxIsReversing:
                    return ThisSnowfoxIsReversingFmodAsset;
                default:
                    return null;
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
