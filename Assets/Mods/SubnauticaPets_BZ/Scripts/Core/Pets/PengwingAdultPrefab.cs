using DaftAppleGames.ModTools.Extensions;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Utility;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    internal static class PengwingAdultPrefab
    {
        private const string ClassId = "PengwingAdultPet";
        private const string IconTextureAssetName = "PengwingAdultIcon_Small.png";
        private const string CloneClassId = "74ded0e7-d394-4703-9e53-4384b37f9433";

        internal static PrefabInfo Info;

        internal static void Register()
        {
            Info = PrefabInfo.WithTechType(ClassId, null, null, unlockAtStart: true)
                .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
            CustomPrefab prefab = new CustomPrefab(Info);
            CloneTemplate cloneTemplate = new CloneTemplate(Info, CloneClassId);
            cloneTemplate.ModifyPrefab += obj =>
            {
                obj.SetActive(false);
                PetPrefabConfigUtils.ConfigureTechTag(obj, Info.TechType);
                GameObject modelGameObject = obj.GetComponentInChildren<Animator>(true).gameObject;
                PetPrefabConfigUtils.ConfigureVFXFabricating(obj, null, -0.2f, 1.8f, Vector3.zero, 1.0f, Vector3.zero);
                PrefabUtils.AddConstructable(obj, Info.TechType, ConstructableFlags.Inside, modelGameObject);
                PetPrefabConfigUtils.ConfigurePickupable(obj, false);
                PetPrefabConfigUtils.ConfigurePetHandTarget(obj);
                PetPrefabConfigUtils.ConfigureLandOnlyCreature(obj);
                PetPrefabConfigUtils.ConfigureSkyApplier(obj);
                PetPrefabConfigUtils.ConfigureAnimator(obj, false);
                obj.DestroyComponentsInChildren<CreatureDeath>();
                PetPrefabConfigUtils.AddPetComponent(obj);
                obj.name = ClassId;
                ModDebugLog.LogDebug($"Done modifying {Info.TechType}");
            };
            prefab.SetGameObject(cloneTemplate);
            RecipeData recipe = ConfigFile.ModMode == ModMode.Adventure
                ? new RecipeData(new Ingredient(TechType.Gold, 1), new Ingredient(PetDnaPrefabs.PengwingAdultDnaPrefab.Info.TechType, 3), new Ingredient(TechType.Aerogel, 1), new Ingredient(TechType.Lubricant, 2))
                : new RecipeData(new Ingredient(TechType.Titanium, 1));
            prefab.SetRecipe(recipe);
            prefab.Register();
        }
    }
}
