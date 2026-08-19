using Nautilus.Assets;
using Nautilus.Crafting;
using UnityEngine;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Describes the prototype Observatory Aquarium base piece.
    /// </summary>
    public class ObservatoryAquariumPrefab : BaseAquariumPrefab
    {
        private const string ClassId = "ObservatoryAquarium";
        private const string DisplayName = "Observatory Aquarium";
        private const string Description =
            "A prototype aquarium base piece built from an Observatory.";
        private const string ConfigurationPrefabAssetName =
            "ObservatoryAquariumPrefab.prefab";
        private const string IconAssetName = "ObservatoryAquariumIcon.png";
        
        private static readonly RecipeData Recipe = new RecipeData(
            new Ingredient(TechType.Titanium, 2),
            new Ingredient(TechType.EnameledGlass, 2));

        public static PrefabInfo PrefabInfo { get; private set; }

        /// <summary>
        /// Registers the Observatory Aquarium prototype.
        /// </summary>
        public static void Register()
        {
            PrefabInfo = RegisterInternal(
                ClassId, DisplayName, Description, IconAssetName, Recipe);
        }

        /// <summary>
        /// Adds aquarium functionality to a completed Observatory Aquarium module.
        /// </summary>
        internal static void ConfigureCompletedBasePiece(GameObject basePieceGameObject)
        {
            BaseAquariumConfigurator.ConfigureFromAssetBundle(
                basePieceGameObject, ConfigurationPrefabAssetName);
        }
    }
}
