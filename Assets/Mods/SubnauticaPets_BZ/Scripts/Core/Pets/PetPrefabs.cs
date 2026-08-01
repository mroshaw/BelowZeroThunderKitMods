using DaftAppleGames.SubnauticaPets.Extensions;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Utility;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

// https://github.com/LeeTwentyThree/Nautilus/blob/master/Nautilus/Documentation/resources/BZ-PrefabPaths.json
// https://github.com/LeeTwentyThree/Nautilus/blob/master/Nautilus/Documentation/resources/SN1-PrefabPaths.json

namespace DaftAppleGames.SubnauticaPets.Pets
{
    internal static class PetPrefabs
    {
        /// <summary>
        ///     Set up all the Pet Prefabs
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

        private static void ConfigureTrivalve(GameObject obj, PrefabInfo Info, string objName, string classId)
        {
            obj.SetActive(false);
            PetPrefabConfigUtils.AddTechTag(obj, Info.TechType);
            var modelGameObject = obj.GetComponentInChildren<Animator>(true).gameObject;
            PetPrefabConfigUtils.AddVFXFabricating(obj, null, -0.2f, 0.8f, new Vector3(0.0f, 0.0f, 0.0f), 1.0f,
                new Vector3(0.0f, 0.0f, 0.0f));
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
            ModDebugLog.LogDebug($"Done modifying {Info.TechType}");
        }

        /// <summary>
        ///     Pengling Baby
        /// </summary>
        internal static class PenglingBabyPrefab
        {
            private const string ClassId = "PenglingBabyPet";
            private const string IconTextureAssetName = "PenglingBabyIcon_Small.png";
            private const string CloneClassId = "807fbbb3-aced-45cd-aba8-db3fb1188f1f";
            // Init PrefabInfo
            internal static PrefabInfo Info;

            /// <summary>
            ///     Set up the Pet Prefab
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);

                var prefab = new CustomPrefab(Info);
                var cloneTemplate = new CloneTemplate(Info, CloneClassId);

                // Modify the cloned model
                cloneTemplate.ModifyPrefab += obj =>
                {
                    obj.SetActive(false);
                    PetPrefabConfigUtils.AddTechTag(obj, Info.TechType);
                    var modelGameObject = obj.GetComponentInChildren<Animator>(true).gameObject;
                    PetPrefabConfigUtils.AddVFXFabricating(obj, null, -0.2f, 0.8f, new Vector3(0.0f, 0.0f, 0.0f), 1.0f,
                        new Vector3(0.0f, 0.0f, 0.0f));
                    PrefabUtils.AddConstructable(obj, Info.TechType, ConstructableFlags.Inside, modelGameObject);
                    obj.DestroyComponentsInChildren<Pickupable>();
                    PetPrefabConfigUtils.AddPetHandTarget(obj);
                    PetPrefabConfigUtils.ConfigureSwimming(obj);
                    PetPrefabConfigUtils.ConfigureSkyApplier(obj);
                    PetPrefabConfigUtils.ConfigureAnimator(obj, false);
                    obj.DestroyComponentsInChildren<CreatureDeath>();
                    PetPrefabConfigUtils.AddPetComponent(obj);
                    obj.name = ClassId;
                    ModDebugLog.LogDebug($"Done modifying {Info.TechType}");
                };

                prefab.SetGameObject(cloneTemplate);

