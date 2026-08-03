using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    internal static class TrivalveYellowPrefab
    {
        private const string ClassId = "TrivalveYellowPet";
        private const string IconTextureAssetName = "TrivalveYellowIcon_Small.png";
        private const string CloneClassId = "e8f2bfd4-49c6-45d1-a029-489b492515a9";

        internal static PrefabInfo Info;

        internal static void Register()
        {
            Info = PrefabInfo.WithTechType(ClassId, null, null, unlockAtStart: true)
                .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
            CustomPrefab prefab = new CustomPrefab(Info);
            CloneTemplate cloneTemplate = new CloneTemplate(Info, CloneClassId);
            cloneTemplate.ModifyPrefab += obj => TrivalveBluePrefab.ConfigureTrivalve(obj, Info, ClassId);
            prefab.SetGameObject(cloneTemplate);
            RecipeData recipe = ConfigFile.ModMode == ModMode.Adventure
                ? new RecipeData(new Ingredient(TechType.Gold, 1), new Ingredient(PetDnaPrefabs.TrivalveYellowDnaPrefab.Info.TechType, 3), new Ingredient(TechType.Aerogel, 1), new Ingredient(TechType.TrivalveYellowEgg, 1))
                : new RecipeData(new Ingredient(TechType.Titanium, 1));
            prefab.SetRecipe(recipe);
            prefab.Register();
        }
    }
}
