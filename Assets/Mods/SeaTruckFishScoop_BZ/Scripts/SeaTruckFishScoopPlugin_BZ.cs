using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;

namespace DaftAppleGames.SeaTruckFishScoop_BZ
{
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class SeaTruckFishScoopPluginBz : BaseUnityPlugin
    {
        // Plugin properties
        private const string MyGuid = "com.mroshaw.seatruckfishscoopmodbz";
        private const string PluginName = "Sea Truck Fish Scoop Mod BZ";
        private const string VersionString = "3.0.1";

        // Config file / UI initialisation
        internal static ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
        private static readonly Harmony Harmony = new Harmony(MyGuid);
        public static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
                        
            // Setup the new module prefab. 
            FishScoopModulePrefab.Init();
            // Patch in our MOD
            Logger.LogInfo(PluginName + " " + VersionString + " " + "loading...");
            // Patch in the SeaTruckUpgrades
            Harmony.PatchAll();
            Logger.LogInfo(PluginName + " " + VersionString + " " + "loaded.");
        }
    }
}