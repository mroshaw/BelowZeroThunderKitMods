using System;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    public abstract class AquariumBase : MonoBehaviour
    {
        // Aquarium component properties
        public abstract int StorageHeight { get; }
        public abstract int StorageWidth { get; }
        
        // Aquarium prefab properties
        internal struct PrefabData
        {
            public string ClassId;
            public string DisplayName;
            public string Description;
            public string IconAssetName;
            public string PrefabAssetName;
            public RecipeData Recipe;
            public AquariumType AquariumType;
            public bool UseCustomMovement;
            public int StorageHeight;
            public int StorageWidth;
            public bool AllowConstructionOnConstructables;
            public float WaveScale;
            public Action<GameObject> PostConfigAction;
            public bool ReplaceModel;
            public bool AddBubbleAudio;
        }

        public static PrefabInfo Info;
        private const TechType CloneTechType = TechType.Aquarium;
        
        internal static void RegisterInternal(PrefabData prefabData)
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
            AquariumHelper helper = newModelInstance.GetComponent<AquariumHelper>();
            helper.ConfigureAquariumPrefab(prefabGameObject, prefabData);
        }
    }
}