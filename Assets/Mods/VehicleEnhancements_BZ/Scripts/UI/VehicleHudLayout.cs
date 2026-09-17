using UnityEngine;

namespace DaftAppleGames.VehicleEnhancements_BZ.UI
{
    internal class VehicleHudLayout : MonoBehaviour
    {
        private RectTransform hudContent;
        private RectTransform indicators;

        internal void Configure(RectTransform content)
        {
            hudContent = content;
            indicators = transform as RectTransform;
            AlignToContent();
        }

        private void OnEnable()
        {
            AlignToContent();
        }

        private void LateUpdate()
        {
            AlignToContent();
        }

        private void AlignToContent()
        {
            if (!hudContent || !indicators)
            {
                return;
            }

            Vector2 contentSize = hudContent.rect.size;
            Vector3 contentCenter = hudContent.TransformPoint(hudContent.rect.center);
            if (indicators.sizeDelta == contentSize && indicators.position == contentCenter)
            {
                return;
            }

            indicators.anchorMin = new Vector2(0.5f, 0.5f);
            indicators.anchorMax = new Vector2(0.5f, 0.5f);
            indicators.pivot = new Vector2(0.5f, 0.5f);
            indicators.sizeDelta = contentSize;
            indicators.position = contentCenter;
        }
    }
}
