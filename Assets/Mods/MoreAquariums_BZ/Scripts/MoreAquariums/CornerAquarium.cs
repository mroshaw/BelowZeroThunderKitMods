using Nautilus.Crafting;

namespace DaftAppleGames.MoreAquariums
{
    public class CornerAquarium : AquariumBase
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
            AquariumType = AquariumType.Corner,
            StorageHeight = 4,
            StorageWidth = 4,
            AllowConstructionOnConstructables = false,
            UseCustomMovement = false,
            WaveScale = 1.0f,
            ReplaceModel = false,
            
            // Recipe for the builder
            Recipe = new RecipeData(
                new Ingredient(TechType.Titanium, 3),
                new Ingredient(TechType.CopperWire, 1),
                new Ingredient(TechType.Glass, 5)),
        };
        
        public static void Register() => RegisterInternal(Data);
    }
}