using System.Collections.Generic;
using DaftAppleGames.SeatruckRecall_BZ.AutoPilot;
using DaftAppleGames.SeatruckRecall_BZ.Extensions;
using DaftAppleGames.SeatruckRecall_BZ.Navigation;
using DaftAppleGames.SeatruckRecall_BZ.Utils;
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

        // Waypoint state text
        private const string WayPointDisplayText = "WAYPOINT: ";

        // UI properties
        private MoonpoolExpansionTerminal _expansionTerminal;
        private GameObject _editScreenGo;
        private GameObject _activeScreenGo;
        private GameObject _inactiveScreenGo;

        // New text controls for state updates
        private GameObject _dockingStatusTextGo;
        private TextMeshProUGUI _dockingStatusText;

        private GameObject _autoPilotStatusTextGo;
        private TextMeshProUGUI _autoPilotStatusText;

        private GameObject _waypointTextGo;
        private TextMeshProUGUI _waypointText;

        private GameObject _recallButtonGo;
        private Button _recallButton;

        private GameObject _abortRecallButtonGo;
        private Button _abortRecallButton;

        // SeatruckRecaller component
        private SeaTruckDockRecaller _seatruckRecaller;

        private void OnEnable()
        {
            _seatruckRecaller = GetComponentInParent<SeaTruckDockRecaller>();

            // Subscribe to recaller status update
            _seatruckRecaller.OnDockingStateChanged.AddListener(DockStateChangedHandler);
            _seatruckRecaller.OnAutoPilotStateChanged.AddListener(AutoPilotStateChangedHandler);
            _seatruckRecaller.OnDockingWaypointChanged.AddListener(WaypointChangedHandler);
        }

        private void OnDisable()
        {
            _seatruckRecaller.OnDockingStateChanged.RemoveListener(DockStateChangedHandler);
            _seatruckRecaller.OnAutoPilotStateChanged.RemoveListener(AutoPilotStateChangedHandler);
            _seatruckRecaller.OnDockingWaypointChanged.RemoveListener(WaypointChangedHandler);
        }

        private void Awake()
        {
            _expansionTerminal = GetComponent<MoonpoolExpansionTerminal>();
            _editScreenGo = gameObject.GetNamedGameObject("EditScreen");
            _activeScreenGo = _editScreenGo.GetNamedGameObject("Active");
            _inactiveScreenGo = _editScreenGo.GetNamedGameObject("Inactive");
            CreateUi();
        }

        /// <summary>
        /// Create the Recall UI
        /// </summary>
        private void CreateUi()
        {
            LogDebug("SeaTruckDockRecallerUi: Creating UI...");

            // Get the InfoPanel so we can parent things nice
            TextMeshProUGUI infoPanel = _expansionTerminal.infoPanel;

            // Create the status text components
            GameObject inactiveTextGo = _inactiveScreenGo.GetNamedGameObject("Text");

            UiUtils.CloneText(inactiveTextGo, "DockingStatusText", "", -150, 50.0f, out _dockingStatusTextGo,
                out _dockingStatusText);
            UiUtils.CloneText(inactiveTextGo, "AutoPilotStatusText", "", -200, 40.0f, out _autoPilotStatusTextGo,
                out _autoPilotStatusText);
            UiUtils.CloneText(inactiveTextGo, "WaypointStatusText", "", -250, 40.0f, out _waypointTextGo,
                out _waypointText);

            // Create the Recall button components
            GameObject existingButton = _activeScreenGo.GetNamedGameObject("Button");

            UiUtils.CloneButton(existingButton, _inactiveScreenGo.transform, "RecallButton", RecallButtonDisplayText,
                250.0f, 20.0f, 3, out _recallButtonGo, out _recallButton );

            // Create the Abort button components
            UiUtils.CloneButton(existingButton, _inactiveScreenGo.transform, "AbortButton", AbortButtonDisplayText,
                250.0f, 20.0f, 3, out _abortRecallButtonGo, out _abortRecallButton);

            LogDebug("SeaTruckDockRecallerUi: Setting up button handlers...");
            _recallButton.onClick.AddListener(RecallButtonHandler);
            _recallButtonGo.SetActive(true);
            _abortRecallButton.onClick.AddListener(AbortButtonHandler);
            _abortRecallButtonGo.SetActive(false);
            LogDebug("SeaTruckDockRecallerUi: Button handler setup complete!");
        }


        /// <summary>
        /// Enable the Recall UI
        /// </summary>
        public void RecallReadyUi()
        {
            _recallButtonGo.SetActive(true);
            _abortRecallButtonGo.SetActive(false);
        }

        /// <summary>
        /// Disable the Recall UI
        /// </summary>
        public void RecallInProgressUi()
        {
            _recallButtonGo.SetActive(false);
            _abortRecallButtonGo.SetActive(true);
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
            _dockingStatusText.text = $"{RecallDisplayText}{_dockRecallDisplayStateTextDict[dockRecallState]}";

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
            _autoPilotStatusText.text = $"{AutoPilotDisplayText}{_autoPilotStateDisplayTextDict[autoPilotState]}";
        }

        private void WaypointChangedHandler(Waypoint waypoint)
        {
            if (waypoint == null)
            {
                LogDebug($"SeaTruckDockRecallerUi: Updating UI with NONE Waypoint");
                _waypointText.text = $"{WayPointDisplayText}NONE";
                return;
            }
            LogDebug($"SeaTruckDockRecallerUi: Updating UI with Waypoint: {waypoint.Name}");
            _waypointText.text = $"{WayPointDisplayText}{waypoint.Name}";
        }
    }
}