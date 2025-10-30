using System.Reflection;
using BepInEx;
using DaftAppleGames.ModUtils;
using HarmonyLib;
using Nautilus.Handlers;

namespace DaftAppleGames.MoreAquariums
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class MoreAquariumsPlugin : BaseUnityPlugin
    {
        private const string MyGuid = "com.mroshaw.biggeraquariumsbz";
        private const string PluginName = "More Aquariums BZ";
        private const string VersionString = "1.3.0";

        private const string AssetBundleName = "biggeraquariumsassetbundle";
        internal static ModAssetBundleUtils ModAssetUtils;
        
        // Config file / UI initialisation
#if !UNITY_EDITOR
        private static readonly Harmony Harmony = new Harmony(MyGuid);
        private static readonly ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
#endif
        internal static ModLog ModDebugLog;

        private void Awake()
        {
            // Set up logging and asset bundle
#if UNITY_EDITOR
            ModDebugLog =  new ModLog(Logger, true);
#else
            ModDebugLog =  new ModLog(Logger, ConfigFile.DetailedLogging);            
            ModAssetUtils = new ModAssetBundleUtils(AssetBundleName, Assembly.GetExecutingAssembly(),true, ModDebugLog);
            // Register our prefabs
            DoubleAquarium.Register();
            CornerAquarium.Register();
            DeskAquarium.Register();
            SphericalAquarium.Register();
            
            // Patch in our MOD
            Harmony.PatchAll();
            ModDebugLog.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
#endif
        }
    }
}