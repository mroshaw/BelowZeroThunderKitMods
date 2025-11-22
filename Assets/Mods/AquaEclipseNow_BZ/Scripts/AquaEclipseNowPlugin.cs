using BepInEx;
using DaftAppleGames.ModTools;
using HarmonyLib;
using Nautilus.Handlers;

namespace DaftAppleGames.AquaEclipseNowPlugin
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class AquaEclipseNowPlugin : BaseUnityPlugin
    {
        private const string MyGuid = "com.mroshaw.aquaeclipsenow";
        private const string PluginName = "Aquaeclipse Now BZ";
        private const string VersionString = "1.0.0";

        // Config file / UI initialisation
        internal static ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
        private static readonly Harmony Harmony = new Harmony(MyGuid);
        
        internal static ModLog ModDebugLog;

        private void Awake()
        {
            ModDebugLog =  new ModLog(Logger, ConfigFile.DetailedLogging);
            
            // Register the new 'eclipsenow' command
            EclipseNowCommand.Register();
            
            // Patch in our MOD
            Harmony.PatchAll();
            ModDebugLog.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }
    }
}