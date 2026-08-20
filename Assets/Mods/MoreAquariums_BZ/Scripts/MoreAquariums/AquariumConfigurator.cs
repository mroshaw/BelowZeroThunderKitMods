using System.Collections.Generic;
using DaftAppleGames.ModTools;
using Sirenix.OdinInspector;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Provides shared authored configuration for all custom aquarium types.
    /// </summary>
    public abstract class AquariumConfigurator : MonoBehaviour
    {
        private const string MovementColliderContainerName = "MovementColliders";
        private const string ExclusionColliderContainerName = "ExclusionColliders";
        private const string ObservatoryGlassRendererName =
            "BaseRoomObservatory_glass";

        [BoxGroup("Aquarium")] [SerializeField] protected int storageHeight;
        [BoxGroup("Aquarium")] [SerializeField] protected int storageWidth;
        [BoxGroup("Aquarium")] [SerializeField] protected bool useCustomMovement;
        [BoxGroup("Aquarium")] [SerializeField] protected float waveScale;
        [BoxGroup("Aquarium")] [SerializeField] protected bool addBubbleAudio;
        [BoxGroup("Mesh Model")] [SerializeField] protected GameObject newAquariumModel;
        [BoxGroup("Object References")] [SerializeField] protected Transform bubbles1Transform;
        [BoxGroup("Object References")] [SerializeField] protected Transform bubbles2Transform;
        [BoxGroup("Object References")] [SerializeField] protected Transform coral1Transform;
        [BoxGroup("Object References")] [SerializeField] protected Transform coral2Transform;
        [BoxGroup("Object References")] [SerializeField] protected Transform[] existingCoralTransforms;
        [BoxGroup("Object References")] [SerializeField] protected Transform[] newCoralTransforms;
        [BoxGroup("Object References")] [SerializeField] protected GameObject rocksObject;
        [BoxGroup("Custom Fish")] [SerializeField] protected FishSettings fishSettings;
        [BoxGroup("Custom Fish")] [SerializeField] protected GameObject[] movementColliderObjects;
        [BoxGroup("Custom Fish")] [SerializeField] protected GameObject[] exclusionColliderObjects;
        [BoxGroup("Fish")] [SerializeField] protected GameObject[] existingTrackObjects;
        [BoxGroup("Fish")] [SerializeField] protected GameObject[] existingAttachObjects;
        [BoxGroup("Fish")] [SerializeField] protected GameObject[] newTrackObjects;
        [BoxGroup("Fish")] [SerializeField] protected GameObject[] newAttachObjects;
        [BoxGroup("Sky Applier")] [SerializeField] protected GameObject[] newNonGlassGameObjects;
        [BoxGroup("Sky Applier")] [SerializeField] protected GameObject[] newGlassGameObjects;

        private static readonly int ScaleParam = Shader.PropertyToID("_Scale");
        private static readonly int FrequencyParam = Shader.PropertyToID("_Frequency");
        private static readonly int SpeedParam = Shader.PropertyToID("_Speed");

        /// <summary>
        /// Adds the shared custom-aquarium marker component.
        /// </summary>
        protected static void AddAquariumComponent(GameObject aquariumGameObject)
        {
            aquariumGameObject.AddComponent<CustomAquarium>();
        }

        /// <summary>
        /// Repositions the vanilla coral hierarchy using the authored coral markers.
        /// </summary>
        protected void ConfigureCoral(GameObject aquariumModelGameObject)
        {
            Transform coralTransform = aquariumModelGameObject.transform.Find("Coral");
            if (!coralTransform || !coral1Transform)
            {
                ModDebugLog.LogError(
                    "Could not configure coral because its vanilla object or primary marker is missing.");
                return;
            }

            GameObject coral = coralTransform.gameObject;
            CopyLocalTransform(coral1Transform, coral.transform);
            coral.SetActive(coral1Transform.gameObject.activeSelf);
            ConfigureIndividualCoral(existingCoralTransforms, coral, waveScale);

            if (coral2Transform)
            {
                GameObject newCoral = Instantiate(coral, coral.transform.parent, true);
                CopyLocalTransform(coral2Transform, newCoral.transform);
                ConfigureIndividualCoral(newCoralTransforms, newCoral, waveScale);
            }
        }

        /// <summary>
        /// Instantiates the authored vanilla-derived rock collection.
        /// </summary>
        protected void ConfigureRocks(GameObject aquariumGameObject,
            Dictionary<GameObject, GameObject> instantiatedObjects)
        {
            if (!rocksObject)
            {
                ModDebugLog.LogDebug("No rocks to add. Skipping.");
                return;
            }

            GameObject newRocks = Instantiate(
                rocksObject, aquariumGameObject.transform, true);
            CopyLocalTransform(rocksObject.transform, newRocks.transform);
            instantiatedObjects.Add(rocksObject, newRocks);
        }

        /// <summary>
        /// Repositions and optionally duplicates the vanilla bubble particle hierarchy.
        /// </summary>
        protected void ConfigureBubbles(GameObject aquariumGameObject)
        {
            Transform bubblesTransform = aquariumGameObject.transform.Find("Bubbles");
            if (!bubblesTransform || !bubbles1Transform)
            {
                ModDebugLog.LogError(
                    "Could not configure bubbles because their vanilla object or primary marker is missing.");
                return;
            }

            GameObject bubbles = bubblesTransform.gameObject;
            CopyLocalTransform(bubbles1Transform, bubbles.transform);
            bubbles.SetActive(bubbles1Transform.gameObject.activeSelf);

            if (bubbles2Transform)
            {
                GameObject newBubbles = Instantiate(
                    bubbles, bubbles.transform.parent, true);
                CopyLocalTransform(bubbles2Transform, newBubbles.transform);
            }

            if (addBubbleAudio)
            {
                AddCustomEmitter(aquariumGameObject);
            }
        }

        /// <summary>
        /// Adds aquarium decorations cloned from a vanilla Aquarium to a non-room aquarium.
        /// </summary>
        protected void ConfigureDecorationsFromVanillaSource(
            GameObject aquariumGameObject, GameObject vanillaAquariumPrefab,
            Dictionary<GameObject, GameObject> instantiatedObjects)
        {
            Constructable vanillaConstructable =
                vanillaAquariumPrefab.GetComponent<Constructable>();
            if (!vanillaConstructable || !vanillaConstructable.model)
            {
                ModDebugLog.LogError(
                    "The vanilla Aquarium prefab has no model for decoration cloning.");
                return;
            }

            Transform vanillaCoral =
                vanillaConstructable.model.transform.Find("Coral");
            Transform vanillaBubbles =
                vanillaAquariumPrefab.transform.Find("Bubbles");
            ReplaceDecorationMarker(coral1Transform, vanillaCoral,
                existingCoralTransforms, waveScale, instantiatedObjects);
            if (coral2Transform)
            {
                ReplaceDecorationMarker(coral2Transform, vanillaCoral,
                    newCoralTransforms, waveScale, instantiatedObjects);
            }

            ReplaceDecorationMarker(bubbles1Transform, vanillaBubbles,
                null, 1.0f, instantiatedObjects);
            if (bubbles2Transform)
            {
                ReplaceDecorationMarker(bubbles2Transform, vanillaBubbles,
                    null, 1.0f, instantiatedObjects);
            }
            if (addBubbleAudio)
            {
                AddCustomEmitter(aquariumGameObject);
            }
        }

        /// <summary>
        /// Adds an FMOD Custom Emitter to an aquarium when bubble audio is enabled.
        /// </summary>
        internal static void AddCustomEmitter(GameObject parentGameObject)
        {
            if (!ConfigFile.BubbleAudioEnabled)
            {
                return;
            }

            FMOD_CustomEmitter customEmitter =
                parentGameObject.EnsureComponent<FMOD_CustomEmitter>();
            ModAudioUtils.ConfigureEmitter(
                customEmitter, BubblesFMODAsset, ModDebugLog);
            customEmitter.playOnAwake = true;
        }

        private void ConfigureIndividualCoral(Transform[] sourceCoralTransforms,
            GameObject targetCoralGameObject, float newWaveScale)
        {
            if (sourceCoralTransforms != null)
            {
                foreach (Transform sourceCoralTransform in sourceCoralTransforms)
                {
                    if (!sourceCoralTransform)
                    {
                        continue;
                    }

                    Transform targetCoralTransform =
                        targetCoralGameObject.transform.Find(
                            sourceCoralTransform.gameObject.name);
                    if (!targetCoralTransform)
                    {
                        ModDebugLog.LogError(
                            $"Could not find vanilla coral '{sourceCoralTransform.name}'.");
                        continue;
                    }

                    CopyLocalTransform(sourceCoralTransform, targetCoralTransform);
                    targetCoralTransform.gameObject.SetActive(
                        sourceCoralTransform.gameObject.activeSelf);
                }
            }

            if (newWaveScale < 1.0f)
            {
                ConfigureCoralMaterials(targetCoralGameObject, newWaveScale);
            }
        }

        private static void ConfigureCoralMaterials(GameObject coralGameObject,
            float newWaveScale)
        {
            Renderer[] coralRenderers =
                coralGameObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer coralRenderer in coralRenderers)
            {
                foreach (Material coralMaterial in coralRenderer.materials)
                {
                    coralMaterial.EnableKeyword("UWE_WAVING");
                    coralMaterial.SetVector(
                        ScaleParam,
                        coralMaterial.GetVector(ScaleParam) * newWaveScale);
                    coralMaterial.SetVector(
                        FrequencyParam,
                        coralMaterial.GetVector(FrequencyParam) * newWaveScale);
                    coralMaterial.SetVector(
                        SpeedParam,
                        coralMaterial.GetVector(SpeedParam) * newWaveScale);
                }
            }
        }

        private void ReplaceDecorationMarker(Transform authoredMarker,
            Transform vanillaDecoration,
            Transform[] authoredChildren, float decorationWaveScale,
            Dictionary<GameObject, GameObject> instantiatedObjects)
        {
            if (!authoredMarker || !vanillaDecoration)
            {
                ModDebugLog.LogError(
                    "Could not replace an aquarium decoration because its marker or vanilla source is missing.");
                return;
            }

            GameObject runtimeMarkerGameObject = FindInstantiatedObject(
                authoredMarker.gameObject, instantiatedObjects);
            if (!runtimeMarkerGameObject)
            {
                ModDebugLog.LogError(
                    $"Could not map decoration marker '{authoredMarker.name}'.");
                return;
            }

            Transform runtimeMarker = runtimeMarkerGameObject.transform;
            GameObject decoration = Instantiate(
                vanillaDecoration.gameObject, runtimeMarker.parent, false);
            decoration.name = vanillaDecoration.name;
            CopyLocalTransform(runtimeMarker, decoration.transform);
            decoration.SetActive(runtimeMarkerGameObject.activeSelf);

            if (authoredChildren != null)
            {
                foreach (Transform authoredChild in authoredChildren)
                {
                    GameObject runtimeChildGameObject = FindInstantiatedObject(
                        authoredChild.gameObject, instantiatedObjects);
                    Transform targetChild =
                        decoration.transform.Find(authoredChild.name);
                    if (!runtimeChildGameObject || !targetChild)
                    {
                        ModDebugLog.LogError(
                            $"Could not map coral marker '{authoredChild.name}'.");
                        continue;
                    }

                    CopyLocalTransform(
                        runtimeChildGameObject.transform, targetChild);
                    targetChild.gameObject.SetActive(
                        runtimeChildGameObject.activeSelf);
                }

                if (decorationWaveScale < 1.0f)
                {
                    ConfigureCoralMaterials(decoration, decorationWaveScale);
                }
            }

            Destroy(runtimeMarkerGameObject);
        }

        /// <summary>
        /// Adds and configures procedural fish movement when enabled.
        /// </summary>
        protected void ConfigureCustomMovement(GameObject aquariumGameObject)
        {
            if (!useCustomMovement)
            {
                return;
            }

            ModDebugLog.LogDebug("Adding FishManager...");
            FishManager fishManager = aquariumGameObject.AddComponent<FishManager>();
            fishManager.SetFishSettings(fishSettings);
            fishManager.SetMovementColliders(
                ConfigureMovementColliders(aquariumGameObject));
            fishManager.SetExclusionColliders(
                ConfigureExclusionColliders(aquariumGameObject));
        }

        /// <summary>
        /// Instantiates authored movement-collider objects without modifying them.
        /// </summary>
        protected List<Collider> ConfigureMovementColliders(GameObject aquariumGameObject)
        {
            GameObject movementColliderContainer =
                new GameObject(MovementColliderContainerName);
            movementColliderContainer.transform.SetParent(aquariumGameObject.transform);
            movementColliderContainer.transform.localPosition = Vector3.zero;
            movementColliderContainer.transform.localRotation = Quaternion.identity;
            movementColliderContainer.transform.localScale = Vector3.one;

            List<Collider> movementColliders = new List<Collider>();
            foreach (GameObject movementColliderObject in movementColliderObjects)
            {
                GameObject newColliderObject = Instantiate(
                    movementColliderObject, movementColliderContainer.transform);
                CopyLocalTransform(
                    movementColliderObject.transform, newColliderObject.transform);

                Collider[] objectColliders = newColliderObject.GetComponents<Collider>();
                foreach (Collider currentCollider in objectColliders)
                {
                    if (currentCollider is BoxCollider || currentCollider is SphereCollider)
                    {
                        movementColliders.Add(currentCollider);
                    }
                }

                ModDebugLog.LogDebug(
                    $"Added supported colliders from {movementColliderObject.name}.");
            }

            return movementColliders;
        }

        private List<Collider> ConfigureExclusionColliders(
            GameObject aquariumGameObject)
        {
            List<Collider> exclusionColliders = new List<Collider>();
            if (exclusionColliderObjects == null ||
                exclusionColliderObjects.Length == 0)
            {
                return exclusionColliders;
            }

            GameObject exclusionColliderContainer =
                new GameObject(ExclusionColliderContainerName);
            exclusionColliderContainer.transform.SetParent(aquariumGameObject.transform);
            exclusionColliderContainer.transform.localPosition = Vector3.zero;
            exclusionColliderContainer.transform.localRotation = Quaternion.identity;
            exclusionColliderContainer.transform.localScale = Vector3.one;

            foreach (GameObject exclusionColliderObject in exclusionColliderObjects)
            {
                if (!exclusionColliderObject)
                {
                    continue;
                }

                GameObject newColliderObject = Instantiate(
                    exclusionColliderObject, exclusionColliderContainer.transform);
                CopyLocalTransform(
                    exclusionColliderObject.transform, newColliderObject.transform);

                Collider[] objectColliders = newColliderObject.GetComponents<Collider>();
                foreach (Collider currentCollider in objectColliders)
                {
                    if (currentCollider is BoxCollider || currentCollider is SphereCollider)
                    {
                        exclusionColliders.Add(currentCollider);
                    }
                }
            }

            return exclusionColliders;
        }

        /// <summary>
        /// Validates shared procedural-movement authoring data.
        /// </summary>
        protected bool ValidateCustomMovementConfiguration()
        {
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
                foreach (Collider currentCollider in colliders)
                {
                    if (currentCollider is BoxCollider || currentCollider is SphereCollider)
                    {
                        hasSupportedCollider = true;
                        break;
                    }
                }

                if (!hasSupportedCollider)
                {
                    ModDebugLog.LogError(
                        $"Movement collider object '{movementColliderObject.name}' must " +
                        "have a BoxCollider or SphereCollider component.");
                    return false;
                }
            }

            if (exclusionColliderObjects == null)
            {
                return true;
            }

            foreach (GameObject exclusionColliderObject in exclusionColliderObjects)
            {
                if (!exclusionColliderObject)
                {
                    ModDebugLog.LogError(
                        "The exclusion collider array contains a missing GameObject reference.");
                    return false;
                }

                Collider[] colliders = exclusionColliderObject.GetComponents<Collider>();
                bool hasSupportedCollider = false;
                foreach (Collider currentCollider in colliders)
                {
                    if (currentCollider is BoxCollider || currentCollider is SphereCollider)
                    {
                        hasSupportedCollider = true;
                        break;
                    }
                }

                if (!hasSupportedCollider)
                {
                    ModDebugLog.LogError(
                        $"Exclusion collider object '{exclusionColliderObject.name}' must " +
                        "have a BoxCollider or SphereCollider component.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates shared fish-track and attachment authoring data.
        /// </summary>
        protected bool ValidateTrackConfiguration(int maximumExistingTrackCount,
            int maximumTrackCount, string aquariumDescription)
        {
            if (storageWidth <= 0 || storageHeight <= 0)
            {
                ModDebugLog.LogError(
                    $"{aquariumDescription} storage dimensions must be positive. " +
                    $"Configured dimensions: {storageWidth}x{storageHeight}.");
                return false;
            }

            long requiredTrackCount = (long)storageWidth * storageHeight;
            if (requiredTrackCount > maximumTrackCount)
            {
                ModDebugLog.LogError(
                    $"{aquariumDescription} requires {requiredTrackCount} tracks, " +
                    $"but supports at most {maximumTrackCount}.");
                return false;
            }

            int existingTrackCount = existingTrackObjects == null
                ? 0
                : existingTrackObjects.Length;
            int existingAttachCount = existingAttachObjects == null
                ? 0
                : existingAttachObjects.Length;
            int newTrackCount = newTrackObjects == null
                ? 0
                : newTrackObjects.Length;
            int newAttachCount = newAttachObjects == null
                ? 0
                : newAttachObjects.Length;

            if (existingTrackCount > maximumExistingTrackCount)
            {
                ModDebugLog.LogError(
                    $"{aquariumDescription} supports at most " +
                    $"{maximumExistingTrackCount} existing tracks, but " +
                    $"{existingTrackCount} were configured.");
                return false;
            }

            if (existingTrackCount != existingAttachCount ||
                newTrackCount != newAttachCount)
            {
                ModDebugLog.LogError(
                    $"{aquariumDescription} track and attachment arrays must have " +
                    "matching lengths.");
                return false;
            }

            if (existingTrackCount + newTrackCount != requiredTrackCount)
            {
                ModDebugLog.LogError(
                    $"{aquariumDescription} storage requires {requiredTrackCount} " +
                    $"tracks, but {existingTrackCount + newTrackCount} were configured.");
                return false;
            }

            return !ContainsMissingReference(existingTrackObjects,
                       aquariumDescription, "existing track") &&
                !ContainsMissingReference(existingAttachObjects,
                    aquariumDescription, "existing attachment") &&
                !ContainsMissingReference(newTrackObjects,
                    aquariumDescription, "new track") &&
                !ContainsMissingReference(newAttachObjects,
                    aquariumDescription, "new attachment");
        }

        private static bool ContainsMissingReference(GameObject[] gameObjects,
            string aquariumDescription, string referenceDescription)
        {
            if (gameObjects == null)
            {
                return false;
            }

            foreach (GameObject gameObject in gameObjects)
            {
                if (!gameObject)
                {
                    ModDebugLog.LogError(
                        $"{aquariumDescription} contains a missing " +
                        $"{referenceDescription} reference.");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the first supported authored movement collider.
        /// </summary>
        protected Collider FindFirstMovementCollider()
        {
            if (movementColliderObjects == null)
            {
                return null;
            }

            foreach (GameObject movementColliderObject in movementColliderObjects)
            {
                if (!movementColliderObject)
                {
                    continue;
                }

                Collider[] colliders = movementColliderObject.GetComponents<Collider>();
                foreach (Collider collider in colliders)
                {
                    if (collider is BoxCollider || collider is SphereCollider)
                    {
                        return collider;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Rebuilds glass and non-glass SkyApplier renderer collections.
        /// </summary>
        protected void ConfigureSkyAppliers(GameObject aquariumGameObject,
            Dictionary<GameObject, GameObject> instantiatedObjects)
        {
            ModDebugLog.LogDebug("Configuring SkyAppliers...");
            SkyApplier[] skyAppliers =
                aquariumGameObject.GetComponentsInChildren<SkyApplier>(true);
            SkyApplier glassSkyApplier = null;
            SkyApplier nonGlassSkyApplier = null;
            foreach (SkyApplier skyApplier in skyAppliers)
            {
                if (skyApplier.anchorSky == Skies.BaseGlass)
                {
                    glassSkyApplier = skyApplier;
                }
                else if (skyApplier.anchorSky == Skies.Auto)
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
            AddMappedRenderers(
                glassRenderers, newGlassGameObjects, instantiatedObjects);

            HashSet<Renderer> explicitNonGlassRenderers = new HashSet<Renderer>();
            AddMappedRenderers(explicitNonGlassRenderers,
                newNonGlassGameObjects, instantiatedObjects);
            foreach (Renderer nonGlassRenderer in explicitNonGlassRenderers)
            {
                glassRenderers.Remove(nonGlassRenderer);
            }

            List<Renderer> nonGlassRenderers = new List<Renderer>();
            Renderer[] allRenderers =
                aquariumGameObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in allRenderers)
            {
                if (renderer && !glassRenderers.Contains(renderer))
                {
                    nonGlassRenderers.Add(renderer);
                }
            }

            glassSkyApplier.renderers = ToRendererArray(glassRenderers);
            nonGlassSkyApplier.renderers = nonGlassRenderers.ToArray();

            // Apply the current sky to renderers added after SkyApplier.Start.
            glassSkyApplier.DebugRefreshSky();
            nonGlassSkyApplier.DebugRefreshSky();

            ModDebugLog.LogDebug(
                $"Configured {nonGlassSkyApplier.renderers.Length} non-glass and " +
                $"{glassSkyApplier.renderers.Length} glass SkyApplier renderers.");
        }

        /// <summary>
        /// Rebuilds the renderer collections used by a Below Zero Observatory base piece.
        /// </summary>
        protected void ConfigureBaseSkyAppliers(GameObject basePieceGameObject,
            Dictionary<GameObject, GameObject> instantiatedObjects,
            Renderer[] originalObservatoryRenderers)
        {
            ModDebugLog.LogDebug("Configuring Observatory SkyAppliers...");
            SkyApplier exteriorSkyApplier = null;
            SkyApplier glassSkyApplier = null;
            SkyApplier interiorSkyApplier = null;
            HashSet<Renderer> originalRendererSet = new HashSet<Renderer>();
            if (originalObservatoryRenderers != null)
            {
                AddRenderers(originalRendererSet, originalObservatoryRenderers);
            }

            SkyApplier[] directSkyAppliers =
                basePieceGameObject.GetComponents<SkyApplier>();

            foreach (SkyApplier skyApplier in directSkyAppliers)
            {
                if (!skyApplier)
                {
                    continue;
                }

                if (skyApplier.anchorSky == Skies.Auto)
                {
                    exteriorSkyApplier = skyApplier;
                    continue;
                }

                bool referencesOriginalRenderer = false;
                bool referencesGlassRenderer = false;
                Renderer[] skyRenderers = skyApplier.renderers;
                if (skyRenderers == null)
                {
                    continue;
                }

                foreach (Renderer renderer in skyRenderers)
                {
                    if (!renderer || !originalRendererSet.Contains(renderer))
                    {
                        continue;
                    }

                    referencesOriginalRenderer = true;
                    if (renderer.name == ObservatoryGlassRendererName)
                    {
                        referencesGlassRenderer = true;
                    }
                }

                if (referencesGlassRenderer)
                {
                    glassSkyApplier = skyApplier;
                }
                else if (referencesOriginalRenderer)
                {
                    interiorSkyApplier = skyApplier;
                }
            }

            if (!exteriorSkyApplier || !glassSkyApplier || !interiorSkyApplier)
            {
                ModDebugLog.LogError(
                    "Could not identify the Observatory exterior, glass, and " +
                    "interior SkyAppliers from their native renderer references.");
                return;
            }

            HashSet<Renderer> glassRenderers = new HashSet<Renderer>();
            AddRenderers(glassRenderers, glassSkyApplier.renderers);
            AddMappedRenderers(
                glassRenderers, newGlassGameObjects, instantiatedObjects);

            HashSet<Renderer> explicitNonGlassRenderers = new HashSet<Renderer>();
            AddMappedRenderers(explicitNonGlassRenderers,
                newNonGlassGameObjects, instantiatedObjects);
            foreach (Renderer nonGlassRenderer in explicitNonGlassRenderers)
            {
                glassRenderers.Remove(nonGlassRenderer);
            }

            HashSet<Renderer> interiorRenderers = new HashSet<Renderer>();
            if (interiorSkyApplier)
            {
                AddRenderers(interiorRenderers, interiorSkyApplier.renderers);
            }

            List<Renderer> exteriorRenderers = new List<Renderer>();
            Renderer[] allRenderers =
                basePieceGameObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in allRenderers)
            {
                if (renderer &&
                    !glassRenderers.Contains(renderer) &&
                    !interiorRenderers.Contains(renderer))
                {
                    exteriorRenderers.Add(renderer);
                }
            }

            exteriorSkyApplier.renderers = exteriorRenderers.ToArray();
            glassSkyApplier.renderers = ToRendererArray(glassRenderers);

            exteriorSkyApplier.DebugRefreshSky();
            glassSkyApplier.DebugRefreshSky();

            if (interiorSkyApplier)
            {
                interiorSkyApplier.renderers = ToRendererArray(interiorRenderers);
                interiorSkyApplier.DebugRefreshSky();
            }

            ModDebugLog.LogDebug(
                $"Configured Observatory SkyAppliers with " +
                $"{exteriorSkyApplier.renderers.Length} exterior, " +
                $"{glassSkyApplier.renderers.Length} glass, and " +
                $"{interiorRenderers.Count} interior renderers.");
        }

        /// <summary>
        /// Maps authored objects to instantiated objects and collects their renderers.
        /// </summary>
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

        /// <summary>
        /// Finds the runtime equivalent of an authored source object.
        /// </summary>
        private static GameObject FindInstantiatedObject(GameObject sourceObject,
            Dictionary<GameObject, GameObject> instantiatedObjects)
        {
            if (!sourceObject)
            {
                return null;
            }

            foreach (KeyValuePair<GameObject, GameObject> mapping in instantiatedObjects)
            {
                GameObject sourceRoot = mapping.Key;
                if (sourceObject == sourceRoot)
                {
                    return mapping.Value;
                }

                if (!sourceObject.transform.IsChildOf(sourceRoot.transform))
                {
                    continue;
                }

                string relativePath = GetRelativePath(
                    sourceRoot.transform, sourceObject.transform);
                Transform mappedTransform = mapping.Value.transform.Find(relativePath);
                return mappedTransform ? mappedTransform.gameObject : null;
            }

            return null;
        }

        /// <summary>
        /// Returns the path of a child relative to a source root.
        /// </summary>
        protected static string GetRelativePath(Transform root, Transform child)
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

        private static void AddRenderers(HashSet<Renderer> target,
            Renderer[] renderers)
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
        /// Copies an authored local transform to an instantiated object.
        /// </summary>
        protected static void CopyLocalTransform(Transform source, Transform target)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }
    }
}
