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
        private const string FloodVisualObjectName = "Flood_BaseObservatory";
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
                    out Dictionary<GameObject, GameObject> instantiatedObjects,
                    out Renderer[] originalObservatoryRenderers))
            {
                Destroy(configurationInstance);
                return;
            }

            UWE.CoroutineHost.StartCoroutine(
                configurator.ConfigureDecorationsAsync(basePieceGameObject,
                    runtimeModel, instantiatedObjects,
                    originalObservatoryRenderers,
                    configurationInstance));
        }

        /// <summary>
        /// Adds native Aquarium and storage components using this prefab's authoring markers.
        /// </summary>
        internal bool ConfigureAquariumPrefab(GameObject basePieceGameObject,
            out GameObject runtimeModel,
            out Dictionary<GameObject, GameObject> instantiatedObjects,
            out Renderer[] originalObservatoryRenderers)
        {
            runtimeModel = null;
            instantiatedObjects = null;
            originalObservatoryRenderers = null;
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
            originalObservatoryRenderers =
                basePieceGameObject.GetComponentsInChildren<Renderer>(true);
            runtimeModel = ConfigureModel(
                basePieceGameObject, newAquariumModel.transform,
                originalObservatoryRenderers);

            // Only add the Flood Water components if enabled in Mod Config
            if (ConfigFile.UseObservatoryFloodWater)
            {
                ConfigurePermanentWaterVisual(basePieceGameObject);
            }
            
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
            Renderer[] originalRenderers,
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
                ConfigureBaseSkyAppliers(basePieceGameObject, instantiatedObjects,
                    originalRenderers);
            }

            Destroy(configurationInstance);
        }

        /// <summary>
        /// Replaces the generated Observatory renderers with the authored aquarium model.
        /// </summary>
        private static GameObject ConfigureModel(GameObject basePieceGameObject,
            Transform newModelMarker, Renderer[] existingRenderers)
        {
            Transform floodVisualTransform =
                basePieceGameObject.transform.Find(FloodVisualObjectName);

            GameObject newModel = Instantiate(newModelMarker.gameObject,
                basePieceGameObject.transform, false);
            newModel.name = newModelMarker.name;
            CopyLocalTransform(newModelMarker, newModel.transform);
            ApplyNativeObservatoryMaterials(existingRenderers, newModel);
            RemoveDeferredMaterialApplicators(newModel);

            foreach (Renderer existingRenderer in existingRenderers)
            {
                if (floodVisualTransform &&
                    existingRenderer.transform.IsChildOf(floodVisualTransform))
                {
                    continue;
                }

                existingRenderer.enabled = false;
            }

            return newModel;
        }

        private static void ApplyNativeObservatoryMaterials(
            Renderer[] observatoryRenderers, GameObject newModel)
        {
            Dictionary<string, Material> nativeMaterials =
                new Dictionary<string, Material>();
            foreach (Renderer observatoryRenderer in observatoryRenderers)
            {
                Material[] materials = observatoryRenderer.sharedMaterials;
                foreach (Material material in materials)
                {
                    if (!material)
                    {
                        continue;
                    }

                    string materialName = NormalizeMaterialName(material.name);
                    if (!nativeMaterials.ContainsKey(materialName))
                    {
                        nativeMaterials.Add(materialName, material);
                    }
                }
            }

            int replacedMaterialCount = 0;
            Renderer[] newModelRenderers =
                newModel.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer newModelRenderer in newModelRenderers)
            {
                Material[] materials = newModelRenderer.sharedMaterials;
                bool materialsChanged = false;
                for (int materialIndex = 0;
                     materialIndex < materials.Length;
                     materialIndex++)
                {
                    Material currentMaterial = materials[materialIndex];
                    if (!currentMaterial)
                    {
                        continue;
                    }

                    string materialName =
                        NormalizeMaterialName(currentMaterial.name);
                    if (!nativeMaterials.TryGetValue(
                            materialName, out Material nativeMaterial))
                    {
                        continue;
                    }

                    materials[materialIndex] = nativeMaterial;
                    materialsChanged = true;
                    replacedMaterialCount++;
                }

                if (materialsChanged)
                {
                    newModelRenderer.sharedMaterials = materials;
                }
            }

            ModDebugLog.LogInfo(
                $"Applied {replacedMaterialCount} native Observatory material " +
                $"bindings to the custom model.");
        }

        private static void RemoveDeferredMaterialApplicators(GameObject newModel)
        {
            ApplyAquariumMaterial[] materialApplicators =
                newModel.GetComponentsInChildren<ApplyAquariumMaterial>(true);
            foreach (ApplyAquariumMaterial materialApplicator in materialApplicators)
            {
                materialApplicator.enabled = false;
                Destroy(materialApplicator);
            }

            ModDebugLog.LogDebug(
                $"Removed {materialApplicators.Length} deferred material applicators " +
                $"from the custom Observatory model.");
        }

        private static string NormalizeMaterialName(string materialName)
        {
            const string InstanceSuffix = " (Instance)";
            if (materialName.EndsWith(InstanceSuffix))
            {
                materialName = materialName.Substring(
                    0, materialName.Length - InstanceSuffix.Length);
            }

            int suffixSeparatorIndex = materialName.LastIndexOf('.');
            int suffixLength = materialName.Length - suffixSeparatorIndex - 1;
            if (suffixSeparatorIndex >= 0 && suffixLength == 3 &&
                char.IsDigit(materialName[suffixSeparatorIndex + 1]) &&
                char.IsDigit(materialName[suffixSeparatorIndex + 2]) &&
                char.IsDigit(materialName[suffixSeparatorIndex + 3]))
            {
                materialName = materialName.Substring(0, suffixSeparatorIndex);
            }

            return materialName;
        }

        private static void ConfigurePermanentWaterVisual(
            GameObject basePieceGameObject)
        {
            Transform floodVisualTransform =
                basePieceGameObject.transform.Find(FloodVisualObjectName);
            if (!floodVisualTransform)
            {
                ModDebugLog.LogError(
                    $"Could not find '{FloodVisualObjectName}' on the Observatory Aquarium.");
                return;
            }

            PermanentBaseWaterVisual waterVisual =
                basePieceGameObject.AddComponent<PermanentBaseWaterVisual>();
            waterVisual.Initialize(floodVisualTransform);
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
            StorageObstacle storageObstacle =
                storageGameObject.AddComponent<StorageObstacle>();
            storageObstacle.storageContainer = storageContainer;
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
