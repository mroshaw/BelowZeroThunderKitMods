using System;
using System.Collections.Generic;
using Nautilus.Utility;
using Sirenix.OdinInspector;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Component to allow switching out the new aquarium models on existing prefabs
    /// </summary>
    public class RoomAquariumConfigurator : AquariumConfigurator
    {
        private const int VanillaTrackCount = 8;
        private const int MaximumTrackCount = 16;

        [BoxGroup("Aquarium")] [SerializeField] private bool allowConstructionOnConstructables;
        [BoxGroup("Aquarium")] [SerializeField] private bool replaceExistingModel;

        [BoxGroup("Mesh Model")] [SerializeField] private MeshFilter aquariumMesh;
        [BoxGroup("Mesh Model")] [SerializeField] private MeshFilter aquariumGlassMesh;
        
        [BoxGroup("Object References")] [SerializeField] private GameObject colliderObject;

        [BoxGroup("Constructable")] [SerializeField] private GameObject constructableBoundsObject;
        
        [BoxGroup("Fish")] [SerializeField] private Animator animator1;
        [BoxGroup("Fish")] [SerializeField] private Animator animator2;
        
        /// <summary>
        /// Takes the "vanilla" aquarium prefab, and reconfigures it as the new aquarium 
        /// </summary>
        internal void ConfigureAquariumPrefab(GameObject vanillaAquariumGo, Action<GameObject> postConfigAction)
        {
            ModDebugLog.LogDebug($"Configuring aquarium prefab: {vanillaAquariumGo}");

            if (!ValidateTrackConfiguration(
                    VanillaTrackCount, MaximumTrackCount, "Room aquarium") ||
                !ValidateCustomMovementConfiguration())
            {
                ModDebugLog.LogError("Aquarium configuration is invalid. Aborting prefab configuration.");
                return;
            }

            Dictionary<GameObject, GameObject> instantiatedObjects =
                new Dictionary<GameObject, GameObject>();

            // Get vanilla references
            Aquarium vanillaAquarium = vanillaAquariumGo.GetComponent<Aquarium>();
            Constructable vanillaConstructable = vanillaAquarium.GetComponent<Constructable>();
            ModDebugLog.LogDebug("Finding model...");
            GameObject aquariumModel = vanillaConstructable.model;
            
            // Configure Storage Container
            ConfigureStorageContainer(vanillaAquariumGo, storageWidth, storageHeight);
            
            // Replace the model meshes
            ConfigureMeshes(vanillaAquariumGo, aquariumModel, instantiatedObjects);
            
            // Duplicate and reposition coral
            ConfigureCoral(aquariumModel);

            // Configure rocks
            ConfigureRocks(vanillaAquariumGo, instantiatedObjects);
            
            // Duplicate and reposition bubbles
            ConfigureBubbles(vanillaAquariumGo);
            
            // Replace the collider
            ConfigureCollider(vanillaAquariumGo);
           
            // If configured, allow construction on other constructables
            vanillaConstructable.allowedOnConstructables = allowConstructionOnConstructables;
            ConfigureConstructable(vanillaAquariumGo);
            
            // Reposition tracks and add new
            ConfigureTracks(vanillaAquariumGo);

            // Add the new component
            AddAquariumComponent(vanillaAquariumGo);
            
            // Call post-prefab config action
            postConfigAction?.Invoke(vanillaAquariumGo);

            // Rebuild both renderer collections after all configuration is complete.
            ConfigureSkyAppliers(vanillaAquariumGo, instantiatedObjects);
            
            ModDebugLog.LogDebug("Done configuring prefab!");
        }

        /// <summary>
        /// Configure the Constructable based on the aquarium prefab data
        /// </summary>
        private void ConfigureConstructable(GameObject vanillaAquariumGo)
        {
            ModDebugLog.LogDebug($"Configuring constructable...");
            Constructable vanillaConstructable = vanillaAquariumGo.GetComponent<Constructable>();
            // If configured, allow construction on other constructables
            vanillaConstructable.allowedOnConstructables = allowConstructionOnConstructables;
            
            ConstructableBounds constructableBounds = vanillaAquariumGo.GetComponentInChildren<ConstructableBounds>();
            if (!constructableBounds)
            {
                ModDebugLog.LogError($"ConstructableBounds not found!");
            }
            
            // ModDebugLog.LogDebug($"Setting bounds to {constructableBoundsPosition}, {constructableBoundsExtents}...");
            // constructableBounds.bounds = new OrientedBounds(constructableBoundsPosition, constructableBoundsRotation , constructableBoundsExtents);
            
            ModDebugLog.LogDebug($"Removing old ConstructableBounds component...");
            Destroy(constructableBounds);
            
            ModDebugLog.LogDebug($"Adding new ConstructableBounds gameobject...");
            GameObject newConstructableBoundsGo =  Instantiate(constructableBoundsObject, vanillaAquariumGo.transform, true);
            newConstructableBoundsGo.name = "ConstructableBounds";
            newConstructableBoundsGo.transform.localPosition = Vector3.zero;
            newConstructableBoundsGo.transform.localRotation = Quaternion.identity;
            newConstructableBoundsGo.transform.localScale = constructableBoundsObject.transform.localScale;
            ModDebugLog.LogDebug($"Done configuring constructable bounds.");
        }

        /// <summary>
        /// Configure the storage container size
        /// </summary>
        private void ConfigureStorageContainer(GameObject vanillaAquariumGo, int newStorageWidth, int newStorageHeight)
        {
            ModDebugLog.LogDebug($"Configuring storage container...");
            StorageContainer storageContainer = vanillaAquariumGo.GetComponentInChildren<StorageContainer>(true);
            storageContainer.height = newStorageHeight;
            storageContainer.width = newStorageWidth;
        }
        
        /// <summary>
        /// Apply appropriate changes to meshes or game model 
        /// </summary>
        private void ConfigureMeshes(GameObject vanillaAquariumGo, GameObject aquariumModel,
            Dictionary<GameObject, GameObject> instantiatedObjects)
        {
            if (replaceExistingModel)
            {
                GameObject replacementModel = ReplaceModel(vanillaAquariumGo, aquariumModel);
                if (replacementModel)
                {
                    instantiatedObjects.Add(newAquariumModel, replacementModel);
                }
            }
            else
            {
                ReplaceMeshes(aquariumModel);
            }
        }

        /// <summary>
        /// Replace the meshes with our custom ones
        /// </summary>
        private void ReplaceMeshes(GameObject aquariumModel)
        {
            ModDebugLog.LogDebug("Replacing meshes...");
            MeshFilter[] meshFilters = aquariumModel.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                // ModDebugLog.LogDebug($"Checking mesh on: {meshFilter.gameObject.name}");
                if (meshFilter.gameObject.name == "Aquarium")
                {
                    ModDebugLog.LogDebug($"Replacing aquarium mesh on: {meshFilter.gameObject.name}");
                    meshFilter.mesh = aquariumMesh.sharedMesh;
                    continue;
                }

                if (meshFilter.gameObject.name == "Aquarium_glass" || meshFilter.gameObject.name == "Aquarium_glass_1")
                {
                    ModDebugLog.LogDebug($"Replacing aquarium glass mesh on: {meshFilter.gameObject.name}");
                    meshFilter.mesh = aquariumGlassMesh.sharedMesh;
                }
            }
        }

        /// <summary>
        /// Replace the entire model
        /// </summary>
        private GameObject ReplaceModel(GameObject vanillaAquariumGo, GameObject aquariumModel)
        {
            // Disable the exist geometry
            Transform animatorTransform1 = FindRequiredTransform(
                aquariumModel.transform, "Aquarium_animation2");
            Transform animatorTransform2 = FindRequiredTransform(
                aquariumModel.transform, "Aquarium_animation");
            if (!animatorTransform1 || !animatorTransform2)
            {
                return null;
            }

            GameObject animatorGo1 = animatorTransform1.gameObject;
            GameObject animatorGo2 = animatorTransform2.gameObject;
            
            ModDebugLog.LogDebug($"Finding geometry...");
            Transform geometryTransform1 = FindRequiredTransform(
                animatorGo1.transform, "Aquarium_geo");
            Transform geometryTransform2 = FindRequiredTransform(
                animatorGo2.transform, "Aquarium_geo");
            if (!geometryTransform1 || !geometryTransform2)
            {
                return null;
            }

            GameObject geometry1 = geometryTransform1.gameObject;
            GameObject geometry2 = geometryTransform2.gameObject;

            ModDebugLog.LogDebug($"Disable geometry...");
            geometry1.SetActive(false);
            geometry2.SetActive(false);
            
            ModDebugLog.LogDebug($"Replacing model...");
            GameObject newModel = Instantiate(newAquariumModel, aquariumModel.transform.parent, false);
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localScale = newAquariumModel.transform.localScale;
            MaterialUtils.ApplySNShaders(newModel);
            Constructable constructable = vanillaAquariumGo.GetComponent<Constructable>();
            constructable.model = newModel;
            return newModel;
        }
        
        /// <summary>
        /// Replace the box collider with colliders for our new models
        /// </summary>
        private void ConfigureCollider(GameObject vanillaAquariumGo)
        {
            ModDebugLog.LogDebug("Replacing collider...");
            Transform oldColliderTransform =  vanillaAquariumGo.transform.Find("Collider");
            if (!oldColliderTransform)
            {
                ModDebugLog.LogError("Could not find collider 'Collider'. Aborting!");
                return;
            }

            GameObject oldCollider = oldColliderTransform.gameObject;
            oldCollider.SetActive(false);
            GameObject newCollider = Instantiate(colliderObject, oldCollider.transform.parent, true);
            newCollider.transform.localPosition = oldCollider.transform.localPosition;
            newCollider.transform.localRotation = oldCollider.transform.localRotation;
            newCollider.transform.localScale = oldCollider.transform.localScale;
        }
        
        /// <summary>
        /// Reconfigures existing Fish Tracks (animation bones) and adds new ones
        /// </summary>
        private void ConfigureTracks(GameObject vanillaAquariumGo)
        {
            // We'll use this to reset the trackObjects on the Aquarium component
            int trackArrayLength = storageWidth * storageHeight;
            ModDebugLog.LogDebug($"Creating new track array of {trackArrayLength} objects...");
            GameObject[] updatedTrackObjects = new GameObject[trackArrayLength];

            // Reconfigure the existing 8 tracks
            Aquarium vanillaAquarium = vanillaAquariumGo.GetComponent<Aquarium>();

            ModDebugLog.LogDebug($"Finding animators...");
            Transform animatorTransform1 = FindRequiredTransform(
                vanillaAquariumGo.transform, "model/Aquarium_animation2");
            Transform animatorTransform2 = FindRequiredTransform(
                vanillaAquariumGo.transform, "model/Aquarium_animation");
            if (!animatorTransform1 || !animatorTransform2)
            {
                return;
            }

            GameObject animatorGo1 = animatorTransform1.gameObject;
            GameObject animatorGo2 = animatorTransform2.gameObject;
            
            ModDebugLog.LogDebug($"Finding track roots...");
            Transform trackRootTransform1To4 = FindRequiredTransform(
                animatorGo1.transform, "root");
            Transform trackRootTransform5To8 = FindRequiredTransform(
                animatorGo2.transform, "root");
            if (!trackRootTransform1To4 || !trackRootTransform5To8)
            {
                return;
            }

            GameObject trackRoot1To4 = trackRootTransform1To4.gameObject;
            GameObject trackRoot5To8 = trackRootTransform5To8.gameObject;

            ModDebugLog.LogDebug($"Finding geometry...");
            Transform geometryTransform1 = FindRequiredTransform(
                animatorGo1.transform, "Aquarium_geo");
            Transform geometryTransform2 = FindRequiredTransform(
                animatorGo2.transform, "Aquarium_geo");
            if (!geometryTransform1 || !geometryTransform2)
            {
                return;
            }

            GameObject geometry1 = geometryTransform1.gameObject;
            GameObject geometry2 = geometryTransform2.gameObject;

            // Update the animators
            // Move the animator gameobject, unparent/reparent children to avoid move
            geometry1.transform.SetParent(null);
            geometry2.transform.SetParent(null);

            // Position Animators
            animatorGo1.transform.localPosition = animator1.transform.localPosition;
            animatorGo1.transform.localRotation = animator1.transform.localRotation;

            animatorGo2.transform.localPosition = animator2.transform.localPosition;
            animatorGo2.transform.localRotation = animator2.transform.localRotation;

            geometry1.transform.SetParent(animatorGo1.transform, true);
            geometry2.transform.SetParent(animatorGo2.transform, true);

            // Update positions of existing track objects
            int currTrackIndex = 0;
            
            foreach (GameObject existingTrack in existingTrackObjects)
            {
                GameObject existingAttach = existingAttachObjects[currTrackIndex];
                ModDebugLog.LogDebug("Processing track containers...");

                ModDebugLog.LogDebug($"Configuring track: {existingTrack.name}");
                GameObject trackRoot = currTrackIndex < 4 ? trackRoot1To4 : trackRoot5To8;

                ModDebugLog.LogDebug($"Looking for track in root: {existingTrack.name}");
                Transform existingTrackTransform = FindRequiredTransform(
                    trackRoot.transform, existingTrack.name);
                if (!existingTrackTransform)
                {
                    return;
                }

                GameObject existingTrackGo = existingTrackTransform.gameObject;
                existingTrackGo.transform.localPosition = existingTrack.transform.localPosition;
                existingTrackGo.transform.localRotation = useCustomMovement ? Quaternion.identity : existingTrack.transform.localRotation;
                existingTrackGo.transform.localScale = existingTrack.transform.localScale;
                
                ModDebugLog.LogDebug($"Looking for attach in track: {existingAttach.name}");
                Transform existingAttachTransform = FindRequiredTransform(
                    existingTrackGo.transform, existingAttach.name);
                if (!existingAttachTransform)
                {
                    return;
                }

                GameObject existingAttachGo = existingAttachTransform.gameObject;

                existingAttachGo.transform.localPosition = existingAttach.transform.localPosition;
                existingAttachGo.transform.localRotation = existingAttach.transform.localRotation;

                updatedTrackObjects[currTrackIndex] = existingAttachGo;
                currTrackIndex++;
            }

            // Create new Fish Tracks
            if (newTrackObjects == null || newTrackObjects.Length == 0)
            {
                    ModDebugLog.LogDebug("No new tracks to create. Skipping.");
            }
            else
            {
                ModDebugLog.LogDebug($"Creating new tracks...");
                int newTrackStartIndex = existingTrackObjects.Length;
                currTrackIndex = 0;
                foreach (GameObject newTrack in newTrackObjects)
                {
                    GameObject newAttach = newAttachObjects[currTrackIndex];
                    GameObject trackRoot = currTrackIndex < 4 ? trackRoot1To4 : trackRoot5To8;

                    ModDebugLog.LogDebug($"Creating track: {trackRoot.name}/{newTrack.name}/{newAttach.name}");
                    GameObject newTrackGo = new GameObject(newTrack.name);
                    newTrackGo.transform.SetParent(trackRoot.transform);
                    GameObject newAttachGo = new GameObject(newAttach.name);

                    newTrackGo.transform.localPosition = newTrack.transform.localPosition;
                    newTrackGo.transform.localRotation = newTrack.transform.localRotation;
                    newTrackGo.transform.localScale = newTrack.transform.localScale;
                    newAttachGo.transform.SetParent(newTrackGo.transform);
                    newAttachGo.transform.localPosition = newAttach.transform.localPosition;
                    newAttachGo.transform.localRotation = newAttach.transform.localRotation;
                    newAttachGo.transform.localScale = newAttach.transform.localScale;
                    ModDebugLog.LogDebug($"Track created successfully");

                    updatedTrackObjects[newTrackStartIndex + currTrackIndex] = newAttachGo;
                    currTrackIndex++;
                }
            }

            // Add and configure the manager that owns runtime fish movement.
            if (useCustomMovement)
            {
                ConfigureCustomMovement(vanillaAquariumGo);
            }

            // Now set the trackObjects on the Aquarium component
            vanillaAquarium.trackObjects = updatedTrackObjects;

            // Get current prefab animators
            ModDebugLog.LogDebug($"Updating animators...");
            Animator anim1 = animatorGo1.GetComponent<Animator>();
            Animator anim2 = animatorGo2.GetComponent<Animator>();

            // Set new ones with additional fish tracks
            anim1.runtimeAnimatorController = animator1.runtimeAnimatorController;
            anim2.runtimeAnimatorController = animator2.runtimeAnimatorController;
            ModDebugLog.LogDebug($"Animators updated");

            if (useCustomMovement)
            {
                ModDebugLog.LogDebug($"Disabling animators...");
                anim1.enabled = false;
                anim2.enabled = false;
            }
            ModDebugLog.LogDebug($"Done configuring new aquarium!");
        }

        private static Transform FindRequiredTransform(Transform parent, string path)
        {
            if (!parent)
            {
                ModDebugLog.LogError(
                    $"Cannot find required transform '{path}' because its parent is null.");
                return null;
            }

            Transform result = parent.Find(path);
            if (!result)
            {
                ModDebugLog.LogError(
                    $"Could not find required transform '{parent.name}/{path}'.");
            }

            return result;
        }
    }
}
