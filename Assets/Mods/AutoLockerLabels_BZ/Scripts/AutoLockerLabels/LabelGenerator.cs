using System.Collections.Generic;
using static DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabelsPlugin;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    internal static class LabelGenerator
    {
        private const string MixedCategoryKey = "AutoLockerLabels_Category_Mixed";
        private const string MixedCategoryFallback = "Mixed";

        private const string EmptyCategoryKey = "AutoLockerLabels_Category_Empty";
        private const string EmptyCategoryFallback = "Empty";
        
        private static readonly AutomaticLabelCategory[] Categories =
        {
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Metals",
                "Metals",
                new[]
                {
                    TechType.Titanium,
                    TechType.Copper,
                    TechType.Silver,
                    TechType.Gold,
                    TechType.Lead,
                    TechType.Lithium,
                    TechType.Nickel,
                    TechType.Magnetite
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Crystals",
                "Crystals",
                new[]
                {
                    TechType.Quartz,
                    TechType.Diamond,
                    TechType.AluminumOxide,
                    TechType.Kyanite
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Batteries",
                "Batteries",
                new[]
                {
                    TechType.Battery,
                    TechType.PrecursorIonBattery,
                    TechType.PowerCell,
                    TechType.PrecursorIonPowerCell
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Electronics",
                "Electronics",
                new[]
                {
                    TechType.Battery,
                    TechType.PrecursorIonBattery,
                    TechType.PowerCell,
                    TechType.PrecursorIonPowerCell,
                    TechType.CopperWire,
                    TechType.WiringKit,
                    TechType.AdvancedWiringKit,
                    TechType.ComputerChip,
                    TechType.ReactorRod
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Food",
                "Food",
                new[]
                {
                    TechType.NutrientBlock,
                    TechType.Snack1,
                    TechType.Snack2,
                    TechType.Snack3,
                    TechType.Coffee,
                    TechType.HoleFish,
                    TechType.Peeper,
                    TechType.Bladderfish,
                    TechType.GarryFish,
                    TechType.Hoverfish,
                    TechType.Reginald,
                    TechType.Spadefish,
                    TechType.Boomerang,
                    TechType.LavaBoomerang,
                    TechType.Eyeye,
                    TechType.LavaEyeye,
                    TechType.Oculus,
                    TechType.Hoopfish,
                    TechType.Spinefish,
                    TechType.CookedHoleFish,
                    TechType.CookedPeeper,
                    TechType.CookedBladderfish,
                    TechType.CookedGarryFish,
                    TechType.CookedHoverfish,
                    TechType.CookedReginald,
                    TechType.CookedSpadefish,
                    TechType.CookedBoomerang,
                    TechType.CookedLavaBoomerang,
                    TechType.CookedEyeye,
                    TechType.CookedLavaEyeye,
                    TechType.CookedOculus,
                    TechType.CookedHoopfish,
                    TechType.CookedSpinefish,
                    TechType.CuredHoleFish,
                    TechType.CuredPeeper,
                    TechType.CuredBladderfish,
                    TechType.CuredGarryFish,
                    TechType.CuredHoverfish,
                    TechType.CuredReginald,
                    TechType.CuredSpadefish,
                    TechType.CuredBoomerang,
                    TechType.CuredLavaBoomerang,
                    TechType.CuredEyeye,
                    TechType.CuredLavaEyeye,
                    TechType.CuredOculus,
                    TechType.CuredHoopfish,
                    TechType.CuredSpinefish,
                    TechType.BulboTreePiece,
                    TechType.PurpleVegetable,
                    TechType.HangingFruit,
                    TechType.Melon,
                    TechType.SmallMelon
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Water",
                "Water",
                new[]
                {
                    TechType.FilteredWater,
                    TechType.DisinfectedWater,
                    TechType.BigFilteredWater,
                    TechType.WaterFiltrationSuitWater
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Tools",
                "Tools",
                new[]
                {
                    TechType.Scanner,
                    TechType.Welder,
                    TechType.Flashlight,
                    TechType.Knife,
                    TechType.DiveReel,
                    TechType.AirBladder,
                    TechType.Flare,
                    TechType.Builder,
                    TechType.LaserCutter,
                    TechType.StasisRifle,
                    TechType.Terraformer,
                    TechType.PropulsionCannon,
                    TechType.RepulsionCannon,
                    TechType.Seaglide,
                    TechType.Constructor,
                    TechType.Beacon,
                    TechType.Gravsphere,
                    TechType.SmallStorage,
                    TechType.CyclopsDecoy,
                    TechType.LEDLight,
                    TechType.Transfuser
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Equipment",
                "Equipment",
                new[]
                {
                    TechType.Tank,
                    TechType.DoubleTank,
                    TechType.PlasteelTank,
                    TechType.HighCapacityTank,
                    TechType.Fins,
                    TechType.UltraGlideFins,
                    TechType.SwimChargeFins,
                    TechType.RadiationSuit,
                    TechType.RadiationHelmet,
                    TechType.RadiationGloves,
                    TechType.ReinforcedDiveSuit,
                    TechType.ReinforcedGloves,
                    TechType.WaterFiltrationSuit,
                    TechType.FirstAidKit,
                    TechType.FireExtinguisher,
                    TechType.Rebreather,
                    TechType.Compass,
                    TechType.Thermometer,
                    TechType.MapRoomHUDChip,
                    TechType.MapRoomCamera,
                    TechType.Pipe,
                    TechType.PipeSurfaceFloater,
                    TechType.PrecursorKey_Purple,
                    TechType.PrecursorKey_Blue,
                    TechType.PrecursorKey_Orange
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Materials",
                "Materials",
                new[]
                {
                    TechType.Titanium,
                    TechType.TitaniumIngot,
                    TechType.FiberMesh,
                    TechType.Silicone,
                    TechType.Glass,
                    TechType.Bleach,
                    TechType.Lubricant,
                    TechType.EnameledGlass,
                    TechType.PlasteelIngot,
                    TechType.HydrochloricAcid,
                    TechType.Benzene,
                    TechType.AramidFibers,
                    TechType.Aerogel,
                    TechType.Polyaniline,
                    TechType.HatchingEnzymes
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Vehicle_Upgrades",
                "Vehicle Upgrades",
                new[]
                {
                    TechType.HullReinforcementModule,
                    TechType.PowerUpgradeModule,
                    TechType.CyclopsHullModule1,
                    TechType.CyclopsHullModule2,
                    TechType.CyclopsHullModule3,
                    TechType.CyclopsShieldModule,
                    TechType.CyclopsSonarModule,
                    TechType.CyclopsSeamothRepairModule,
                    TechType.CyclopsDecoyModule,
                    TechType.CyclopsFireSuppressionModule,
                    TechType.CyclopsThermalReactorModule,
                    TechType.SeamothReinforcementModule,
                    TechType.VehiclePowerUpgradeModule,
                    TechType.SeamothSolarCharge,
                    TechType.VehicleStorageModule,
                    TechType.SeamothElectricalDefense,
                    TechType.VehicleArmorPlating,
                    TechType.SeamothTorpedoModule,
                    TechType.SeamothSonarModule,
                    TechType.VehicleHullModule1,
                    TechType.VehicleHullModule2,
                    TechType.VehicleHullModule3,
                    TechType.ExosuitJetUpgradeModule,
                    TechType.ExosuitDrillArmModule,
                    TechType.ExosuitThermalReactorModule,
                    TechType.ExosuitClawArmModule,
                    TechType.ExosuitPropulsionArmModule,
                    TechType.ExosuitGrapplingArmModule,
                    TechType.ExosuitTorpedoArmModule,
                    TechType.GasTorpedo,
                    TechType.WhirlpoolTorpedo
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Eggs",
                "Eggs",
                new[]
                {
                    TechType.SafeShallowsEgg,
                    TechType.KelpForestEgg,
                    TechType.GrassyPlateausEgg,
                    TechType.GrandReefsEgg,
                    TechType.MushroomForestEgg,
                    TechType.KooshZoneEgg,
                    TechType.TwistyBridgesEgg,
                    TechType.LavaZoneEgg,
                    TechType.StalkerEgg,
                    TechType.ReefbackEgg,
                    TechType.SpadefishEgg,
                    TechType.RabbitrayEgg,
                    TechType.MesmerEgg,
                    TechType.JumperEgg,
                    TechType.SandsharkEgg,
                    TechType.JellyrayEgg,
                    TechType.BonesharkEgg,
                    TechType.CrabsnakeEgg,
                    TechType.ShockerEgg,
                    TechType.GasopodEgg,
                    TechType.CrashEgg,
                    TechType.CrabsquidEgg,
                    TechType.CutefishEgg,
                    TechType.LavaLizardEgg
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Collectibles",
                "Collectibles",
                new[]
                {
                    TechType.ArcadeGorgetoy,
                    TechType.LabEquipment1,
                    TechType.LabEquipment2,
                    TechType.LabEquipment3,
                    TechType.Cap1,
                    TechType.Cap2,
                    TechType.StarshipSouvenir,
                    TechType.PosterAurora,
                    TechType.PosterExoSuit1,
                    TechType.PosterExoSuit2,
                    TechType.PosterKitty,
                    TechType.ToyCar
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Raw_Materials",
                "Raw Materials",
                new[]
                {
                    TechType.Titanium,
                    TechType.Copper,
                    TechType.Silver,
                    TechType.Gold,
                    TechType.Lead,
                    TechType.Lithium,
                    TechType.Nickel,
                    TechType.Magnetite,
                    TechType.Quartz,
                    TechType.Diamond,
                    TechType.AluminumOxide,
                    TechType.Kyanite,
                    TechType.Salt,
                    TechType.Sulphur,
                    TechType.UraniniteCrystal,
                    TechType.PrecursorIonCrystal,
                    TechType.ScrapMetal,
                    TechType.DepletedReactorRod,
                    TechType.StalkerTooth,
                    TechType.GasPod,
                    TechType.Floater,
                    TechType.BloodOil,
                    TechType.AcidMushroom,
                    TechType.WhiteMushroom,
                    TechType.JellyPlant,
                    TechType.CreepvinePiece,
                    TechType.CreepvineSeedCluster,
                    TechType.CoralChunk,
                    TechType.JeweledDiskPiece,
                    TechType.KooshChunk,
                    TechType.TreeMushroomPiece,
                    TechType.PurpleBrainCoralPiece,
                    TechType.SeaTreaderPoop,
                    TechType.OrangeMushroomSpore,
                    TechType.PurpleVasePlantSeed,
                    TechType.AcidMushroomSpore,
                    TechType.WhiteMushroomSpore,
                    TechType.PinkMushroomSpore,
                    TechType.PurpleRattleSpore,
                    TechType.MelonSeed,
                    TechType.SpikePlantSeed,
                    TechType.BluePalmSeed,
                    TechType.PurpleFanSeed,
                    TechType.SmallFanSeed,
                    TechType.PurpleTentacleSeed,
                    TechType.JellyPlantSeed,
                    TechType.GabeSFeatherSeed,
                    TechType.SeaCrownSeed,
                    TechType.MembrainTreeSeed,
                    TechType.PinkFlowerSeed,
                    TechType.FernPalmSeed,
                    TechType.OrangePetalsPlantSeed,
                    TechType.EyesPlantSeed,
                    TechType.RedGreenTentacleSeed,
                    TechType.PurpleStalkSeed,
                    TechType.RedBasketPlantSeed,
                    TechType.RedBushSeed,
                    TechType.RedConePlantSeed,
                    TechType.ShellGrassSeed,
                    TechType.SpottedLeavesPlantSeed,
                    TechType.RedRollPlantSeed,
                    TechType.PurpleBranchesSeed,
                    TechType.SnakeMushroomSpore
                })
        };

        internal static string Generate(ItemsContainer container)
        {
            if (container == null || container.count == 0)
            {
                return GetLocalizedLabel(EmptyCategoryKey, EmptyCategoryFallback);
            }

            List<TechType> itemTypes = container.GetItemTypes();

            if (itemTypes.Count == 1)
            {
                return GetItemLabel(itemTypes[0]);
            }

            int totalCount = 0;
            int highestCount = 0;
            TechType dominantType = TechType.None;

            foreach (TechType techType in itemTypes)
            {
                int count = container.GetCount(techType);
                totalCount += count;

                if (count > highestCount)
                {
                    highestCount = count;
                    dominantType = techType;
                }
            }

            if (IsDominant(highestCount, totalCount))
            {
                return GetItemLabel(dominantType);
            }

            if (TryGetCommonCategoryLabel(
                    itemTypes,
                    out string categoryLabel))
            {
                return categoryLabel;
            }

            return GetLocalizedLabel(
                MixedCategoryKey,
                MixedCategoryFallback);
        }

        private static bool TryGetCommonCategoryLabel(
            List<TechType> itemTypes,
            out string categoryLabel)
        {
            foreach (AutomaticLabelCategory category in Categories)
            {
                if (!category.ContainsAll(itemTypes))
                {
                    continue;
                }

                categoryLabel = GetLocalizedLabel(
                    category.LanguageKey,
                    category.FallbackLabel);
                return true;
            }

            categoryLabel = string.Empty;
            return false;
        }

        private static bool IsDominant(
            int highestCount,
            int totalCount)
        {
            float dominantItemRatio = ConfigFile.DominantItemRatio / 100f;
            if (totalCount <= 0)
            {
                return false;
            }

            return (float)highestCount / totalCount >=
                   dominantItemRatio;
        }

        private static string GetItemLabel(TechType techType)
        {
            Language language = Language.main;
            string localizedName = language == null
                ? string.Empty
                : language.Get(techType);

            return string.IsNullOrWhiteSpace(localizedName)
                ? techType.ToString()
                : localizedName;
        }

        private static string GetLocalizedLabel(
            string languageKey,
            string fallbackLabel)
        {
            Language language = Language.main;

            if (language == null)
            {
                return fallbackLabel;
            }

            string localizedLabel = language.Get(languageKey);

            if (string.IsNullOrWhiteSpace(localizedLabel) ||
                localizedLabel == languageKey)
            {
                return fallbackLabel;
            }

            return localizedLabel;
        }

        private sealed class AutomaticLabelCategory
        {
            private readonly HashSet<TechType> itemTypes;

            internal string LanguageKey { get; }

            internal string FallbackLabel { get; }

            internal AutomaticLabelCategory(
                string languageKey,
                string fallbackLabel,
                IEnumerable<TechType> itemTypes)
            {
                LanguageKey = languageKey;
                FallbackLabel = fallbackLabel;
                this.itemTypes = new HashSet<TechType>(itemTypes);
            }

            internal bool ContainsAll(List<TechType> contents)
            {
                foreach (TechType techType in contents)
                {
                    if (!itemTypes.Contains(techType))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
