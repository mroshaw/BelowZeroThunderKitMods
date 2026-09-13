using BepInEx;
using HarmonyLib;
using DaftAppleGames.SeaTruckSpeedMod_BZ.Config;
using DaftAppleGames.ModTools;
using Nautilus.Handlers;

namespace DaftAppleGames.SeaTruckSpeedMod_BZ
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class SeaTruckSpeedPluginBz : BaseUnityPlugin
    {
        // Plugin properties
        private const string MyGuid = "com.mroshaw.seatruckspeedmodbz";
        private const string PluginName = "Sea Truck Speed Mod BZ";
        private const string VersionString = "2.2.1";

        // Config file / Log initialisation
#if !UNITY_EDITOR
        internal static ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
        internal static ModLog ModDebugLog;
        internal static bool DetailedLoggingEnabled => ConfigFile.DetailedLogging;
#else
        internal static readonly ModConfigFile ConfigFile;
        internal static ModLog ModDebugLog = new ModLog(null, true);
        internal static bool DetailedLoggingEnabled => true;
#endif

        private static readonly Harmony Harmony = new Harmony(MyGuid);

        /// <summary>
        /// Configure the mod
        /// </summary>
        private void Awake()
        {
            // Initialise Logger
            ModDebugLog = new ModLog(Logger, ConfigFile.DetailedLogging);

            // Patch in our MOD
            Harmony.PatchAll();
            ModDebugLog.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }
    }
}
