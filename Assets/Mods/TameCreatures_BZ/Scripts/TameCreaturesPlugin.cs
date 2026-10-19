using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using DaftAppleGames.ModTools;
using Nautilus.Handlers;

namespace DaftAppleGames.TameCreatures_BZ
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class TameCreaturesPlugin : BaseUnityPlugin
    {
        private const string MyGuid = "com.mroshaw.tamecreaturesbz";
        private const string PluginName = "Tame Creatures BZ";
        internal const string VersionString = "1.0.0";
        
        private static readonly Harmony Harmony = new Harmony(MyGuid);
        
        // Config file / Log initialisation
#if !UNITY_EDITOR
        internal static ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
        internal static ModLog ModDebugLog;
#else
        internal static readonly ModConfigFile ConfigFile;
        internal static ModLog ModDebugLog = new ModLog(null, true);
#endif
        private void Awake()
        {
            // Initialise Logger
            ModDebugLog = new ModLog(Logger, ConfigFile.DetailedLogging);
            
            // Patch in our MOD
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }
    }
}
