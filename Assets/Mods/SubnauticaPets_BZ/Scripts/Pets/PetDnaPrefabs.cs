using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Utility;
using UnityEngine;
using UWE;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    ///     Creates the Pet DNA collectible objects and their world distributions.
    /// </summary>
    internal static class PetDnaPrefabs
    {
        private const string PrefabAssetName = "DNASampleTube.prefab";
        private const string EncKey = "PetDna";
        private const string EncPath = "Lifeforms/Fauna";
        private const string DatabankPopupImageAssetName = "PetDnaDataBankPopupImageTexture.png";
        private const string DatabankMainImageAssetName = "PetDnaDataBankMainImageTexture.png";

        private const int SpawnCount = 1;
        private const float NestSpawnProbability = 10.0f;

        private static readonly BiomeType[] ArcticSeaMonkeyNestBiomes =
        {
            BiomeType.ArcticKelp_SeamonkeyNest1,
            BiomeType.ArcticKelp_SeamonkeyNest2,
            BiomeType.ArcticKelp_SeamonkeyNest3,
            BiomeType.ArcticKelp_SeamonkeyNest4,
            BiomeType.ArcticKelp_SeamonkeyNest5
        };

        private static readonly BiomeType[] LilyPadsSeaMonkeyNestBiomes =
        {
            BiomeType.LilyPads_Crevice_SeamonkeyNest1,
            BiomeType.LilyPads_Crevice_SeamonkeyNest2,
            BiomeType.LilyPads_Crevice_SeamonkeyNest3,
            BiomeType.LilyPads_Crevice_SeamonkeyNest4,
            BiomeType.LilyPads_Crevice_SeamonkeyNest5
        };

        /// <summary>
        ///     Registers all DNA prefabs.
        /// </summary>
        internal static void RegisterAll()
        {
            GameObject dnaModelPrefab =
                ModAssetUtils.GetObjectFromAssetBundle<GameObject>(PrefabAssetName) as GameObject;
            MaterialUtils.ApplySNShaders(dnaModelPrefab);

            // CatDnaPrefab.Register(dnaModelPrefab);
            PengwingAdultDnaPrefab.Register(dnaModelPrefab);
            PenglingBabyDnaPrefab.Register(dnaModelPrefab);
            PinnacaridDnaPrefab.Register(dnaModelPrefab);
            SnowstalkerBabyDnaPrefab.Register(dnaModelPrefab);
            TrivalveBlueDnaPrefab.Register(dnaModelPrefab);
            TrivalveYellowDnaPrefab.Register(dnaModelPrefab);
            ConfigureDataBank();
        }

        private static PrefabInfo RegisterDnaPrefab(string classId, string textureName, Color color,
            GameObject dnaModelPrefab, NestDistribution nestDistribution, params SpawnBiome[] spawnBiomes)
        {
            ModDebugLog.LogDebug($"PetDnaPrefab: Register Prefab for {classId}...");

            PrefabInfo prefabInfo = PrefabInfo
                .WithTechType(classId, null, null, unlockAtStart: true)
                .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(textureName) as Sprite);

            CustomPrefab customPrefab = new CustomPrefab(prefabInfo);
            CloneTemplate cloneTemplate = new CloneTemplate(customPrefab.Info, TechType.Quartz)
            {
                ModifyPrefab = prefab =>
                {
                    prefab.SetActive(false);

                    GameObject oldModel = prefab.GetComponentInChildren<MeshRenderer>(true).gameObject;
                    oldModel.SetActive(false);

                    GameObject newModel = Object.Instantiate(dnaModelPrefab, prefab.transform, true);
                    newModel.name = "newmodel";
                    newModel.transform.localPosition = Vector3.zero;
                    newModel.transform.localRotation = Quaternion.identity;

                    MaterialUtils.ApplySNShaders(newModel);
                    PrefabUtils.AddBasicComponents(prefab, customPrefab.Info.ClassID, customPrefab.Info.TechType,
                        LargeWorldEntity.CellLevel.VeryFar);
                    PrefabUtils.AddResourceTracker(prefab, TechType.None);
                    PetPrefabConfigUtils.SetMeshRenderersColor(newModel, "Ends", color);
                    PetPrefabConfigUtils.AddRotateModel(newModel, "DNA");
                    PetPrefabConfigUtils.AddScaleOnStart(prefab, 0.4f);
                    prefab.AddComponent<PetDna>();
                }
            };

            WorldEntityInfo entityInfo = new WorldEntityInfo
            {
                classId = customPrefab.Info.ClassID,
                techType = customPrefab.Info.TechType,
                localScale = Vector3.one,
                cellLevel = LargeWorldEntity.CellLevel.VeryFar,
                slotType = EntitySlot.Type.Small
            };

            customPrefab.SetGameObject(cloneTemplate);
            customPrefab.SetSpawns(entityInfo, CreateBiomeData(spawnBiomes, nestDistribution));
            customPrefab.Register();
            return prefabInfo;
        }

        private static LootDistributionData.BiomeData[] CreateBiomeData(SpawnBiome[] biomes,
            NestDistribution nestDistribution)
        {
            int nestCount = 0;
            if ((nestDistribution & NestDistribution.ArcticKelp) != 0)
                nestCount += ArcticSeaMonkeyNestBiomes.Length;
            if ((nestDistribution & NestDistribution.LilyPads) != 0)
                nestCount += LilyPadsSeaMonkeyNestBiomes.Length;

            LootDistributionData.BiomeData[] distribution =
                new LootDistributionData.BiomeData[biomes.Length + nestCount];

            for (int index = 0; index < biomes.Length; index++)
            {
                distribution[index] = new LootDistributionData.BiomeData
                {
                    biome = biomes[index].Biome,
                    count = SpawnCount,
                    probability = biomes[index].Probability
                };
            }

            int destinationIndex = biomes.Length;
            if ((nestDistribution & NestDistribution.ArcticKelp) != 0)
                destinationIndex = AddNestBiomes(distribution, destinationIndex, ArcticSeaMonkeyNestBiomes);
            if ((nestDistribution & NestDistribution.LilyPads) != 0)
                AddNestBiomes(distribution, destinationIndex, LilyPadsSeaMonkeyNestBiomes);

            return distribution;
        }

        private static int AddNestBiomes(LootDistributionData.BiomeData[] distribution, int destinationIndex,
            BiomeType[] nestBiomes)
        {
            for (int index = 0; index < nestBiomes.Length; index++)
            {
                distribution[destinationIndex++] = new LootDistributionData.BiomeData
                {
                    biome = nestBiomes[index],
                    count = SpawnCount,
                    probability = NestSpawnProbability
                };
            }

            return destinationIndex;
        }

        private static SpawnBiome Spawn(BiomeType biome, float probability)
        {
            return new SpawnBiome(biome, probability);
        }

        private static void ConfigureDataBank()
        {
            PetPrefabConfigUtils.ConfigureDatabankEntry(EncKey, EncPath, DatabankMainImageAssetName,
                DatabankPopupImageAssetName);
            Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(EncKey, Story.GoalType.Encyclopedia,
                PengwingAdultDnaPrefab.Info.TechType);
            Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(EncKey, Story.GoalType.Encyclopedia,
                PenglingBabyDnaPrefab.Info.TechType);
            Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(EncKey, Story.GoalType.Encyclopedia,
                PinnacaridDnaPrefab.Info.TechType);
            Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(EncKey, Story.GoalType.Encyclopedia,
                SnowstalkerBabyDnaPrefab.Info.TechType);
            Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(EncKey, Story.GoalType.Encyclopedia,
                TrivalveYellowDnaPrefab.Info.TechType);
            Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(EncKey, Story.GoalType.Encyclopedia,
                TrivalveBlueDnaPrefab.Info.TechType);
        }

        [System.Flags]
        private enum NestDistribution
        {
            None = 0,
            ArcticKelp = 1,
            LilyPads = 2,
            All = ArcticKelp | LilyPads
        }

        private struct SpawnBiome
        {
            internal readonly BiomeType Biome;
            internal readonly float Probability;

            internal SpawnBiome(BiomeType biome, float probability)
            {
                Biome = biome;
                Probability = probability;
            }
        }

        internal static class CatDnaPrefab
        {
            private const string ClassId = "CatPetDna";
            private const string TextureAssetName = "CatDnaStrandTexture.png";
            internal static PrefabInfo Info;

            internal static void Register(GameObject dnaModelPrefab)
            {
                Info = RegisterDnaPrefab(ClassId, TextureAssetName, Color.grey, dnaModelPrefab, NestDistribution.All,
                    Spawn(BiomeType.EastArctic_Ground, 0.08f),
                    Spawn(BiomeType.WestArctic_Ground, 0.08f),
                    Spawn(BiomeType.TwistyBridges_Ground, 0.08f),
                    Spawn(BiomeType.GlacialBasin_BikeCrashSite, 0.08f));
            }
        }

        internal static class PengwingAdultDnaPrefab
        {
            private const string ClassId = "PengwingAdultPetDna";
            private const string TextureAssetName = "PengwingAdultDnaStrandTexture.png";
            internal static PrefabInfo Info;

            internal static void Register(GameObject dnaModelPrefab)
            {
                Info = RegisterDnaPrefab(ClassId, TextureAssetName, Color.grey, dnaModelPrefab,
                    NestDistribution.All,
                    Spawn(BiomeType.TwistyBridges_Shallow_Ground, 0.08f),
                    Spawn(BiomeType.TwistyBridges_Ground, 0.08f),
                    Spawn(BiomeType.ArcticKelp_Grass, 0.12f),
                    Spawn(BiomeType.ArcticKelp_Rock, 0.12f),
                    Spawn(BiomeType.SparseArctic_Ground, 0.12f),
                    Spawn(BiomeType.EastArctic_Ground, 0.12f),
                    Spawn(BiomeType.WestArctic_Ground, 0.12f),
                    Spawn(BiomeType.GlacialBay, 0.08f),
                    Spawn(BiomeType.GlacialBasin_Generic, 0.08f),
                    Spawn(BiomeType.GlacialBasin_BikeCrashSite, 0.08f),
                    Spawn(BiomeType.LilyPads_ShipWreck_Ground, 0.08f),
                    Spawn(BiomeType.LilyPads_ShipWreck_Grass, 0.08f));
            }
        }

        internal static class PenglingBabyDnaPrefab
        {
            private const string ClassId = "PenglingBabyPetDna";
            private const string TextureAssetName = "PenglingBabyDnaStrandTexture.png";
            internal static PrefabInfo Info;

            internal static void Register(GameObject dnaModelPrefab)
            {
                Info = RegisterDnaPrefab(ClassId, TextureAssetName, Color.magenta, dnaModelPrefab,
                    NestDistribution.All,
                    Spawn(BiomeType.TwistyBridges_Shallow_Ground, 0.12f),
                    Spawn(BiomeType.TwistyBridges_Shallow_Coral, 0.12f),
                    Spawn(BiomeType.TwistyBridges_Ground, 0.10f),
                    Spawn(BiomeType.ArcticKelp_Grass, 0.10f),
                    Spawn(BiomeType.ArcticKelp_Rock, 0.10f),
                    Spawn(BiomeType.SparseArctic_Ground, 0.10f),
                    Spawn(BiomeType.EastArctic_Ground, 0.10f),
                    Spawn(BiomeType.WestArctic_Ground, 0.10f),
                    Spawn(BiomeType.GlacialBay, 0.08f),
                    Spawn(BiomeType.GlacialBasin_Generic, 0.10f),
                    Spawn(BiomeType.GlacialBasin_BikeCrashSite, 0.08f),
                    Spawn(BiomeType.GlacialBasin_SpyPenguin, 0.10f),
                    Spawn(BiomeType.LilyPads_ShipWreck_Ground, 0.06f),
                    Spawn(BiomeType.LilyPads_ShipWreck_Grass, 0.06f));
            }
        }

        internal static class SnowstalkerBabyDnaPrefab
        {
            private const string ClassId = "SnowstalkerBabyPetDna";
            private const string TextureAssetName = "SnowstalkerBabyDnaStrandTexture.png";
            internal static PrefabInfo Info;

            internal static void Register(GameObject dnaModelPrefab)
            {
                Info = RegisterDnaPrefab(ClassId, TextureAssetName, Color.white, dnaModelPrefab,
                    NestDistribution.ArcticKelp,
                    Spawn(BiomeType.EastArctic_Ground, 0.06f),
                    Spawn(BiomeType.WestArctic_Ground, 0.06f),
                    Spawn(BiomeType.GlacialBay, 0.10f),
                    Spawn(BiomeType.GlacialBasin_Generic, 0.12f),
                    Spawn(BiomeType.GlacialBasin_BikeCrashSite, 0.12f),
                    Spawn(BiomeType.GlacialBasin_SpyPenguin, 0.12f),
                    Spawn(BiomeType.ArcticSpires_Generic, 0.12f),
                    Spawn(BiomeType.ArcticSpires_Cave, 0.12f),
                    Spawn(BiomeType.LilyPads_ShipWreck_Ground, 0.08f),
                    Spawn(BiomeType.LilyPads_ShipWreck_Grass, 0.08f));
            }
        }

        internal static class TrivalveBlueDnaPrefab
        {
            private const string ClassId = "TrivalveBluePetDna";
            private const string TextureAssetName = "TrivalveBlueDnaStrandTexture.png";
            internal static PrefabInfo Info;

            internal static void Register(GameObject dnaModelPrefab)
            {
                Info = RegisterDnaPrefab(ClassId, TextureAssetName, Color.blue, dnaModelPrefab,
                    NestDistribution.LilyPads,
                    Spawn(BiomeType.WestArctic_Ground, 0.12f),
                    Spawn(BiomeType.ArcticSpires_Generic, 0.12f),
                    Spawn(BiomeType.ArcticSpires_Cave, 0.12f),
                    Spawn(BiomeType.PurpleVents_Crevice_Ground, 0.06f),
                    Spawn(BiomeType.LilyPads_Crevice_Ground, 0.08f),
                    Spawn(BiomeType.LilyPads_Deep_Grass, 0.08f),
                    Spawn(BiomeType.LilyPads_Deep_Ground, 0.08f),
                    Spawn(BiomeType.TreeSpires_BigFissure_Ground, 0.06f),
                    Spawn(BiomeType.TreeSpires_BigTree_Ground, 0.06f),
                    Spawn(BiomeType.CrystalCave_Castle_Ground, 0.05f),
                    Spawn(BiomeType.CrystalCave_Ground, 0.05f),
                    Spawn(BiomeType.CrystalCave_Inner_Ground, 0.05f),
                    Spawn(BiomeType.LilyPads_ShipWreck_Ground, 0.08f),
                    Spawn(BiomeType.LilyPads_ShipWreck_Grass, 0.08f),
                    Spawn(BiomeType.PurpleVents_ShipWreck_Ground, 0.08f),
                    Spawn(BiomeType.MiningSite_Ground, 0.06f),
                    Spawn(BiomeType.MargArea_BaseGround, 0.06f));
            }
        }

        internal static class TrivalveYellowDnaPrefab
        {
            private const string ClassId = "TrivalveYellowPetDna";
            private const string TextureAssetName = "TrivalveYellowDnaStrandTexture.png";
            internal static PrefabInfo Info;

            internal static void Register(GameObject dnaModelPrefab)
            {
                Info = RegisterDnaPrefab(ClassId, TextureAssetName, Color.yellow, dnaModelPrefab,
                    NestDistribution.LilyPads,
                    Spawn(BiomeType.LilyPads_Crevice_Ground, 0.10f),
                    Spawn(BiomeType.LilyPads_Deep_Grass, 0.12f),
                    Spawn(BiomeType.LilyPads_Deep_Ground, 0.12f),
                    Spawn(BiomeType.LilyPads_Islands_Cave_Ground, 0.12f),
                    Spawn(BiomeType.TreeSpires_BigFissure_Ground, 0.06f),
                    Spawn(BiomeType.CrystalCave_Castle_Ground, 0.06f),
                    Spawn(BiomeType.CrystalCave_Ground, 0.06f),
                    Spawn(BiomeType.CrystalCave_Inner_Ground, 0.06f),
                    Spawn(BiomeType.FabricatorCavern_Ground, 0.05f),
                    Spawn(BiomeType.FabricatorCavern_Grass, 0.05f),
                    Spawn(BiomeType.LilyPads_ShipWreck_Ground, 0.08f),
                    Spawn(BiomeType.LilyPads_ShipWreck_Grass, 0.08f),
                    Spawn(BiomeType.PurpleVents_ShipWreck_Ground, 0.08f),
                    Spawn(BiomeType.MiningSite_Ground, 0.06f),
                    Spawn(BiomeType.MargArea_BaseGround, 0.06f));
            }
        }

        internal static class PinnacaridDnaPrefab
        {
            private const string ClassId = "PinnacaridPetDna";
            private const string TextureAssetName = "PinnacaridDnaStrandTexture.png";
            internal static PrefabInfo Info;

            internal static void Register(GameObject dnaModelPrefab)
            {
                Info = RegisterDnaPrefab(ClassId, TextureAssetName, Color.blue, dnaModelPrefab,
                    NestDistribution.All,
                    Spawn(BiomeType.ArcticKelp_Grass, 0.12f),
                    Spawn(BiomeType.ArcticKelp_Rock, 0.12f),
                    Spawn(BiomeType.SparseArctic_Ground, 0.10f),
                    Spawn(BiomeType.EastArctic_Ground, 0.12f),
                    Spawn(BiomeType.WestArctic_Ground, 0.12f),
                    Spawn(BiomeType.PurpleVents_Ground, 0.06f),
                    Spawn(BiomeType.PurpleVents_Crevice_Ground, 0.06f),
                    Spawn(BiomeType.ThermalSpires_Ground, 0.06f),
                    Spawn(BiomeType.ThermalSpires_Cave_Ground, 0.06f),
                    Spawn(BiomeType.LilyPads_Crevice_Ground, 0.06f),
                    Spawn(BiomeType.LilyPads_Deep_Grass, 0.06f),
                    Spawn(BiomeType.LilyPads_Deep_Ground, 0.06f),
                    Spawn(BiomeType.LilyPads_Islands_Cave_Ground, 0.06f),
                    Spawn(BiomeType.TreeSpires_BigFissure_Ground, 0.06f),
                    Spawn(BiomeType.TreeSpires_BigTree_Ground, 0.06f),
                    Spawn(BiomeType.CrystalCave_Castle_Ground, 0.04f),
                    Spawn(BiomeType.CrystalCave_Ground, 0.04f),
                    Spawn(BiomeType.CrystalCave_Inner_Ground, 0.04f),
                    Spawn(BiomeType.LilyPads_ShipWreck_Ground, 0.08f),
                    Spawn(BiomeType.LilyPads_ShipWreck_Grass, 0.08f),
                    Spawn(BiomeType.PurpleVents_ShipWreck_Ground, 0.08f),
                    Spawn(BiomeType.MiningSite_Ground, 0.06f),
                    Spawn(BiomeType.MargArea_BaseGround, 0.06f));
            }
        }
    }
}
