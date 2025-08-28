using DaftAppleGames.SubnauticaPets.Extensions;
using DaftAppleGames.SubnauticaPets.Utils;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Utility;
using UnityEngine;

// https://github.com/LeeTwentyThree/Nautilus/blob/master/Nautilus/Documentation/resources/BZ-PrefabPaths.json
// https://github.com/LeeTwentyThree/Nautilus/blob/master/Nautilus/Documentation/resources/SN1-PrefabPaths.json

namespace DaftAppleGames.SubnauticaPets.Pets
{
    internal static class PetPrefabs
    {
        /// <summary>
        /// Set up all the Pet Prefabs
        /// </summary>
        internal static void RegisterAll()
        {
            PenglingBabyPrefab.Register();
            PengwingAdultPrefab.Register();
            PinnacaridPrefab.Register();
            SnowstalkerBabyPrefab.Register();
            TrivalveBluePrefab.Register();
            TrivalveYellowPrefab.Register();
        }
        /// <summary>
        /// Pengling Baby
        /// </summary>
        internal static class PenglingBabyPrefab
        {
            // Init PrefabInfo
            internal static PrefabInfo Info;
            private const string ClassId = "PenglingBabyPet";
            private const string IconTextureAssetName = "PenglingBabyTexture.png";
            private const string CloneClassId = "807fbbb3-aced-45cd-aba8-db3fb1188f1f";
            
            /// <summary>
            /// Set up the Pet Prefab
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);

                CustomPrefab prefab = new CustomPrefab(Info);
                CloneTemplate cloneTemplate = new CloneTemplate(Info, CloneClassId);

                // Modify the cloned model
                cloneTemplate.ModifyPrefab += obj =>
                {
                    obj.SetActive(false);
                    PetPrefabConfigUtils.AddTechTag(obj, Info.TechType);
                    GameObject modelGameObject = obj.GetComponentInChildren<Animator>(true).gameObject;
                    PetPrefabConfigUtils.AddVFXFabricating(obj, null, -0.2f, 0.8f, new Vector3(0.0f, 0.0f, 0.0f), 1.0f, new Vector3(0.0f, 0.0f, 0.0f));
                    PrefabUtils.AddConstructable(obj, Info.TechType, ConstructableFlags.Inside, modelGameObject);
                    obj.DestroyComponentsInChildren<Pickupable>();
                    PetPrefabConfigUtils.AddPetHandTarget(obj);
                    PetPrefabConfigUtils.ConfigureSwimming(obj);
                    PetPrefabConfigUtils.ConfigureSkyApplier(obj);
                    PetPrefabConfigUtils.ConfigureAnimator(obj, false);
                    obj.DestroyComponentsInChildren<CreatureDeath>();
                    PetPrefabConfigUtils.AddPetComponent(obj);
                    obj.name = ClassId;
                    LogUtils.LogDebug(LogArea.Prefabs, $"Done modifying {Info.TechType}");
                };

                prefab.SetGameObject(cloneTemplate);

