using System.Collections.Generic;
using DaftAppleGames.SeaTruckRecall_BZ.Navigation;
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
        private readonly Dictionary<AutoPilotState, string> _autoPilotStateDisplayTextDict = new Dictionary<AutoPilotState, string>()
        {
            { AutoPilotState.Ready, "READY" },
            { AutoPilotState.Moving, "MOVING" },
            { AutoPilotState.Arrived , "ARRIVED" },
            { AutoPilotState.Blocked, "ROUTE BLOCKED!" },
            { AutoPilotState.Parking, "PARKING" },
            { AutoPilotState.Docking , "DOCKING"},
            { AutoPilotState.Docked , "DOCKED" },
            { AutoPilotState.Aborted , "ABORTED!" },
        };
        
        [Header("Settings")]
        [SerializeField] private SeaTruckDockRecaller _seaTruckRecaller;
        
        // UI properties
        [SerializeField] private GameObject activeScreenGo;
        [SerializeField] private GameObject inactiveScreenGo;

        // Text controls for state updates
        [SerializeField] private TMP_Text dockingStatusText;
        [SerializeField] private TMP_Text autoPilotStatusText;
        [SerializeField] private Button recallButton;
        [SerializeField] private Button abortRecallButton;

        // Used for notifications on the console UI
        private UIAlerts _uiAlerts;
        
        private void OnEnable()
        {
            if (!_seaTruckRecaller)
            {
                _seaTruckRecaller = GetComponentInParent<SeaTruckDockRecaller>();
            } 

            // Recaller events
            _seaTruckRecaller?.onAutoPilotChanged?.AddListener(AutoPilotChangedHandler);
            _seaTruckRecaller?.onDockingStateChanged.AddListener(RecallerStateChangedHandler);
            
            // UI events
            recallButton?.onClick.AddListener(RecallButtonHandler);
            abortRecallButton?.onClick.AddListener(AbortButtonHandler);
        }

        private void OnDisable()
        {
            // Recaller events
            _seaTruckRecaller?.onAutoPilotChanged?.RemoveListener(AutoPilotChangedHandler);
            _seaTruckRecaller?.onDockingStateChanged.RemoveListener(RecallerStateChangedHandler);
            
            // UI events
            recallButton?.onClick.RemoveListener(RecallButtonHandler);
            abortRecallButton?.onClick.RemoveListener(AbortButtonHandler);
        }

        private void Awake()
        {
            _uiAlerts = GetComponentInChildren<UIAlerts>(true);
            dockingStatusText.text = "INITIALISING...";
            _uiAlerts.AddAlert("INITIALISING NAV GRID...");
        }

        private void Start()
        {
            SetButtonsToReady(false);
        }

        /// <summary>
        /// Called by patch code to set the recaller
        /// </summary>
        internal void SetRecaller(SeaTruckDockRecaller newRecaller)
        {
            _seaTruckRecaller =  newRecaller;
        }
        
        /// <summary>
        /// Enable the Recall UI
        /// </summary>
        private void SetButtonsToReady(bool interactable)
        {
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
            if (_seaTruckRecaller.IsDockReady())
            {
                ModDebugLog.LogDebug("SeaTruckDockRecallerUi: Recalling closest SeaTruck");
                SetButtonsToRecalling();
                _seaTruckRecaller.RecallClosestSeaTruck();
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
            _seaTruckRecaller.AbortRecall();
        }

        /// <summary>
        /// Handle changes to the state of the attached Recaller
        /// </summary>
        private void RecallerStateChangedHandler(DockRecallState state)
        {
            switch (state)
            {
                case DockRecallState.Ready:
                    dockingStatusText.text = "READY";
                    _uiAlerts.AddAlert("DOCK READY!");
                    SetButtonsToReady(true);
                    break;
                
                case DockRecallState.PathingError:
                    dockingStatusText.text = "READY";
                    _uiAlerts.AddAlert("PATHING ERROR! ABORTING!");
                    SetButtonsToReady(true);
                    break;
                
                case DockRecallState.Recalling:
                    dockingStatusText.text = "RECALLING";
                    _uiAlerts.AddAlert("RECALLING SEATRUCK!");
                    SetButtonsToRecalling();
                    break;
                
                case DockRecallState.Aborted:
                    dockingStatusText.text = "READY";
                    _uiAlerts.AddAlert("NAVIGATION ABORTED!");
                    SetButtonsToReady(true);
                    break;
                
                case DockRecallState.Parking:
                    dockingStatusText.text = "PARKING";
                    _uiAlerts.AddAlert("PARKING SEATRUCK!");
                    break;
                
                case DockRecallState.Docking:
                    dockingStatusText.text = "DOCKING";
                    _uiAlerts.AddAlert("SEATRUCK IS DOCKING!");
                    break;
                case DockRecallState.Docked:
                    dockingStatusText.text = "DOCKED";
                    _uiAlerts.AddAlert("SEATRUCK DOCKED!");
                    SetButtonsToReady(false);
                    break;
            }
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
        private void AutoPilotStateChangedHandler(AutoPilotState autoPilotState)
        {
            // LogDebug($"SeaTruckDockRecallerUi: Updating UI with AutoPilotState: {autoPilotState.ToString()}");
            autoPilotStatusText.text = $"{AutoPilotDisplayText}{_autoPilotStateDisplayTextDict[autoPilotState]}";
        }

        /// <summary>
        /// Display details of waypoints as they change
        /// </summary>
        private void AutoPilotWaypointChangedHandler(Waypoint newWaypoint, float distanceToTarget)
        {
            _uiAlerts.AddAlert($"MOVING TO WAYPOINT: {newWaypoint}");
            _uiAlerts.AddAlert($"DISTANCE TO TARGET: {distanceToTarget:F}");
        }
}
}