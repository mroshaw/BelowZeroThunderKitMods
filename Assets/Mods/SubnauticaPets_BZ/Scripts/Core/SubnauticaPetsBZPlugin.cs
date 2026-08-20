using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using DaftAppleGames.ModTools;
using DaftAppleGames.SubnauticaPets.BaseParts;
using DaftAppleGames.SubnauticaPets.Pets;
using HarmonyLib;
using Nautilus.Handlers;

namespace DaftAppleGames.SubnauticaPets
{
    [BepInDependency("com.snmodding.nautilus")] [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class SubnauticaPetsPlugin : BaseUnityPlugin
    {
        private const string MyGuid = "com.daftapplegames.subnauticapets2";
        private const string PluginName = "SubnauticaPets2";
        internal const string VersionString = "2.13.1";

        private const string AssetBundleName = "subnauticapets2assetbundle";
        
        private static Version LatestSaveDataVersion = new Version(1, 0, 0, 0);

        internal static ManualLogSource Log = new ManualLogSource(PluginName);
        internal static ModAssetBundleUtils ModAssetUtils;
        
        // Public PetSaver as a persistent list of active pets
        internal static PetSaver PetSaver;

        // SaveData instance for managing loading of Pet config data
        internal static HashSet<PetSaver.PetDetails> LoadedPetDetailsHashSet;

        // Keep tabs on currently selected options
        internal static TechType SelectedCreaturePetType;
#if !UNITY_EDITOR
        // Mod Options Config
        internal static ModConfigFile ConfigFile = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();
#else
        internal static ModConfigFile ConfigFile;
#endif
        // Mod Debug Log
#if UNITY_EDITOR
        internal static ModLog ModDebugLog = new ModLog(Log, true);
#else
        internal static ModLog ModDebugLog;
#endif

        private static readonly Harmony Harmony = new Harmony(MyGuid);

        private void Awake()
        {
#if !UNITY_EDITOR
            // Initialise Logger
            ModDebugLog =  new ModLog(Logger, ConfigFile.DetailedLogging);
#else
            ModDebugLog = new ModLog(Logger, true);
#endif
            // Initialise AssetBundle
            ModAssetUtils = new ModAssetBundleUtils(AssetBundleName, Assembly.GetExecutingAssembly(),true, ModDebugLog);
            
            // Init Localisation
            LanguageHandler.RegisterLocalizationFolder();

            // Create PetSaver instance
            PetSaver = gameObject.AddComponent<PetSaver>();
            var saveData = SaveDataHandler.RegisterSaveDataCache<SaveData>();
            // Save the HashSet
            saveData.OnStartedSaving += (sender, e) =>
            {
                ModDebugLog.LogDebug("Started Saving Data...");
                var data = e.Instance as SaveData;
                HashSet<PetSaver.PetDetails> petDetails = PetSaver.GetPetListAsHashSet();
                data.PetDetailsHashSet = petDetails;
                LoadedPetDetailsHashSet = petDetails;
                ModDebugLog.LogDebug("Started Saving Data... Done.");
            };
            // Load the HashSet
            saveData.OnFinishedLoading += (sender, e) =>
            {
                ModDebugLog.LogDebug("Finished Loading Data...");
                var data = e.Instance as SaveData;
                if (data.PetDetailsHashSet != null)
                    LoadedPetDetailsHashSet = data.PetDetailsHashSet;
                else
                    LoadedPetDetailsHashSet = new HashSet<PetSaver.PetDetails>();

                CraftData.PreparePrefabIDCache();
                PetSaver.Init();
                PetSaver.LoadData();
                ModDebugLog.LogDebug("Finished Loading Data... Done.");
            };
            // Apply all of our patches
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");

            // Sets up our static Log, so it can be used elsewhere in code.
            Log = Logger;

            // Register our new prefabs
            PetDnaPrefabs.RegisterAll();
            PetDnaDebugCommand.Register();
            PetPrefabs.RegisterAll();
            CustomPetPrefabs.RegisterAll();
            PetFabricatorPrefab.Register();
            PetConsolePrefab.Register();
            PetFabricatorFragmentPrefab.Register();
            PetConsoleFragmentPrefab.Register();
        }
    }
}
