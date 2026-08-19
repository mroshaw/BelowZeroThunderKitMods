using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Applies game-native materials that are not provided by Nautilus's material applicator.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ApplyAquariumMaterial : MonoBehaviour
    {
        private const string ExteriorGlassWaterFixMaterialName =
            "GlassExteriorWaterFix";
        private const string ExteriorGlassWaterFixShaderName =
            "UWE/GlassExteriorWaterFix";
        private const string VanillaAquariumGlassMaterialName =
            "Aquarium_glass";
        private const string ObservatoryExteriorGlassMaterialName =
            "Base_exterior_Room_Observatory_glass";
        private const string ObservatoryInteriorGlassMaterialName =
            "Base_interior_Room_Observatory_glass";

        /// <summary>
        /// Defines where the configured material is applied.
        /// </summary>
        public enum MaterialSetMode
        {
            SingleRenderer,
            AllChildRenderers,
            AllChildRenderersIncludingInactive
        }

        /// <summary>
        /// Defines the game-native material to apply.
        /// </summary>
        public enum MaterialType
        {
            ExteriorGlassWaterFix,
            VanillaAquariumGlass,
            ObservatoryGlassExterior,
            ObservatoryGlassInterior
        }

        [BoxGroup("Material")]
        [SerializeField]
        private MaterialSetMode materialSetMode;

        [BoxGroup("Material")]
        [SerializeField]
        private MaterialType materialType;

        [BoxGroup("Material")]
        [SerializeField]
        private bool runAtStart = true;

        [BoxGroup("Single Renderer")]
        [ShowIf(nameof(IsSingleRendererMode))]
        [Required]
        [SerializeField]
        private Renderer targetRenderer;

        [BoxGroup("Single Renderer")]
        [ShowIf(nameof(IsSingleRendererMode))]
        [SerializeField]
        private int[] materialIndices = { 0 };

        private static Material exteriorGlassWaterFixMaterial;
        private static Material vanillaAquariumGlassMaterial;
        private static Material observatoryExteriorGlassMaterial;
        private static Material observatoryInteriorGlassMaterial;

        private bool IsSingleRendererMode =>
            materialSetMode == MaterialSetMode.SingleRenderer;

        private void OnValidate()
        {
            if (!targetRenderer)
            {
                TryGetComponent(out targetRenderer);
            }
        }

        private void Start()
        {
            if (runAtStart)
            {
                AssignMaterials();
            }
        }

        /// <summary>
        /// Applies the configured game-native material to the configured renderers.
        /// </summary>
        public void AssignMaterials()
        {
            UWE.CoroutineHost.StartCoroutine(AssignMaterialsAsync());
        }

        private IEnumerator AssignMaterialsAsync()
        {
            Material material = null;
            yield return GetMaterialAsync(materialType,
                loadedMaterial => material = loadedMaterial);
            if (!material)
            {
                yield break;
            }

            switch (materialSetMode)
            {
                case MaterialSetMode.SingleRenderer:
                    ApplyToSingleRenderer(material);
                    break;
                case MaterialSetMode.AllChildRenderers:
                    ApplyToChildRenderers(material, false);
                    break;
                case MaterialSetMode.AllChildRenderersIncludingInactive:
                    ApplyToChildRenderers(material, true);
                    break;
            }
        }

        private void ApplyToSingleRenderer(Material material)
        {
            if (!targetRenderer)
            {
                ModDebugLog.LogError(
                    $"ApplyAquariumMaterial on '{name}' has no target Renderer.");
                return;
            }

            Material[] rendererMaterials = targetRenderer.materials;
            foreach (int materialIndex in materialIndices)
            {
                if (materialIndex < 0 ||
                    materialIndex >= rendererMaterials.Length)
                {
                    ModDebugLog.LogError(
                        $"Material index {materialIndex} is invalid for Renderer " +
                        $"'{targetRenderer.name}'.");
                    continue;
                }

                rendererMaterials[materialIndex] = material;
            }

            targetRenderer.materials = rendererMaterials;
        }

        private void ApplyToChildRenderers(Material material,
            bool includeInactive)
        {
            Renderer[] renderers =
                GetComponentsInChildren<Renderer>(includeInactive);
            foreach (Renderer childRenderer in renderers)
            {
                Material[] rendererMaterials = childRenderer.materials;
                for (int materialIndex = 0;
                     materialIndex < rendererMaterials.Length;
                     materialIndex++)
                {
                    rendererMaterials[materialIndex] = material;
                }

                childRenderer.materials = rendererMaterials;
            }
        }

        private static IEnumerator GetMaterialAsync(
            MaterialType requestedMaterialType,
            System.Action<Material> result)
        {
            switch (requestedMaterialType)
            {
                case MaterialType.ExteriorGlassWaterFix:
                    result(GetExteriorGlassWaterFixMaterial());
                    yield break;
                case MaterialType.VanillaAquariumGlass:
                    yield return GetVanillaAquariumGlassMaterialAsync(result);
                    yield break;
                case MaterialType.ObservatoryGlassExterior:
                    result(GetLoadedMaterialByName(
                        ObservatoryExteriorGlassMaterialName,
                        ref observatoryExteriorGlassMaterial));
                    yield break;
                case MaterialType.ObservatoryGlassInterior:
                    result(GetLoadedMaterialByName(
                        ObservatoryInteriorGlassMaterialName,
                        ref observatoryInteriorGlassMaterial));
                    yield break;
                default:
                    result(null);
                    yield break;
            }
        }

        private static IEnumerator GetVanillaAquariumGlassMaterialAsync(
            System.Action<Material> result)
        {
            if (vanillaAquariumGlassMaterial)
            {
                result(vanillaAquariumGlassMaterial);
                yield break;
            }

            CoroutineTask<GameObject> prefabTask =
                CraftData.GetPrefabForTechTypeAsync(TechType.Aquarium);
            yield return prefabTask;

            GameObject aquariumPrefab = prefabTask.GetResult();
            if (!aquariumPrefab)
            {
                ModDebugLog.LogError(
                    "Could not load the vanilla Aquarium prefab for its glass material.");
                result(null);
                yield break;
            }

            Renderer[] renderers =
                aquariumPrefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer sourceRenderer in renderers)
            {
                Material[] sourceMaterials = sourceRenderer.sharedMaterials;
                foreach (Material sourceMaterial in sourceMaterials)
                {
                    if (!sourceMaterial ||
                        !IsNamedMaterial(sourceMaterial,
                            VanillaAquariumGlassMaterialName))
                    {
                        continue;
                    }

                    vanillaAquariumGlassMaterial = new Material(sourceMaterial)
                    {
                        name = VanillaAquariumGlassMaterialName
                    };
                    result(vanillaAquariumGlassMaterial);
                    yield break;
                }
            }

            ModDebugLog.LogError(
                $"Could not find material '{VanillaAquariumGlassMaterialName}' " +
                "on the vanilla Aquarium prefab.");
            result(null);
        }

        private static bool IsNamedMaterial(Material material,
            string expectedName)
        {
            return material.name == expectedName ||
                   material.name == expectedName + " (Instance)";
        }

        private static Material GetLoadedMaterialByName(string materialName,
            ref Material cachedMaterial)
        {
            if (cachedMaterial)
            {
                return cachedMaterial;
            }

            Material[] loadedMaterials =
                Resources.FindObjectsOfTypeAll<Material>();
            foreach (Material loadedMaterial in loadedMaterials)
            {
                if (!loadedMaterial ||
                    !IsNamedMaterial(loadedMaterial, materialName))
                {
                    continue;
                }

                cachedMaterial = new Material(loadedMaterial)
                {
                    name = materialName
                };
                return cachedMaterial;
            }

            ModDebugLog.LogError(
                $"Could not find loaded material '{materialName}'.");
            return null;
        }

        private static Material GetExteriorGlassWaterFixMaterial()
        {
            if (exteriorGlassWaterFixMaterial)
            {
                return exteriorGlassWaterFixMaterial;
            }

            Material[] loadedMaterials =
                Resources.FindObjectsOfTypeAll<Material>();
            foreach (Material loadedMaterial in loadedMaterials)
            {
                if (loadedMaterial && loadedMaterial.shader &&
                    loadedMaterial.shader.name ==
                    ExteriorGlassWaterFixShaderName)
                {
                    exteriorGlassWaterFixMaterial =
                        new Material(loadedMaterial)
                        {
                            name = ExteriorGlassWaterFixMaterialName
                        };
                    return exteriorGlassWaterFixMaterial;
                }
            }

            Shader shader = Shader.Find(ExteriorGlassWaterFixShaderName);
            if (!shader)
            {
                ModDebugLog.LogError(
                    $"Could not find shader '{ExteriorGlassWaterFixShaderName}'.");
                return null;
            }

            exteriorGlassWaterFixMaterial = new Material(shader)
            {
                name = ExteriorGlassWaterFixMaterialName
            };
            return exteriorGlassWaterFixMaterial;
        }
    }
}
