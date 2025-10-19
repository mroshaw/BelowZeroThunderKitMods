using System.Reflection;
using BepInEx;
using DaftAppleGames.ModUtils;
using HarmonyLib;
using Nautilus.Handlers;

namespace DaftAppleGames.BiggerAquariums
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class BiggerAquariumsPlugin : BaseUnityPlugin
    {
        private const string MyGuid = "com.mroshaw.biggeraquariumsbz";
        private const string PluginName = "Bigger Aquariums BZ";
        private const string VersionString = "1.0.0";

        private const string AssetBundleName = "biggeraquariumsassetbundle";
        internal static ModAssetBundleUtils ModAssetUtils;
        
        // Config file / UI initialisation
        private static readonly Harmony Harmony = new Harmony(MyGuid);
        private static readonly ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
        internal static ModLog ModDebugLog;

        private void Awake()
        {
            // Set up logging and asset bundle
            ModDebugLog =  new ModLog(Logger, ConfigFile.DetailedLogging);
            ModAssetUtils = new ModAssetBundleUtils(AssetBundleName, Assembly.GetExecutingAssembly(),true, ModDebugLog);

            // Register our prefabs
            DoubleAquarium.Register();
            CornerAquarium.Register();
            // CurvedAquarium.Register();
            // LShapedAquarium.Register();
            
            // Patch in our MOD
            Harmony.PatchAll();
            ModDebugLog.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }
    }
}