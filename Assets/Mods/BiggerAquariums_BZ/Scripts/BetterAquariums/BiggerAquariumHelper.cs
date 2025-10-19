using UnityEngine;
using static DaftAppleGames.BiggerAquariums.BiggerAquariumsPlugin;

namespace DaftAppleGames.BiggerAquariums
{
    public enum BiggerAquariumType
    {
        Double,
        Corner,
        Curved,
        LShaped
    }

    /// <summary>
    /// Component to allow switching out the new aquarium models on existing prefabs
    /// </summary>
    public class BiggerAquariumHelper : MonoBehaviour
    {
        [Header("Main Objects")] [SerializeField] private MeshFilter aquariumMesh;
        [SerializeField] private MeshFilter aquariumGlassMesh;
        [SerializeField] private Transform bubbles1Transform;
        [SerializeField] private Transform bubbles2Transform;
        [SerializeField] private Transform coral1Transform;
        [SerializeField] private Transform coral2Transform;
        [SerializeField] private Transform[] newCoralTransforms;
        [SerializeField] private GameObject rocksObject;
        [SerializeField] private GameObject colliderObject;

        [Header("Fish Attach Points")] [SerializeField] private Animator animator1;
        [SerializeField] private Animator animator2;
        [SerializeField] private GameObject[] existingTrackObjects;
        [SerializeField] private GameObject[] existingAttachObjects;
        [SerializeField] private GameObject[] newTrackObjects;
        [SerializeField] private GameObject[] newAttachObjects;

        /// <summary>
        /// Takes the "vanilla" aquarium prefab, and reconfigures it as the "Bigger" aquarium 
        /// </summary>
        internal void ConfigureAquariumPrefab(GameObject vanillaAquariumGo, BiggerAquariumType aquariumType)
        {
            ModDebugLog.LogDebug($"Configuring aquarium prefab: {vanillaAquariumGo}");

            // Get vanilla references
            Aquarium vanillaAquarium = vanillaAquariumGo.GetComponent<Aquarium>();
            Constructable vanillaConstructable = vanillaAquarium.GetComponent<Constructable>();
            ModDebugLog.LogDebug("Finding model...");
            GameObject aquariumModel = vanillaConstructable.model;

            // Replace the model meshes
            ConfigureMeshes(aquariumModel);
            
            // Duplicate and reposition coral
            ConfigureCoral(aquariumModel);

            // Configure rocks
            ConfigureRocks(vanillaAquariumGo);
            
            // Duplicate and reposition bubbles
            ConfigureBubbles(vanillaAquariumGo);
            
            // Replace the collider
            ConfigureCollider(vanillaAquariumGo);
            
            // Reposition tracks and add new
            ConfigureTracks(vanillaAquariumGo);

            // Add the new component
            AddAquariumComponent(vanillaAquariumGo, aquariumType);
            
            ModDebugLog.LogDebug("Done configuring prefab!");
        }

