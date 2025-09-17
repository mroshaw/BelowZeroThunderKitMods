using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace DaftAppleGames.SeatruckRecall_BZ
{
    // Mod supports "Teleporting" a Seatruck, and forcing a an "Autopilot" behaviour
    public enum RecallMoveMethod
    {
        Instant,
        Teleport,
        Smooth,
        Fixed
    };

    [BepInPlugin(MyGuid, PluginName, VersionString)]
    internal class SeaTruckDockRecallPlugin : BaseUnityPlugin
    {
        // Plugin properties
        private const string MyGuid = "com.mroshaw.seatruckrecallbz";
        private const string PluginName = "Sea Truck Recall Mod BZ";
        private const string VersionString = "1.2.0";

        // Config file / UI initialisation
#if UNITY_EDITOR
        internal static ModConfigFile ConfigFile;
#else
        internal static ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
        
#endif
        private static readonly Harmony Harmony = new Harmony(MyGuid);
        internal static ManualLogSource PluginLog;
        
        /// <summary>
        /// Set up the mod plugin
        /// </summary>
        private void Awake()
        {
            // Patch in our mod
            Logger.LogInfo(PluginName + " " + VersionString + " " + "loading...");
            Harmony.PatchAll();
            Logger.LogInfo(PluginName + " " + VersionString + " " + "loaded.");
            PluginLog = Logger;
        }
        
        // Static logging methods that also work in the Unity editor
        internal static void LogError(string logMessage)
        {
#if UNITY_EDITOR
            Debug.Log(logMessage);
#else
            PluginLog.LogError(logMessage);
#endif
        }
        
        internal static void LogDebug(string logMessage)
        {
#if UNITY_EDITOR
            Debug.Log(logMessage);
#else
            PluginLog.LogDebug(logMessage);
#endif
        }
        
        internal static void LogInfo(string logMessage)
        {
#if UNITY_EDITOR
            Debug.Log(logMessage);
#else
            PluginLog.LogInfo(logMessage);
#endif
        }
    }
}