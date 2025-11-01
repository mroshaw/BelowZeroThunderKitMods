using Nautilus.Crafting;

namespace DaftAppleGames.MoreAquariums
{
    public class CornerAquariumPrefab : AquariumPrefab
    {
        private const string ClassId = "CornerAquarium";
        private const string DisplayName = "Corner Aquarium";
        private const string Description = "A double-sized, corner aquarium.";
        private const string IconAssetName = "CornerAquariumIcon.png";
        private const string PrefabAssetName = "CornerAquariumPrefab.prefab";
        
        // Recipe for the builder
        private static readonly RecipeData Recipe = new RecipeData(
            new Ingredient(TechType.Titanium, 2),
            new Ingredient(TechType.CopperWire, 1),
            new Ingredient(TechType.Glass, 5));
        
        // Register the new prefab
        public static void Register() => RegisterInternal(ClassId, DisplayName, Description, IconAssetName, PrefabAssetName, Recipe);
    }
}