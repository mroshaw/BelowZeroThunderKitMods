using Nautilus.Crafting;

namespace DaftAppleGames.MoreAquariums
{
    public class SphericalAquariumPrefab : AquariumPrefab
    {
        /// Properties of the aquarium
        private const string ClassId = "SphericalAquarium";
        private const string DisplayName = "Spherical Aquarium";
        private const string Description = "A spherical aquarium with capacity for 16 fish.";
        private const string IconAssetName = "SphericalAquariumIcon.png";
        private const string PrefabAssetName = "SphericalAquariumPrefab.prefab";
        
        // Recipe for the builder
        private static readonly RecipeData Recipe = new RecipeData(
            new Ingredient(TechType.Titanium, 2),
            new Ingredient(TechType.CopperWire, 1),
            new Ingredient(TechType.Glass, 5));
        
        // Register the new prefab
        public static void Register() => RegisterInternal(ClassId, DisplayName, Description, IconAssetName, PrefabAssetName, Recipe);
    }
}