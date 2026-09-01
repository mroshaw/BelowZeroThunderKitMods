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
                    TechType.Kyanite,
                    TechType.PrecursorIonCrystal,
                    TechType.PrecursorCacheCrystal
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
                    TechType.ReactorRod,
                    TechType.RadioTowerPPU,
                    TechType.RadioTowerTOM
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Food",
                "Food",
                new[]
                {
                    TechType.NutrientBlock,
                    TechType.Coffee,
                    TechType.ArcticPeeper,
                    TechType.ArrowRay,
                    TechType.DiscusFish,
                    TechType.FeatherFish,
                    TechType.FeatherFishRed,
                    TechType.NootFish,
                    TechType.SpinnerFish,
                    TechType.Symbiote,
                    TechType.Triops,
                    TechType.Bladderfish,
                    TechType.Boomerang,
                    TechType.Hoopfish,
                    TechType.Spinefish,
                    TechType.CookedBladderfish,
                    TechType.CookedBoomerang,
                    TechType.CookedHoopfish,
                    TechType.CookedSpinefish,
                    TechType.CuredBladderfish,
                    TechType.CuredBoomerang,
                    TechType.CuredHoopfish,
                    TechType.CuredSpinefish,
                    TechType.CookedArcticPeeper,
                    TechType.CookedArrowRay,
                    TechType.CookedDiscusFish,
                    TechType.CookedFeatherFish,
                    TechType.CookedFeatherFishRed,
                    TechType.CookedNootFish,
                    TechType.CookedSpinnerfish,
                    TechType.CookedSymbiote,
                    TechType.CookedTriops,
                    TechType.CuredArcticPeeper,
                    TechType.CuredArrowRay,
                    TechType.CuredDiscusFish,
                    TechType.CuredFeatherFish,
                    TechType.CuredFeatherFishRed,
                    TechType.CuredNootFish,
                    TechType.CuredSpinnerfish,
                    TechType.CuredSymbiote,
                    TechType.CuredTriops,
                    TechType.PurpleVegetable,
                    TechType.HangingFruit,
                    TechType.Melon,
                    TechType.SmallMelon,
                    TechType.HeatFruit,
                    TechType.LeafyFruit,
                    TechType.IceFruit,
                    TechType.SmallMaroonPlantFruit,
                    TechType.SnowStalkerFruit,
                    TechType.SnowStalkerPlantLeaf,
                    TechType.SpicyFruitSalad
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Water",
                "Water",
                new[]
                {
                    TechType.FilteredWater,
                    TechType.DisinfectedWater,
                    TechType.BigFilteredWater,
                    TechType.WaterFiltrationSuitWater,
                    TechType.WaterPurificationTablet
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
                    TechType.PropulsionCannon,
                    TechType.Seaglide,
                    TechType.Constructor,
                    TechType.Beacon,
                    TechType.Gravsphere,
                    TechType.SmallStorage,
                    TechType.Thumper,
                    TechType.TeleportationTool,
                    TechType.MetalDetector,
                    TechType.QuantumLocker,
                    TechType.SpyPenguin,
                    TechType.SpyPenguinRemote,
                    TechType.LEDLight,
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
                    TechType.FlashlightHelmet,
                    TechType.ColdSuit,
                    TechType.ColdSuitGloves,
                    TechType.ColdSuitHelmet,
                    TechType.SuitBoosterTank,
                    TechType.ReinforcedDiveSuit,
                    TechType.ReinforcedGloves,
                    TechType.WaterFiltrationSuit,
                    TechType.FirstAidKit,
                    TechType.Rebreather,
                    TechType.Compass,
                    TechType.MapRoomHUDChip,
                    TechType.MapRoomCamera,
                    TechType.Pipe,
                    TechType.PipeSurfaceFloater,
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
                    TechType.Lubricant,
                    TechType.EnameledGlass,
                    TechType.PlasteelIngot,
                    TechType.HydrochloricAcid,
                    TechType.Benzene,
                    TechType.AramidFibers,
                    TechType.Aerogel,
                    TechType.Polyaniline,
                    TechType.HydraulicFluid,
                    TechType.ReinforcedGlass,
                    TechType.FrozenCreatureAntidote,
                    TechType.RadioTowerPPU,
                    TechType.RadioTowerTOM,
                    TechType.PrecursorNPCOrgans,
                    TechType.PrecursorNPCTissue,
                    TechType.PrecursorNPCSkeleton
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Vehicle_Upgrades",
                "Vehicle Upgrades",
                new[]
                {
                    TechType.VehicleStorageModule,
                    TechType.ExosuitJetUpgradeModule,
                    TechType.ExosuitDrillArmModule,
                    TechType.ExosuitThermalReactorModule,
                    TechType.ExosuitClawArmModule,
                    TechType.ExosuitPropulsionArmModule,
                    TechType.ExosuitGrapplingArmModule,
                    TechType.ExosuitTorpedoArmModule,
                    TechType.HoverbikeJumpModule,
                    TechType.HoverbikeIceWormReductionModule,
                    TechType.SeaTruckUpgradeAfterburner,
                    TechType.SeaTruckUpgradeEnergyEfficiency,
                    TechType.SeaTruckUpgradePerimeterDefense,
                    TechType.SeaTruckUpgradeHorsePower,
                    TechType.SeaTruckUpgradeHull1,
                    TechType.SeaTruckUpgradeHull2,
                    TechType.SeaTruckUpgradeHull3,
                    TechType.GasTorpedo,
                    TechType.WhirlpoolTorpedo
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Eggs",
                "Eggs",
                new[]
                {
                    TechType.LavaZoneEgg,
                    TechType.ShockerEgg,
                    TechType.SeaMonkeyEgg,
                    TechType.ArcticRayEgg,
                    TechType.BruteSharkEgg,
                    TechType.LilyPaddlerEgg,
                    TechType.PinnacaridEgg,
                    TechType.SquidSharkEgg,
                    TechType.TitanHolefishEgg,
                    TechType.TrivalveBlueEgg,
                    TechType.TrivalveYellowEgg,
                    TechType.BrinewingEgg,
                    TechType.CryptosuchusEgg,
                    TechType.GlowWhaleEgg,
                    TechType.JellyfishEgg,
                    TechType.PenguinEgg,
                    TechType.RockPuncherEgg
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Creatures",
                "Creatures",
                new[]
                {
                    TechType.PenguinBaby,
                    TechType.SeaMonkeyBaby,
                    TechType.Rockgrub
                }),
            new AutomaticLabelCategory(
                "AutoLockerLabels_Category_Collectibles",
                "Collectibles",
                new[]
                {
                    TechType.PosterAurora,
                    TechType.PosterExoSuit1,
                    TechType.PosterExoSuit2,
                    TechType.PosterKitty,
                    TechType.Poster,
                    TechType.PosterSpyPenguin,
                    TechType.PosterMotivational,
                    TechType.PosterSeatruck,
                    TechType.PosterLilArchitect,
                    TechType.PictureLilKids1,
                    TechType.PosterJeremiahNoBirds,
                    TechType.PictureFredPengling,
                    TechType.PictureVinhPostcard,
                    TechType.PictureSamPotato,
                    TechType.EmmanuelPendulum,
                    TechType.FredShavingKit,
                    TechType.PosterSeatruck2,
                    TechType.PosterMercury,
                    TechType.PicturePotatoPortrait,
                    TechType.PictureLilKids2,
                    TechType.PosterMotivational2,
                    TechType.PosterMotivational3,
                    TechType.PictureSamDanielleHappy,
                    TechType.AromatherapyLamp,
                    TechType.PictureVinhBiologyArt,
                    TechType.PictureDanielleAbstractArt,
                    TechType.SamNecklace,
                    TechType.PictureSamHand,
                    TechType.PosterAlterraBounty,
                    TechType.PosterParvan,
                    TechType.PosterBunkerCommunity,
                    TechType.SpyPenguinMap,
                    TechType.PosterZetaRollerDerby,
                    TechType.PosterSpyPenguinConcepts,
                    TechType.PosterBoardgame,
                    TechType.PosterHangInThere,
                    TechType.PosterSpyPenguinBlueprint,
                    TechType.PosterParvanBiome,
                    TechType.SnowBall
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
                    TechType.PrecursorCacheCrystal,
                    TechType.ScrapMetal,
                    TechType.DepletedReactorRod,
                    TechType.JellyPlant,
                    TechType.CreepvinePiece,
                    TechType.CreepvineSeedCluster,
                    TechType.JeweledDiskPiece,
                    TechType.TreeMushroomPiece,
                    TechType.SnowStalkerFur,
                    TechType.GenericRibbon,
                    TechType.GenericRibbonSeed,
                    TechType.KelpRootPustule,
                    TechType.GenericSpiralChunk,
                    TechType.LilyPadResource,
                    TechType.DeepLilyShroom,
                    TechType.TwistyBridgesMushroomChunk,
                    TechType.FrozenRiverPlant2Seeds,
                    TechType.SmallMaroonPlantSeed,
                    TechType.MelonSeed,
                    TechType.JellyPlantSeed,
                    TechType.PurpleStalkSeed,
                    TechType.RedBushSeed,
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
