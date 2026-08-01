using DaftAppleGames.ModTools;
using DaftAppleGames.SubnauticaPets.Extensions;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Utility;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.BaseParts
{
    /// <summary>
    ///     Static class for creating the new Pet Console
    /// </summary>
    internal static class PetConsolePrefab
    {
        private const string ClassId = "PetConsole";
        private const TechType CloneTechType = TechType.PictureFrame;
        private const string IconAssetName = "PetConsoleIconTexture.png";
        private const string EncPath = "Tech/Habitats";
        private const string DatabankPopupImageAssetName = "PetConsoleDataBankPopupImageTexture.png";
        private const string DatabankMainImageAssetName = "PetConsoleDataBankMainImageTexture.png";
        private const string ConsolePrefabAssetName = "PetConsoleUI.prefab";

        private const string ConsoleAlertAudioAssetName = "ConsoleAlert.wav";
        private const string ConsoleRenameAudioAssetName = "ConsoleRename.wav";
        internal static PrefabInfo Info;

        /// <summary>
        ///     Register Pet Console
        /// </summary>
        internal static void Register()
        {
            Info = PrefabInfo
                .WithTechType(ClassId, null, null, unlockAtStart: false)
                .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconAssetName) as Sprite);
            var consolePrefab = new CustomPrefab(Info);

            // We'll use the PictureFrame as a template
            PrefabTemplate consoleTemplate = new CloneTemplate(consolePrefab.Info, CloneTechType)
            {
                // Reconfigure the prefab, once it's been created
                ModifyPrefab = prefabGameObject => { ConfigurePrefab(prefabGameObject); }
            };

            // Define the recipe for the new Console, depends on whether in "Adventure" or "Creative" mode.
            RecipeData recipe;
            if (SubnauticaPetsPlugin.ConfigFile.ModMode == ModMode.Adventure)
                recipe = new RecipeData(
                    new Ingredient(TechType.Titanium, 3),
                    new Ingredient(TechType.ComputerChip, 1),
                    new Ingredient(TechType.CopperWire, 2),
                    new Ingredient(TechType.Glass, 1));
            else
                // Only costs 1 titanium in "Easy" mode
                recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));

            // Set the recipe.
            consolePrefab.SetRecipe(recipe);

            consolePrefab.SetUnlock(Info.TechType)
                .WithAnalysisTech(
                    ModAssetUtils.GetObjectFromAssetBundle<Sprite>(DatabankPopupImageAssetName) as Sprite)
                .WithPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule)
                .WithEncyclopediaEntry(EncPath,
                    ModAssetUtils.GetObjectFromAssetBundle<Sprite>(DatabankPopupImageAssetName) as Sprite,
                    ModAssetUtils
                        .GetObjectFromAssetBundle<Texture2D>(DatabankMainImageAssetName) as Texture2D);

            consolePrefab.SetGameObject(consoleTemplate);
            consolePrefab.Register();
            ModDebugLog.LogDebug("Pet Console Registered Successfully!");
        }

        private static void ConfigurePrefab(GameObject prefabGameObject)
        {
            // Get rid of the existing UI
            prefabGameObject.SetActive(false);
            prefabGameObject.DestroyComponentsInChildren<PictureFrame>();
            var screen = prefabGameObject.transform.Find("Screen").gameObject;
            Object.Destroy(screen);

            // Get Console UI Prefab from Asset Bundle and add to the picture frame
            GameObject petConsoleInstance =
                ModAssetUtils.GetPrefabInstanceFromAssetBundle(ConsolePrefabAssetName, true);
            petConsoleInstance.transform.SetParent(prefabGameObject.transform);
            petConsoleInstance.transform.localPosition = new Vector3(0, 0, 0.018f);
            petConsoleInstance.transform.localRotation = new Quaternion(0, 180, 0, 1);
            petConsoleInstance.transform.localScale = new Vector3(0.002f, 0.002f, 1f);

            // Add a Constructable Listener and enable the console screen when constructed
            prefabGameObject.EnsureComponent<ConsoleConstructedNotifier>();
            ModDebugLog.LogDebug("ConsoleConstructedNotifier added...");

            // Add Audio FMOD components
            var alertEmitterGo = new GameObject("AlertEmitter")
            {
                transform =
                {
                    parent = petConsoleInstance.transform,
                    localPosition = Vector3.zero
                }
            };

            var renameEmitterGo = new GameObject("RenameEmitter")
            {
                transform =
                {
                    parent = petConsoleInstance.transform,
                    localPosition = Vector3.zero
                }
            };

            FMOD_CustomEmitter alertEmitter = alertEmitterGo.AddComponent<FMOD_CustomEmitter>();
            ModAudioUtils.RegisterSound(ConsoleAlertAudioAssetName, AudioUtils.BusPaths.SurfaceAmbient, ModAssetUtils, ModDebugLog, 0.1f, 8.0f, 0, true);
            FMODAsset consoleAlertFmodAsset = AudioUtils.GetFmodAsset(ConsoleAlertAudioAssetName);
            ModAudioUtils.ConfigureEmitter(alertEmitter, consoleAlertFmodAsset, ModDebugLog);
            
            FMOD_CustomEmitter renameEmitter = renameEmitterGo.AddComponent<FMOD_CustomEmitter>();
            ModAudioUtils.RegisterSound(ConsoleRenameAudioAssetName, AudioUtils.BusPaths.SurfaceAmbient, ModAssetUtils, ModDebugLog, 0.1f, 8.0f, 0, true);
            FMODAsset consoleRenameFmodAsset = AudioUtils.GetFmodAsset(ConsoleRenameAudioAssetName);
            ModAudioUtils.ConfigureEmitter(renameEmitter, consoleRenameFmodAsset, ModDebugLog);
        }
    }
}