                // Set the recipe, depends on whether in "Adventure" or "Creative" mode.
                RecipeData recipe = null;
                if (SubnauticaPetsPlugin.ModConfig.ModMode == ModMode.Adventure)
                {
                    recipe = new RecipeData(
                        new Ingredient(TechType.Gold, 1),
                        new Ingredient(PetDnaPrefabs.PenglingBabyDnaPrefab.Info.TechType, 5));
                }
                else
                {
                    recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
                }
                CraftingGadget crafting = prefab.SetRecipe(recipe);
                prefab.Register();
            }
        }

        /// <summary>
        /// Pengwing Adult
        /// </summary>
        internal static class PengwingAdultPrefab
        {
            // Init PrefabInfo
            internal static PrefabInfo Info;
            private const string ClassId = "PengwingAdultPet";
            private const string IconTextureAssetName = "PengwingAdultTexture.png";
            private const string CloneClassId = "74ded0e7-d394-4703-9e53-4384b37f9433";
            
            /// <summary>
            /// Set up the Pengwing Adult Prefab
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);

                CustomPrefab prefab = new CustomPrefab(Info);
                CloneTemplate cloneTemplate = new CloneTemplate(Info, CloneClassId);

                // Modify the cloned model
                cloneTemplate.ModifyPrefab += obj =>
                {
                    obj.SetActive(false);
                    PetPrefabConfigUtils.AddTechTag(obj, Info.TechType);
                    GameObject modelGameObject = obj.GetComponentInChildren<Animator>(true).gameObject;
                    PetPrefabConfigUtils.AddVFXFabricating(obj, null, -0.2f, 1.2f, new Vector3(0.0f, 0.0f, 0.0f), 1.0f, new Vector3(0.0f, 0.0f, 0.0f));
                    PrefabUtils.AddConstructable(obj, Info.TechType, ConstructableFlags.Inside, modelGameObject);
                    PetPrefabConfigUtils.UpdatePickupable(obj, false);
                    PetPrefabConfigUtils.AddPetHandTarget(obj);
                    PetPrefabConfigUtils.ConfigureSwimming(obj);
                    PetPrefabConfigUtils.ConfigureSkyApplier(obj);
                    PetPrefabConfigUtils.ConfigureAnimator(obj, false);
                    obj.DestroyComponentsInChildren<CreatureDeath>();
                    PetPrefabConfigUtils.AddPetComponent(obj);
                    obj.name = ClassId;
                    LogUtils.LogDebug(LogArea.Prefabs, $"Done modifying {Info.TechType}");
                };

                prefab.SetGameObject(cloneTemplate);

                // Set the recipe, depends on whether in "Adventure" or "Creative" mode.
                RecipeData recipe = null;
                if (SubnauticaPetsPlugin.ModConfig.ModMode == ModMode.Adventure)
                {
                    recipe = new RecipeData(
                    new Ingredient(TechType.Gold, 1),
                    new Ingredient(PetDnaPrefabs.PengwingAdultDnaPrefab.Info.TechType, 3));
                }
                else
                {
                    recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
                }
                CraftingGadget crafting = prefab.SetRecipe(recipe);
                prefab.Register();
            }
        }

        /// <summary>
        /// Pinnacarid
        /// </summary>
        internal static class PinnacaridPrefab
        {
            // Init PrefabInfo
            internal static PrefabInfo Info;
            private const string ClassId = "PinnacaridPet";
            private const string IconTextureAssetName = "PinnacaridTexture.png";
            private const string CloneClassId = "f9eccfe2-a06f-4c06-bc57-01c2e50ffbe8";
            
            /// <summary>
            /// Set up the Pinnacarid Prefab
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);

                CustomPrefab prefab = new CustomPrefab(Info);
                CloneTemplate cloneTemplate = new CloneTemplate(Info, CloneClassId);

                // Modify the cloned model
                cloneTemplate.ModifyPrefab += obj =>
                {
                    obj.SetActive(false);
                    PetPrefabConfigUtils.AddTechTag(obj, Info.TechType);
                    GameObject modelGameObject = obj.GetComponentInChildren<Animator>(true).gameObject;
                    PetPrefabConfigUtils.AddVFXFabricating(obj, null, -0.2f, 0.6f, new Vector3(0.0f, 0.0f, 0.0f), 1.0f, new Vector3(0.0f, 0.0f, 0.0f));
                    PrefabUtils.AddConstructable(obj, Info.TechType, ConstructableFlags.Inside, modelGameObject);
                    obj.DestroyComponentsInChildren<Pickupable>();
                    PetPrefabConfigUtils.AddPetHandTarget(obj);
                    PetPrefabConfigUtils.ConfigureSwimming(obj);
                    PetPrefabConfigUtils.ConfigureSkyApplier(obj);
                    PetPrefabConfigUtils.ConfigureAnimator(obj, false);
                    PetPrefabConfigUtils.AddPetComponent(obj);
                    obj.name = ClassId;
                    obj.SetActive(false);
                    LogUtils.LogDebug(LogArea.Prefabs, $"Done modifying {Info.TechType}");
                };

                prefab.SetGameObject(cloneTemplate);

                // Set the recipe, depends on whether in "Adventure" or "Creative" mode.
                RecipeData recipe = null;
                if (SubnauticaPetsPlugin.ModConfig.ModMode == ModMode.Adventure)
                {
                    recipe = new RecipeData(
                    new Ingredient(TechType.Gold, 1),
                    new Ingredient(PetDnaPrefabs.PinnacaridDnaPrefab.Info.TechType, 3));
                }
                else
                {
                    recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
                }
                CraftingGadget crafting = prefab.SetRecipe(recipe);
                prefab.Register();
            }
        }

        /// <summary>
        /// Snowstalker Baby
        /// </summary>
        internal static class SnowstalkerBabyPrefab
        {
            // Init PrefabInfo
            internal static PrefabInfo Info;
            private const string ClassId = "SnowstalkerBabyPet";
            private const string IconTextureAssetName = "SnowstalkerBabyTexture.png";
            private const string CloneClassId = "78d3dbce-856f-4eba-951c-bd99870554e2";

            /// <summary>
            /// Set up the Snowstalker Baby Prefab
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);

                CustomPrefab prefab = new CustomPrefab(Info);
                CloneTemplate cloneTemplate = new CloneTemplate(Info, CloneClassId);

                // Modify the cloned model
                cloneTemplate.ModifyPrefab += obj =>
                {
                    obj.SetActive(false);
                    PetPrefabConfigUtils.AddTechTag(obj, Info.TechType);
                    GameObject modelGameObject = obj.GetComponentInChildren<Animator>(true).gameObject;
                    PetPrefabConfigUtils.AddVFXFabricating(obj, null, -0.2f, 1.0f, new Vector3(0.0f, 0.0f, 0.0f), 1.0f, new Vector3(0.0f, 0.0f, 0.0f));
                    PrefabUtils.AddConstructable(obj, Info.TechType, ConstructableFlags.Inside, modelGameObject);
                    obj.DestroyComponentsInChildren<Pickupable>();
                    PetPrefabConfigUtils.AddPetHandTarget(obj);
                    PetPrefabConfigUtils.ConfigureSwimming(obj);
                    PetPrefabConfigUtils.ConfigureSkyApplier(obj);
                    PetPrefabConfigUtils.ConfigureAnimator(obj, false);
                    PetPrefabConfigUtils.ConfigureMovement(obj);
                    PetPrefabConfigUtils.CleanNavUpMesh(obj);
                    PetPrefabConfigUtils.AddPetComponent(obj);
                    obj.name = ClassId;
                    LogUtils.LogDebug(LogArea.Prefabs, $"Done modifying {Info.TechType}");
                };

                prefab.SetGameObject(cloneTemplate);

                // Set the recipe, depends on whether in "Adventure" or "Creative" mode.
                RecipeData recipe = null;
                if (SubnauticaPetsPlugin.ModConfig.ModMode == ModMode.Adventure)
                {
                    recipe = new RecipeData(
                   new Ingredient(TechType.Gold, 1),
                   new Ingredient(PetDnaPrefabs.SnowstalkerBabyDnaPrefab.Info.TechType, 3));
                }
                else
                {
                    recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
                }
                CraftingGadget crafting = prefab.SetRecipe(recipe);
                prefab.Register();
            }
        }

        private static void ConfigureTrivalve(GameObject obj, PrefabInfo Info, string objName, string classId)
        {
            obj.SetActive(false);
            PetPrefabConfigUtils.AddTechTag(obj, Info.TechType);
            GameObject modelGameObject = obj.GetComponentInChildren<Animator>(true).gameObject;
            PetPrefabConfigUtils.AddVFXFabricating(obj, null, -0.2f, 0.8f, new Vector3(0.0f, 0.0f, 0.0f), 1.0f, new Vector3(0.0f, 0.0f, 0.0f));
            PrefabUtils.AddConstructable(obj, Info.TechType, ConstructableFlags.Inside, modelGameObject);
            obj.DestroyComponentsInChildren<Pickupable>();
            // obj.DisableComponentsInChildren<LargeWorldEntity>();
            PetPrefabConfigUtils.AddPetHandTarget(obj);
            PetPrefabConfigUtils.ConfigureSwimming(obj);
            PetPrefabConfigUtils.ConfigureSkyApplier(obj);
            PetPrefabConfigUtils.ConfigureAnimator(obj, false);
            PetPrefabConfigUtils.AddPetComponent(obj);
            obj.name = objName;
            obj.SetActive(false);
            LogUtils.LogDebug(LogArea.Prefabs, $"Done modifying {Info.TechType}");

        }

        /// <summary>
        /// Trivalve Blue
        /// </summary>
        internal static class TrivalveBluePrefab
        {
            // Init PrefabInfo
            internal static PrefabInfo Info;
            private const string ClassId = "TrivalveBluePet";
            private const string IconTextureAssetName = "TrivalveBlueTexture.png";
            private const string CloneClassId = "f5a2317f-6116-4fc6-8e81-824fd8ba9684";

            /// <summary>
            /// Set up the Trivalve Blue Prefab
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);

                CustomPrefab prefab = new CustomPrefab(Info);
                CloneTemplate cloneTemplate = new CloneTemplate(Info, CloneClassId);

                // Modify the cloned model
                cloneTemplate.ModifyPrefab += obj =>
                {
                    ConfigureTrivalve(obj, Info, ClassId, ClassId);
                };

                prefab.SetGameObject(cloneTemplate);

                // Set the recipe, depends on whether in "Adventure" or "Creative" mode.
                RecipeData recipe = null;
                if (SubnauticaPetsPlugin.ModConfig.ModMode == ModMode.Adventure)
                {
                    recipe = new RecipeData(
                   new Ingredient(TechType.Gold, 1),
                   new Ingredient(PetDnaPrefabs.TrivalveBlueDnaPrefab.Info.TechType, 3));
                }
                else
                {
                    recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
                }
                CraftingGadget crafting = prefab.SetRecipe(recipe);
                prefab.Register();
            }
        }

        /// <summary>
        /// Trivalve Blue
        /// </summary>
        internal static class TrivalveYellowPrefab
        {
            // Init PrefabInfo
            internal static PrefabInfo Info;
            private const string ClassId = "TrivalveYellowPet";
            private const string IconTextureAssetName = "TrivalveYellowTexture.png";
            private const string CloneClassId = "e8f2bfd4-49c6-45d1-a029-489b492515a9";
            
            /// <summary>
            /// Set up the Trivalve Blue Prefab
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);

                CustomPrefab prefab = new CustomPrefab(Info);
                CloneTemplate cloneTemplate = new CloneTemplate(Info, CloneClassId);

                // Modify the cloned model
                cloneTemplate.ModifyPrefab += obj =>
                {
                    ConfigureTrivalve(obj, Info, ClassId, ClassId);
                };

                prefab.SetGameObject(cloneTemplate);

                // Set the recipe, depends on whether in "Adventure" or "Creative" mode.
                RecipeData recipe = null;
                if (SubnauticaPetsPlugin.ModConfig.ModMode == ModMode.Adventure)
                {
                    recipe = new RecipeData(
                    new Ingredient(TechType.Gold, 1),
                    new Ingredient(PetDnaPrefabs.TrivalveYellowDnaPrefab.Info.TechType, 3));
                }
                else
                {
                    recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
                }
                CraftingGadget crafting = prefab.SetRecipe(recipe);
                prefab.Register();
            }
        }
    }
}