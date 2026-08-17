using System;
using System.Collections.Generic;
using DaftAppleGames.ModTools;
using Nautilus.Utility;
using Sirenix.OdinInspector;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Component to allow switching out the new aquarium models on existing prefabs
    /// </summary>
    public class AquariumConfigurator : MonoBehaviour
    {
        private const int VanillaTrackCount = 8;
        private const int MaximumTrackCount = 16;

        [BoxGroup("Aquarium")] [SerializeField] private int storageHeight;
        [BoxGroup("Aquarium")] [SerializeField] private int storageWidth;
        [BoxGroup("Aquarium")] [SerializeField] private bool useCustomMovement;
        [BoxGroup("Aquarium")] [SerializeField] private bool allowConstructionOnConstructables;
        [BoxGroup("Aquarium")] [SerializeField] private float waveScale;
        [BoxGroup("Aquarium")] [SerializeField] private bool replaceExistingModel;
        [BoxGroup("Aquarium")] [SerializeField] private bool addBubbleAudio;

        [BoxGroup("Mesh Model")] [SerializeField] private MeshFilter aquariumMesh;
        [BoxGroup("Mesh Model")] [SerializeField] private MeshFilter aquariumGlassMesh;
        [BoxGroup("Mesh Model")] [SerializeField] private GameObject newAquariumModel;
        
        [BoxGroup("Object References")] [SerializeField] private Transform bubbles1Transform;
        [BoxGroup("Object References")] [SerializeField] private Transform bubbles2Transform;
        [BoxGroup("Object References")] [SerializeField] private Transform coral1Transform;
        [BoxGroup("Object References")] [SerializeField] private Transform coral2Transform;
        [BoxGroup("Object References")] [SerializeField] private Transform[] existingCoralTransforms;
        [BoxGroup("Object References")] [SerializeField] private Transform[] newCoralTransforms;
        [BoxGroup("Object References")] [SerializeField] private GameObject rocksObject;
        [BoxGroup("Object References")] [SerializeField] private GameObject colliderObject;

        [BoxGroup("Sky Applier")] [SerializeField] private GameObject[] newNonGlassGameObjects;
        [BoxGroup("Sky Applier")] [SerializeField] private GameObject[] newGlassGameObjects;
        
        [BoxGroup("Constructable")] [SerializeField] private GameObject constructableBoundsObject;
        
        [BoxGroup("Fish")] [SerializeField] private Animator animator1;
        [BoxGroup("Fish")] [SerializeField] private Animator animator2;
        [BoxGroup("Fish")] [SerializeField] private GameObject[] existingTrackObjects;
        [BoxGroup("Fish")] [SerializeField] private GameObject[] existingAttachObjects;
        [BoxGroup("Fish")] [SerializeField] private GameObject[] newTrackObjects;
        [BoxGroup("Fish")] [SerializeField] private GameObject[] newAttachObjects;
        
        [BoxGroup("Custom Fish")] [SerializeField] private FishSettings fishSettings;
        [BoxGroup("Custom Fish")] [SerializeField] private GameObject[] movementColliderObjects;
        
        // Used to control the waving animation of the coral and plants
        private static readonly int WaveUpMinParam = Shader.PropertyToID("_WaveUpMin");
        private static readonly int ScaleParam = Shader.PropertyToID("_Scale");
        private static readonly int FrequencyParam = Shader.PropertyToID("_Frequency");
        private static readonly int SpeedParam = Shader.PropertyToID("_Speed");

        /// <summary>
        /// Takes the "vanilla" aquarium prefab, and reconfigures it as the new aquarium 
        /// </summary>
        internal void ConfigureAquariumPrefab(GameObject vanillaAquariumGo, Action<GameObject> postConfigAction)
        {
            ModDebugLog.LogDebug($"Configuring aquarium prefab: {vanillaAquariumGo}");

            if (!ValidateConfiguration())
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
        /// Copies then repositions the second coral object to make things a bit more natural
        /// </summary>
        private void ConfigureCoral(GameObject aquariumModelGo)
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
        private void ConfigureIndividualCoral(Transform[] sourceCoralTransforms, GameObject targetCoralGo, float newWaveScale)
        {
            // Iterate through each coral game object, find it and reposition it
            foreach (Transform coralTransform in sourceCoralTransforms)
            {
                ModDebugLog.LogDebug($"Setting new position of: {coralTransform.gameObject.name}");
                Transform originalCoralTransform = FindRequiredTransform(
                    targetCoralGo.transform, coralTransform.gameObject.name);
                if (!originalCoralTransform)
                {
                    return;
                }

                GameObject origCoral = originalCoralTransform.gameObject;

                // Reposition to the new position
                origCoral.transform.localPosition = coralTransform.localPosition;
                origCoral.transform.localRotation = coralTransform.localRotation;
                origCoral.transform.localScale = coralTransform.localScale;
                origCoral.SetActive(coralTransform.gameObject.activeSelf);

            }

            if (newWaveScale < 1.0f)
            {
                ConfigureCoralMaterials(targetCoralGo, newWaveScale);
            }
        }

        /// <summary>
        /// Configures animated "waving" by applying a scale factor
        /// </summary> m>
        private void ConfigureCoralMaterials(GameObject coralGo, float newWaveScale)
        {
            ModDebugLog.LogDebug($"Configuring coral materials... Using waveScale: {newWaveScale}");
            Renderer[] coralRenderers = coralGo.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer coralRenderer in coralRenderers)
            {
                foreach (Material coralMaterial in coralRenderer.materials)
                {
                    coralMaterial.EnableKeyword("UWE_WAVING");
                    float currUpMin = coralMaterial.GetFloat(WaveUpMinParam);
                    // coralMaterial.SetFloat(WaveUpMinParam, 1.0f);
                    
                    Vector4 currScale = coralMaterial.GetVector(ScaleParam);
                    Vector4 newScale = currScale * newWaveScale;
                    // ModDebugLog.LogDebug($"Setting scale of: {coralMaterial.name} from {currScale.ToString("F3")} to {newScale.ToString("F3")}");
                    coralMaterial.SetVector(ScaleParam, newScale);
                    Vector4 currFrequency = coralMaterial.GetVector(FrequencyParam);
                    // ModDebugLog.LogDebug($"Setting new frequency of: {coralMaterial.name} to {currFrequency * waveScale}");
                    coralMaterial.SetVector(FrequencyParam, currFrequency * newWaveScale);
                    Vector2 currSpeed = coralMaterial.GetVector(SpeedParam);
                    // ModDebugLog.LogDebug($"Setting new speed of: {coralMaterial.name} to {currSpeed * waveScale}");
                    coralMaterial.SetVector(SpeedParam, currSpeed * newWaveScale);
                }
            }
        }
        
        /// <summary>
        /// Copy and reposition the rocks from our new model
        /// </summary>
        private void ConfigureRocks(GameObject vanillaAquariumGo,
            Dictionary<GameObject, GameObject> instantiatedObjects)
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
            newRocks.transform.localRotation = rocksObject.transform.localRotation;
            newRocks.transform.localScale = rocksObject.transform.localScale;
            instantiatedObjects.Add(rocksObject, newRocks);
        }

        /// <summary>
        /// Rebuilds the glass and non-glass SkyApplier renderer collections.
        /// </summary>
        private void ConfigureSkyAppliers(GameObject vanillaAquariumGo,
            Dictionary<GameObject, GameObject> instantiatedObjects)
        {
            ModDebugLog.LogDebug("Configuring SkyAppliers...");
            SkyApplier[] skyAppliers =
                vanillaAquariumGo.GetComponentsInChildren<SkyApplier>(true);
            SkyApplier glassSkyApplier = null;
            SkyApplier nonGlassSkyApplier = null;

            foreach (SkyApplier skyApplier in skyAppliers)
            {
                if (skyApplier.anchorSky == Skies.BaseGlass)
                {
                    glassSkyApplier = skyApplier;
                }
                else
                {
                    nonGlassSkyApplier = skyApplier;
                }
            }

            if (!glassSkyApplier || !nonGlassSkyApplier)
            {
                ModDebugLog.LogError(
                    "Could not find both glass and non-glass Aquarium SkyAppliers.");
                return;
            }

            HashSet<Renderer> glassRenderers = new HashSet<Renderer>();
            AddRenderers(glassRenderers, glassSkyApplier.renderers);
            AddMappedRenderers(glassRenderers, newGlassGameObjects, instantiatedObjects);

            HashSet<Renderer> explicitNonGlassRenderers = new HashSet<Renderer>();
            AddMappedRenderers(explicitNonGlassRenderers, newNonGlassGameObjects,
                instantiatedObjects);
            foreach (Renderer currRenderer in explicitNonGlassRenderers)
            {
                glassRenderers.Remove(currRenderer);
            }

            List<Renderer> nonGlassRenderers = new List<Renderer>();
            Renderer[] allRenderers =
                vanillaAquariumGo.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer currRenderer in allRenderers)
            {
                if (currRenderer && !glassRenderers.Contains(currRenderer))
                {
                    nonGlassRenderers.Add(currRenderer);
                }
            }

            glassSkyApplier.renderers = ToRendererArray(glassRenderers);
            nonGlassSkyApplier.renderers = nonGlassRenderers.ToArray();

            ModDebugLog.LogDebug(
                $"Configured {nonGlassSkyApplier.renderers.Length} non-glass and " +
                $"{glassSkyApplier.renderers.Length} glass SkyApplier renderers.");
        }

        private static void AddMappedRenderers(HashSet<Renderer> renderers,
            GameObject[] sourceObjects,
            Dictionary<GameObject, GameObject> instantiatedObjects)
        {
            if (sourceObjects == null)
            {
                return;
            }

            foreach (GameObject sourceObject in sourceObjects)
            {
                GameObject instantiatedObject =
                    FindInstantiatedObject(sourceObject, instantiatedObjects);
                if (!instantiatedObject)
                {
                    string sourceName = sourceObject ? sourceObject.name : "null";
                    ModDebugLog.LogError(
                        $"Could not map SkyApplier object '{sourceName}'.");
                    continue;
                }

                AddRenderers(renderers,
                    instantiatedObject.GetComponentsInChildren<Renderer>(true));
            }
        }

        private static GameObject FindInstantiatedObject(GameObject sourceObject,
            Dictionary<GameObject, GameObject> instantiatedObjects)
        {
            if (!sourceObject)
            {
                return null;
            }

            foreach (KeyValuePair<GameObject, GameObject> instantiatedObject in
                     instantiatedObjects)
            {
                GameObject sourceRoot = instantiatedObject.Key;
                if (sourceObject == sourceRoot)
                {
                    return instantiatedObject.Value;
                }

                if (!sourceObject.transform.IsChildOf(sourceRoot.transform))
                {
                    continue;
                }

                string relativePath = GetRelativePath(
                    sourceRoot.transform, sourceObject.transform);
                Transform mappedTransform = instantiatedObject.Value.transform.Find(relativePath);
                return mappedTransform ? mappedTransform.gameObject : null;
            }

            return null;
        }

        private static string GetRelativePath(Transform root, Transform child)
        {
            List<string> pathParts = new List<string>();
            Transform current = child;
            while (current && current != root)
            {
                pathParts.Add(current.name);
                current = current.parent;
            }

            pathParts.Reverse();
            return string.Join("/", pathParts.ToArray());
        }

        private static void AddRenderers(HashSet<Renderer> target, Renderer[] renderers)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer)
                {
                    target.Add(renderer);
                }
            }
        }

        private static Renderer[] ToRendererArray(HashSet<Renderer> renderers)
        {
            Renderer[] result = new Renderer[renderers.Count];
            renderers.CopyTo(result);
            return result;
        }
        
        /// <summary>
        /// Copy and reposition and second set of bubbles
        /// </summary>
        private void ConfigureBubbles(GameObject vanillaAquariumGo)
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
                ModDebugLog.LogDebug("Adding bubbles emitter to custom aquarium...");
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

        /// <summary>
        /// Validates that configured fish tracks match the aquarium storage capacity.
        /// </summary>
        private bool ValidateTrackConfiguration()
        {
            if (storageWidth <= 0 || storageHeight <= 0)
            {
                ModDebugLog.LogError(
                    $"Storage dimensions must be positive. Configured dimensions: " +
                    $"{storageWidth}x{storageHeight}.");
                return false;
            }

            long requiredTrackCount = (long)storageWidth * storageHeight;
            if (requiredTrackCount > MaximumTrackCount)
            {
                ModDebugLog.LogError(
                    $"Aquarium requires {requiredTrackCount} tracks, but the current " +
                    $"track implementation supports at most {MaximumTrackCount}.");
                return false;
            }

            int existingTrackCount = existingTrackObjects?.Length ?? 0;
            int existingAttachCount = existingAttachObjects?.Length ?? 0;
            int newTrackCount = newTrackObjects?.Length ?? 0;
            int newAttachCount = newAttachObjects?.Length ?? 0;

            if (existingTrackCount > VanillaTrackCount)
            {
                ModDebugLog.LogError(
                    $"Only {VanillaTrackCount} existing vanilla tracks are available, " +
                    $"but {existingTrackCount} were configured.");
                return false;
            }

            if (existingTrackCount != existingAttachCount)
            {
                ModDebugLog.LogError(
                    $"Existing track count ({existingTrackCount}) does not match " +
                    $"existing attach count ({existingAttachCount}).");
                return false;
            }

            if (newTrackCount != newAttachCount)
            {
                ModDebugLog.LogError(
                    $"New track count ({newTrackCount}) does not match " +
                    $"new attach count ({newAttachCount}).");
                return false;
            }

            if (existingTrackCount + newTrackCount != requiredTrackCount)
            {
                ModDebugLog.LogError(
                    $"Storage dimensions require {requiredTrackCount} fish tracks, " +
                    $"but {existingTrackCount} existing and {newTrackCount} new tracks " +
                    "were configured.");
                return false;
            }

            if (ContainsNullEntry(existingTrackObjects, "existing track") ||
                ContainsNullEntry(existingAttachObjects, "existing attach") ||
                ContainsNullEntry(newTrackObjects, "new track") ||
                ContainsNullEntry(newAttachObjects, "new attach"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the complete aquarium authoring configuration.
        /// </summary>
        private bool ValidateConfiguration()
        {
            if (!ValidateTrackConfiguration())
            {
                return false;
            }

            if (!useCustomMovement)
            {
                return true;
            }

            if (!fishSettings)
            {
                ModDebugLog.LogError(
                    "Custom fish movement is enabled, but no FishSettings asset is configured.");
                return false;
            }

            if (movementColliderObjects == null || movementColliderObjects.Length == 0)
            {
                ModDebugLog.LogError(
                    "Custom fish movement is enabled, but no movement collider objects are configured.");
                return false;
            }

            foreach (GameObject movementColliderObject in movementColliderObjects)
            {
                if (!movementColliderObject)
                {
                    ModDebugLog.LogError(
                        "The movement collider array contains a missing GameObject reference.");
                    return false;
                }

                Collider[] colliders = movementColliderObject.GetComponents<Collider>();
                bool hasSupportedCollider = false;
                foreach (Collider currCollider in colliders)
                {
                    if (currCollider is BoxCollider || currCollider is SphereCollider)
                    {
                        hasSupportedCollider = true;
                        break;
                    }
                }

                if (!hasSupportedCollider)
                {
                    ModDebugLog.LogError(
                        $"Movement collider object '{movementColliderObject.name}' must have " +
                        "a BoxCollider or SphereCollider component.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reports whether a configured GameObject array contains a missing reference.
        /// </summary>
        private static bool ContainsNullEntry(GameObject[] objects, string entryDescription)
        {
            if (objects == null)
            {
                return false;
            }

            foreach (GameObject configuredObject in objects)
            {
                if (!configuredObject)
                {
                    ModDebugLog.LogError(
                        $"The {entryDescription} array contains a missing GameObject reference.");
                    return true;
                }
            }

            return false;
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

                Collider[] objectColliders = newColliderObject.GetComponents<Collider>();
                foreach (Collider currCollider in objectColliders)
                {
                    if (currCollider is BoxCollider || currCollider is SphereCollider)
                    {
                        newMovementColliders.Add(currCollider);
                    }
                }

                ModDebugLog.LogDebug(
                    $"Added supported colliders from {movementColliderObject.name}");
            }

            return newMovementColliders;
        }
        
        /// <summary>
        /// Add the correct component
        /// </summary>
        private void AddAquariumComponent(GameObject vanillaAquariumGo)
        {
            vanillaAquariumGo.AddComponent<CustomAquarium>();
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
