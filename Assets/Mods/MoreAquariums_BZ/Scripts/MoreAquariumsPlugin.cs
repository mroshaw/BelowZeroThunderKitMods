using System.Reflection;
using BepInEx;
using DaftAppleGames.ModUtils;
using HarmonyLib;
using Nautilus.Handlers;
using Nautilus.Utility;

namespace DaftAppleGames.MoreAquariums
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class MoreAquariumsPlugin : BaseUnityPlugin
    {
        private const string MyGuid = "com.mroshaw.biggeraquariumsbz";
        private const string PluginName = "More Aquariums BZ";
        private const string VersionString = "1.3.0";

        private const string AssetBundleName = "biggeraquariumsassetbundle";
        
        // Bubble audio asset for use by custom emitters
        private const string BubblesAudioClipName = "AquariumBubblesLoop2_Quiet";
        internal static FMODAsset BubblesFMODAsset;
        internal static ModAssetBundleUtils ModAssetUtils;
        
        // Config file / UI initialisation
#if !UNITY_EDITOR
        private static readonly Harmony Harmony = new Harmony(MyGuid);
        internal static readonly ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
#else
        internal static readonly ModConfigFile ConfigFile;
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

            // Register custom sounds
            RegisterCustomSounds();

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

#if !UNITY_EDITOR
        /// <summary>
        /// Register custom sounds for use in the mod
        /// </summary>
        private void RegisterCustomSounds()
        {
            ModAudioUtils.RegisterSound(BubblesAudioClipName, AudioUtils.BusPaths.PlayerSFXs, ModAssetUtils, ModDebugLog, 1.0f, 10.0f, 0, true);
            BubblesFMODAsset = AudioUtils.GetFmodAsset(BubblesAudioClipName);
        }
#endif
        
    }
}