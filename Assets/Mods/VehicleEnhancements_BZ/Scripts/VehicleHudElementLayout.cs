using UnityEngine;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ
{
    // Run after VehicleHudLayout has aligned the shared indicators container.
    [DefaultExecutionOrder(100)]
    internal class VehicleHudElementLayout : MonoBehaviour
    {
        internal enum Element
        {
            Speedometer,
            Hsi,
            TimeAndWeather,
            ReversingCamera
        }

        private const float EdgePadding = 10.0f;
        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform elementRect;
        private RectTransform parentRect;
        private RectTransform canvasRect;
        private RectTransform[] contentRects;
        private Element element;
        private EnhancedVehicle vehicle;
        private Vector2 basePosition;
        private Vector3 baseScale;
        private bool hasLayout;
        private Vector2 lastAdjustment;
        private float lastScale;
        private Rect lastParentRect;
        private Rect lastCanvasRect;
        private Matrix4x4 lastParentToCanvas;

        internal void Configure(EnhancedVehicle selectedVehicle, Element selectedElement)
        {
            vehicle = selectedVehicle;
            element = selectedElement;
            elementRect = transform as RectTransform;
            if (!elementRect)
            {
                return;
            }

            basePosition = elementRect.anchoredPosition;
            baseScale = elementRect.localScale;
            parentRect = transform.parent as RectTransform;
            hasLayout = false;
            canvasRect = null;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas)
            {
                canvasRect = canvas.rootCanvas.transform as RectTransform;
            }

            // The dial roots have zero-sized rects. Their immediate children define the
            // visible footprint; deeper descendants include oversized, masked HSI artwork.
            contentRects = new RectTransform[transform.childCount + 1];
            contentRects[0] = elementRect;
            for (int index = 0; index < transform.childCount; index++)
            {
                contentRects[index + 1] = transform.GetChild(index) as RectTransform;
            }
        }

        private void LateUpdate()
        {
            ApplyLayout();
        }

        /// <summary>
        /// Applies current settings, including when a display is activated during LateUpdate.
        /// </summary>
        internal void ApplyLayout()
        {
            if (!elementRect || !parentRect || contentRects is null)
            {
                return;
            }

            GetAdjustment(out Vector2 adjustment, out float scale);

            adjustment.x = Mathf.Clamp(adjustment.x, ModConfigFile.MinimumHudAdjust, ModConfigFile.MaximumHudAdjust);
            adjustment.y = Mathf.Clamp(adjustment.y, ModConfigFile.MinimumHudAdjust, ModConfigFile.MaximumHudAdjust);
            scale = Mathf.Clamp(scale, ModConfigFile.MinimumHudScale, ModConfigFile.MaximumHudScale);
            Rect canvasBounds = canvasRect ? canvasRect.rect : new Rect();
            Matrix4x4 parentToCanvas = canvasRect
                ? canvasRect.worldToLocalMatrix * parentRect.localToWorldMatrix
                : parentRect.localToWorldMatrix;
            if (hasLayout && adjustment == lastAdjustment && scale == lastScale &&
                parentRect.rect == lastParentRect && canvasBounds == lastCanvasRect && parentToCanvas == lastParentToCanvas)
            {
                return;
            }

            hasLayout = true;
            lastAdjustment = adjustment;
            lastScale = scale;
            lastParentRect = parentRect.rect;
            lastCanvasRect = canvasBounds;
            lastParentToCanvas = parentToCanvas;
            elementRect.anchoredPosition = basePosition + adjustment;

            // Parent HUD scaling is inherited, so it is not multiplied again here.
            elementRect.localScale = baseScale * scale;
            KeepOnScreen();
        }

        private void GetAdjustment(out Vector2 adjustment, out float scale)
        {
            if (element == Element.ReversingCamera)
            {
                adjustment = new Vector2(ConfigFile.SeatruckCameraHorizontalAdjust, ConfigFile.SeatruckCameraVerticalAdjust);
                scale = ConfigFile.SeatruckCameraUiScale;
                return;
            }

            switch (vehicle)
            {
                case EnhancedVehicle.PrawnSuit:
                    switch (element)
                    {
                        case Element.Hsi:
                            adjustment = new Vector2(ConfigFile.PrawnHsiHorizontalAdjust, ConfigFile.PrawnHsiVerticalAdjust);
                            scale = ConfigFile.PrawnHsiUiScale;
                            return;
                        case Element.TimeAndWeather:
                            adjustment = new Vector2(ConfigFile.PrawnTimeHorizontalAdjust, ConfigFile.PrawnTimeVerticalAdjust);
                            scale = ConfigFile.PrawnTimeUiScale;
                            return;
                        default:
                            adjustment = new Vector2(ConfigFile.PrawnSpeedometerHorizontalAdjust, ConfigFile.PrawnSpeedometerVerticalAdjust);
                            scale = ConfigFile.PrawnSpeedometerUiScale;
                            return;
                    }
                case EnhancedVehicle.Snowfox:
                    switch (element)
                    {
                        case Element.Hsi:
                            adjustment = new Vector2(ConfigFile.SnowfoxHsiHorizontalAdjust, ConfigFile.SnowfoxHsiVerticalAdjust);
                            scale = ConfigFile.SnowfoxHsiUiScale;
                            return;
                        case Element.TimeAndWeather:
                            adjustment = new Vector2(ConfigFile.SnowfoxTimeHorizontalAdjust, ConfigFile.SnowfoxTimeVerticalAdjust);
                            scale = ConfigFile.SnowfoxTimeUiScale;
                            return;
                        default:
                            adjustment = new Vector2(ConfigFile.SnowfoxSpeedometerHorizontalAdjust, ConfigFile.SnowfoxSpeedometerVerticalAdjust);
                            scale = ConfigFile.SnowfoxSpeedometerUiScale;
                            return;
                    }
                default:
                    switch (element)
                    {
                        case Element.Hsi:
                            adjustment = new Vector2(ConfigFile.SeatruckHsiHorizontalAdjust, ConfigFile.SeatruckHsiVerticalAdjust);
                            scale = ConfigFile.SeatruckHsiUiScale;
                            return;
                        case Element.TimeAndWeather:
                            adjustment = new Vector2(ConfigFile.SeatruckTimeHorizontalAdjust, ConfigFile.SeatruckTimeVerticalAdjust);
                            scale = ConfigFile.SeatruckTimeUiScale;
                            return;
                        default:
                            adjustment = new Vector2(ConfigFile.SeatruckSpeedometerHorizontalAdjust, ConfigFile.SeatruckSpeedometerVerticalAdjust);
                            scale = ConfigFile.SeatruckSpeedometerUiScale;
                            return;
                    }
            }
        }

        private void KeepOnScreen()
        {
            if (!canvasRect)
            {
                return;
            }

            Rect available = canvasRect.rect;
            available.xMin += EdgePadding;
            available.xMax -= EdgePadding;
            available.yMin += EdgePadding;
            available.yMax -= EdgePadding;
            if (available.width <= 0.0f || available.height <= 0.0f)
            {
                return;
            }

            Bounds bounds = GetContentBounds();
            float fitScale = Mathf.Min(1.0f, Mathf.Min(
                available.width / Mathf.Max(bounds.size.x, 0.001f),
                available.height / Mathf.Max(bounds.size.y, 0.001f)));
            if (fitScale < 1.0f)
            {
                elementRect.localScale *= fitScale;
                bounds = GetContentBounds();
            }

            Vector3 shift = new Vector3(
                Mathf.Clamp(bounds.center.x, available.xMin + bounds.extents.x, available.xMax - bounds.extents.x) - bounds.center.x,
                Mathf.Clamp(bounds.center.y, available.yMin + bounds.extents.y, available.yMax - bounds.extents.y) - bounds.center.y,
                0.0f);
            elementRect.position += canvasRect.TransformVector(shift);
        }

        private Bounds GetContentBounds()
        {
            Bounds bounds = new Bounds(canvasRect.InverseTransformPoint(elementRect.position), Vector3.zero);
            foreach (RectTransform contentRect in contentRects)
            {
                if (!contentRect)
                {
                    continue;
                }

                contentRect.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    bounds.Encapsulate(canvasRect.InverseTransformPoint(corner));
                }
            }

            return bounds;
        }
    }
}
