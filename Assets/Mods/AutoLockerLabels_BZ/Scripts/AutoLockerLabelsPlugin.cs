using System.Reflection;
using BepInEx;
using HarmonyLib;
using DaftAppleGames.ModTools;
using Nautilus.Handlers;

namespace DaftAppleGames.AutoLockerLabels_BZ
{
    [BepInPlugin(MyGuid, PluginName, VersionString)] public class AutoLockerLabelsPlugin : BaseUnityPlugin
    {
        private const string MyGuid = "com.mroshaw.autolockerlabels";
        private const string PluginName = "AutoLockerLabels BZ";
        private const string VersionString = "1.0.1";
        
        private const string AssetBundleName = "autolockerlabelassetbundle";
        private const string ManagerPrefabName = "Label.prefab";
        
        private static readonly Harmony Harmony = new Harmony(MyGuid);
        internal static ModAssetBundleUtils ModAssetUtils;
        
        // Config file / Log initialisation
#if !UNITY_EDITOR
        internal static ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
        internal static ModLog ModDebugLog;
#else
        internal static readonly ModConfigFile ConfigFile;
        internal static ModLog ModDebugLog = new ModLog(null, true);
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
            
            // Initialise save data
            SaveData =
                SaveDataHandler.RegisterSaveDataCache<SaveData>();
            
            // Patch in our MOD
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }
    }
}
