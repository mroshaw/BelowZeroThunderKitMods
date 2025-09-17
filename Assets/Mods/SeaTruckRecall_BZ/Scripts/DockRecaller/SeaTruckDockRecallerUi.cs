using System.Collections.Generic;
using DaftAppleGames.SeatruckRecall_BZ.AutoPilot;
using DaftAppleGames.SeatruckRecall_BZ.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DaftAppleGames.SeatruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeatruckRecall_BZ.DockRecaller
{
    /// <summary>
    /// MonoBehaviour class implementing the UI elements of the
    /// Seatruck Recall component
    /// </summary>
    internal class SeaTruckDockRecallerUi : MonoBehaviour
    {
        private readonly Dictionary<DockRecallState, string> _dockRecallDisplayStateTextDict = new Dictionary<DockRecallState, string>()
        {
            { DockRecallState.Initialising, "INITIALISING..." },
            { DockRecallState.Ready, "READY" },
            { DockRecallState.Aborted, "ABORTED" },
            { DockRecallState.Recalling , "IN PROGRESS..." },
            { DockRecallState.Docked,"DOCKED" },
            { DockRecallState.Parking, "PARKING"}
        };

        // Autopilot state text
        private const string AutoPilotDisplayText = "AUTOPILOT: ";
        private readonly Dictionary<AutoPilotState, string> _autoPilotStateDisplayTextDict = new Dictionary<AutoPilotState, string>()
        {
            { AutoPilotState.Ready, "READY" },
            { AutoPilotState.CalculatingRoute, "CALCULATING ROUTE" },
            { AutoPilotState.Moving, "MOVING" },
            { AutoPilotState.Arrived , "READY" },
            { AutoPilotState.Blocked, "ROUTE BLOCKED!" },
            { AutoPilotState.Aborted , "READY" },
        };
        
        // UI properties
        [SerializeField] private GameObject activeScreenGo;
        [SerializeField] private GameObject inactiveScreenGo;

        // New text controls for state updates
        [SerializeField] private TextMeshProUGUI dockingStatusText;
        [SerializeField] private TextMeshProUGUI autoPilotStatusText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button recallButton;
        [SerializeField] private Button abortRecallButton;

        private SeaTruckDockRecaller _seatruckRecaller;
        
        private void OnEnable()
        {
            if (!_seatruckRecaller)
            {
                _seatruckRecaller = GetComponentInParent<SeaTruckDockRecaller>();
            } 

            // Recaller events
            _seatruckRecaller.OnDockingStateChanged.AddListener(DockStateChangedHandler);
            _seatruckRecaller.OnAutoPilotChanged.AddListener(AutoPilotChangedHandler);
            
            // UI events
            recallButton.onClick.AddListener(RecallButtonHandler);
            abortRecallButton.onClick.AddListener(AbortButtonHandler);
        }

        private void OnDisable()
        {
            // Recaller events
            _seatruckRecaller.OnDockingStateChanged.RemoveListener(DockStateChangedHandler);
            
            // UI events
            recallButton.onClick.RemoveListener(RecallButtonHandler);
            abortRecallButton.onClick.RemoveListener(AbortButtonHandler);
        }

        /// <summary>
        /// Enable the Recall UI
        /// </summary>
        private void RecallReadyUi(bool interactable)
        {
            recallButton.gameObject.SetActive(true);
            recallButton.interactable = interactable;
            abortRecallButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Disable the Recall UI
        /// </summary>
        private void RecallInProgressUi()
        {
            recallButton.gameObject.SetActive(false);
            abortRecallButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// Handle the recall button click event
        /// </summary>
        private void RecallButtonHandler()
        {
            LogDebug("SeaTruckDockRecallerUi: Recall button clicked!");
            if (_seatruckRecaller.IsDockReady())
            {
                LogDebug("SeaTruckDockRecallerUi: Recalling closest SeaTruck");
                // RecallInProgressUi();
                _seatruckRecaller.RecallClosestSeaTruck();
            }
            else
            {
                LogDebug("SeaTruckDockRecallerUi: Recaller is busy!");
            }
        }

        /// <summary>
        /// Handle the abort button click event
        /// </summary>
        private void AbortButtonHandler()
        {
            LogDebug("SeaTruckDockRecallerUi: Abort button clicked!");
            _seatruckRecaller.AbortRecall();
        }

        private void DockStateChangedHandler(DockRecallState dockRecallState)
        {
            // Update the UI
            LogDebug($"SeaTruckDockRecallerUi: Updating UI with DockRecallState: {dockRecallState.ToString()}");
            dockingStatusText.text = $"{_dockRecallDisplayStateTextDict[dockRecallState]}";

            // Enable or disable UI components
            switch (dockRecallState)
            {
                case DockRecallState.Ready:
                case DockRecallState.Aborted:
                    RecallReadyUi(true);
                    break;
                case DockRecallState.Initialising:
                    RecallReadyUi(false);
                    break;
                default:
                    RecallInProgressUi();
                    break;
            }
        }

        private void AutoPilotChangedHandler(SeaTruckAutoPilot oldAutoPilot, SeaTruckAutoPilot newAutoPilot)
        {
            if (oldAutoPilot)
            {
                oldAutoPilot.OnAutoPilotStateChanged.RemoveListener(AutoPilotStateChangedHandler);
                oldAutoPilot.OnAutoPilotWaypointChanged.RemoveListener(AutoPilotWaypointChangedHandler);
            }

            if (newAutoPilot)
            {
                newAutoPilot.OnAutoPilotStateChanged.AddListener(AutoPilotStateChangedHandler);
                newAutoPilot.OnAutoPilotWaypointChanged.AddListener(AutoPilotWaypointChangedHandler);
            }
        }
        
        private void AutoPilotStateChangedHandler(AutoPilotState autoPilotState)
        {
            LogDebug($"SeaTruckDockRecallerUi: Updating UI with AutoPilotState: {autoPilotState.ToString()}");
            autoPilotStatusText.text = $"{AutoPilotDisplayText}{_autoPilotStateDisplayTextDict[autoPilotState]}";
        }

        private void AutoPilotWaypointChangedHandler(Waypoint newWaypoint, float distanceToTarget)
        {
            SetStatusText($"DISTANCE TO TARGET: {distanceToTarget:F}");
        }
        
        private void SetStatusText(string text)
        {
            statusText.text = text;
        }
    }
}