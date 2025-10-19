using Nautilus.Crafting;

namespace DaftAppleGames.BiggerAquariums
{
    public class CornerAquarium : BiggerAquarium
    {
        // Non-static properties of the aquarium component
        public override int StorageHeight => 4;
        public override int StorageWidth => 4;
        
        // Static properties of the prefab
        private static readonly PrefabData Data = new PrefabData
        {
            ClassId = "CornerAquarium",
            DisplayName = "Corner Aquarium",
            Description = "A double-sized, corner aquarium.",
            IconAssetName = "CornerAquariumIcon.png",
            PrefabAssetName = "CornerAquariumPrefab.prefab",
            AquariumType = BiggerAquariumType.Corner,
            
            // Recipe for the builder
            Recipe = new RecipeData(
                new Ingredient(TechType.Titanium, 3),
                new Ingredient(TechType.CopperWire, 1),
                new Ingredient(TechType.Glass, 5)),
        };
        
        public static void Register() => RegisterInternal(Data);
    }
}