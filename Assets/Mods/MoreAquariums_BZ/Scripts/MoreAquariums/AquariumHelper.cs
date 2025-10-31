using System.Collections.Generic;
using DaftAppleGames.ModUtils;
using Nautilus.Utility;
using Sirenix.OdinInspector;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;
using Nautilus.Utility.ThunderkitUtilities;

namespace DaftAppleGames.MoreAquariums
{
    public enum AquariumType
    {
        Double,
        Corner,
        Curved,
        LShaped,
        Desk,
        Spherical
    }

    /// <summary>
    /// Component to allow switching out the new aquarium models on existing prefabs
    /// </summary>
    public class AquariumHelper : MonoBehaviour
    {
        [Header("Mesh/Model")]
        [SerializeField] private MeshFilter aquariumMesh;
        [SerializeField] private MeshFilter aquariumGlassMesh;
        [SerializeField] private GameObject newAquariumModel;
        
        [Header("Main Objects")]
        [SerializeField] private Transform bubbles1Transform;
        [SerializeField] private Transform bubbles2Transform;
        [SerializeField] private Transform coral1Transform;
        [SerializeField] private Transform coral2Transform;
        [SerializeField] private Transform[] existingCoralTransforms;
        [SerializeField] private Transform[] newCoralTransforms;
        [SerializeField] private GameObject rocksObject;
        [SerializeField] private GameObject colliderObject;
        
        [Header("Constructable")]
        [SerializeField] private GameObject constructableBoundsObject;

        [Header("Fish Animation")]
        [SerializeField] private Animator animator1;
        [SerializeField] private Animator animator2;
        [SerializeField] private GameObject[] existingTrackObjects;
        [SerializeField] private GameObject[] existingAttachObjects;
        [SerializeField] private GameObject[] newTrackObjects;
        [SerializeField] private GameObject[] newAttachObjects;

        [Header("Custom Movement")]
        [SerializeField] private FishSettings fishSettings;
        [SerializeField] private GameObject[] movementColliderObjects;
        
        // Used to control the waving animation of the coral and plants
        private static readonly int WaveUpMinParam = Shader.PropertyToID("_WaveUpMin");
        private static readonly int ScaleParam = Shader.PropertyToID("_Scale");
        private static readonly int FrequencyParam = Shader.PropertyToID("_Frequency");
        private static readonly int SpeedParam = Shader.PropertyToID("_Speed");

        /// <summary>
        /// Takes the "vanilla" aquarium prefab, and reconfigures it as the new aquarium 
        /// </summary>
        internal void ConfigureAquariumPrefab(GameObject vanillaAquariumGo, AquariumBase.PrefabData prefabData)
        {
            ModDebugLog.LogDebug($"Configuring aquarium prefab: {vanillaAquariumGo}");

            // Get vanilla references
            Aquarium vanillaAquarium = vanillaAquariumGo.GetComponent<Aquarium>();
            Constructable vanillaConstructable = vanillaAquarium.GetComponent<Constructable>();
            ModDebugLog.LogDebug("Finding model...");
            GameObject aquariumModel = vanillaConstructable.model;
            
            // Configure Storage Container
            ConfigureStorageContainer(vanillaAquariumGo, prefabData.StorageWidth, prefabData.StorageHeight);
            
            // Replace the model meshes
            ConfigureMeshes(vanillaAquariumGo, aquariumModel, prefabData.ReplaceModel);
            
            // Duplicate and reposition coral
            ConfigureCoral(aquariumModel, prefabData.WaveScale);

            // Configure rocks
            ConfigureRocks(vanillaAquariumGo);
            
            // Duplicate and reposition bubbles
            ConfigureBubbles(vanillaAquariumGo, prefabData.AddBubbleAudio);
            
            // Replace the collider
            ConfigureCollider(vanillaAquariumGo);
           
            // If configured, allow construction on other constructables
            vanillaConstructable.allowedOnConstructables = prefabData.AllowConstructionOnConstructables;
            ConfigureConstructable(vanillaAquariumGo, prefabData.AllowConstructionOnConstructables, prefabData.ReplaceModel);
            
            // Reposition tracks and add new
            ConfigureTracks(vanillaAquariumGo, prefabData.UseCustomMovement);

            // Add the new component
            AddAquariumComponent(vanillaAquariumGo, prefabData.AquariumType);
            
            // Call post-prefab config action
            prefabData.PostConfigAction?.Invoke(vanillaAquariumGo);
            
            ModDebugLog.LogDebug("Done configuring prefab!");
        }

