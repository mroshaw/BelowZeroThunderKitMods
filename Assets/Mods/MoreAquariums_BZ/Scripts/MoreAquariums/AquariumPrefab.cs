using System;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    public abstract class AquariumPrefab
    {
        // Aquarium prefab properties
        private const TechType CloneTechType = TechType.Aquarium;
        
        internal static PrefabInfo RegisterInternal(string classId, string displayName, string description,
            string iconAssetName, string prefabAssetName, RecipeData recipeData, Action<GameObject> postConfigAction = null)
        {
            PrefabInfo info = PrefabInfo
                .WithTechType(classId, displayName, description, unlockAtStart: true)
                .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(iconAssetName) as Sprite);
            CustomPrefab aquariumPrefab = new CustomPrefab(info);
            
            // Clone the existing Aquarium
            PrefabTemplate aquariumTemplate = new CloneTemplate(aquariumPrefab.Info, CloneTechType)
            {
                // Reconfigure the prefab, once it's been created
                ModifyPrefab = go => ConfigurePrefab(go, prefabAssetName, postConfigAction)
            };
            
            // Set the recipe, unlock and register
            aquariumPrefab.SetGameObject(aquariumTemplate);
            aquariumPrefab.SetRecipe(recipeData);
            aquariumPrefab.SetUnlock(TechType.Aquarium)
                .WithPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule);
            aquariumPrefab.Register();
            ModDebugLog.LogDebug($"{displayName} registered successfully!");

            return info;
        }

        /// <summary>
        /// Configure the new prefab, using the new model asset
        /// </summary>
        private static void ConfigurePrefab(GameObject prefabGameObject, string prefabAssetName, Action<GameObject> postConfigAction = null)
        {
            // Get new model from the asset bundle
            GameObject configurationInstance =
                ModAssetUtils.GetPrefabInstanceFromAssetBundle(prefabAssetName, false);

            if (!configurationInstance)
            {
                ModDebugLog.LogError(
                    $"Could not load Aquarium configuration prefab '{prefabAssetName}'.");
                return;
            }

            try
            {
                // Call the helper to replace and reconfigure the prefab.
                AquariumConfigurator configurator =
                    configurationInstance.GetComponent<AquariumConfigurator>();
                if (!configurator)
                {
                    ModDebugLog.LogError(
                        $"Aquarium configuration prefab '{prefabAssetName}' has no " +
                        $"AquariumConfigurator component.");
                    return;
                }

                configurator.ConfigureAquariumPrefab(prefabGameObject, postConfigAction);
            }
            finally
            {
                UnityEngine.Object.Destroy(configurationInstance);
            }
        }
    }
}
