using DaftAppleGames.ModTools;
using DaftAppleGames.ModTools.Extensions;
using DaftAppleGames.SubnauticaPets.Utils;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Nautilus.Utility;
using UnityEngine;
using UnityEngine.AI;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    internal static class PetPrefabConfigUtils
    {
        /// <summary>
        ///     Adds and configures components for Custom Pets
        /// </summary>
        internal static void ConfigureCustomPet(GameObject targetGameObject, string audioClipName, string busPath,
            float audioVolume)
        {
            ModDebugLog.LogDebug("Setting up FMOD Emitter");
            FMOD_CustomEmitter customEmitter = targetGameObject.EnsureComponent<FMOD_CustomEmitter>();
            ModAudioUtils.RegisterSound(audioClipName, busPath, ModAssetUtils, ModDebugLog, 0.1f, 8.0f, 0, false);
            FMODAsset petFmodAsset = AudioUtils.GetFmodAsset(audioClipName);
            ModAudioUtils.ConfigureEmitter(customEmitter,  petFmodAsset, ModDebugLog);
        }

        /// <summary>
        ///     Adds the specified child pet class component to the given creature GameObject
        ///     based on the given PetCreatureType
        /// </summary>
        internal static void AddPetComponent(GameObject targetGameObject)
        {
            targetGameObject.EnsureComponent<Pet>();
        }

        internal static void ConfigurePrefabIdentifier(GameObject targetGameObject, string classId, TechType techType)
        {
            targetGameObject.EnsureComponent<PrefabIdentifier>().ClassId = classId;

            if (techType != TechType.None) targetGameObject.EnsureComponent<TechTag>().type = techType;
        }

        /// <summary>
        ///     Configure the animator component
        /// </summary>
        internal static void ConfigureAnimator(GameObject targetGameObject, bool isEnabled)
        {
            ModDebugLog.LogDebug("ConfigureAnimator started...");
            var creature = targetGameObject.GetComponent<Creature>();
            var animator = creature.GetAnimator();
            animator.enabled = isEnabled;
            ModDebugLog.LogDebug("ConfigureAnimator done.");
        }

        /// <summary>
        ///     Adds the ScaleOnStart component
        /// </summary>
        internal static void ConfigureScaleOnStart(GameObject targetGameObject, float scaleFactor)
        {
            ModDebugLog.LogDebug("AddScaleOnStart started...");
            var scaleOnStart = targetGameObject.EnsureComponent<ScaleOnStart>();
            scaleOnStart.Scale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            ModDebugLog.LogDebug("AddScaleOnStart done.");
        }

        /// <summary>
        ///     Add VFX Fabricator component
        /// </summary>
        internal static void ConfigureVFXFabricating(GameObject targetGameObject, string pathToModel, float minY, float maxY,
            Vector3 posOffset, float scaleFactor, Vector3 eulerOffset)
        {
            ModDebugLog.LogDebug("AddVFXFabricating started...");
            var modelGameObject = targetGameObject.GetComponentInChildren<Animator>().gameObject;
            if (modelGameObject != null)
                PrefabUtils.AddVFXFabricating(modelGameObject, pathToModel, minY, maxY, posOffset, scaleFactor,
                    eulerOffset);

            ModDebugLog.LogDebug("AddVFXFabricating done.");
        }
        
        /// <summary>
        ///     Sets all Mesh Renderers to the given colour
        /// </summary>
        internal static void SetMeshRenderersColor(GameObject targetGameObject, string modelGameObjectName, Color color)
        {
            ModDebugLog.LogDebug("SetMeshRenderersColor started...");
            targetGameObject.FindChild(modelGameObjectName).GetComponent<MeshRenderer>().material.color = color;
            ModDebugLog.LogDebug("SetMeshRenderersColor done.");
        }

        /// <summary>
        ///     Adds a RotateModel component
        /// </summary>
        internal static void ConfigureRotateModel(GameObject targetGameObject, string modelGameObjectName)
        {
            ModDebugLog.LogDebug("AddRotateModel started...");
            var dnaGameObject = targetGameObject.transform.Find(modelGameObjectName).gameObject;
            var rotateModel = dnaGameObject.EnsureComponent<RotateModel>();
            rotateModel.RotationSpeed = 0.1f;
            ModDebugLog.LogDebug("AddRotateModel done.");
        }

        /// <summary>
        ///     Adds a TechTag component
        /// </summary>
        internal static void ConfigureTechTag(GameObject targetGameObject, TechType techType)
        {
            ModDebugLog.LogDebug("AddTechTag started...");
            var techTag = targetGameObject.EnsureComponent<TechTag>();
            techTag.type = techType;
            ModDebugLog.LogDebug("AddTechTag done");
        }

        /// <summary>
        ///     Updates the Pickupable component
        /// </summary>
        internal static void ConfigurePickupable(GameObject targetGameObject, bool isPickupable)
        {
            ModDebugLog.LogDebug("UpdatePickupable started...");
            // Prevent fragments from being phsyically picked up
            var pickupable = targetGameObject.GetComponent<Pickupable>();
            if (pickupable) pickupable.isPickupable = isPickupable;

            ModDebugLog.LogDebug("UpdatePickupable done.");
        }

        /// <summary>
        ///     Add the PetHandTarget component
        /// </summary>
        internal static void ConfigurePetHandTarget(GameObject targetGameObject)
        {
            ModDebugLog.LogDebug("AddPetHandTarget started...");
            targetGameObject.AddComponent<PetHandTarget>();
            ModDebugLog.LogDebug("AddPetHandTarget done.");
        }

        /// <summary>
        ///     Configure Pet Traits for "friendly" creatures
        /// </summary>
        internal static void ConfigurePetTraits(GameObject targetGameObject)
        {
            ModDebugLog.LogDebug("ConfigurePetTraits started...");
            var creature = targetGameObject.GetComponent<Creature>();
            if (creature)
            {
                creature.Friendliness.Value = 1.0f;
                creature.Happy.Value = 1.0f;
                creature.Aggression.Value = 0.0f;
                creature.Scared.Value = 0.0f;
                creature.Curiosity.Value = 1.0f;
                creature.Hunger.Value = 1.0f;
                creature.Tired.Value = 0.0f;
            }

            ModDebugLog.LogDebug("ConfigurePetTraits done.");
        }

        /// <summary>
        ///     Update the state of the RigidBody
        /// </summary>
        internal static void SetRigidBodyKinematic(GameObject targetGameObject, bool isKinematic)
        {
            ModDebugLog.LogDebug("SetRigidBodyKinematic started...");
            var rigidbody = targetGameObject.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rigidbody.isKinematic = isKinematic;
            }

            ModDebugLog.LogDebug("SetRigidBodyKinematic done.");
        }

        /// <summary>
        ///     Configures the Sky and SkyApplier, to ensure
        ///     creature mesh shaders don't look "dull".
        /// </summary>
        internal static void ConfigureSkyApplier(GameObject targetGameObject)
        {
            ModDebugLog.LogDebug("ConfigureSkyApplier started...");
            var pet = targetGameObject.GetComponent<Pet>();

            var skyApplier = targetGameObject.EnsureComponent<SkyApplier>();
            ModDebugLog.LogDebug("Pet: ConfigureSkyApplier added SkyApplier component.");

            ModDebugLog.LogDebug("Pet: ConfigureSkyApplier setting SkyApplier Sky.");
            // skyApplier.SetSky(Skies.BaseInterior);

            ModDebugLog.LogDebug("Pet: ConfigureSkyApplier updating renderers...");
            var creatureRenderers = targetGameObject.GetComponentsInChildren<Renderer>(true);
            ModDebugLog.LogDebug($"Pet: ConfigureSkyApplier found {creatureRenderers.Length} renderers...");
            skyApplier.dynamic = false;
            if (creatureRenderers.Length > 0) skyApplier.renderers = creatureRenderers;

            ModDebugLog.LogDebug("ConfigureSkyApplier done.");
        }

        /// <summary>
        ///     Sets up a PDA Databank entry
        /// </summary>
        internal static void ConfigureDatabankEntry(string encyKey, string encyPath, string mainImageTextureName,
            string popupImageTextureName)
        {
            var mainImage =
                ModAssetUtils.GetObjectFromAssetBundle<Texture2D>(mainImageTextureName) as Texture2D;
            var popupImageSprite =
                ModAssetUtils.GetObjectFromAssetBundle<Sprite>(popupImageTextureName) as Sprite;
            if (!popupImageSprite)
            {
                var popupImageTexture =
                    ModAssetUtils.GetObjectFromAssetBundle<Texture2D>(popupImageTextureName) as Texture2D;
                popupImageSprite = ModAssetUtils.GetSpriteFromTexture(popupImageTexture);
            }

            PDAHandler.AddEncyclopediaEntry(encyKey, encyPath, null, null,
                mainImage, popupImageSprite);
        }

        public static void RegisterCustomPet(PrefabInfo prefabInfo, string classId, string bundlePrefabName,
            string audioClipName,
            TechType techType, TechType dnaTechType)
        {
            var prefab = new CustomPrefab(prefabInfo);

            var prefabGameObject =
                ModAssetUtils.GetObjectFromAssetBundle<GameObject>(bundlePrefabName) as GameObject;

            var model = prefabGameObject.transform.Find("model").gameObject;
            var petEyes = prefabGameObject.transform.Find("Eyes");

            // Standard components
            ConfigurePrefabIdentifier(prefabGameObject, classId, techType);
            PrefabUtils.AddConstructable(prefabGameObject, prefabInfo.TechType, ConstructableFlags.Base, model);
            PrefabUtils.AddVFXFabricating(prefabGameObject, "model", -0.2f, 0.9f, new Vector3(0.0f, 0.0f, 0.0f), 0.7f,
                new Vector3(0.0f, 0.0f, 0.0f));
            MaterialUtils.ApplySNShaders(prefabGameObject);

            // Custom Pet Components
            ConfigureCustomPet(prefabGameObject, audioClipName, AudioUtils.BusPaths.UnderwaterCreatures, 10.0f);
            prefab.SetGameObject(prefabGameObject);

            // Set the recipe, depends on whether in "Adventure" or "Creative" mode.
            RecipeData recipe = null;
            if (ConfigFile.ModMode == ModMode.Adventure)
            {
                if (dnaTechType != TechType.None)
                    recipe = new RecipeData(
                        new Ingredient(TechType.Gold, 1),
                        new Ingredient(TechType.Titanium, 1),
                        new Ingredient(TechType.Salt, 1),
                        new Ingredient(dnaTechType, 2));
                else
                    recipe = new RecipeData(
                        new Ingredient(TechType.Gold, 1),
                        new Ingredient(TechType.Titanium, 1),
                        new Ingredient(TechType.Salt, 1));
            }
            else
            {
                recipe = new RecipeData(new Ingredient(TechType.Titanium, 1));
            }

            var crafting = prefab.SetRecipe(recipe);
            prefab.Register();
        }

        /// <summary>
        ///     Configure Swimming components
        /// </summary>
        internal static void ConfigureLandOnlyCreature(GameObject targetGameObject)
        {
            ModDebugLog.LogDebug("ConfigureLandOnlyCreature started...");
            // Prevent Pet from swimming in interiors   
            ModDebugLog.LogDebug("... ConfigurePetCreature:  LandCreatureGravity...");
            var landCreatureGravity = targetGameObject.GetComponent<LandCreatureGravity>();
            landCreatureGravity.forceLandMode = true;
            landCreatureGravity.enabled = true;
            ModDebugLog.LogDebug("ConfigureLandOnlyCreature done.");
        }

        /// <summary>
        ///     Cleans up all the NavMesh related components on the Pet Game Object
        /// </summary>
        internal static void CleanNavUpMesh(GameObject targetGameObject)
        {
            ModDebugLog.LogDebug("CleanNavUpMesh started...");
            // Remove NavMesh components
            targetGameObject.DestroyComponentsInChildren<MoveOnNavMesh>();
            targetGameObject.DisableComponentsInChildren<NavMeshFollowing>();
            targetGameObject.DisableComponentsInChildren<NavMeshAgent>();
            ModDebugLog.LogDebug("CleanNavUpMesh done.");
        }

        /// <summary>
        ///     Override the SnowStalker movement
        /// </summary>
        internal static void ConfigureMovement(GameObject targetGameObject)
        {
            ModDebugLog.LogDebug("ConfigureMovement started...");
            var snowStalker = targetGameObject.GetComponent<SnowStalkerBaby>();

            // Add a SurfaceMovement component, get that little bugger moving around!
            ModDebugLog.LogDebug("... Configuring movement components ...");
            var onSurfaceTracker = targetGameObject.EnsureComponent<OnSurfaceTracker>();
            var walkBehaviour = targetGameObject.EnsureComponent<WalkBehaviour>();
            var onSurfaceMovement = targetGameObject.EnsureComponent<OnSurfaceMovement>();
            var moveOnSurface = targetGameObject.EnsureComponent<MoveOnSurface>();

            // Configure walking and movement components
            onSurfaceMovement.onSurfaceTracker = onSurfaceTracker;
            onSurfaceMovement.locomotion = targetGameObject.EnsureComponent<Locomotion>();
            moveOnSurface.onSurfaceMovement = onSurfaceMovement;
            moveOnSurface.moveRadius = 7.0f;
            walkBehaviour.onSurfaceMovement = onSurfaceMovement;
            walkBehaviour.onSurfaceTracker = onSurfaceTracker;
            snowStalker.onSurfaceMovement = onSurfaceMovement;
            ModDebugLog.LogDebug("... Configuring movement components ... Done");

            // Add Obstacle Avoidance components
            ModDebugLog.LogDebug("... Configuring AvoidObstaclesOnLand...");
            var avoidObstaclesOnLand = targetGameObject.EnsureComponent<AvoidObstaclesOnLand>();
            var avoidObstaclesOnSurface = targetGameObject.EnsureComponent<AvoidObstaclesOnSurface>();
            avoidObstaclesOnLand.creature = snowStalker;
            avoidObstaclesOnSurface.creature = snowStalker;
            avoidObstaclesOnLand.swimBehaviour = walkBehaviour;
            avoidObstaclesOnLand.scanDistance = 0.5f;
            ModDebugLog.LogDebug("... Configuring AvoidObstaclesOnLand... Done");

            // Configure swim behaviour
            ModDebugLog.LogDebug("... Configuring SwimRandom and LastTarget...");
            var lastTarget = targetGameObject.EnsureComponent<LastTarget>();
            var swimRandom = targetGameObject.EnsureComponent<SwimRandom>();
            swimRandom.swimBehaviour = walkBehaviour;
            ModDebugLog.LogDebug("ConfigureMovement started... Done.");
        }
    }
}
