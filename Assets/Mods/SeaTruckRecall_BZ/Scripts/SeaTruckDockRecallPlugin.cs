using System.Reflection;
using BepInEx;
using DaftAppleGames.ModUtils;
using HarmonyLib;
using Nautilus.Handlers;

namespace DaftAppleGames.SeaTruckRecall_BZ
{
    // Mod supports "Teleporting" a SeaTruck, and forcing a an "Autopilot" behaviour
    public enum RecallMoveMethod
    {
        Teleport,
        Autopilot,
    };

    [BepInPlugin(MyGuid, PluginName, VersionString)]
    internal class SeaTruckDockRecallPlugin : BaseUnityPlugin
    {
        // Plugin properties
        private const string MyGuid = "com.mroshaw.SeaTruckrecallbz";
        private const string PluginName = "Sea Truck Recall Mod BZ";
        private const string VersionString = "1.0.0";

        private const string AssetBundleName = "seatruckrecallbzassetbundle";
        
        // Config file / UI initialisation
#if UNITY_EDITOR
        internal static ModConfigFile ConfigFile;
#else
        internal static ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
        
#endif
        private static readonly Harmony Harmony = new Harmony(MyGuid);

        // Setup helpers
#if UNITY_EDITOR
        internal static ModLog ModDebugLog = new ModLog(null, true);
#else
        internal static ModLog ModDebugLog;
#endif
        internal static ModAssetBundleUtils ModAssetUtils;
        
        /// <summary>
        /// Set up the mod plugin
        /// </summary>
        private void Awake()
        {
            // Setup logging and asset bundle
            ModDebugLog =  new ModLog(Logger, ConfigFile.DetailedLogging);
            ModAssetUtils = new ModAssetBundleUtils(AssetBundleName, Assembly.GetExecutingAssembly(),true, ModDebugLog);
            
            // Patch in our mod
            ModDebugLog.LogInfo(PluginName + " " + VersionString + " " + "loading...");
            Harmony.PatchAll();
            ModDebugLog.LogInfo(PluginName + " " + VersionString + " " + "loaded.");
        }
    }
}