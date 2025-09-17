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
        // UI labels
        private const string RecallButtonDisplayText = "RECALL SEATRUCK";
        private const string AbortButtonDisplayText = "ABORT RECALL";
        // Waypoint state text
        private const string WayPointDisplayText = "WAYPOINT: ";
        // Dock state text
        private const string RecallDisplayText = "RECALL: ";
        private readonly Dictionary<DockRecallState, string> _dockRecallDisplayStateTextDict = new Dictionary<DockRecallState, string>()
        {
            { DockRecallState.None, "INITIALISING..." },
            { DockRecallState.Ready, "READY" },
            { DockRecallState.Aborted, "ABORTED" },
            { DockRecallState.Recalling , "IN PROGRESS..." },
            { DockRecallState.Docked,"READY" },
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
        [SerializeField] private TextMeshProUGUI waypointText;
        [SerializeField] private GameObject recallButtonGo;
        [SerializeField] private Button recallButton;
        [SerializeField] private GameObject abortRecallButtonGo;
        [SerializeField] private Button abortRecallButton;

        private SeaTruckDockRecaller _seatruckRecaller;
        private MoonpoolExpansionTerminal _expansionTerminal;
        
        private void OnEnable()
        {
            _seatruckRecaller = GetComponentInParent<SeaTruckDockRecaller>();

            // Subscribe to recaller status update
            _seatruckRecaller.OnDockingStateChanged.AddListener(DockStateChangedHandler);
            _seatruckRecaller.OnAutoPilotStateChanged.AddListener(AutoPilotStateChangedHandler);
            _seatruckRecaller.OnDockingWaypointChanged.AddListener(WaypointChangedHandler);
            recallButton.onClick.AddListener(RecallButtonHandler);
            abortRecallButton.onClick.AddListener(AbortButtonHandler);
        }

        private void OnDisable()
        {
            _seatruckRecaller.OnDockingStateChanged.RemoveListener(DockStateChangedHandler);
            _seatruckRecaller.OnAutoPilotStateChanged.RemoveListener(AutoPilotStateChangedHandler);
            _seatruckRecaller.OnDockingWaypointChanged.RemoveListener(WaypointChangedHandler);
            recallButton.onClick.RemoveListener(RecallButtonHandler);
            abortRecallButton.onClick.RemoveListener(AbortButtonHandler);
        }

        private void Awake()
        {
            _expansionTerminal = GetComponent<MoonpoolExpansionTerminal>();
        }
        
        /// <summary>
        /// Enable the Recall UI
        /// </summary>
        public void RecallReadyUi()
        {
            recallButtonGo.SetActive(true);
            abortRecallButtonGo.SetActive(false);
        }

        /// <summary>
        /// Disable the Recall UI
        /// </summary>
        public void RecallInProgressUi()
        {
            recallButtonGo.SetActive(false);
            abortRecallButtonGo.SetActive(true);
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
                RecallInProgressUi();
                _seatruckRecaller.RecallClosestSeatruck();
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
            dockingStatusText.text = $"{RecallDisplayText}{_dockRecallDisplayStateTextDict[dockRecallState]}";

            // Enable or disable UI components
            switch (dockRecallState)
            {
                case DockRecallState.Ready:
                case DockRecallState.None:
                case DockRecallState.NoneInRange:
                case DockRecallState.Aborted:
                    RecallReadyUi();
                    break;
                default:
                    RecallInProgressUi();
                    break;
            }
        }

        private void AutoPilotStateChangedHandler(AutoPilotState autoPilotState)
        {
            LogDebug($"SeaTruckDockRecallerUi: Updating UI with AutoPilotState: {autoPilotState.ToString()}");
            autoPilotStatusText.text = $"{AutoPilotDisplayText}{_autoPilotStateDisplayTextDict[autoPilotState]}";
        }

        private void WaypointChangedHandler(Waypoint waypoint)
        {
            if (waypoint == null)
            {
                LogDebug($"SeaTruckDockRecallerUi: Updating UI with NONE Waypoint");
                waypointText.text = $"{WayPointDisplayText}NONE";
                return;
            }
            LogDebug($"SeaTruckDockRecallerUi: Updating UI with Waypoint: {waypoint.Name}");
            waypointText.text = $"{WayPointDisplayText}{waypoint.Name}";
        }
    }
}