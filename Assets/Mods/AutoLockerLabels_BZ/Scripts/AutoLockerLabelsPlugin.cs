using System.Reflection;
using BepInEx;
using HarmonyLib;
using DaftAppleGames.ModTools;
using Nautilus.Handlers;
using DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels;

namespace DaftAppleGames.AutoLockerLabels_BZ
{
    [BepInPlugin(MyGuid, PluginName, VersionString)] public class AutoLockerLabelsPlugin : BaseUnityPlugin
    {
        private const string MyGuid = "com.mroshaw.autolockerlabels";
        private const string PluginName = "AutoLockerLabels BZ";
        internal const string VersionString = "1.0.4";
        
        private const string AssetBundleName = "autolockerlabelassetbundle";
        
        private static readonly Harmony Harmony = new Harmony(MyGuid);
        internal static ModAssetBundleUtils ModAssetUtils;
        
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

        // Save data
        internal static SaveData SaveData { get; private set; }
        
        private void Awake()
        {
            // Initialise Logger
            ModDebugLog = new ModLog(Logger, ConfigFile.DetailedLogging);
            
            // Initialise localisation
            LanguageHandler.RegisterLocalizationFolder();
            
            // Initialise AssetBundle
            ModAssetUtils =
                new ModAssetBundleUtils(AssetBundleName, Assembly.GetExecutingAssembly(), true, ModDebugLog);

            // Load the effective built-in and player category configuration.
            LabelGenerator.InitializeCategories();
            
            // Initialise save data
            SaveData =
                SaveDataHandler.RegisterSaveDataCache<SaveData>();
            
            // Patch in our MOD
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }
    }
}
