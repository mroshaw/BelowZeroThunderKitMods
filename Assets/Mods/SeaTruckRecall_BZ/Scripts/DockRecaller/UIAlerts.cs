using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    public class UIAlerts : MonoBehaviour
    {
        [Header("References")] [Tooltip("The prefab for each alert, must contain a TextMeshProUGUI.")]
        [SerializeField] private GameObject alertPrefab;

        [Tooltip("The parent transform that will hold all alerts (e.g. a Vertical Layout Group).")]
        [SerializeField] private Transform alertContainer;

        [Header("Settings")] [Tooltip("How long each alert stays visible before fading starts (in seconds).")]
        [SerializeField] private float displayDuration = 10f;

        [Tooltip("How long it takes for the alert to fade out (in seconds).")]
        [SerializeField] private float fadeDuration = 1f;
        
        private readonly List<AlertInstance> _activeAlerts = new List<AlertInstance>();

        private ScrollRect _scrollRect;
        private bool _canShowAlerts;
        
        private void Awake()
        {
            _scrollRect = alertContainer.GetComponentInParent<ScrollRect>();
        }

        private void OnEnable()
        {
            _canShowAlerts = true;
        }

        private void Update()
        {
            float elapsedTime = Time.unscaledDeltaTime;
            for (int alertIndex = _activeAlerts.Count - 1; alertIndex >= 0; alertIndex--)
            {
                AlertInstance alert = _activeAlerts[alertIndex];
                alert.Age += elapsedTime;

                if (alert.Age < displayDuration)
                {
                    continue;
                }

                float fadeProgress = fadeDuration > 0.0f
                    ? Mathf.Clamp01((alert.Age - displayDuration) / fadeDuration)
                    : 1.0f;
                alert.CanvasGroup.alpha = 1.0f - fadeProgress;

                if (fadeProgress < 1.0f)
                {
                    continue;
                }

                _activeAlerts.RemoveAt(alertIndex);
                Destroy(alert.AlertGameObjectInstance);
            }
        }
        
        /// <summary>
        /// Clean up the UI if disabled
        /// </summary>
        private void OnDisable()
        {
            _canShowAlerts = false;
            ModDebugLog.LogDebug("UIAlerts disabled - cleaning up.");
            CleanUp();
        }
        
        // Public method to add a new alert
        internal void AddAlert(string newAlert)
        {
            // Ignore new alerts if the UI has been disabled
            if (!_canShowAlerts)
            {
                return;
            }
            
            if (alertPrefab == null || alertContainer == null)
            {
                ModDebugLog.LogWarning("UIAlerts: Missing prefab or container.");
                return;
            }

            // Create new alert object
            GameObject alertGO = Instantiate(alertPrefab, alertContainer);
            alertGO.SetActive(true);
            TextMeshProUGUI alertText = alertGO.GetComponentInChildren<TextMeshProUGUI>();

            if (alertText == null)
            {
                ModDebugLog.LogWarning("UIAlerts: Alert prefab missing TextMeshProUGUI.");
                Destroy(alertGO);
                return;
            }

            alertText.text = newAlert;
            CanvasGroup canvasGroup = alertGO.GetComponent<CanvasGroup>();
            if (!canvasGroup)
            {
                canvasGroup = alertGO.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1.0f;

            AlertInstance instance = new AlertInstance
            {
                AlertGameObjectInstance = alertGO,
                CanvasGroup = canvasGroup,
                Age = 0.0f
            };

            _activeAlerts.Add(instance);
            StartCoroutine(ScrollToBottomNextFrame());
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

        /// <summary>
        /// Clears up all alerts and resets the component
        /// </summary>
        private void CleanUp()
        {
            StopAllCoroutines();
            ModDebugLog.LogDebug($"UIAlerts is cleaning up {_activeAlerts.Count} alerts...");
            foreach (AlertInstance alert in _activeAlerts)
            {
                ModDebugLog.LogDebug($"Destroying alert object...");
                Destroy(alert.AlertGameObjectInstance);
            }
            
            ModDebugLog.LogDebug($"Clear alert list...");
            _activeAlerts.Clear();
        }
        
        private class AlertInstance
        {
            internal GameObject AlertGameObjectInstance;
            internal CanvasGroup CanvasGroup;
            internal float Age;
        }
    }
}
