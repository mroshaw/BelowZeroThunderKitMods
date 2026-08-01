using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    ///     Draws the Moonpool Pet trigger bounds in-game for testing.
    /// </summary>
    internal class MoonpoolTriggerVisualizer : MonoBehaviour
    {
        private const float LineWidth = 0.035f;
        private Material lineMaterial;

        internal void Init(BoxCollider triggerCollider)
        {
            LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = false;
            lineRenderer.startWidth = LineWidth;
            lineRenderer.endWidth = LineWidth;
            lineRenderer.positionCount = 16;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader)
            {
                lineMaterial = new Material(shader);
                lineRenderer.material = lineMaterial;
            }
            lineRenderer.startColor = Color.magenta;
            lineRenderer.endColor = Color.magenta;

            Vector3 center = triggerCollider.center;
            Vector3 extents = triggerCollider.size * 0.5f;
            Vector3 bottom0 = center + new Vector3(-extents.x, -extents.y, -extents.z);
            Vector3 bottom1 = center + new Vector3(extents.x, -extents.y, -extents.z);
            Vector3 bottom2 = center + new Vector3(extents.x, -extents.y, extents.z);
            Vector3 bottom3 = center + new Vector3(-extents.x, -extents.y, extents.z);
            Vector3 top0 = center + new Vector3(-extents.x, extents.y, -extents.z);
            Vector3 top1 = center + new Vector3(extents.x, extents.y, -extents.z);
            Vector3 top2 = center + new Vector3(extents.x, extents.y, extents.z);
            Vector3 top3 = center + new Vector3(-extents.x, extents.y, extents.z);

            lineRenderer.SetPositions(new[]
            {
                bottom0, bottom1, bottom2, bottom3, bottom0,
                top0, top1, top2, top3, top0,
                top1, bottom1, bottom2, top2, top3, bottom3
            });
        }

        private void OnDestroy()
        {
            if (lineMaterial) Destroy(lineMaterial);
        }
    }
}