        /// <summary>
        /// Configure the Constructable based on the aquarium prefab data
        /// </summary>
        private void ConfigureConstructable(GameObject vanillaAquariumGo, bool allowConstructionOnConstructables, bool replaceModel)
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
        private void ConfigureStorageContainer(GameObject vanillaAquariumGo, int storageWidth, int storageHeight)
        {
            ModDebugLog.LogDebug($"Configuring storage container...");
            StorageContainer storageContainer = vanillaAquariumGo.GetComponentInChildren<StorageContainer>(true);
            storageContainer.height = storageHeight;
            storageContainer.width = storageWidth;
        }
        
        /// <summary>
        /// Apply appropriate changes to meshes or game model 
        /// </summary>
        private void ConfigureMeshes(GameObject vanillaAquariumGo, GameObject aquariumModel, bool replaceModel)
        {
            if (replaceModel)
            {
                ReplaceModel(vanillaAquariumGo, aquariumModel);
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
        private void ReplaceModel(GameObject vanillaAquariumGo, GameObject aquariumModel)
        {
            // Disable the exist geometry
            GameObject animatorGo1 = aquariumModel.transform.Find("Aquarium_animation2").gameObject;
            GameObject animatorGo2 = aquariumModel.transform.Find("Aquarium_animation").gameObject;
            
            ModDebugLog.LogDebug($"Finding geometry...");
            GameObject geometry1 = animatorGo1.transform.Find("Aquarium_geo").gameObject;
            GameObject geometry2 = animatorGo2.transform.Find("Aquarium_geo").gameObject;

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
        }
        
        /// <summary>
        /// Copies then repositions the second coral object to make things a bit more natural
        /// </summary>
        private void ConfigureCoral(GameObject aquariumModelGo, float waveScale)
        {
            ModDebugLog.LogDebug("Configuring coral...");
            Transform coralTransform = aquariumModelGo.transform.Find("Coral");
            if (!coralTransform)
            {
                ModDebugLog.LogError("Could not find aquarium Coral gameobject! Aborting!");
                return;
            }
            GameObject coral = coralTransform.gameObject;
            
            // Move and scale existing coral
            coral.transform.localPosition = coral1Transform.localPosition;
            coral.transform.localRotation = coral1Transform.localRotation;
            coral.transform.localScale = coral1Transform.localScale;
            coral.SetActive(coral1Transform.gameObject.activeSelf);
            
            ConfigureIndividualCoral(existingCoralTransforms, coral, waveScale);
            
            // If we need a second coral, duplicate, position and scale
            if (coral2Transform != null)
            {
                ModDebugLog.LogDebug("Duplicating coral...");

                GameObject newCoral = Instantiate(coral, coral.transform.parent, true);
                // New coral moved to new location, rotation and scale matches existing coral model
                newCoral.transform.localPosition = coral2Transform.localPosition;
                newCoral.transform.localRotation = coral2Transform.localRotation;
                newCoral.transform.localScale = coral2Transform.transform.localScale;
                
                ConfigureIndividualCoral(newCoralTransforms, newCoral, waveScale);
            }
            else
            {
                ModDebugLog.LogDebug("No additional coral to duplicate.");
            }
        }

        /// <summary>
        /// Position and scale the target coral from the source
        /// </summary>
        private void ConfigureIndividualCoral(Transform[] sourceCoralTransforms, GameObject targetCoralGo, float waveScale)
        {
            // Iterate through each coral game object, find it and reposition it
            foreach (Transform coralTransform in sourceCoralTransforms)
            {
                ModDebugLog.LogDebug($"Setting new position of: {coralTransform.gameObject.name}");
                GameObject origCoral = targetCoralGo.transform.Find(coralTransform.gameObject.name).gameObject;
                if (!origCoral)
                {
                    ModDebugLog.LogError(
                        $"Could not find coral gameobject named: {coralTransform.gameObject.name}! Aborting!");
                    return;
                }

                // Reposition to the new position
                origCoral.transform.localPosition = coralTransform.localPosition;
                origCoral.transform.localRotation = coralTransform.localRotation;
                origCoral.transform.localScale = coralTransform.localScale;
                origCoral.SetActive(coralTransform.gameObject.activeSelf);

                if (waveScale < 1.0f)
                {
                    ConfigureCoralMaterials(targetCoralGo, waveScale);
                }
            }
        }

        /// <summary>
        /// Configures animated "waving" by applying a scale factor
        /// </summary> m>
        private void ConfigureCoralMaterials(GameObject coralGo, float waveScale)
        {
            ModDebugLog.LogDebug($"Configuring coral materials... Using waveScale: {waveScale}");
            Renderer[] coralRenderers = coralGo.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer coralRenderer in coralRenderers)
            {
                foreach (Material coralMaterial in coralRenderer.materials)
                {
                    coralMaterial.EnableKeyword("UWE_WAVING");
                    float currUpMin = coralMaterial.GetFloat(WaveUpMinParam);
                    // coralMaterial.SetFloat(WaveUpMinParam, 1.0f);
                    
                    Vector4 currScale = coralMaterial.GetVector(ScaleParam);
                    Vector4 newScale = currScale * waveScale;
                    // ModDebugLog.LogDebug($"Setting scale of: {coralMaterial.name} from {currScale.ToString("F3")} to {newScale.ToString("F3")}");
                    coralMaterial.SetVector(ScaleParam, newScale);
                    Vector4 currFrequency = coralMaterial.GetVector(FrequencyParam);
                    // ModDebugLog.LogDebug($"Setting new frequency of: {coralMaterial.name} to {currFrequency * waveScale}");
                    coralMaterial.SetVector(FrequencyParam, currFrequency * waveScale);
                    Vector2 currSpeed = coralMaterial.GetVector(SpeedParam);
                    // ModDebugLog.LogDebug($"Setting new speed of: {coralMaterial.name} to {currSpeed * waveScale}");
                    coralMaterial.SetVector(SpeedParam, currSpeed * waveScale);
                }
            }
        }
        
        /// <summary>
        /// Copy and reposition the rocks from our new model
        /// </summary>
        private void ConfigureRocks(GameObject vanillaAquariumGo)
        {
            if (!rocksObject)
            {
                ModDebugLog.LogDebug("No rocks to add. Skipping.");
                return;
            }
            
            ModDebugLog.LogDebug("Configuring rocks...");
            // Add the rocks
            GameObject newRocks = Instantiate(rocksObject, vanillaAquariumGo.transform, true);
            newRocks.transform.localPosition = rocksObject.transform.localPosition;
            newRocks.transform.localScale = Vector3.one;
            
            ModDebugLog.LogDebug("Updating SkyApplier...");
            SkyApplier skyApplier = vanillaAquariumGo.GetComponent<SkyApplier>();
            if (!skyApplier)
            {
                ModDebugLog.LogDebug("Looking for SkyApplier in children...");
                skyApplier = vanillaAquariumGo.GetComponentInChildren<SkyApplier>();
            }
            skyApplier.renderers = vanillaAquariumGo.GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>
        /// Copy and reposition and second set of bubbles
        /// </summary>
        private void ConfigureBubbles(GameObject vanillaAquariumGo, bool addBubbleAudio)
        {
            // Duplicate the bubbles
            ModDebugLog.LogDebug("Repositioning bubbles...");
            Transform bubblesTransform = vanillaAquariumGo.transform.Find("Bubbles");
            if (!bubblesTransform)
            {
                ModDebugLog.LogError("Could not find aquarium Bubbles gameobject! Aborting!");
                return;
            }
            GameObject bubbles =bubblesTransform.gameObject;
            
            bubbles.transform.localPosition = bubbles1Transform.localPosition;
            bubbles.transform.localRotation = bubbles1Transform.localRotation;
            bubbles.transform.localScale = bubbles1Transform.localScale;
            bubbles.SetActive(bubbles1Transform.gameObject.activeSelf);
            
            // If we have additional bubbles, duplicate, position and scale
            if (bubbles2Transform)
            {
                ModDebugLog.LogDebug("Duplicating bubbles...");
            
                GameObject newBubbles = Instantiate(bubbles, bubbles.transform.parent, true);
                newBubbles.transform.localPosition = bubbles2Transform.localPosition;
                newBubbles.transform.localRotation = bubbles2Transform.localRotation;
                newBubbles.transform.localScale = bubbles2Transform.localScale;
            }
            else
            {
                ModDebugLog.LogDebug("No additional bubbles to duplicate.");
            }
            
            // Add bubbles audio to the main game object
            if (addBubbleAudio)
            {
                AddCustomEmitter(vanillaAquariumGo);
            }
        }

        /// <summary>
        /// Adds an FMOD Custom Emitter to the bubbles
        /// </summary>
        internal static void AddCustomEmitter(GameObject parentGameObject)
        {
            if (ConfigFile.BubbleAudioEnabled)
            {
                ModDebugLog.LogDebug($"Adding bubbles CustomEmitter to {parentGameObject.name}");
                FMOD_CustomEmitter customEmitter = parentGameObject.EnsureComponent<FMOD_CustomEmitter>();
                ModAudioUtils.ConfigureEmitter(customEmitter, BubblesFMODAsset, ModDebugLog);
                customEmitter.playOnAwake = true;
            }
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
        private void ConfigureTracks(GameObject vanillaAquariumGo, bool customMovement)
        {
            // We'll use this to reset the trackObjects on the Aquarium component
            int trackArrayLength = newTrackObjects == null || newTrackObjects.Length > 0 ? 16 : 8;
            ModDebugLog.LogDebug($"Creating new track array of {trackArrayLength} objects...");
            GameObject[] updatedTrackObjects = new GameObject[trackArrayLength];

            // If using custom movement, we'll need a FishManager on the Game Object with some settings
            if (customMovement)
            {
                ConfigureCustomMovement(vanillaAquariumGo);
            }
            
            // Reconfigure the existing 8 tracks
            Aquarium vanillaAquarium = vanillaAquariumGo.GetComponent<Aquarium>();

            ModDebugLog.LogDebug($"Finding animators...");
            GameObject animatorGo1 = vanillaAquariumGo.transform.Find("model/Aquarium_animation2").gameObject;
            GameObject animatorGo2 = vanillaAquariumGo.transform.Find("model/Aquarium_animation").gameObject;
            
            ModDebugLog.LogDebug($"Finding track roots...");
            GameObject trackRoot1To4 = animatorGo1.transform.Find("root").gameObject;
            GameObject trackRoot5To8 = animatorGo2.transform.Find("root").gameObject;

            ModDebugLog.LogDebug($"Finding geometry...");
            GameObject geometry1 = animatorGo1.transform.Find("Aquarium_geo").gameObject;
            GameObject geometry2 = animatorGo2.transform.Find("Aquarium_geo").gameObject;

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
                GameObject existingTrackGo = trackRoot.transform.Find(existingTrack.name).gameObject;
                existingTrackGo.transform.localPosition = existingTrack.transform.localPosition;
                existingTrackGo.transform.localRotation = customMovement ? Quaternion.identity : existingTrack.transform.localRotation;
                existingTrackGo.transform.localScale = existingTrack.transform.localScale;
                
                ModDebugLog.LogDebug($"Looking for attach in track: {existingAttach.name}");
                GameObject existingAttachGo = existingTrackGo.transform.Find(existingAttach.name).gameObject;

                existingAttachGo.transform.localPosition = existingAttach.transform.localPosition;
                existingAttachGo.transform.localRotation = existingAttach.transform.localRotation;

                if (customMovement)
                {
                    // Add custom movement component
                    ModDebugLog.LogDebug("Adding custom movement script...");
                    existingTrackGo.AddComponent<AquariumFishExt>();
                }
                
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
                    newAttachGo.transform.SetParent(newTrackGo.transform);
                    newAttachGo.transform.localPosition = newAttach.transform.localPosition;
                    ModDebugLog.LogDebug($"Track created successfully");

                    if (customMovement)
                    {
                        // Add custom movement component
                        ModDebugLog.LogDebug("Adding customer movement script...");
                        newTrackGo.AddComponent<AquariumFishExt>();
                    }

                    updatedTrackObjects[currTrackIndex + 8] = newAttachGo;
                    currTrackIndex++;
                }
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

            if (customMovement)
            {
                ModDebugLog.LogDebug($"Disabling animators...");
                anim1.enabled = false;
                anim2.enabled = false;
            }
            ModDebugLog.LogDebug($"Done configuring new aquarium!");
        }

        /// <summary>
        /// Configure components necessary for custom procedural movement
        /// </summary>
        private void ConfigureCustomMovement(GameObject vanillaAquariumGo)
        {
            ModDebugLog.LogDebug("Adding FishManager...");
            FishManager fishManager = vanillaAquariumGo.AddComponent<FishManager>();
            
            ModDebugLog.LogDebug("Applying fish settings...");
            fishManager.SetFishSettings(fishSettings);
            fishManager.SetMovementColliders(ConfigureMovementColliders(vanillaAquariumGo));
        }
        
        /// <summary>
        /// Configure the Movement Collider, if custom movement is needed
        /// </summary>
        private List<Collider> ConfigureMovementColliders(GameObject vanillaAquariumGo)
        {
            GameObject movementColliderContainer = new GameObject("MovementColliders");
            movementColliderContainer.transform.SetParent(vanillaAquariumGo.transform);
            movementColliderContainer.transform.localPosition = Vector3.zero;
            movementColliderContainer.transform.localRotation = Quaternion.identity;
            movementColliderContainer.transform.localScale = Vector3.one;
            
            List<Collider> newMovementColliders = new List<Collider>();
            foreach (GameObject movementColliderObject in movementColliderObjects)
            {
                GameObject newColliderObject = Instantiate(movementColliderObject, movementColliderContainer.transform);
                newColliderObject.transform.localPosition = movementColliderObject.transform.localPosition;
                newColliderObject.transform.localRotation = movementColliderObject.transform.localRotation;
                newColliderObject.transform.localScale = movementColliderObject.transform.localScale;

                newMovementColliders.Add(newColliderObject.GetComponent<Collider>());
            }

            return newMovementColliders;
        }
        
        /// <summary>
        /// Add the correct component
        /// </summary>
        private void AddAquariumComponent(GameObject vanillaAquariumGo, AquariumType aquariumType)
        {
            switch (aquariumType)
            {
                case AquariumType.Double:
                    vanillaAquariumGo.AddComponent<DoubleAquarium>();
                    break;
                case AquariumType.Curved:
                    vanillaAquariumGo.AddComponent<CurvedAquarium>();
                    break;
                case AquariumType.LShaped:
                    vanillaAquariumGo.AddComponent<LShapedAquarium>();
                    break;
                case AquariumType.Corner:
                    vanillaAquariumGo.AddComponent<CornerAquarium>();
                    break;
                case AquariumType.Desk:
                    vanillaAquariumGo.AddComponent<DeskAquarium>();
                    break;
                case AquariumType.Spherical:
                    vanillaAquariumGo.AddComponent<SphericalAquarium>();
                    break;
            }
        }
    }
}