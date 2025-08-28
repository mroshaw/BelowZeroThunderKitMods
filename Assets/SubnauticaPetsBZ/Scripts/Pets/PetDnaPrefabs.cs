using DaftAppleGames.SubnauticaPets.Utils;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Utility;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    /// Static class for creating the "Pet DNA" collectable objects
    /// </summary>
    internal class PetDnaPrefabs
    {
        private const string PrefabAssetName = "DNASampleTube.prefab";
        private const string EncKey = "PetDna";
        private const string EncPath = "Lifeforms/Fauna";
        private const string DatabankPopupImageAssetName = "PetDnaDataBankPopupImageTexture.png";
        private const string DatabankMainImageAssetName = "PetDnaDataBankMainImageTexture.png";
        
        /// <summary>
        /// Register all prefabs
        /// </summary>
        internal static void RegisterAll()
        {
            // Get the DNA model prefab from the Asset Bundle
            GameObject dnaModelPrefab = CustomAssetBundleUtils.GetObjectFromAssetBundle<GameObject>(PrefabAssetName) as GameObject;
            MaterialUtils.ApplySNShaders(dnaModelPrefab);

            // Register DNA spawn prefabs
            CatDnaPrefab.Register(dnaModelPrefab);
            PengwingAdultDnaPrefab.Register(dnaModelPrefab);
            PenglingBabyDnaPrefab.Register(dnaModelPrefab);
            PinnacaridDnaPrefab.Register(dnaModelPrefab);
            SnowstalkerBabyDnaPrefab.Register(dnaModelPrefab);
            TrivalveBlueDnaPrefab.Register(dnaModelPrefab);
            TrivalveYellowDnaPrefab.Register(dnaModelPrefab);
            ConfigureDataBank();
        }

        /// <summary>
        /// Cat DNA Prefab
        /// </summary>
        internal static class CatDnaPrefab
        {
            internal static PrefabInfo Info;
            private const string ClassId = "CatPetDna";
            private const string TextureAssetName = "CatTexture.png";
            private const int FindCount = 1;
            private const float FindProbability = 0.3f;

            /// <summary>
            /// Register Cat DNA
            /// </summary>
            internal static void Register(GameObject dnaModelGameObject)
            {
                Info = RegisterDnaPrefab(ClassId, null, null, TextureAssetName, Color.grey,
                    new LootDistributionData.BiomeData[]
                    {
                        new LootDistributionData.BiomeData { biome = BiomeType.InactiveLavaZone_Corridor_Floor, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.InactiveLavaZone_Corridor_Floor_Far, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.InactiveLavaZone_LavaPit_Floor, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.JellyshroomCaves_CaveFloor, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.UnderwaterIslands_TechSite, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.UnderwaterIslands_TechSite_Scatter, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.UnderwaterIslands_ValleyFloor, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.CrashZone_Sand, count = FindCount, probability = FindProbability},
                    }, dnaModelGameObject);
            }
        }
        /// <summary>
        /// Pengwing Adult DNA
        /// </summary>
        internal static class PengwingAdultDnaPrefab
        {
            internal static PrefabInfo Info;
            private const string ClassId = "PengwingAdultPetDna";
            private const string TextureAssetName = "PengwingAdultDnaStrandTexture.png";
            private const int FindCount = 1;
            private const float FindProbability = 0.6f;

            /// <summary>
            /// Registers Pengwing Adult DNA
            /// </summary>
            internal static void Register(GameObject dnaModelGameObject)
            {
                Info = RegisterDnaPrefab(ClassId, null, null, TextureAssetName, Color.grey,
                    new LootDistributionData.BiomeData[] {
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest1, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest3, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest5, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest1, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest3, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest5, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_ShipWreck_Ground, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.PurpleVents_ShipWreck_Open, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ShipWreck2_Open, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ShipWreck3_Open, count = FindCount, probability = FindProbability},

                    }, dnaModelGameObject);
            }
        }

        /// <summary>
        /// Pengling Baby DNA
        /// </summary>
        internal static class PenglingBabyDnaPrefab
        {
            internal static PrefabInfo Info;
            private const string ClassId = "PenglingBabyPetDna";
            private const string TextureAssetName = "PenglingBabyDnaStrandTexture.png";
            private const int FindCount = 1;
            private const float FindProbability = 0.3f;

            /// <summary>
            /// Register Pengling Baby DNA
            /// </summary>
            /// <param name="dnaModelGameObject"></param>
            internal static void Register(GameObject dnaModelGameObject)
            {
                Info = RegisterDnaPrefab(ClassId, null, null, TextureAssetName, Color.magenta,
                    new LootDistributionData.BiomeData[] {
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest1, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest2, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest4, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.GlacialBasin_BikeCrashSite, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest2, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest4, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest5, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_ShipWreck_Open, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.MargArea_Ground, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.PurpleVents_ShipWreck_Ground, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ShipWreck1_Open, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ShipWreck3_Open, count = FindCount, probability = FindProbability},

                    }, dnaModelGameObject);
            }
        }

        /// <summary>
        /// Snowstalker Baby DNA
        /// </summary>
        internal static class SnowstalkerBabyDnaPrefab
        {
            internal static PrefabInfo Info;
            private const string ClassId = "SnowstalkerBabyPetDna";
            private const string TextureAssetName = "SnowstalkerBabyDnaStrandTexture.png";
            private const int FindCount = 1;
            private const float FindProbability = 0.3f;
            
            /// <summary>
            /// Register Snowstalker Baby DNA
            /// </summary>
            /// <param name="dnaModelGameObject"></param>
            internal static void Register(GameObject dnaModelGameObject)
            {
                Info = RegisterDnaPrefab(ClassId, null, null, TextureAssetName, Color.white,
                    new LootDistributionData.BiomeData[] {
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest1, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest3, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest5, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.GlacialBasin_BikeCrashSite, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest1, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest3, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest5, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_ShipWreck_Ground, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.PurpleVents_ShipWreck_Open, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ShipWreck2_Open, count = FindCount, probability = FindProbability},
                    }, dnaModelGameObject);
            }
        }

        /// <summary>
        /// Trivalve Blue DNA
        /// </summary>
        internal static class TrivalveBlueDnaPrefab
        {
            internal static PrefabInfo Info;
            private const string ClassId = "TrivalveBluePetDna";
            private const string TextureAssetName = "TrivalveBlueDnaStrandTexture.png";
            private const int FindCount = 1;
            private const float FindProbability = 0.3f;

            /// <summary>
            /// Register Trivalve Blue DNA
            /// </summary>
            internal static void Register(GameObject dnaModelGameObject)
            {
                Info = RegisterDnaPrefab(ClassId, null, null, TextureAssetName, Color.blue,
                    new LootDistributionData.BiomeData[] {
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest1, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest2, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest4, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.GlacialBasin_BikeCrashSite, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest1, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest2, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest4, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_ShipWreck_Open, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.MargArea_Ground, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.PurpleVents_ShipWreck_Ground, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ShipWreck1_Open, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ShipWreck3_Open, count = FindCount, probability = FindProbability},
                    }, dnaModelGameObject);
            }
        }

        /// <summary>
        /// Trivalve Yellow DNA
        /// </summary>
        internal static class TrivalveYellowDnaPrefab
        {
            internal static PrefabInfo Info;
            private const string ClassId = "TrivalveYellowPetDna";
            private const string TextureAssetName = "TrivalveYellowDnaStrandTexture.png";
            private const int FindCount = 1;
            private const float FindProbability = 0.3f;

            /// <summary>
            /// Register Trivalve Yellow DNA
            /// </summary>
            internal static void Register(GameObject dnaModelGameObject)
            {
                Info = RegisterDnaPrefab(ClassId, null, null, TextureAssetName, Color.yellow,
                    new LootDistributionData.BiomeData[] {
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest1, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest3, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest5, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.GlacialBasin_BikeCrashSite, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest1, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest3, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest5, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_ShipWreck_Ground, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.MargArea_Ground, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.PurpleVents_ShipWreck_Open, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ShipWreck2_Open, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ShipWreck3_Open, count = FindCount, probability = FindProbability},
                    }, dnaModelGameObject);
            }
        }

        /// <summary>
        /// Pinnacarid DNA
        /// </summary>
        internal static class PinnacaridDnaPrefab
        {
            internal static PrefabInfo Info;
            private const string ClassId = "PinnacaridPetDna";
            private const string TextureAssetName = "PinnacaridDnaStrandTexture.png";
            private const int FindCount = 1;
            private const float FindProbability = 0.3f;

            /// <summary>
            /// Register Pinnacarid DNA
            /// </summary>
            /// <param name="dnaModelGameObject"></param>
            internal static void Register(GameObject dnaModelGameObject)
            {
                Info = RegisterDnaPrefab(ClassId, null, null, TextureAssetName, Color.blue,
                    new LootDistributionData.BiomeData[] {
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest2, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest3, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ArcticKelp_SeamonkeyNest5, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.GlacialBasin_BikeCrashSite, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest1, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest3, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_Crevice_SeamonkeyNest5, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.LilyPads_ShipWreck_Ground, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.PurpleVents_ShipWreck_Open, count = FindCount, probability = FindProbability},
                        new LootDistributionData.BiomeData { biome = BiomeType.ShipWreck2_Open, count = FindCount, probability = FindProbability},
                    }, dnaModelGameObject);
            }
        }


        private static PrefabInfo RegisterDnaPrefab(string classId, string displayName, string description, string textureName,
            Color color, LootDistributionData.BiomeData[] lootBiome, GameObject dnaModelPrefab)
        {
            LogUtils.LogDebug(LogArea.Prefabs, $"PetDnaPrefab: Register Prefab for {classId}...");

            PrefabInfo prefabInfo = PrefabInfo
                .WithTechType(classId, displayName, description, unlockAtStart: true)
                .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(textureName) as Sprite);

            CustomPrefab clonePrefab = new CustomPrefab(prefabInfo);

            CloneTemplate cloneTemplate = new CloneTemplate(clonePrefab.Info, TechType.Quartz)
            {
                ModifyPrefab = obj =>
                {
                    obj.SetActive(false);
                    // Disable the old model
                    GameObject modelGameObject = obj.GetComponentInChildren<MeshRenderer>(true).gameObject;
                    modelGameObject.SetActive(false);

                    GameObject newModel = Object.Instantiate(dnaModelPrefab);
                    newModel.name = "newmodel";
                    // Add new model
                    newModel.transform.SetParent(obj.transform);
                    newModel.transform.localPosition = new Vector3(0, 0, 0);
                    newModel.transform.localRotation = new Quaternion(0, 0, 0, 0);

                    // obj.FindChild("Quartz_small").SetActive(false);

                    MaterialUtils.ApplySNShaders(newModel);

                    // Configure Prefab
                    PrefabUtils.AddBasicComponents(obj, clonePrefab.Info.ClassID, clonePrefab.Info.TechType, LargeWorldEntity.CellLevel.VeryFar);
                    PrefabUtils.AddResourceTracker(obj, TechType.None);
                    PetPrefabConfigUtils.SetMeshRenderersColor(newModel, "Ends", color);
                    PetPrefabConfigUtils.AddRotateModel(newModel, "DNA");
                    // PrefabConfigUtils.AddRigidBody(obj);
                    // PrefabConfigUtils.AddFreezeOnSettle(obj);
                    // PrefabConfigUtils.AddDnaCapsuleCollider(obj);
                    PetPrefabConfigUtils.AddScaleOnStart(obj, 0.4f);
                    obj.AddComponent<PetDna>();
                }
            };
            clonePrefab.SetGameObject(cloneTemplate);
            clonePrefab.SetSpawns(lootBiome);
            clonePrefab.Register();
            return prefabInfo;
        }

        /// <summary>
        /// Adds all DataBank entries
        /// </summary>
        private static void ConfigureDataBank()
        {
            PetPrefabConfigUtils.ConfigureDatabankEntry(EncKey, EncPath, DatabankMainImageAssetName, DatabankPopupImageAssetName);
            Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(EncKey, Story.GoalType.Encyclopedia, PengwingAdultDnaPrefab.Info.TechType);
            Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(EncKey, Story.GoalType.Encyclopedia, PenglingBabyDnaPrefab.Info.TechType);
            Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(EncKey, Story.GoalType.Encyclopedia, PinnacaridDnaPrefab.Info.TechType);
            Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(EncKey, Story.GoalType.Encyclopedia, SnowstalkerBabyDnaPrefab.Info.TechType);
            Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(EncKey, Story.GoalType.Encyclopedia, TrivalveYellowDnaPrefab.Info.TechType);
            Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(EncKey, Story.GoalType.Encyclopedia, TrivalveBlueDnaPrefab.Info.TechType);
        }
    }
}