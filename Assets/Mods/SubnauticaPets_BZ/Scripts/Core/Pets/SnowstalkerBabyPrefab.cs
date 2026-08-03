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
    internal static class SnowstalkerBabyPrefab
    {
        private const string ClassId = "SnowstalkerBabyPet";
        private const string IconTextureAssetName = "SnowstalkerBabyIcon_Small.png";
        private const string CloneClassId = "78d3dbce-856f-4eba-951c-bd99870554e2";

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
                PetPrefabConfigUtils.ConfigureVFXFabricating(obj, null, -0.1f, 1.0f, Vector3.zero, 1.0f, Vector3.zero);
                PrefabUtils.AddConstructable(obj, Info.TechType, ConstructableFlags.Inside, modelGameObject);
                obj.DestroyComponentsInChildren<Pickupable>();
                PetPrefabConfigUtils.ConfigurePetHandTarget(obj);
                PetPrefabConfigUtils.ConfigureLandOnlyCreature(obj);
                PetPrefabConfigUtils.ConfigureSkyApplier(obj);
                PetPrefabConfigUtils.ConfigureAnimator(obj, false);
                PetPrefabConfigUtils.ConfigureMovement(obj);
                PetPrefabConfigUtils.CleanNavUpMesh(obj);
                PetPrefabConfigUtils.AddPetComponent(obj);
                obj.name = ClassId;
                ModDebugLog.LogDebug($"Done modifying {Info.TechType}");
            };
            prefab.SetGameObject(cloneTemplate);
            RecipeData recipe = ConfigFile.ModMode == ModMode.Adventure
                ? new RecipeData(new Ingredient(TechType.Gold, 1), new Ingredient(PetDnaPrefabs.SnowstalkerBabyDnaPrefab.Info.TechType, 3), new Ingredient(TechType.Aerogel, 2), new Ingredient(TechType.FiberMesh, 1))
                : new RecipeData(new Ingredient(TechType.Titanium, 1));
            prefab.SetRecipe(recipe);
            prefab.Register();
        }
    }
}