                // Set the recipe, depends on whether in "Adventure" or "Creative" mode.
                RecipeData recipe = null;
                if (ConfigFile.ModMode == ModMode.Adventure)
                    recipe = new RecipeData(
                        new Ingredient(TechType.Gold, 1),
                        new Ingredient(PetDnaPrefabs.PenglingBabyDnaPrefab.Info.TechType, 3),
                        new Ingredient(TechType.Aerogel, 1),
                        new Ingredient(TechType.Lubricant, 1));
                else
                    recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
                var crafting = prefab.SetRecipe(recipe);
                prefab.Register();
            }
        }

        /// <summary>
        ///     Pengwing Adult
        /// </summary>
        internal static class PengwingAdultPrefab
        {
            private const string ClassId = "PengwingAdultPet";
            private const string IconTextureAssetName = "PengwingAdultIcon_Small.png";
            private const string CloneClassId = "74ded0e7-d394-4703-9e53-4384b37f9433";
            // Init PrefabInfo
            internal static PrefabInfo Info;

            /// <summary>
            ///     Set up the Pengwing Adult Prefab
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);

                var prefab = new CustomPrefab(Info);
                var cloneTemplate = new CloneTemplate(Info, CloneClassId);

                // Modify the cloned model
                cloneTemplate.ModifyPrefab += obj =>
                {
                    obj.SetActive(false);
                    PetPrefabConfigUtils.AddTechTag(obj, Info.TechType);
                    var modelGameObject = obj.GetComponentInChildren<Animator>(true).gameObject;
                    PetPrefabConfigUtils.AddVFXFabricating(obj, null, -0.2f, 1.2f, new Vector3(0.0f, 0.0f, 0.0f), 1.0f,
                        new Vector3(0.0f, 0.0f, 0.0f));
                    PrefabUtils.AddConstructable(obj, Info.TechType, ConstructableFlags.Inside, modelGameObject);
                    PetPrefabConfigUtils.UpdatePickupable(obj, false);
                    PetPrefabConfigUtils.AddPetHandTarget(obj);
                    PetPrefabConfigUtils.ConfigureSwimming(obj);
                    PetPrefabConfigUtils.ConfigureSkyApplier(obj);
                    PetPrefabConfigUtils.ConfigureAnimator(obj, false);
                    obj.DestroyComponentsInChildren<CreatureDeath>();
                    PetPrefabConfigUtils.AddPetComponent(obj);
                    obj.name = ClassId;
                    ModDebugLog.LogDebug($"Done modifying {Info.TechType}");
                };

                prefab.SetGameObject(cloneTemplate);

                // Set the recipe, depends on whether in "Adventure" or "Creative" mode.
                RecipeData recipe = null;
                if (ConfigFile.ModMode == ModMode.Adventure)
                    recipe = new RecipeData(
                        new Ingredient(TechType.Gold, 1),
                        new Ingredient(PetDnaPrefabs.PengwingAdultDnaPrefab.Info.TechType, 3),
                        new Ingredient(TechType.Aerogel, 1),
                        new Ingredient(TechType.Lubricant, 2));
                else
                    recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
                var crafting = prefab.SetRecipe(recipe);
                prefab.Register();
            }
        }

        /// <summary>
        ///     Pinnacarid
        /// </summary>
        internal static class PinnacaridPrefab
        {
            private const string ClassId = "PinnacaridPet";
            private const string IconTextureAssetName = "PinnacaridIcon_Small.png";
            private const string CloneClassId = "f9eccfe2-a06f-4c06-bc57-01c2e50ffbe8";
            // Init PrefabInfo
            internal static PrefabInfo Info;

            /// <summary>
            ///     Set up the Pinnacarid Prefab
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);

                var prefab = new CustomPrefab(Info);
                var cloneTemplate = new CloneTemplate(Info, CloneClassId);

                // Modify the cloned model
                cloneTemplate.ModifyPrefab += obj =>
                {
                    obj.SetActive(false);
                    PetPrefabConfigUtils.AddTechTag(obj, Info.TechType);
                    var modelGameObject = obj.GetComponentInChildren<Animator>(true).gameObject;
                    PetPrefabConfigUtils.AddVFXFabricating(obj, null, -0.2f, 0.6f, new Vector3(0.0f, 0.0f, 0.0f), 1.0f,
                        new Vector3(0.0f, 0.0f, 0.0f));
                    PrefabUtils.AddConstructable(obj, Info.TechType, ConstructableFlags.Inside, modelGameObject);
                    obj.DestroyComponentsInChildren<Pickupable>();
                    PetPrefabConfigUtils.AddPetHandTarget(obj);
                    PetPrefabConfigUtils.ConfigureSwimming(obj);
                    PetPrefabConfigUtils.ConfigureSkyApplier(obj);
                    PetPrefabConfigUtils.ConfigureAnimator(obj, false);
                    PetPrefabConfigUtils.AddPetComponent(obj);
                    obj.name = ClassId;
                    obj.SetActive(false);
                    ModDebugLog.LogDebug($"Done modifying {Info.TechType}");
                };

                prefab.SetGameObject(cloneTemplate);

                // Set the recipe, depends on whether in "Adventure" or "Creative" mode.
                RecipeData recipe = null;
                if (ConfigFile.ModMode == ModMode.Adventure)
                    recipe = new RecipeData(
                        new Ingredient(TechType.Gold, 1),
                        new Ingredient(PetDnaPrefabs.PinnacaridDnaPrefab.Info.TechType, 3),
                        new Ingredient(TechType.Polyaniline, 1),
                        new Ingredient(TechType.Benzene, 1));
                else
                    recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
                var crafting = prefab.SetRecipe(recipe);
                prefab.Register();
            }
        }

        /// <summary>
        ///     Snowstalker Baby
        /// </summary>
        internal static class SnowstalkerBabyPrefab
        {
            private const string ClassId = "SnowstalkerBabyPet";
            private const string IconTextureAssetName = "SnowstalkerBabyIcon_Small.png";
            private const string CloneClassId = "78d3dbce-856f-4eba-951c-bd99870554e2";
            // Init PrefabInfo
            internal static PrefabInfo Info;

            /// <summary>
            ///     Set up the Snowstalker Baby Prefab
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);

                var prefab = new CustomPrefab(Info);
                var cloneTemplate = new CloneTemplate(Info, CloneClassId);

                // Modify the cloned model
                cloneTemplate.ModifyPrefab += obj =>
                {
                    obj.SetActive(false);
                    PetPrefabConfigUtils.AddTechTag(obj, Info.TechType);
                    var modelGameObject = obj.GetComponentInChildren<Animator>(true).gameObject;
                    PetPrefabConfigUtils.AddVFXFabricating(obj, null, -0.2f, 1.0f, new Vector3(0.0f, 0.0f, 0.0f), 1.0f,
                        new Vector3(0.0f, 0.0f, 0.0f));
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
                    ModDebugLog.LogDebug($"Done modifying {Info.TechType}");
                };

                prefab.SetGameObject(cloneTemplate);

                // Set the recipe, depends on whether in "Adventure" or "Creative" mode.
                RecipeData recipe = null;
                if (ConfigFile.ModMode == ModMode.Adventure)
                    recipe = new RecipeData(
                        new Ingredient(TechType.Gold, 1),
                        new Ingredient(PetDnaPrefabs.SnowstalkerBabyDnaPrefab.Info.TechType, 3),
                        new Ingredient(TechType.Aerogel, 2),
                        new Ingredient(TechType.FiberMesh, 1));
                else
                    recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
                var crafting = prefab.SetRecipe(recipe);
                prefab.Register();
            }
        }

        /// <summary>
        ///     Trivalve Blue
        /// </summary>
        internal static class TrivalveBluePrefab
        {
            private const string ClassId = "TrivalveBluePet";
            private const string IconTextureAssetName = "TrivalveBlueIcon_Small.png";
            private const string CloneClassId = "f5a2317f-6116-4fc6-8e81-824fd8ba9684";
            // Init PrefabInfo
            internal static PrefabInfo Info;

            /// <summary>
            ///     Set up the Trivalve Blue Prefab
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);

                var prefab = new CustomPrefab(Info);
                var cloneTemplate = new CloneTemplate(Info, CloneClassId);

                // Modify the cloned model
                cloneTemplate.ModifyPrefab += obj => { ConfigureTrivalve(obj, Info, ClassId, ClassId); };

                prefab.SetGameObject(cloneTemplate);

                // Set the recipe, depends on whether in "Adventure" or "Creative" mode.
                RecipeData recipe = null;
                if (ConfigFile.ModMode == ModMode.Adventure)
                    recipe = new RecipeData(
                        new Ingredient(TechType.Gold, 1),
                        new Ingredient(PetDnaPrefabs.TrivalveBlueDnaPrefab.Info.TechType, 3),
                        new Ingredient(TechType.Aerogel, 1),
                        new Ingredient(TechType.TrivalveBlueEgg, 1));
                else
                    recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
                var crafting = prefab.SetRecipe(recipe);
                prefab.Register();
            }
        }

        /// <summary>
        ///     Trivalve Blue
        /// </summary>
        internal static class TrivalveYellowPrefab
        {
            private const string ClassId = "TrivalveYellowPet";
            private const string IconTextureAssetName = "TrivalveYellowIcon_Small.png";
            private const string CloneClassId = "e8f2bfd4-49c6-45d1-a029-489b492515a9";
            // Init PrefabInfo
            internal static PrefabInfo Info;

            /// <summary>
            ///     Set up the Trivalve Blue Prefab
            /// </summary>
            internal static void Register()
            {
                Info = PrefabInfo
                    .WithTechType(ClassId, null, null, unlockAtStart: true)
                    .WithIcon(ModAssetUtils.GetObjectFromAssetBundle<Sprite>(IconTextureAssetName) as Sprite);

                var prefab = new CustomPrefab(Info);
                var cloneTemplate = new CloneTemplate(Info, CloneClassId);

                // Modify the cloned model
                cloneTemplate.ModifyPrefab += obj => { ConfigureTrivalve(obj, Info, ClassId, ClassId); };

                prefab.SetGameObject(cloneTemplate);

                // Set the recipe, depends on whether in "Adventure" or "Creative" mode.
                RecipeData recipe = null;
                if (ConfigFile.ModMode == ModMode.Adventure)
                    recipe = new RecipeData(
                        new Ingredient(TechType.Gold, 1),
                        new Ingredient(PetDnaPrefabs.TrivalveYellowDnaPrefab.Info.TechType, 3),
                        new Ingredient(TechType.Aerogel, 1),
                        new Ingredient(TechType.TrivalveYellowEgg, 1));
                else
                    recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
                var crafting = prefab.SetRecipe(recipe);
                prefab.Register();
            }
        }
    }
}