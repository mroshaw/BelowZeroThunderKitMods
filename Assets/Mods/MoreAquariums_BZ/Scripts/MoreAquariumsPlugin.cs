using System.Reflection;
using BepInEx;
using DaftAppleGames.ModTools;
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
        private const string VersionString = "1.5.0";

        private const string AssetBundleName = "biggeraquariumsassetbundle";
        
        // Bubble audio asset for use by custom emitters
        private const string BubblesAudioClipName = "AquariumBubblesLoop2_Quiet";
        internal static FMODAsset BubblesFMODAsset;
        internal static ModAssetBundleUtils ModAssetUtils;
        
        // Config file / UI initialisation
        private static readonly Harmony Harmony = new Harmony(MyGuid);
#if !UNITY_EDITOR
        internal static readonly ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
        internal static ModLog ModDebugLog;
#else
        internal static readonly ModConfigFile ConfigFile;
        internal static ModLog ModDebugLog = new ModLog(null, true);
#endif

        private void Awake()
        {
            // Set up logging and asset bundle
            ModDebugLog =  new ModLog(Logger, ConfigFile.DetailedLogging);            
            ModAssetUtils = new ModAssetBundleUtils(AssetBundleName, Assembly.GetExecutingAssembly(),true, ModDebugLog);
            BaseAquariumPersistence.Initialize();
            // Register custom sounds
            RegisterCustomSounds();

            // Register our prefabs
            DoubleAquariumPrefab.Register();
            CornerAquariumPrefab.Register();
            DeskAquariumPrefab.Register();
            SphericalAquariumPrefab.Register();
            ObservatoryAquariumPrefab.Register();
            
            // Patch in our MOD
            Harmony.PatchAll();
            ModDebugLog.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }
        
        /// <summary>
        /// Register custom sounds for use in the mod
        /// </summary>
        private void RegisterCustomSounds()
        {
            ModDebugLog.LogDebug("Registering FMOD asset...");
            ModAudioUtils.RegisterSound(BubblesAudioClipName, AudioUtils.BusPaths.PlayerSFXs, ModAssetUtils, ModDebugLog, 1.0f, 10.0f, 0, true);
            BubblesFMODAsset = AudioUtils.GetFmodAsset(BubblesAudioClipName);
            if (!BubblesFMODAsset)
            {
                ModDebugLog.LogError(
                    $"Could not retrieve registered FMOD asset '{BubblesAudioClipName}'.");
                return;
            }

            ModDebugLog.LogDebug($"Registered FMOD Asset: {BubblesFMODAsset.name}");
        }
    }
}
