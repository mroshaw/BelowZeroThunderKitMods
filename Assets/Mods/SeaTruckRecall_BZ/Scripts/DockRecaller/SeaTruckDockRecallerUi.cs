using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    /// <summary>
    /// MonoBehaviour class implementing the UI elements of the
    /// SeaTruck Recall component
    /// </summary>
    internal class SeaTruckDockRecallerUi : MonoBehaviour
    {
        // Autopilot state text
        private const string AutoPilotDisplayText = "AUTOPILOT: ";
        private const string AutoPilotPreviousDisplayText = "PREVIOUS: ";
        private readonly Dictionary<AutoPilotState, string> _autoPilotStateDisplayTextDict =
            new Dictionary<AutoPilotState, string>()
            {
                { AutoPilotState.Ready, "READY" },
                { AutoPilotState.Moving, "MOVING" },
                { AutoPilotState.Replanning, "REPLANNING" },
                { AutoPilotState.Arrived, "ARRIVED" },
                { AutoPilotState.Stuck, "ROUTE BLOCKED!" },
                { AutoPilotState.Parking, "PARKING" },
                { AutoPilotState.Docking, "DOCKING" },
                { AutoPilotState.Docked, "DOCKED" },
                { AutoPilotState.Aborted, "ABORTED!" },
            };

        // Recaller state text
        private readonly Dictionary<DockRecallState, string> _recallerStateDisplayTextDict =
            new Dictionary<DockRecallState, string>()
            {
                { DockRecallState.Initialising, "INITIALISING..." },
                { DockRecallState.Ready, "READY" },
                { DockRecallState.NoTrucksFound, "NOTHING IN RANGE!" },
                { DockRecallState.FindingPath, "FINDING PATH" },
                { DockRecallState.Recalling, "RECALLING" },
                { DockRecallState.Stuck, "ROUTE BLOCKED - ABORTED!" },
                { DockRecallState.Docking, "DOCKING" },
                { DockRecallState.Docked, "DOCKED" },
                { DockRecallState.Aborted, "ABORTED!" },
                { DockRecallState.PathingError, "PATHING ERROR - ABORTED!" },
            };

        // Recaller state log entries
        private readonly Dictionary<DockRecallState, string> _recallerLogDisplayTextDict =
            new Dictionary<DockRecallState, string>()
            {
                { DockRecallState.Initialising, "RECALLER IS INITIALISING. PLEASE WAIT..." },
                { DockRecallState.Ready, "RECALLER IS READY!" },
                { DockRecallState.NoTrucksFound, "NO SEATRUCKS WITHIN RANGE!" },
                { DockRecallState.FindingPath, "FINDING PATH FOR SELECTED SEATRUCK..." },
                { DockRecallState.Recalling, "RECALLING CLOSEST SEATRUCK!" },
                { DockRecallState.Stuck, "ERROR: STRUCK ROUTE IS BLOCKED - ABORTED!" },
                { DockRecallState.Docking, "SEATRUCK IS DOCKING..." },
                { DockRecallState.Docked, "SEATRUCK IS DOCKED!" },
                { DockRecallState.Aborted, "RECALL HAS BEEN ABORTED!" },
                { DockRecallState.PathingError, "ERROR: PROBLEM WITH PATHING - ABORTED!" },
            };

        [Header("Settings")]
        [SerializeField] private SeaTruckDockRecaller seaTruckRecaller;

        // UI properties
        [SerializeField] private GameObject nothingDockedScreen;
        [SerializeField] private GameObject activeScreenGo;
        [SerializeField] private GameObject inactiveScreenGo;

        // Text controls for state updates
        [SerializeField] private TMP_Text dockingStatusText;
        [SerializeField] private TMP_Text autoPilotStatusText;
        [SerializeField] private TMP_Text autoPilotPreviousStatusText;
        [SerializeField] private Button recallButton;
        [SerializeField] private Button abortRecallButton;

        // Used for notifications on the console UI
        private UIAlerts _uiAlerts;

        private void OnEnable()
        {
            // Recaller events
            seaTruckRecaller?.onAutoPilotChanged?.AddListener(AutoPilotChangedHandler);
            seaTruckRecaller?.onDockingStateChanged.AddListener(RecallerStateChangedHandler);

            // UI events
            recallButton?.onClick.AddListener(RecallButtonHandler);
            abortRecallButton?.onClick.AddListener(AbortButtonHandler);
        }

        private void OnDisable()
        {
            // Recaller events
            seaTruckRecaller?.onAutoPilotChanged?.RemoveListener(AutoPilotChangedHandler);
            seaTruckRecaller?.onDockingStateChanged.RemoveListener(RecallerStateChangedHandler);

            // UI events
            recallButton?.onClick.RemoveListener(RecallButtonHandler);
            abortRecallButton?.onClick.RemoveListener(AbortButtonHandler);
        }

        /// <summary>
        /// Components are set by Reparent method, but are covered here just in case
        /// </summary>
        private void Awake()
        {
            if (!seaTruckRecaller)
            {
                seaTruckRecaller = GetComponentInParent<SeaTruckDockRecaller>();
            }

            if (!_uiAlerts)
            {
                _uiAlerts = GetComponentInChildren<UIAlerts>(true);
            }
        }

        private void Start()
        {
            SetButtonsToReady(false);
        }

        /// <summary>
        /// Used to move the actual UI components out of the prefab onto the existing
        /// console screen 
        /// </summary>
        internal void ReparentScreen(Transform newParent)
        {
            ModDebugLog.LogDebug("Reparenting UI screen...");
            if (!seaTruckRecaller)
            {
                seaTruckRecaller = GetComponentInParent<SeaTruckDockRecaller>();
            }
            
            if (!_uiAlerts)
            {
                _uiAlerts = GetComponentInChildren<UIAlerts>(true);
            }
            
            ModDebugLog.LogDebug($"_uiAlerts is: {_uiAlerts}");
            
            nothingDockedScreen.transform.SetParent(newParent);
            nothingDockedScreen.transform.localPosition = Vector3.zero;
            nothingDockedScreen.transform.localRotation = Quaternion.identity;
            nothingDockedScreen.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
        }
        
        /// <summary>
        /// Called by patch code to set the recaller
        /// </summary>
        internal void SetRecaller(SeaTruckDockRecaller newRecaller)
        {
            seaTruckRecaller = newRecaller;
        }

        /// <summary>
        /// Enable the Recall UI
        /// </summary>
        private void SetButtonsToReady(bool interactable)
        {
            ModDebugLog.LogDebug($"UI buttons set to ready with interactable: {interactable}");
            recallButton.gameObject.SetActive(true);
            recallButton.interactable = interactable;
            abortRecallButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Disable the Recall UI
        /// </summary>
        private void SetButtonsToRecalling()
        {
            recallButton.gameObject.SetActive(false);
            abortRecallButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// Handle the recall button click event
        /// </summary>
        private void RecallButtonHandler()
        {
            ModDebugLog.LogDebug("SeaTruckDockRecallerUi: Recall button clicked!");
            if (seaTruckRecaller.IsDockReady())
            {
                ModDebugLog.LogDebug("SeaTruckDockRecallerUi: Recalling closest SeaTruck");
                SetButtonsToRecalling();
                seaTruckRecaller.RecallClosestSeaTruck();
            }
            else
            {
                _uiAlerts.AddAlert("DOCK IS BUSY!");
                ModDebugLog.LogDebug("SeaTruckDockRecallerUi: Recaller is busy!");
            }
        }

        /// <summary>
        /// Handle the abort button click event
        /// </summary>
        private void AbortButtonHandler()
        {
            ModDebugLog.LogDebug("SeaTruckDockRecallerUi: Abort button clicked!");
            seaTruckRecaller.AbortRecall();
        }

        /// <summary>
        /// Handle changes to the state of the attached Recaller
        /// </summary>
        private void RecallerStateChangedHandler(DockRecallState state)
        {
            ModDebugLog.LogDebug($"RecallerUI: DockState changed to: {state}");
            dockingStatusText.text = GetRecallerStateText(state);
            _uiAlerts.AddAlert(GetRecallerLogText(state));

            switch (state)
            {
                case DockRecallState.Ready:
                case DockRecallState.PathingError:
                case DockRecallState.Aborted:
                    SetButtonsToReady(true);
                    break;
                case DockRecallState.Recalling:
                    SetButtonsToRecalling();
                    break;
                case DockRecallState.Docking:
                    break;
                case DockRecallState.Docked:
                    SetButtonsToReady(false);
                    break;
            }
        }


        /// <summary>
        /// Derives the display state from the state of the AutoPilot
        /// </summary>
        private string GetAutoPilotStateText(AutoPilotState state)
        {
            if (_autoPilotStateDisplayTextDict.TryGetValue(state, out string text))
            {
                return text;
            }

            ModDebugLog.LogError($"SeaTruckDockRecallerUI: cannot find UI text for AutoPilotState: {state}");
            return "";
        }

        /// <summary>
        /// Derives the display state from the state of the Recaller
        /// </summary>
        private string GetRecallerStateText(DockRecallState state)
        {
            if (_recallerStateDisplayTextDict.TryGetValue(state, out string text))
            {
                return text;
            }

            ModDebugLog.LogError($"SeaTruckDockRecallerUI: cannot find UI text for DockRecallState: {state}");
            return "";
        }


        /// <summary>
        /// Derives the log text from the state of the Recaller
        /// </summary>
        private string GetRecallerLogText(DockRecallState state)
        {
            if (_recallerLogDisplayTextDict.TryGetValue(state, out string text))
            {
                return text;
            }

            ModDebugLog.LogError($"SeaTruckDockRecallerUI: cannot find log text for DockRecallState: {state}");
            return "";
        }

        /// <summary>
        /// Subscribe / unsubscribe to AutoPilot events when the autopilot changes
        /// </summary>
        private void AutoPilotChangedHandler(SeaTruckAutoPilot oldAutoPilot, SeaTruckAutoPilot newAutoPilot)
        {
            if (oldAutoPilot)
            {
                oldAutoPilot.onStateChanged.RemoveListener(AutoPilotStateChangedHandler);
                oldAutoPilot.onWaypointChanged.RemoveListener(AutoPilotWaypointChangedHandler);
            }

            if (newAutoPilot)
            {
                newAutoPilot.onStateChanged.AddListener(AutoPilotStateChangedHandler);
                newAutoPilot.onWaypointChanged.AddListener(AutoPilotWaypointChangedHandler);
            }
        }

        /// <summary>
        /// Show details of AutoPilot state on console UI
        /// </summary>
        private void AutoPilotStateChangedHandler(AutoPilotState oldAutoPilotState, AutoPilotState newAutoPilotState)
        {
            ModDebugLog.LogDebug($"RecallerUI: AutoPilotState changed to: {newAutoPilotState.ToString()}");
            autoPilotStatusText.text = $"{AutoPilotDisplayText}{GetAutoPilotStateText(newAutoPilotState)}";
            autoPilotPreviousStatusText.text =
                $"{AutoPilotPreviousDisplayText}{GetAutoPilotStateText(oldAutoPilotState)}";
        }

        /// <summary>
        /// Display details of waypoints as they change
        /// </summary>
        private void AutoPilotWaypointChangedHandler(Waypoint newWaypoint, int currentWaypoint, int totalWaypoints,
            float distanceToTarget)
        {
            _uiAlerts.AddAlert($"MOVING TO WAYPOINT {currentWaypoint} of {totalWaypoints}: {newWaypoint.Position}");
            _uiAlerts.AddAlert($"DISTANCE TO TARGET: {distanceToTarget:F}");
        }
    }
}
