using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Utility;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    internal static class PinnacaridPrefab
    {
        private const string ClassId = "PinnacaridPet";
        private const string IconTextureAssetName = "PinnacaridIcon_Small.png";
        private const string CloneClassId = "f9eccfe2-a06f-4c06-bc57-01c2e50ffbe8";

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
                PetPrefabConfigUtils.ConfigureVFXFabricating(obj, null, -0.1f, 0.9f, Vector3.zero, 1.0f, Vector3.zero);
                PrefabUtils.AddConstructable(obj, Info.TechType, ConstructableFlags.Inside, modelGameObject);
                obj.name = ClassId;
                obj.SetActive(false);
                ModDebugLog.LogDebug($"Done modifying {Info.TechType}");
            };
            prefab.SetGameObject(cloneTemplate);
            RecipeData recipe = ConfigFile.ModMode == ModMode.Adventure
                ? new RecipeData(new Ingredient(TechType.Gold, 1), new Ingredient(PetDnaPrefabs.PinnacaridDnaPrefab.Info.TechType, 3), new Ingredient(TechType.Polyaniline, 1), new Ingredient(TechType.Benzene, 1))
                : new RecipeData(new Ingredient(TechType.Titanium, 1));
            prefab.SetRecipe(recipe);
            prefab.Register();
        }
    }
}
