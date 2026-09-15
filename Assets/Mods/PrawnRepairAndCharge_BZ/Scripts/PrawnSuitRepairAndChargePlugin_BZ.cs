using BepInEx;
using HarmonyLib;
using DaftAppleGames.ModTools;
using Nautilus.Handlers;

namespace DaftAppleGames.PrawnSuitRepairAndCharge_BZ
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class PrawnSuitRepairAndChargePluginBz : BaseUnityPlugin
    {
        // Plugin properties
        private const string MyGuid = "com.mroshaw.prawnsuitrepairandchargemodbz";
        private const string PluginName = "Prawn Suit Repair And Charge Mod BZ";
        private const string VersionString = "2.3.0";

        // Config file / UI initialisation
        private static readonly Harmony Harmony = new Harmony(MyGuid);
#if !UNITY_EDITOR
        internal static ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
        internal static ModLog ModDebugLog;
        internal static bool DetailedLoggingEnabled => ConfigFile.DetailedLogging;
#else
        internal static readonly ModConfigFile ConfigFile = new ModConfigFile();
        internal static ModLog ModDebugLog = new ModLog(null, true);
        internal static bool DetailedLoggingEnabled => true;
#endif

        private void Awake()
        {
            ModDebugLog = new ModLog(Logger, DetailedLoggingEnabled);
            // Patch in our MOD
            Harmony.PatchAll();
            ModDebugLog.LogInfo(PluginName + " " + VersionString + " " + "loaded.");
        }
    }
}
