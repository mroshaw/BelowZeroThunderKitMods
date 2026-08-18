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
            ExteriorGlassWaterFix
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
            Material material = GetMaterial(materialType);
            if (!material)
            {
                return;
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

        private static Material GetMaterial(MaterialType requestedMaterialType)
        {
            switch (requestedMaterialType)
            {
                case MaterialType.ExteriorGlassWaterFix:
                    return GetExteriorGlassWaterFixMaterial();
                default:
                    return null;
            }
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
