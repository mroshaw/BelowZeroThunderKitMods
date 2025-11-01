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
        internal struct PrefabData
        {
            public string ClassId;
            public string DisplayName;
            public string Description;
            public string IconAssetName;
            public string PrefabAssetName;
            public RecipeData Recipe;
            
            public Action<GameObject> PostConfigAction;
        }

        public static PrefabInfo Info;
        private const TechType CloneTechType = TechType.Aquarium;
        
        internal static void RegisterInternal(string classId, string displayName, string description,
            string iconAssetName, string prefabAssetName, RecipeData recipeData, Action<GameObject> postConfigAction = null)
        {
            Info = PrefabInfo
                .WithTechType(classId, displayName, description, unlockAtStart: true)
                .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(iconAssetName) as Sprite);
            CustomPrefab aquariumPrefab = new CustomPrefab(Info);
            
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
        }

        /// <summary>
        /// Configure the new prefab, using the new model asset
        /// </summary>
        private static void ConfigurePrefab(GameObject prefabGameObject, string prefabAssetName, Action<GameObject> postConfigAction = null)
        {
            // Get new model from the asset bundle
            GameObject newModelInstance =
                ModAssetUtils.GetPrefabInstanceFromAssetBundle(prefabAssetName, false);
            
            // Call the helper to replace and reconfigure the prefab
            AquariumConfigurator configurator = newModelInstance.GetComponent<AquariumConfigurator>();
            configurator.ConfigureAquariumPrefab(prefabGameObject, postConfigAction);
        }
    }
}