using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Crafting;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    internal static class RockPuncherPrefab
    {
        private const string ClassId = "RockPuncherPet";
        private const string PrefabAssetName = "PetRockPuncher.prefab";
        private const string IconTextureAssetName = "RockPuncherIcon_Small.png";
        private const string CloneClassId = "b6e25aff-b0cd-48ef-91d1-a187af94a992";

        internal static PrefabInfo Info;

        internal static void Register()
        {
            Info = PrefabInfo.WithTechType(ClassId, null, null, unlockAtStart: true)
                .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);
            CustomPrefab prefab = new CustomPrefab(Info);
            prefab.SetGameObject(result =>
                PetPrefabs.CreateWithVanillaModel(result, Info, PrefabAssetName, CloneClassId,
                    PetPrefabConfigUtils.ConfigureLandOnlyCreature));
            RecipeData recipe = ConfigFile.ModMode == ModMode.Adventure
                ? new RecipeData(new Ingredient(TechType.Gold, 1), new Ingredient(PetDnaPrefabs.RockPuncherDnaPrefab.Info.TechType, 3), new Ingredient(TechType.Nickel, 1), new Ingredient(TechType.Quartz, 2))
                : new RecipeData(new Ingredient(TechType.Titanium, 1));
            prefab.SetRecipe(recipe);
            prefab.Register();
        }
    }
}
