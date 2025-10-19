using Nautilus.Crafting;

namespace DaftAppleGames.BiggerAquariums
{
    public class DoubleAquarium : BiggerAquarium
    {
        // Non-static properties of the aquarium component
        public override int StorageHeight => 4;
        public override int StorageWidth => 4;
        
        // Properties of the Aquarium
        private static readonly PrefabData Data = new PrefabData
        {
            ClassId = "DoubleAquarium",
            DisplayName = "Double Aquarium",
            Description = "A double-sized aquarium for use in long rooms.",
            IconAssetName = "DoubleAquariumIcon.png",
            PrefabAssetName = "DoubleAquariumPrefab.prefab",
            AquariumType = BiggerAquariumType.Double,
            
            // Recipe for the builder
            Recipe = new RecipeData(
                new Ingredient(TechType.Titanium, 2),
                new Ingredient(TechType.CopperWire, 1),
                new Ingredient(TechType.Glass, 4)),
        };
        
        public static void Register() => RegisterInternal(Data);
    }
}