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
    internal static class PenglingBabyPrefab
    {
        private const string ClassId = "PenglingBabyPet";
        private const string IconTextureAssetName = "PenglingBabyIcon_Small.png";
        private const string CloneClassId = "807fbbb3-aced-45cd-aba8-db3fb1188f1f";

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
                PetPrefabConfigUtils.ConfigureVFXFabricating(obj, null, -0.2f, 0.8f, Vector3.zero, 1.0f, Vector3.zero);
                PrefabUtils.AddConstructable(obj, Info.TechType, ConstructableFlags.Inside, modelGameObject);
                obj.DestroyComponentsInChildren<Pickupable>();
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
                ? new RecipeData(new Ingredient(TechType.Gold, 1), new Ingredient(PetDnaPrefabs.PenglingBabyDnaPrefab.Info.TechType, 3), new Ingredient(TechType.Aerogel, 1), new Ingredient(TechType.Lubricant, 1))
                : new RecipeData(new Ingredient(TechType.Titanium, 1));
            prefab.SetRecipe(recipe);
            prefab.Register();
        }
    }
}
