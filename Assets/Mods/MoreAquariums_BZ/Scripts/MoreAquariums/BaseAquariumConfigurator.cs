using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Configures aquarium functionality on an Observatory-derived base piece.
    /// </summary>
    public class BaseAquariumConfigurator : AquariumConfigurator
    {
        private const string FishTrackContainerName = "FishTracks";
        private const string StorageObjectName = "AquariumStorage";
        private const string StorageRootName = "StorageRoot";
        private const int AquariumCapacity = 20;

        [SerializeField] private GameObject interactionObject;

        /// <summary>
        /// Loads a configuration prefab and applies it to a completed base piece.
        /// </summary>
        internal static void ConfigureFromAssetBundle(GameObject basePieceGameObject,
            string configurationPrefabAssetName)
        {
            if (basePieceGameObject.GetComponent<Aquarium>())
            {
                return;
            }

            GameObject configurationInstance =
                ModAssetUtils.GetPrefabInstanceFromAssetBundle(
                    configurationPrefabAssetName, false);
            if (!configurationInstance)
            {
                ModDebugLog.LogError(
                    $"Could not load base aquarium configuration prefab " +
                    $"'{configurationPrefabAssetName}'.");
                return;
            }

            BaseAquariumConfigurator configurator =
                configurationInstance.GetComponent<BaseAquariumConfigurator>();
            if (!configurator)
            {
                ModDebugLog.LogError(
                    $"Configuration prefab '{configurationPrefabAssetName}' has no " +
                    $"BaseAquariumConfigurator component.");
                Destroy(configurationInstance);
                return;
            }

            if (!configurator.ConfigureAquariumPrefab(basePieceGameObject,
                    out GameObject runtimeModel,
                    out Dictionary<GameObject, GameObject> instantiatedObjects))
            {
                Destroy(configurationInstance);
                return;
            }

            UWE.CoroutineHost.StartCoroutine(
                configurator.ConfigureDecorationsAsync(basePieceGameObject,
                    runtimeModel, instantiatedObjects, configurationInstance));
        }

        /// <summary>
        /// Adds native Aquarium and storage components using this prefab's authoring markers.
        /// </summary>
        internal bool ConfigureAquariumPrefab(GameObject basePieceGameObject,
            out GameObject runtimeModel,
            out Dictionary<GameObject, GameObject> instantiatedObjects)
        {
            runtimeModel = null;
            instantiatedObjects = null;
            if (!basePieceGameObject)
            {
                ModDebugLog.LogError(
                    "Cannot configure the Observatory Aquarium because its prefab is missing.");
                return false;
            }

            Transform interactionMarker = interactionObject
                ? interactionObject.transform
                : null;
            Collider interactionBounds = FindEnabledSupportedCollider(interactionMarker);
            Collider movementCollider = FindFirstMovementCollider();
            if (!interactionMarker || !interactionBounds ||
                !movementCollider || !newAquariumModel)
            {
                ModDebugLog.LogError(
                    "The base aquarium configuration prefab is missing its interaction " +
                    "collider, movement collider, or model.");
                return false;
            }

            if (!ValidateCustomMovementConfiguration())
            {
                return false;
            }

            if (!ValidateTrackConfiguration(
                    AquariumCapacity, AquariumCapacity,
                    "Observatory Aquarium"))
            {
                return false;
            }

            StorageContainer storageContainer = ConfigureStorage(
                basePieceGameObject, interactionMarker, interactionBounds);
            runtimeModel = ConfigureModel(
                basePieceGameObject, newAquariumModel.transform);
            instantiatedObjects =
                new Dictionary<GameObject, GameObject>();
            instantiatedObjects.Add(newAquariumModel, runtimeModel);
            Aquarium aquarium = basePieceGameObject.AddComponent<Aquarium>();
            aquarium.storageContainer = storageContainer;
            aquarium.fishRoot = ConfigureFishTracks(
                basePieceGameObject, out GameObject[] trackObjects);
            aquarium.trackObjects = trackObjects;
            aquarium.Subscribe(true);

            ConfigureCustomMovement(basePieceGameObject);

            BaseAquariumHandTarget handTarget =
                basePieceGameObject.AddComponent<BaseAquariumHandTarget>();
            handTarget.Initialize(storageContainer);
            BaseAquariumInventory inventory =
                basePieceGameObject.AddComponent<BaseAquariumInventory>();
            inventory.Initialize(storageContainer);
            AddAquariumComponent(basePieceGameObject);
            ModDebugLog.LogDebug(
                $"Added Aquarium functionality with {trackObjects.Length} fish tracks " +
                "and a root interaction target.");
            return true;
        }

        /// <summary>
        /// Loads vanilla Aquarium decorations and applies them to the completed base piece.
        /// </summary>
        private IEnumerator ConfigureDecorationsAsync(GameObject basePieceGameObject,
            GameObject runtimeModel,
            Dictionary<GameObject, GameObject> instantiatedObjects,
            GameObject configurationInstance)
        {
            CoroutineTask<GameObject> prefabTask =
                CraftData.GetPrefabForTechTypeAsync(TechType.Aquarium);
            yield return prefabTask;

            GameObject vanillaAquariumPrefab = prefabTask.GetResult();
            if (!vanillaAquariumPrefab)
            {
                ModDebugLog.LogError(
                    "Could not load the vanilla Aquarium prefab for base aquarium decorations.");
            }
            else if (basePieceGameObject && runtimeModel)
            {
                ConfigureDecorationsFromVanillaSource(basePieceGameObject,
                    vanillaAquariumPrefab, instantiatedObjects);
                yield return null;
                ModDebugLog.LogDebug(
                    "Configured Observatory Aquarium coral, rocks, and bubbles from vanilla sources.");
            }

            if (basePieceGameObject && runtimeModel)
            {
                ConfigureSkyAppliers(basePieceGameObject, instantiatedObjects);
            }

            Destroy(configurationInstance);
        }

        /// <summary>
        /// Replaces the generated Observatory renderers with the authored aquarium model.
        /// </summary>
        private static GameObject ConfigureModel(GameObject basePieceGameObject,
            Transform newModelMarker)
        {
            Renderer[] existingRenderers =
                basePieceGameObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer existingRenderer in existingRenderers)
            {
                existingRenderer.enabled = false;
            }

            GameObject newModel = Instantiate(newModelMarker.gameObject,
                basePieceGameObject.transform, false);
            newModel.name = newModelMarker.name;
            CopyLocalTransform(newModelMarker, newModel.transform);
            return newModel;
        }

        /// <summary>
        /// Creates the interactive storage used by the Aquarium component.
        /// </summary>
        private StorageContainer ConfigureStorage(GameObject basePieceGameObject,
            Transform interactionMarker, Collider interactionBounds)
        {
            GameObject storageGameObject = new GameObject(StorageObjectName);
            storageGameObject.SetActive(false);
            int useableLayer = LayerMask.NameToLayer("Useable");
            if (useableLayer >= 0)
            {
                storageGameObject.layer = useableLayer;
            }
            storageGameObject.transform.SetParent(basePieceGameObject.transform, false);
            CopyLocalTransform(interactionMarker, storageGameObject.transform);

            CopySupportedCollider(interactionBounds, storageGameObject);

            GameObject storageRootGameObject = new GameObject(StorageRootName);
            storageRootGameObject.transform.SetParent(storageGameObject.transform, false);
            storageRootGameObject.SetActive(false);
            ChildObjectIdentifier storageRoot =
                storageRootGameObject.AddComponent<ChildObjectIdentifier>();

            StorageContainer storageContainer =
                storageGameObject.AddComponent<StorageContainer>();
            storageContainer.prefabRoot = basePieceGameObject;
            storageContainer.width = storageWidth;
            storageContainer.height = storageHeight;
            storageContainer.hoverText = "Aquarium";
            storageContainer.storageLabel = "Aquarium";
            storageContainer.storageRoot = storageRoot;
            storageContainer.preventDeconstructionIfNotEmpty = true;
            storageGameObject.SetActive(true);
            return storageContainer;
        }

        /// <summary>
        /// Creates the fish roots consumed by the native Aquarium component.
        /// </summary>
        private GameObject ConfigureFishTracks(GameObject basePieceGameObject,
            out GameObject[] trackObjects)
        {
            GameObject fishRoot = new GameObject(FishTrackContainerName);
            fishRoot.transform.SetParent(basePieceGameObject.transform, false);

            trackObjects = new GameObject[AquariumCapacity];
            int trackIndex = 0;
            AddConfiguredTracks(fishRoot.transform, existingTrackObjects,
                existingAttachObjects, trackObjects, ref trackIndex);
            AddConfiguredTracks(fishRoot.transform, newTrackObjects,
                newAttachObjects, trackObjects, ref trackIndex);

            return fishRoot;
        }

        private static void AddConfiguredTracks(Transform fishRoot,
            GameObject[] configuredTracks, GameObject[] configuredAttachments,
            GameObject[] runtimeAttachments, ref int trackIndex)
        {
            int configuredIndex = 0;
            foreach (GameObject configuredTrack in configuredTracks)
            {
                GameObject configuredAttachment =
                    configuredAttachments[configuredIndex];
                GameObject runtimeTrack = new GameObject(configuredTrack.name);
                runtimeTrack.transform.SetParent(fishRoot, false);
                CopyLocalTransform(configuredTrack.transform, runtimeTrack.transform);

                GameObject runtimeAttachment =
                    new GameObject(configuredAttachment.name);
                runtimeAttachment.transform.SetParent(runtimeTrack.transform, false);
                CopyLocalTransform(
                    configuredAttachment.transform, runtimeAttachment.transform);
                runtimeAttachments[trackIndex] = runtimeAttachment;
                trackIndex++;
                configuredIndex++;
            }
        }

        /// <summary>
        /// Finds the enabled box or sphere collider authored on a configuration marker.
        /// </summary>
        private static Collider FindEnabledSupportedCollider(Transform marker)
        {
            if (!marker)
            {
                return null;
            }

            Collider[] colliders = marker.GetComponents<Collider>();
            foreach (Collider collider in colliders)
            {
                if (collider.enabled &&
                    (collider is BoxCollider || collider is SphereCollider))
                {
                    return collider;
                }
            }

            return null;
        }

        /// <summary>
        /// Copies a supported authored collider to a generated GameObject.
        /// </summary>
        private static Collider CopySupportedCollider(Collider source,
            GameObject destination)
        {
            BoxCollider sourceBox = source as BoxCollider;
            if (sourceBox)
            {
                BoxCollider destinationBox = destination.AddComponent<BoxCollider>();
                destinationBox.center = sourceBox.center;
                destinationBox.size = sourceBox.size;
                destinationBox.isTrigger = sourceBox.isTrigger;
                return destinationBox;
            }

            SphereCollider sourceSphere = source as SphereCollider;
            SphereCollider destinationSphere = destination.AddComponent<SphereCollider>();
            destinationSphere.center = sourceSphere.center;
            destinationSphere.radius = sourceSphere.radius;
            destinationSphere.isTrigger = sourceSphere.isTrigger;
            return destinationSphere;
        }

    }
}
