using System.Collections.Generic;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using UnityEngine;
using static DaftAppleGames.SeaTruckFishScoop_BZ.SeaTruckFishScoopPluginBz;

namespace DaftAppleGames.SeaTruckFishScoop_BZ
{
    /// <summary>
    /// Set up the prefab for the new upgrade module, for use in a standard upgrade workbench
    /// </summary>
    public static class FishScoopModulePrefab
    {
        internal static PrefabInfo PrefabInfo;
        private const float EnergyCost = 0.0f;
        
        private const string IconAssetName = "FishScoopIcon.png";
        internal static void Init()
        {
            Log.LogDebug("Initialising FishScoop Upgrade Module...");
            PrefabInfo = PrefabInfo.WithTechType("SeaTruckFishScoopUpgrade", "SeaTruck Fish Scoop", "Scoop fish directly into attached aquariums. Great for stocking up on bio-fuel!", unlockAtStart: true)
                .WithIcon(GetIconSprite());
            CustomPrefab prefab = new CustomPrefab(PrefabInfo);

            CloneTemplate clone = new CloneTemplate(PrefabInfo, TechType.SeaTruckUpgradePerimeterDefense);
            prefab.SetGameObject(clone);

            Log.LogDebug("Set Recipe...");
            prefab.SetRecipe(new RecipeData()
                {
                    craftAmount = 1,
                    Ingredients = new List<Ingredient>()
                    {
                        new Ingredient(TechType.ComputerChip, 1),
                        new Ingredient(TechType.AdvancedWiringKit, 1),
                        new Ingredient(TechType.Titanium, 2)
                    }
                })
                .WithCraftingTime(5f)
                .WithFabricatorType(CraftTree.Type.Workbench);
            
            prefab.SetVehicleUpgradeModule(EquipmentType.SeaTruckModule, QuickSlotType.Toggleable)
                .WithEnergyCost(EnergyCost)
                // Currently, BZ doesn't seem to actually implement Toggleable, so this is handled manually
                // in SeaTruckUpgradesPatches and in the FishScoop component
                // .WithOnModuleToggled(ScoopToggled)
                .WithOnModuleAdded(ScoopAdded)
                .WithOnModuleRemoved(ScoopRemoved);
            
            prefab.Register();
        }

        /// <summary>
        /// Use for debugging
        /// </summary>
        private static void ScoopAdded(SeaTruckUpgrades upgrade, SeaTruckMotor seaTruck, int slotId)
        {
            Log.LogDebug($"Fish Scoop upgrade added in slot {slotId} on {seaTruck}.");
            seaTruck.GetComponent<FishScoop>()?.Equip(slotId);
        }
        
        /// <summary>
        /// Turn off the scoop if the upgrade is removed
        /// </summary>
        private static void ScoopRemoved(SeaTruckUpgrades upgrade, SeaTruckMotor seaTruck, int slotId)
        {
            Log.LogDebug($"Fish Scoop upgrade removed from slot {slotId} on {seaTruck}.");
            seaTruck.GetComponent<FishScoop>()?.Unequip(slotId);
        }
        
        /// <summary>
        /// Use for debugging
        /// </summary>
        private static void ScoopToggled(SeaTruckUpgrades upgrade, SeaTruckMotor seaTruck, int slotId, float charge, bool toggled)
        {
            Log.LogDebug($"Fish Scoop toggled to {toggled} on {seaTruck} .");
        }

        /// <summary>
        /// Get the icon sprite from the Asset Bundle
        /// </summary>
        /// <returns></returns>
        private static Sprite GetIconSprite()
        {
            Texture2D iconTexture = CustomAssetBundleUtils.GetObjectFromAssetBundle<Texture2D>(IconAssetName) as Texture2D;
            Sprite iconSprite = CustomAssetBundleUtils.GetSpriteFromTexture(iconTexture);
            return iconSprite;
        }
    }
}
