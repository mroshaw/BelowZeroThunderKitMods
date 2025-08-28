using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using DaftAppleGames.SubnauticaPets.BaseParts;
using DaftAppleGames.SubnauticaPets.Pets;
using DaftAppleGames.SubnauticaPets.Utils;
using HarmonyLib;
using Nautilus.Handlers;
using Nautilus.Json;

namespace DaftAppleGames.SubnauticaPets
{
    [BepInDependency("com.snmodding.nautilus")]
    [BepInPlugin(MyGuid, PluginName, VersionString)]
    public class SubnauticaPetsPlugin : BaseUnityPlugin
    {
        private const string MyGuid = "com.daftapplegames.subnauticapetsbz";
        private const string PluginName = "SubnauticaPetsBZ";
        private const string VersionString = "2.7.0";

        private static Version LatestSaveDataVersion = new Version(1, 0, 0, 0);

        internal static ManualLogSource Log = new ManualLogSource(PluginName);

        // Public PetSaver as a persistent list of active pets
        internal static PetSaver PetSaver;

        // SaveData instance for managing loading of Pet config data
        internal static HashSet<PetSaver.PetDetails> LoadedPetDetailsHashSet;

        // Keep tabs on currently selected options
        internal static TechType SelectedCreaturePetType;

        // Mod Options Config
        internal static ModConfigFile ModConfig = OptionsPanelHandler.RegisterModOptions<ModConfigFile>();

        private static readonly Harmony Harmony = new Harmony(MyGuid);
        
        private void Awake()
        {
            // Init Localisation
            LanguageHandler.RegisterLocalizationFolder();
            
            // Create PetSaver instance
            PetSaver = gameObject.AddComponent<PetSaver>();
            SaveData saveData = SaveDataHandler.RegisterSaveDataCache<SaveData>();
            // Save the HashSet
            saveData.OnStartedSaving += (object sender, JsonFileEventArgs e) =>
            {
                LogUtils.LogDebug(LogArea.Main, "Started Saving Data...");
                SaveData data = e.Instance as SaveData;
                data.PetDetailsHashSet = PetSaver.GetPetListAsHashSet();
                LogUtils.LogDebug(LogArea.Main, "Started Saving Data... Done.");
            };
            // Load the HashSet
            saveData.OnFinishedLoading += (object sender, JsonFileEventArgs e) =>
            {
                LogUtils.LogDebug(LogArea.Main, "Finished Loading Data...");
                SaveData data = e.Instance as SaveData;
                if (data.PetDetailsHashSet != null)
                {
                    LoadedPetDetailsHashSet = data.PetDetailsHashSet;
                }
                else
                {
                    LoadedPetDetailsHashSet = new HashSet<PetSaver.PetDetails>();
                }

                CraftData.PreparePrefabIDCache();
                PetSaver.Init();
                LogUtils.LogDebug(LogArea.Main, "Finished Loading Data... Done.");
            };
            // Apply all of our patches
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");

            // Sets up our static Log, so it can be used elsewhere in code.
            Log = Logger;

            // Register our new prefabs
            PetDnaPrefabs.RegisterAll();
            PetPrefabs.RegisterAll();
            CustomPetPrefabs.RegisterAll();
            PetFabricatorPrefab.Register();
            PetConsolePrefab.Register();
            PetFabricatorFragmentPrefab.Register();
            PetConsoleFragmentPrefab.Register();

        }
    }
}