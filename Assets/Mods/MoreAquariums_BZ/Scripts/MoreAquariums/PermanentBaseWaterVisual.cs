using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Keeps an Observatory's native flood visuals full without changing base flood state.
    /// </summary>
    public class PermanentBaseWaterVisual : MonoBehaviour
    {
        private const string WaterPlaneObjectName = "x_BaseWaterPlane_RoomObs";
        private const float FullLeakAmount = 1.0f;

        private BaseWaterPlane waterPlane;
        private Renderer waterSurfaceRenderer;

        /// <summary>
        /// Connects this controller to the Observatory's existing flood hierarchy.
        /// </summary>
        internal void Initialize(Transform floodVisualTransform)
        {
            waterPlane = floodVisualTransform.GetComponent<BaseWaterPlane>();

            Transform waterSurfaceTransform =
                floodVisualTransform.Find(WaterPlaneObjectName);
            waterSurfaceRenderer = waterSurfaceTransform
                ? waterSurfaceTransform.GetComponent<Renderer>()
                : null;

            if (!waterPlane || !waterSurfaceRenderer)
            {
                ModDebugLog.LogError(
                    "Could not initialize the Observatory Aquarium's permanent water visuals.");
                enabled = false;
                return;
            }

            ApplyFullWaterVisual();
        }

        private void LateUpdate()
        {
            if (!waterPlane || !waterSurfaceRenderer)
            {
                return;
            }

            ApplyFullWaterVisual();
        }

        private void ApplyFullWaterVisual()
        {
            waterPlane.hostTrans = transform;
            waterPlane.waterlevel =
                transform.position.y + Base.cellSize.y * 0.5f;
            waterPlane.leakAmount = FullLeakAmount;
            waterSurfaceRenderer.enabled = true;
        }
    }
}
