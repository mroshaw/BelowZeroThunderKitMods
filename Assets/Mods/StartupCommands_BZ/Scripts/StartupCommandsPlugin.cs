using System.Reflection;
using BepInEx;
using DaftAppleGames.ModTools;
using HarmonyLib;
using Nautilus.Handlers;

namespace DaftAppleGames.StartupCommand
{
    // https://discord.com/channels/324207629784186882/941616622286823424/1432826446220627998
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class StartupCommandPlugin : BaseUnityPlugin
    {
        private const string MyGuid = "com.mroshaw.startupcommands";
        private const string PluginName = "Startup Commands BZ";
        internal const string VersionString = "1.0.0";

        // Contains the command config UI prefab
        private const string AssetBundleName = "startupcommandassetbundle";
       
        // Config file / UI initialisation
        // This is the Mod Options config
        internal static ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
        
        // This is the Script config - this is different to the ModOptions, as we don't have a string/text handler there
        internal static ScriptConfigFile ScriptConfigFile = new ScriptConfigFile();
        
        // Load our UI prefab from asset bundle
        internal static ModAssetBundleUtils ModAssetUtils;
        
        private static readonly Harmony Harmony = new Harmony(MyGuid);
        
        internal static ModLog ModDebugLog;
        
        private void Awake()
        {
            ModDebugLog =  new ModLog(Logger, ConfigFile.DetailedLogging);
            
            // Initialise AssetBundle
            ModAssetUtils = new ModAssetBundleUtils(AssetBundleName, Assembly.GetExecutingAssembly(),true, ModDebugLog);
            
            // Load command config
            ScriptConfigFile.Load();
            
            // Patch in our MOD
            Harmony.PatchAll();
            ModDebugLog.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }
    }
}