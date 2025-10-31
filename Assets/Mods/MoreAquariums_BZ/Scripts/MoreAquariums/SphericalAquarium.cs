using Nautilus.Crafting;

namespace DaftAppleGames.MoreAquariums
{
    public class SphericalAquarium : AquariumBase
    {
        // Non-static properties of the aquarium component
        public override int StorageHeight => 4;
        public override int StorageWidth => 4;
        
        // Properties of the Aquarium
        private static readonly PrefabData Data = new PrefabData
        {
            ClassId = "SphericalAquarium",
            DisplayName = "Spherical Aquarium",
            Description = "A spherical aquarium with capacity for 16 fish.",
            IconAssetName = "SphericalAquariumIcon.png",
            PrefabAssetName = "SphericalAquariumPrefab.prefab",
            StorageHeight = 4,
            StorageWidth = 4,
            AllowConstructionOnConstructables = false,
            AquariumType = AquariumType.Spherical,
            UseCustomMovement = true,
            WaveScale = 1.0f,
            ReplaceModel = true,
            AddBubbleAudio = true,
            
            // Recipe for the builder
            Recipe = new RecipeData(
                new Ingredient(TechType.Titanium, 2),
                new Ingredient(TechType.CopperWire, 1),
                new Ingredient(TechType.Glass, 4)),
        };
        
        public static void Register() => RegisterInternal(Data);
    }
}