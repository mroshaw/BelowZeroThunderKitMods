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
    internal static class TrivalveBluePrefab
    {
        private const string ClassId = "TrivalveBluePet";
        private const string IconTextureAssetName = "TrivalveBlueIcon_Small.png";
        private const string CloneClassId = "f5a2317f-6116-4fc6-8e81-824fd8ba9684";

        internal static PrefabInfo Info;

        internal static void Register()
        {
            Info = PrefabInfo.WithTechType(ClassId, null, null, unlockAtStart: true)
                .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
            CustomPrefab prefab = new CustomPrefab(Info);
            CloneTemplate cloneTemplate = new CloneTemplate(Info, CloneClassId);
            cloneTemplate.ModifyPrefab += obj => ConfigureTrivalve(obj, Info, ClassId);
            prefab.SetGameObject(cloneTemplate);
            RecipeData recipe = ConfigFile.ModMode == ModMode.Adventure
                ? new RecipeData(new Ingredient(TechType.Gold, 1), new Ingredient(PetDnaPrefabs.TrivalveBlueDnaPrefab.Info.TechType, 3), new Ingredient(TechType.Aerogel, 1), new Ingredient(TechType.TrivalveBlueEgg, 1))
                : new RecipeData(new Ingredient(TechType.Titanium, 1));
            prefab.SetRecipe(recipe);
            prefab.Register();
        }
        
        internal static void ConfigureTrivalve(GameObject obj, PrefabInfo info, string objectName)
        {
            obj.SetActive(false);
            PetPrefabConfigUtils.ConfigureTechTag(obj, info.TechType);
            GameObject modelGameObject = obj.GetComponentInChildren<Animator>(true).gameObject;
            PetPrefabConfigUtils.ConfigureVFXFabricating(obj, null, -0.1f, 0.65f, Vector3.zero, 1.0f, Vector3.zero);
            PrefabUtils.AddConstructable(obj, info.TechType, ConstructableFlags.Inside, modelGameObject);
            obj.DestroyComponentsInChildren<Pickupable>();
            PetPrefabConfigUtils.ConfigurePetHandTarget(obj);
            PetPrefabConfigUtils.ConfigureLandOnlyCreature(obj);
            PetPrefabConfigUtils.ConfigureSkyApplier(obj);
            PetPrefabConfigUtils.ConfigureAnimator(obj, false);
            PetPrefabConfigUtils.AddPetComponent(obj);
            obj.name = objectName;
            obj.SetActive(false);
            ModDebugLog.LogDebug($"Done modifying {info.TechType}");
        }
    }
}
