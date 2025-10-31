using System.Reflection;
using BepInEx;
using DaftAppleGames.ModUtils;
using HarmonyLib;
using Nautilus.Handlers;

namespace DaftAppleGames.SeaTruckFishScoop_BZ
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class SeaTruckFishScoopPluginBz : BaseUnityPlugin
    {
        // Plugin properties
        private const string MyGuid = "com.mroshaw.SeaTruckfishscoopmodbz";
        private const string PluginName = "Sea Truck Fish Scoop Mod BZ";
        private const string VersionString = "3.2.0";
        private const string AssetBundleName = "seatruckfishscoopassetbundle";

        
        // Config file / UI initialisation
        internal static ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
        private static readonly Harmony Harmony = new Harmony(MyGuid);

        // Setup helpers
        internal static ModLog ModDebugLog;
        internal static ModAssetBundleUtils ModAssetUtils;
        
        private void Awake()
        {
            // Setup logging and asset bundle
            ModDebugLog =  new ModLog(Logger, ConfigFile.DetailedLogging);
            ModAssetUtils = new ModAssetBundleUtils(AssetBundleName, Assembly.GetExecutingAssembly(),true, ModDebugLog);

            // Setup the new module prefab. 
            FishScoopModulePrefab.Init();
            // Patch in our MOD
            ModDebugLog.LogInfo(PluginName + " " + VersionString + " " + "loading...");
            // Patch in the SeaTruckUpgrades
            Harmony.PatchAll();
            ModDebugLog.LogInfo(PluginName + " " + VersionString + " " + "loaded.");
        }
    }
}