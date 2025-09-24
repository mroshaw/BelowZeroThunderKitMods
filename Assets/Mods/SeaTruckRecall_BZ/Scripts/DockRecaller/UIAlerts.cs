using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    public class UIAlerts : MonoBehaviour
    {
        [Header("References")] [Tooltip("The prefab for each alert, must contain a TextMeshProUGUI.")]
        [SerializeField] private GameObject alertPrefab;

        [Tooltip("The parent transform that will hold all alerts (e.g. a Vertical Layout Group).")]
        [SerializeField] private Transform alertContainer;

        [Header("Settings")] [Tooltip("How long each alert stays visible before fading starts (in seconds).")]
        [SerializeField] private float displayDuration = 3f;

        [Tooltip("How long it takes for the alert to fade out (in seconds).")]
        [SerializeField] private float fadeDuration = 1f;
        
        private readonly List<AlertInstance> _activeAlerts = new List<AlertInstance>();

        private ScrollRect _scrollRect;

        private void Awake()
        {
            _scrollRect = alertContainer.GetComponentInParent<ScrollRect>();
        }
        
        // Public method to add a new alert
        internal void AddAlert(string newAlert)
        {
            if (alertPrefab == null || alertContainer == null)
            {
                Debug.LogWarning("UIAlerts: Missing prefab or container.");
                return;
            }

            // Create new alert object
            GameObject alertGO = Instantiate(alertPrefab, alertContainer);
            alertGO.SetActive(true);
            TextMeshProUGUI alertText = alertGO.GetComponentInChildren<TextMeshProUGUI>();

            if (alertText == null)
            {
                Debug.LogWarning("UIAlerts: Alert prefab missing TextMeshProUGUI.");
                Destroy(alertGO);
                return;
            }

            alertText.text = newAlert;

            AlertInstance instance = new AlertInstance
            {
                AlertGameObjectInstance = alertGO,
                AlertText = alertText
            };

            _activeAlerts.Add(instance);
            StartCoroutine(ScrollToBottomNextFrame());
            StartCoroutine(HandleAlertLifetime(instance));
        }

        private IEnumerator HandleAlertLifetime(AlertInstance alert)
        {
            // Full visible phase
            yield return new WaitForSeconds(displayDuration);

            // Fade phase
            float elapsed = 0f;
            Color startColor = alert.AlertText.color;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);

                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                alert.AlertText.color = c;

                yield return null;
            }

            // Cleanup
            _activeAlerts.Remove(alert);
            Destroy(alert.AlertGameObjectInstance);
        }

        /// <summary>
        /// Forces the ScrollView to show the latest entries
        /// </summary>
        /// <returns></returns>
        private IEnumerator ScrollToBottomNextFrame()
        {
            yield return null; // wait one frame
            Canvas.ForceUpdateCanvases();
            
            _scrollRect.verticalNormalizedPosition = 0f;
        }
        
        private class AlertInstance
        {
            internal GameObject AlertGameObjectInstance;
            internal TextMeshProUGUI AlertText;
        }
    }
}