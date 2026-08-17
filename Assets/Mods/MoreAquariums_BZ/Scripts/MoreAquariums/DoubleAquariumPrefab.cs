using Nautilus.Assets;
using Nautilus.Crafting;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Describes a Double Aquarium prefab
    /// </summary>
    public class DoubleAquariumPrefab : AquariumPrefab
    {
        public static PrefabInfo PrefabInfo { get; private set; }
        
        /// Properties of the aquarium
        private const string ClassId = "DoubleAquarium";
        private const string DisplayName = "Double Aquarium";
        private const string Description = "A double-sized aquarium for use in long rooms.";
        private const string IconAssetName = "DoubleAquariumIcon.png";
        private const string PrefabAssetName = "DoubleAquariumPrefab.prefab";
        // Recipe for the builder
        private static readonly RecipeData Recipe = new RecipeData(
            new Ingredient(TechType.Titanium, 2),
            new Ingredient(TechType.CopperWire, 1),
            new Ingredient(TechType.Glass, 4));
        
        // Register the new prefab
        public static void Register() => PrefabInfo = RegisterInternal(ClassId, DisplayName, Description, IconAssetName, PrefabAssetName, Recipe);
    }
}