using System.Reflection;
using BepInEx;
using DaftAppleGames.ModTools;
using DaftAppleGames.SeaTruckRecall_BZ.DockRecaller;
using HarmonyLib;
using Nautilus.Handlers;

namespace DaftAppleGames.SeaTruckRecall_BZ
{
    // Mod supports teleporting a SeaTruck or navigating it using one of two movement models.
    public enum RecallMoveMethod
    {
        Teleport,
        Physics,
        Input,
    };

    [BepInPlugin(MyGuid, PluginName, VersionString)]
    internal class SeaTruckDockRecallPlugin : BaseUnityPlugin
    {
        // Plugin properties
        private const string MyGuid = "com.mroshaw.SeaTruckrecallbz";
        private const string PluginName = "Sea Truck Recall Mod BZ";
        private const string VersionString = "1.2.1";

        private const string AssetBundleName = "seatruckrecallbzassetbundle";
        private const string StrategicGraphAssetPath =
            "assets/mods/seatruckrecall_bz/navgraphs/belowzerostrategicnavigationgraph.asset";
        
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
        internal static StrategicNavigationGraph LoadedStrategicNavigationGraph;
        
        /// <summary>
        /// Set up the mod plugin
        /// </summary>
        private void Awake()
        {
            // Setup logging and asset bundle
            ModDebugLog =  new ModLog(Logger, ConfigFile.DetailedLogging);
            ModAssetUtils = new ModAssetBundleUtils(AssetBundleName, Assembly.GetExecutingAssembly(),true, ModDebugLog);
            LoadStrategicNavigationGraph();
            
            // Patch in our mod
            ModDebugLog.LogInfo(PluginName + " " + VersionString + " " + "loading...");
            Harmony.PatchAll();
            ModDebugLog.LogInfo(PluginName + " " + VersionString + " " + "loaded.");
        }

        private static void LoadStrategicNavigationGraph()
        {
            string[] assetNames = ModAssetUtils.GetAllAssetNames();
            ModDebugLog.LogDebug($"AssetBundle contains {assetNames.Length} assets:");
            foreach (string assetName in assetNames)
            {
                ModDebugLog.LogDebug($"AssetBundle asset: {assetName}");
            }

            LoadedStrategicNavigationGraph = ModAssetUtils
                .GetObjectFromAssetBundle<StrategicNavigationGraph>(StrategicGraphAssetPath) as
                StrategicNavigationGraph;
            if (!LoadedStrategicNavigationGraph)
            {
                ModDebugLog.LogError($"Failed to load strategic navigation graph directly from " +
                                     $"'{StrategicGraphAssetPath}'.");
                return;
            }

            ModDebugLog.LogDebug($"Direct strategic graph load returned " +
                                 $"'{LoadedStrategicNavigationGraph.name}' " +
                                 $"(instance {LoadedStrategicNavigationGraph.GetInstanceID()}) with " +
                                 $"{LoadedStrategicNavigationGraph.NodeCount} nodes and " +
                                 $"{LoadedStrategicNavigationGraph.StoredConnectionCount} connections.");
        }
    }
}
