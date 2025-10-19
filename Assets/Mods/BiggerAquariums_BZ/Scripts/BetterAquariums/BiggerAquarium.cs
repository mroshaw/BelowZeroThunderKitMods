using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using UnityEngine;
using static DaftAppleGames.BiggerAquariums.BiggerAquariumsPlugin;

namespace DaftAppleGames.BiggerAquariums
{
    public abstract class BiggerAquarium : MonoBehaviour
    {
        // Aquarium component properties
        public abstract int StorageHeight { get; }
        public abstract int StorageWidth { get; }
        
        // Aquarium prefab properties
        protected struct PrefabData
        {
            public string ClassId;
            public string DisplayName;
            public string Description;
            public string IconAssetName;
            public string PrefabAssetName;
            public RecipeData Recipe;
            public BiggerAquariumType AquariumType;
        }

        public static PrefabInfo Info;
        private const TechType CloneTechType = TechType.Aquarium;
        
        protected static void RegisterInternal(PrefabData prefabData)
        {
            Info = PrefabInfo
                .WithTechType(prefabData.ClassId, prefabData.DisplayName, prefabData.Description, unlockAtStart: true)
                .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(prefabData.IconAssetName) as Sprite);
            CustomPrefab aquariumPrefab = new CustomPrefab(Info);
            
            // Clone the existing Aquarium
            PrefabTemplate aquariumTemplate = new CloneTemplate(aquariumPrefab.Info, CloneTechType)
            {
                // Reconfigure the prefab, once it's been created
                ModifyPrefab = go => ConfigurePrefab(go, prefabData)
            };
            
            // Set the recipe, unlock and register
            aquariumPrefab.SetGameObject(aquariumTemplate);
            aquariumPrefab.SetRecipe(prefabData.Recipe);
            aquariumPrefab.SetUnlock(TechType.Aquarium)
                .WithPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule);
            aquariumPrefab.Register();
            ModDebugLog.LogDebug($"{prefabData.DisplayName} registered successfully!");
        }

        /// <summary>
        /// Configure the new prefab, using the new model asset
        /// </summary>
        private static void ConfigurePrefab(GameObject prefabGameObject, PrefabData prefabData)
        {
            // Get new model from the asset bundle
            GameObject newModelInstance =
                ModAssetUtils.GetPrefabInstanceFromAssetBundle(prefabData.PrefabAssetName, false);
            
            // Call the helper to replace and reconfigure the prefab
            BiggerAquariumHelper helper = newModelInstance.GetComponent<BiggerAquariumHelper>();
            helper.ConfigureAquariumPrefab(prefabGameObject, prefabData.AquariumType);
        }
    }
}