        /// <summary>
        /// Replace the meshes with our custom ones
        /// </summary>
        private void ConfigureMeshes(GameObject aquariumModel)
        {
            ModDebugLog.LogDebug("Replacing meshes...");
            MeshFilter[] meshFilters = aquariumModel.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                ModDebugLog.LogDebug($"Checking mesh on: {meshFilter.gameObject.name}");
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
                    continue;
                }
            }
        }

        /// <summary>
        /// Copies then repositions the second coral object to make things a bit more natural
        /// </summary>
        private void ConfigureCoral(GameObject aquariumModelGo)
        {
            // Duplicate coral
            ModDebugLog.LogDebug("Duplicating coral...");
            GameObject coral = aquariumModelGo.transform.Find("Coral").gameObject;
            if (!coral)
            {
                ModDebugLog.LogError("Could not find aquarium Coral gameobject! Aborting!");
                return;
            }

            GameObject newCoral = Instantiate(coral, coral.transform.parent, true);

            // Existing coral moves, no rotation changes
            coral.transform.localPosition = coral1Transform.localPosition;
            coral.transform.localRotation = coral1Transform.localRotation;

            // New coral moved to new location, rotation and scale matches existing coral model
            newCoral.transform.localPosition = coral2Transform.localPosition;
            newCoral.transform.localRotation = coral2Transform.localRotation;
            newCoral.transform.localScale = coral.transform.localScale;

            // Iterate through each coral game object, find it and reposition it
            foreach (Transform coralTransform in newCoralTransforms)
            {
                ModDebugLog.LogDebug($"Setting new position of: {coralTransform.gameObject.name}");
                GameObject origCoral = newCoral.transform.Find(coralTransform.gameObject.name).gameObject;
                if (!origCoral)
                {
                    ModDebugLog.LogError(
                        $"Could not find coral gameobject named: {coralTransform.gameObject.name}! Aborting!");
                    return;
                }

                // Reposition to the new position
                origCoral.transform.localPosition = coralTransform.localPosition;
                origCoral.SetActive(coralTransform.gameObject.activeSelf);
            }
        }
        
        /// <summary>
        /// Copy and reposition the rocks from our new model
        /// </summary>
        private void ConfigureRocks(GameObject vanillaAquariumGo)
        {
            ModDebugLog.LogDebug("Configuring rocks...");
            // Add the rocks
            GameObject newRocks = Instantiate(rocksObject, vanillaAquariumGo.transform, true);
            newRocks.transform.localPosition = rocksObject.transform.localPosition;
            newRocks.transform.localScale = Vector3.one;
            
            ModDebugLog.LogDebug("Updating SkyAppliers...");
            SkyApplier skyApplier = vanillaAquariumGo.GetComponentInChildren<SkyApplier>();
            skyApplier.renderers = vanillaAquariumGo.GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>
        /// Copy and reposition and second set of bubbles
        /// </summary>
        private void ConfigureBubbles(GameObject vanillaAquariumGo)
        {
            // Duplicate the bubbles
            ModDebugLog.LogDebug("Duplicating bubbles...");
            GameObject bubbles = vanillaAquariumGo.transform.Find("Bubbles").gameObject;
            if (!bubbles)
            {
                ModDebugLog.LogError("Could not find aquarium Bubbles gameobject! Aborting!");
                return;
            }

            GameObject newBubbles = Instantiate(bubbles, bubbles.transform.parent, true);
            bubbles.transform.localPosition = bubbles1Transform.localPosition;
            bubbles.transform.localRotation = bubbles1Transform.localRotation;

            newBubbles.transform.localPosition = bubbles2Transform.localPosition;
            newBubbles.transform.localRotation = bubbles2Transform.localRotation;
            newBubbles.transform.localScale = bubbles1Transform.localScale;
        }
        
        /// <summary>
        /// Replace the box collider with colliders for our new models
        /// </summary>
        private void ConfigureCollider(GameObject vanillaAquariumGo)
        {
            ModDebugLog.LogDebug("Replacing collider...");
            GameObject oldCollider = vanillaAquariumGo.transform.Find("Collider").gameObject;
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
            GameObject[] updatedTrackObjects = new GameObject[16];

            // Reconfigure the existing 8 tracks
            Aquarium vanillaAquarium = vanillaAquariumGo.GetComponent<Aquarium>();

            ModDebugLog.LogDebug($"Finding animators...");
            GameObject animatorGo1 = vanillaAquariumGo.transform.Find("model/Aquarium_animation2").gameObject;
            GameObject animatorGo2 = vanillaAquariumGo.transform.Find("model/Aquarium_animation").gameObject;
            ModDebugLog.LogDebug($"Found animators");

            ModDebugLog.LogDebug($"Finding track roots...");
            GameObject trackRoot1To4 = animatorGo1.transform.Find("root").gameObject;
            GameObject trackRoot5To8 = animatorGo2.transform.Find("root").gameObject;
            ModDebugLog.LogDebug($"Found track roots");

            ModDebugLog.LogDebug($"Finding geometry...");
            GameObject geometry1 = animatorGo1.transform.Find("Aquarium_geo").gameObject;
            GameObject geometry2 = animatorGo2.transform.Find("Aquarium_geo").gameObject;
            ModDebugLog.LogDebug($"Found geometry");

            // Update the animators
            // Move the animator gameobject, unparent/reparent children to avoid move
            // trackRoot1To4.transform.SetParent(null);
            geometry1.transform.SetParent(null);
            // trackRoot5To8.transform.SetParent(null);
            geometry2.transform.SetParent(null);

            // Position Animators
            animatorGo1.transform.localPosition = animator1.transform.localPosition;
            animatorGo1.transform.localRotation = animator1.transform.localRotation;

            animatorGo2.transform.localPosition = animator2.transform.localPosition;
            animatorGo2.transform.localRotation = animator2.transform.localRotation;

            geometry1.transform.SetParent(animatorGo1.transform, true);
            // trackRoot5To8.transform.SetParent(animatorGo2.transform);
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
                existingTrackGo.transform.localRotation = existingTrack.transform.localRotation;

                ModDebugLog.LogDebug($"Looking for attach in track: {existingAttach.name}");
                GameObject existingAttachGo = existingTrackGo.transform.Find(existingAttach.name).gameObject;
                existingAttachGo.transform.localPosition = existingAttach.transform.localPosition;
                existingAttachGo.transform.localRotation = existingAttach.transform.localRotation;

                updatedTrackObjects[currTrackIndex] = existingAttachGo;
                currTrackIndex++;
            }

            // Create new Fish Tracks
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

                updatedTrackObjects[currTrackIndex + 8] = newAttachGo;
                currTrackIndex++;
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

            ModDebugLog.LogDebug($"Done configuring new aquarium!");
        }
        
        /// <summary>
        /// Add the correct component
        /// </summary>
        private void AddAquariumComponent(GameObject vanillaAquariumGo, BiggerAquariumType aquariumType)
        {
            switch (aquariumType)
            {
                case BiggerAquariumType.Double:
                    vanillaAquariumGo.AddComponent<DoubleAquarium>();
                    break;
                case BiggerAquariumType.Curved:
                    vanillaAquariumGo.AddComponent<CurvedAquarium>();
                    break;
                case BiggerAquariumType.LShaped:
                    vanillaAquariumGo.AddComponent<LShapedAquarium>();
                    break;
                case BiggerAquariumType.Corner:
                    vanillaAquariumGo.AddComponent<CornerAquarium>();
                    break;
            }
        }
    }
}