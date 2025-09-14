using DaftAppleGames.SubnauticaPets.Extensions;
using DaftAppleGames.SubnauticaPets.Pets;
using DaftAppleGames.SubnauticaPets.Utils;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.BaseParts
{
    /// <summary>
    /// Static class for creating the new Pet Fabricator
    /// </summary>
    internal static class PetFabricatorPrefab
    {
        // Pubic PrefabInfo, for anything that needs it
        internal static PrefabInfo Info;
        private const string ClassId = "PetFabricator";
        private const string IconAssetName = "PetFabricatorIconTexture.png";
        private const string EncPath = "Tech/Habitats";
        private const string DatabankPopupImageAssetName = "PetFabricatorDataBankPopupImageTexture.png";
        private const string DatabankMainImageAssetName = "PetFabricatorDataBankMainImageTexture.png";
        
        /// <summary>
        /// Makes the new Pet Fabricator available for use.
        /// </summary>
        internal static void Register()
        {
            // Unlock at start if in Creative mode
            Info = PrefabInfo
                .WithTechType(ClassId, null, null, unlockAtStart: false)
                .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(IconAssetName) as Sprite);

            CustomPrefab fabricatorPrefab = new CustomPrefab(Info);

            FabricatorGadget fabGadget = fabricatorPrefab.CreateFabricator(out CraftTree.Type treeType)
                .AddCraftNode(PetPrefabs.PenglingBabyPrefab.Info.TechType)
                .AddCraftNode(PetPrefabs.PengwingAdultPrefab.Info.TechType)
                .AddCraftNode(PetPrefabs.PinnacaridPrefab.Info.TechType)
                .AddCraftNode(PetPrefabs.SnowstalkerBabyPrefab.Info.TechType)
                .AddCraftNode(PetPrefabs.TrivalveBluePrefab.Info.TechType)
                .AddCraftNode(PetPrefabs.TrivalveYellowPrefab.Info.TechType);

            // If enabled, add the "bonus pets" to the fabricator
            if (SubnauticaPetsPlugin.ModConfig.EnableBonusPets)
            {
                fabGadget
                .AddCraftNode(CustomPetPrefabs.CatPetPrefab.Info.TechType)
                .AddCraftNode(CustomPetPrefabs.DogPetPrefab.Info.TechType)
                .AddCraftNode(CustomPetPrefabs.RabbitPetPrefab.Info.TechType)
                .AddCraftNode(CustomPetPrefabs.SealPetPrefab.Info.TechType)
                .AddCraftNode(CustomPetPrefabs.WalrusPetPrefab.Info.TechType)
                .AddCraftNode(CustomPetPrefabs.FoxPetPrefab.Info.TechType);
            }

            FabricatorTemplate fabPrefab = new FabricatorTemplate(Info, treeType)
            {
                FabricatorModel = FabricatorTemplate.Model.Workbench,
                ModifyPrefab = obj =>
                {
                    obj.SetActive(false);
                    obj.AddComponent<PetFabricator>();
                    obj.ApplyNewMeshTexture("PetFabricatorTexture", "");
                    obj.SetActive(false);
                }
            };

            fabricatorPrefab.SetGameObject(fabPrefab);

            // Define the recipe for the new Fabricator, depends on whether in "Adventure" or "Creative" mode.
            RecipeData recipe = null;
            if (SubnauticaPetsPlugin.ModConfig.ModMode == ModMode.Adventure)
            {
                recipe = new RecipeData(
                    new Ingredient(TechType.Titanium, 5),
                    new Ingredient(TechType.ComputerChip, 1),
                    new Ingredient(TechType.CopperWire, 2),
                    new Ingredient(PetDnaPrefabs.PengwingAdultDnaPrefab.Info.TechType, 1),
                    new Ingredient(PetDnaPrefabs.PenglingBabyDnaPrefab.Info.TechType, 1),
                    new Ingredient(PetDnaPrefabs.SnowstalkerBabyDnaPrefab.Info.TechType, 1),
                    new Ingredient(PetDnaPrefabs.TrivalveBlueDnaPrefab.Info.TechType, 1),
                    new Ingredient(PetDnaPrefabs.TrivalveYellowDnaPrefab.Info.TechType, 1),
                    new Ingredient(PetDnaPrefabs.PinnacaridDnaPrefab.Info.TechType, 1));
            }
            else
            {
                // Only costs 1 titanium in "Easy" mode
                recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
            }

            // Set the recipe
            fabricatorPrefab.SetRecipe(recipe);

            // Set up the scanning and fragment unlocks
            fabricatorPrefab.SetUnlock(Info.TechType, 3)
                .WithPdaGroupCategory(TechGroup.InteriorModules, TechCategory.InteriorModule)
                .WithAnalysisTech(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(DatabankPopupImageAssetName) as Sprite, null,
                    null)
                .WithEncyclopediaEntry(EncPath,
                    CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(DatabankPopupImageAssetName) as Sprite,
                    CustomAssetBundleUtils.GetObjectFromAssetBundle<Texture2D>(DatabankMainImageAssetName) as Texture2D);
            fabricatorPrefab.Register();
        }
    }
}