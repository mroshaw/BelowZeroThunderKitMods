using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Provides registration shared by Observatory-derived aquarium base pieces.
    /// </summary>
    public abstract class BaseAquariumPrefab
    {
        private const TechType CloneTechType = TechType.BaseObservatory;

        /// <summary>
        /// Registers an unchanged Observatory clone as a custom base piece.
        /// </summary>
        internal static PrefabInfo RegisterInternal(string classId, string displayName,
            string description, string iconAssetName, RecipeData recipeData)
        {
            PrefabInfo info = PrefabInfo
                .WithTechType(classId, displayName, description, unlockAtStart: true)
                .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(iconAssetName) as Sprite);
            CustomPrefab baseAquariumPrefab = new CustomPrefab(info);

            CloneTemplate observatoryTemplate = new CloneTemplate(
                baseAquariumPrefab.Info, CloneTechType)
            {
                ModifyPrefab = prefabGameObject => ConfigureClonedPrefab(
                    prefabGameObject, displayName, info.TechType)
            };

            baseAquariumPrefab.SetGameObject(observatoryTemplate);
            baseAquariumPrefab.SetRecipe(recipeData);
            baseAquariumPrefab.SetPdaGroupCategoryAfter(
                TechGroup.BasePieces, TechCategory.BasePiece, CloneTechType);
            baseAquariumPrefab.Register();

            ModDebugLog.LogDebug(
                $"{displayName} prototype registered as an unchanged Observatory clone.");
            return info;
        }

        /// <summary>
        /// Applies the custom identity and confirms that the clone remains constructable.
        /// </summary>
        private static void ConfigureClonedPrefab(GameObject prefabGameObject,
            string displayName, TechType techType)
        {
            if (!prefabGameObject)
            {
                ModDebugLog.LogError(
                    $"Could not create the {displayName} Observatory clone.");
                return;
            }

            BaseDeconstructable[] deconstructableComponents =
                prefabGameObject.GetComponentsInChildren<BaseDeconstructable>(true);
            foreach (BaseDeconstructable deconstructableComponent in
                     deconstructableComponents)
            {
                deconstructableComponent.recipe = techType;
            }

            Constructable constructable = prefabGameObject.GetComponent<Constructable>();
            if (!constructable)
            {
                ModDebugLog.LogError(
                    $"The {displayName} Observatory clone has no Constructable component.");
                return;
            }

            if (!constructable.model)
            {
                ModDebugLog.LogError(
                    $"The {displayName} Observatory clone has no construction model.");
                return;
            }

            if (!constructable.model.GetComponent<BaseAquariumGhost>())
            {
                constructable.model.AddComponent<BaseAquariumGhost>();
            }

            ModDebugLog.LogDebug(
                $"Validated {displayName} clone. Model: {constructable.model.name}. " +
                $"Updated {deconstructableComponents.Length} deconstructable recipes and " +
                $"added the base aquarium ghost marker.");
        }
    }
}
