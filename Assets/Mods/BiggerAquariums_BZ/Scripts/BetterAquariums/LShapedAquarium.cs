using Nautilus.Crafting;

namespace DaftAppleGames.BiggerAquariums
{
    internal class LShapedAquarium : BiggerAquarium
    {
        // Non-static properties of the aquarium component
        public override int StorageHeight => 4;
        public override int StorageWidth => 4;
        
        // Properties of the Aquarium
        private static readonly PrefabData Data = new PrefabData
        {
            ClassId = "LShapedAquarium",
            DisplayName = "L Shaped Aquarium",
            Description = "A double-sized, L shaped aquarium for use in long rooms.",
            IconAssetName = "LShapedAquariumIcon.png",
            PrefabAssetName = "LShapedAquariumPrefab.prefab",
            AquariumType = BiggerAquariumType.LShaped,
            
            // Recipe for the builder
            Recipe = new RecipeData(
                new Ingredient(TechType.Titanium, 3),
                new Ingredient(TechType.CopperWire, 2),
                new Ingredient(TechType.Glass, 5)),
        };
        
        public static void Register() => RegisterInternal(Data);
    }
}