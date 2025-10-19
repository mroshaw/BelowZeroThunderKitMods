using Nautilus.Crafting;

namespace DaftAppleGames.BiggerAquariums
{
    public class CurvedAquarium : BiggerAquarium
    {
        // Non-static properties of the aquarium component
        public override int StorageHeight => 4;
        public override int StorageWidth => 4;
        
        // Static properties of the prefab
        private static readonly PrefabData Data = new PrefabData
        {
            ClassId = "CurvedAquarium",
            DisplayName = "Curved Aquarium",
            Description = "A double-sized, curved aquarium for use in long rooms.",
            IconAssetName = "CurvedAquariumIcon.png",
            PrefabAssetName = "CurvedAquariumPrefab.prefab",
            AquariumType = BiggerAquariumType.Curved,
            
            // Recipe for the builder
            Recipe = new RecipeData(
                new Ingredient(TechType.Titanium, 3),
                new Ingredient(TechType.CopperWire, 2),
                new Ingredient(TechType.Glass, 5)),
        };
        
        public static void Register() => RegisterInternal(Data);
    }
}