using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    // Recaller Status
    internal enum DockRecallState
    {
        None,
        Initialising,
        NoTrucksFound,
        FindingPath,
        PathingError,
        Ready,
        Recalling,
        Stuck,
        Aborted,
        Docking,
        Docked
    }

    /// <summary>
    /// MonoBehaviour class to attach to a SeaTruckDock
    /// that implements the recall behaviour
    /// </summary>
    internal class SeaTruckDockRecaller : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool createGridOnStart = true;
        [SerializeField] private bool instantNav = false;
        [SerializeField] private int maximumReplanAttempts = 3;
        [SerializeField] private int maximumNoProgressSegments = 3;
        [SerializeField] private float dockingStagingDistance = 30.0f;
        [SerializeField] private float dockingTriggerPenetration = 0.5f;
        [SerializeField] private float maximumDockingStagingDistance = 60.0f;
        [SerializeField] private StrategicNavigationGraph strategicNavigationGraph;
        
        [Header("Debug")]
        [SerializeField] private SeaTruckAutoPilot currentAutoPilot;
        
        [Header("Events")]
        // Event publishing latest recall state and distance
        [SerializeField] internal AutoPilotChangedEvent onAutoPilotChanged = new AutoPilotChangedEvent();
        [SerializeField]internal DockStateChangedEvent onDockingStateChanged = new DockStateChangedEvent();
        
        private readonly List<Waypoint> _instantNavWaypoints = new List<Waypoint>();
        private Waypoint _dockStaging;
        private Waypoint _dockAlignment;
        private Waypoint _dockEngagement;
        
        // Useful internal components
        private MoonpoolExpansionManager _dockingManager;

        // Internal tracking and audit
        private DockRecallState _currentRecallState = DockRecallState.None;
        
        private bool _gridReady;
        private NavGridHelper _navGridHelper;
        private int _pathRequestVersion;
        private int _replanAttemptCount;
        private readonly List<Vector3> _strategicRoute = new List<Vector3>();
        private int _strategicRouteIndex;
        private bool _localGridGenerating;
        private bool _activeSegmentIncludesDockApproach;
        private bool _segmentAdvancePending;
        private float _segmentStartDistanceToStrategicTarget;
        private int _noProgressSegmentCount;
        private readonly Collider[] _stagingClearanceHits = new Collider[64];

        internal void ConfigureStrategicNavigationGraph(StrategicNavigationGraph directlyLoadedGraph)
        {
            string prefabGraphDescription = strategicNavigationGraph
                ? $"'{strategicNavigationGraph.name}' (instance {strategicNavigationGraph.GetInstanceID()}) with " +
                  $"{strategicNavigationGraph.NodeCount} nodes and " +
                  $"{strategicNavigationGraph.StoredConnectionCount} connections"
                : "no graph";
            string directGraphDescription = directlyLoadedGraph
                ? $"'{directlyLoadedGraph.name}' (instance {directlyLoadedGraph.GetInstanceID()}) with " +
                  $"{directlyLoadedGraph.NodeCount} nodes and " +
                  $"{directlyLoadedGraph.StoredConnectionCount} connections"
                : "no graph";
            bool sameInstance = strategicNavigationGraph && directlyLoadedGraph &&
                                strategicNavigationGraph == directlyLoadedGraph;

            ModDebugLog.LogDebug($"Strategic graph comparison: prefab reference has {prefabGraphDescription}; " +
                                 $"direct AssetBundle load has {directGraphDescription}; same instance is " +
                                 $"{sameInstance}.");

            if (directlyLoadedGraph && directlyLoadedGraph.NodeCount > 0 &&
                (!strategicNavigationGraph || strategicNavigationGraph.NodeCount == 0))
            {
                strategicNavigationGraph = directlyLoadedGraph;
                ModDebugLog.LogDebug("Replaced the prefab strategic graph reference with the directly loaded " +
                                     "AssetBundle graph.");
            }
        }

        private void OnEnable()
        {
            if (!_navGridHelper)
            {
                _navGridHelper = GetComponent<NavGridHelper>();
            }
            AllSeaTruckDockRecallers.AddInstance(this);
        }

        private void OnDisable()
        {
            AllSeaTruckDockRecallers.RemoveInstance(this);
        }

        private void Awake()
        {
            _navGridHelper = GetComponent<NavGridHelper>();
            _dockingManager = GetComponent<MoonpoolExpansionManager>();
            SetDockedAutoPilot();
        }
        
        private void Start()
        {
            // Init useful local components
#if !UNITY_EDITOR
            _dockingManager = gameObject.transform.parent.GetComponent<MoonpoolExpansionManager>();
#endif

            // Set up the docking waypoints
            CreateDockApproach();

            if (createGridOnStart)
            {
                GenerateNavGrid();
            }
        }

        /// <summary>
        /// Check to see if something is docked when the component first starts
        /// </summary>
        private void SetDockedAutoPilot()
        {
            if (_dockingManager && _dockingManager.dockedHead && !currentAutoPilot)
            {
                SeaTruckAutoPilot autoPilot = _dockingManager.dockedHead.GetComponent<SeaTruckAutoPilot>();
                if (autoPilot)
                {
                    SetAutoPilot(autoPilot);
                }
            }
        }
        
        /// <summary>
        /// Sets the SeaTruck linked to this recaller
        /// </summary>
        private void SetAutoPilot(SeaTruckAutoPilot newAutoPilot)
        {
            SeaTruckAutoPilot oldAutoPilot = currentAutoPilot;
            currentAutoPilot = newAutoPilot;
            
            ModDebugLog.LogDebug($"AutoPilot changed from: {oldAutoPilot} to {newAutoPilot}");
            
            if (oldAutoPilot)
            {
                oldAutoPilot.onStateChanged?.RemoveListener(AutoPilotStateChangeHandler);
            }

            if (newAutoPilot)
            {
                newAutoPilot.onStateChanged?.AddListener(AutoPilotStateChangeHandler);
            }
            
            onAutoPilotChanged?.Invoke(oldAutoPilot, newAutoPilot);
        }
        
        private IEnumerator GenerateNavGridAsync(Vector3 centerPosition)
        {
            SetDockState(DockRecallState.Initialising);
            yield return _navGridHelper.RefreshNavGridAsync(centerPosition, GridReadyHandler);
        }
        
        /// <summary>
        /// Generate the NavGrid centered around this dock
        /// </summary>
        internal void GenerateNavGrid()
        {
            SetDockState(DockRecallState.Initialising);
            _navGridHelper.RefreshNavGrid(_dockStaging.Position, GridReadyHandler);
        }

        private void GridReadyHandler(GenerateStatus gridStatus)
        {
            _gridReady = gridStatus == GenerateStatus.Success;
            if (_gridReady)
            {
                SetDockState(DockRecallState.Ready);
            }
        }
        
        private void AutoPilotStateChangeHandler(AutoPilotState oldState, AutoPilotState newState)
        {
            switch (newState)
            {
                case AutoPilotState.Ready:
                    SetDockState(DockRecallState.Ready);
                    break;
                case AutoPilotState.Moving:
                    SetDockState(DockRecallState.Recalling);
                    break;
                case AutoPilotState.Replanning:
                    SetDockState(DockRecallState.FindingPath);
                    break;
                case AutoPilotState.Aborted:
                    ShowRecallInterruptedMessage();
                    if (_currentRecallState != DockRecallState.PathingError &&
                        _currentRecallState != DockRecallState.Stuck)
                    {
                        SetDockState(DockRecallState.Aborted);
                    }
                    SetAutoPilot(null);
                    SetDockState(_localGridGenerating
                        ? DockRecallState.Initialising
                        : DockRecallState.Ready);
                    break;
                case AutoPilotState.Stuck:
                    if (_activeSegmentIncludesDockApproach)
                    {
                        FailRecall(DockRecallState.Stuck);
                    }
                    else
                    {
                        BeginReplan();
                    }
                    break;
                case AutoPilotState.Arrived:
                    HandleSegmentArrived();
                    break;
                case AutoPilotState.Docking:
                    SetDockState(DockRecallState.Docking);
                    break;
                case AutoPilotState.Docked:
                    SetDockState(DockRecallState.Docked);
                    break;
            }
        }
        
        /// <summary>
        /// Release the currently docked seatruck. Does a check to see if we have a Seatruck in the
        /// dock that isn't registered (e.g. from a game save load)
        /// </summary>
        public void ReleaseCurrentlyDocked()
        {
            if (_dockingManager.dockedHead && !currentAutoPilot)
            {
                SetDockedAutoPilot();
            }

            if (currentAutoPilot)
            {
                currentAutoPilot.ReleaseFromDock();
                SetAutoPilot(null);
            }
            SetDockState(DockRecallState.Ready);
        }

        /// <summary>
        /// Public method to cancel in-progress Recall
        /// </summary>
        internal void AbortRecall()
        {
            ModDebugLog.LogDebug("Aborting Recall...");
            _pathRequestVersion++;
            _navGridHelper.CancelPathGeneration();
            currentAutoPilot.AbortNavigation();
        }

        /// <summary>
        /// Public method to recall the closest SeaTruck
        /// </summary>
        public void RecallClosestSeaTruck()
        {
            if (!IsDockReady())
            {
                ModDebugLog.LogDebug("Dock is already occupied or busy!");
                return;
            }
            ModDebugLog.LogDebug("Finding closest SeaTruck...");
            SeaTruckAutoPilot closestAutoPilot = AllSeaTruckAutoPilots.GetClosestAutoPilot(transform.position,
                _navGridHelper.RecallRange);
            if (closestAutoPilot == null)
            {
                // Couldn't find a closest SeaTruck
                ModDebugLog.LogDebug("No SeaTrucks found!");
                SetDockState(DockRecallState.NoTrucksFound);
                SetDockState(DockRecallState.Ready);
                return;
            }

            // Recall the SeaTruck
            SetAutoPilot(closestAutoPilot);
            CreateDockApproach(currentAutoPilot);
            _replanAttemptCount = 0;
            _noProgressSegmentCount = 0;
            
#if UNITY_EDITOR
            if (instantNav)
#else
            if (ConfigFile.RecallMoveMethod == RecallMoveMethod.Teleport)
#endif
            {
                InstantNav();
                return;
            }
            
            CreateStrategicRoute();
            BeginLocalNavigationSegment(false);
        }

        /// <summary>
        /// Teleports the SeaTruck to the docking staging point, then navigates through the trigger.
        /// </summary>
        private void InstantNav()
        {
            if (currentAutoPilot)
            {
                currentAutoPilot.transform.position = _dockStaging.Position;
                currentAutoPilot.transform.rotation = _dockAlignment.Rotation;
                _activeSegmentIncludesDockApproach = true;
                if (!currentAutoPilot.StartNavigation(_instantNavWaypoints))
                {
                    _activeSegmentIncludesDockApproach = false;
                    FailRecall(DockRecallState.PathingError);
                }
            }
        }
        
        private void PathReadyHandler(int requestVersion, SeaTruckAutoPilot requestedAutoPilot,
            bool includesDockApproach, GenerateStatus pathStatus, NavPath navPath)
        {
            if (requestVersion != _pathRequestVersion || requestedAutoPilot != currentAutoPilot)
            {
                ModDebugLog.LogDebug("Ignoring stale navigation path result.");
                return;
            }

            if (pathStatus == GenerateStatus.Success)
            {
                List<Waypoint> waypoints = navPath.GetWayPointsFromNavPath();
                 
                if (includesDockApproach)
                {
                    // The grid endpoint is a cell near the exact staging position. Replace it instead of
                    // appending a short return leg that can make a fast SeaTruck overshoot and turn around.
                    if (waypoints.Count > 0)
                    {
                        waypoints[waypoints.Count - 1] = _dockStaging;
                    }
                    else
                    {
                        waypoints.Add(_dockStaging);
                    }
                    waypoints.Add(_dockAlignment);
                    waypoints.Add(_dockEngagement);
                }
                _activeSegmentIncludesDockApproach = includesDockApproach;
                if (!currentAutoPilot.StartNavigation(waypoints))
                {
                    FailRecall(DockRecallState.PathingError);
                }
            }
            else
            {
                FailRecall(DockRecallState.PathingError);
            }
        }

        private void ShowRecallInterruptedMessage()
        {
            switch (_currentRecallState)
            {
                case DockRecallState.PathingError:
                    ErrorMessage.AddMessage("SeaTruck recall interrupted: no safe route could be found.");
                    break;
                case DockRecallState.Stuck:
                    ErrorMessage.AddMessage("SeaTruck recall interrupted: the SeaTruck is stuck.");
                    break;
                default:
                    ErrorMessage.AddMessage("SeaTruck recall cancelled.");
                    break;
            }
        }

        private void BeginReplan()
        {
            if (!currentAutoPilot)
            {
                return;
            }

            if (_replanAttemptCount >= maximumReplanAttempts)
            {
                ModDebugLog.LogDebug("Maximum route replanning attempts reached.");
                FailRecall(DockRecallState.Stuck);
                return;
            }

            _replanAttemptCount++;
            ModDebugLog.LogDebug($"Replanning local route, attempt {_replanAttemptCount} of {maximumReplanAttempts}.");
            BeginLocalNavigationSegment(true);
        }

        private void BeginLocalNavigationSegment(bool prepareAutoPilot)
        {
            if (!currentAutoPilot)
            {
                return;
            }

            if (prepareAutoPilot)
            {
                currentAutoPilot.PrepareForReplan();
            }
            else
            {
                currentAutoPilot.BeginPlanning();
            }

            Vector3 startPosition = currentAutoPilot.transform.position;
            Vector3 destination = _strategicRoute[_strategicRouteIndex];
            Vector3 destinationOffset = destination - startPosition;
            _segmentStartDistanceToStrategicTarget = destinationOffset.magnitude;
            float planningDistance = _navGridHelper.LocalPlanningDistance;
            bool reachesStrategicTarget = destinationOffset.magnitude <= planningDistance;
            bool includesDockApproach = reachesStrategicTarget && _strategicRouteIndex == _strategicRoute.Count - 1;
            Vector3 segmentDestination = reachesStrategicTarget
                ? destination
                : startPosition + destinationOffset.normalized * planningDistance;

            LogSegmentEnvironment(startPosition, destination, segmentDestination, reachesStrategicTarget,
                includesDockApproach);

            SetDockState(DockRecallState.FindingPath);
            _localGridGenerating = true;
            int requestVersion = ++_pathRequestVersion;
            SeaTruckAutoPilot requestedAutoPilot = currentAutoPilot;
            ModDebugLog.LogDebug($"Generating rolling navigation grid at {startPosition}; local destination is {segmentDestination}.");
            _navGridHelper.RefreshNavGrid(startPosition,
                gridStatus => LocalGridReadyHandler(requestVersion, requestedAutoPilot, segmentDestination,
                    includesDockApproach, gridStatus), currentAutoPilot.gameObject);
        }

        private void LocalGridReadyHandler(int requestVersion, SeaTruckAutoPilot requestedAutoPilot,
            Vector3 segmentDestination, bool includesDockApproach, GenerateStatus gridStatus)
        {
            _localGridGenerating = false;
            if (requestVersion != _pathRequestVersion || requestedAutoPilot != currentAutoPilot)
            {
                if (!currentAutoPilot)
                {
                    _gridReady = gridStatus == GenerateStatus.Success;
                    SetDockState(_gridReady ? DockRecallState.Ready : DockRecallState.PathingError);
                }
                return;
            }

            if (gridStatus != GenerateStatus.Success)
            {
                FailRecall(DockRecallState.PathingError);
                return;
            }

            _navGridHelper.GenerateNavPath(currentAutoPilot.transform.position, segmentDestination,
                (pathStatus, navPath) => PathReadyHandler(requestVersion, requestedAutoPilot,
                    includesDockApproach, pathStatus, navPath));
        }

        private void LogSegmentEnvironment(Vector3 startPosition, Vector3 strategicTarget,
            Vector3 segmentDestination, bool reachesStrategicTarget, bool includesDockApproach)
        {
            Vector3 playerPosition = Player.main ? Player.main.transform.position : Vector3.zero;
            ModDebugLog.LogDebug($"Segment diagnostics: SeaTruck {startPosition}, player {playerPosition}, " +
                                 $"player distance {Vector3.Distance(startPosition, playerPosition):F1}, strategic " +
                                 $"target {_strategicRouteIndex + 1}/{_strategicRoute.Count} at {strategicTarget}, " +
                                 $"local destination {segmentDestination}, reaches target {reachesStrategicTarget}, " +
                                 $"includes dock approach {includesDockApproach}.");
            LogVerticalProbe("SeaTruck", startPosition);
            LogVerticalProbe("Local destination", segmentDestination);
        }

        private void LogVerticalProbe(string label, Vector3 position)
        {
            RaycastHit hit;
            bool hitDown = Physics.Raycast(position + Vector3.up, Vector3.down, out hit, 500.0f,
                _navGridHelper.NavGridIncludeLayerMask, QueryTriggerInteraction.Ignore);
            string downDescription = hitDown
                ? $"'{hit.collider.gameObject.name}' at {hit.point}, distance {hit.distance:F1}"
                : "no hit within 500m";
            bool hitUp = Physics.Raycast(position + Vector3.down, Vector3.up, out hit, 500.0f,
                _navGridHelper.NavGridIncludeLayerMask, QueryTriggerInteraction.Ignore);
            string upDescription = hitUp
                ? $"'{hit.collider.gameObject.name}' at {hit.point}, distance {hit.distance:F1}"
                : "no hit within 500m";
            ModDebugLog.LogDebug($"{label} vertical probes: down {downDescription}; up {upDescription}.");
        }

        private void HandleSegmentArrived()
        {
            if (!currentAutoPilot || _segmentAdvancePending)
            {
                return;
            }

            if (_activeSegmentIncludesDockApproach)
            {
                if (!_dockingManager || !_dockingManager.IsOccupied())
                {
                    ModDebugLog.LogDebug("SeaTruck reached the docking engagement point without starting docking.");
                    FailRecall(DockRecallState.Stuck);
                }
                return;
            }

            Vector3 strategicTarget = _strategicRoute[_strategicRouteIndex];
            float distanceToStrategicTarget = Vector3.Distance(currentAutoPilot.transform.position, strategicTarget);
            if (distanceToStrategicTarget <= _navGridHelper.distanceBetweenCells * 2.0f &&
                _strategicRouteIndex < _strategicRoute.Count - 1)
            {
                _strategicRouteIndex++;
                _noProgressSegmentCount = 0;
            }
            else if (_segmentStartDistanceToStrategicTarget - distanceToStrategicTarget <
                     _navGridHelper.distanceBetweenCells)
            {
                _noProgressSegmentCount++;
                if (_noProgressSegmentCount >= maximumNoProgressSegments)
                {
                    ModDebugLog.LogDebug("Rolling navigation did not make sufficient progress toward its strategic target.");
                    FailRecall(DockRecallState.Stuck);
                    return;
                }
            }
            else
            {
                _noProgressSegmentCount = 0;
            }

            _segmentAdvancePending = true;
            StartCoroutine(AdvanceToNextSegment());
        }

        private void CreateStrategicRoute()
        {
            _strategicRouteIndex = 0;
            Vector3 startPosition = currentAutoPilot.transform.position;
            Vector3 destination = _dockStaging.Position;
            string routeFailureReason;
            if (!StrategicRoutePlanner.TryCalculateRoute(strategicNavigationGraph, startPosition, destination,
                    _strategicRoute, out routeFailureReason))
            {
                _strategicRoute.Clear();
                _strategicRoute.Add(destination);
                ModDebugLog.LogDebug($"No strategic graph route is available ({routeFailureReason}); " +
                                     "using direct rolling navigation.");
                return;
            }

            float skipDistance = _navGridHelper.distanceBetweenCells * 2.0f;
            while (_strategicRouteIndex < _strategicRoute.Count - 1 &&
                   Vector3.Distance(startPosition, _strategicRoute[_strategicRouteIndex]) <= skipDistance)
            {
                _strategicRouteIndex++;
            }
            ModDebugLog.LogDebug($"Strategic route contains {_strategicRoute.Count} navigation points.");
        }

        private IEnumerator AdvanceToNextSegment()
        {
            yield return null;
            _segmentAdvancePending = false;
            if (!currentAutoPilot)
            {
                yield break;
            }

            _replanAttemptCount = 0;
            BeginLocalNavigationSegment(true);
        }

        private void FailRecall(DockRecallState failureState)
        {
            SetDockState(failureState);
            if (currentAutoPilot)
            {
                currentAutoPilot.AbortNavigation();
            }
            else
            {
                SetDockState(DockRecallState.Ready);
            }
        }
        
        /// <summary>
        /// Creates the proven runway points that place the SeaTruck's nose, rather than its pivot, into the dock.
        /// </summary>
        private void CreateDockApproach(SeaTruckAutoPilot autoPilot = null)
        {
            Transform dockingEnd = _dockingManager && _dockingManager.bay
                ? _dockingManager.bay.dockingEndPos
                : null;
            if (!dockingEnd || !autoPilot)
            {
                CreateFallbackDockApproach();
                return;
            }

            Quaternion dockingRotation = dockingEnd.rotation;
            Vector3 inwardDirection = dockingRotation * Vector3.forward;
            Vector3 outwardDirection = -inwardDirection;
            float firstTriggerPassDistance;
            if (!TryGetFirstTriggerPassDistance(dockingEnd.position, inwardDirection,
                    out firstTriggerPassDistance))
            {
                CreateFallbackDockApproach();
                return;
            }

            Vector3 cabinFrontOffset;
            float cabinTurnRadius;
            GetCabinGeometry(autoPilot.transform, out cabinFrontOffset, out cabinTurnRadius);
            Vector3 engagementPosition = dockingEnd.position +
                                         inwardDirection * (firstTriggerPassDistance + dockingTriggerPenetration);
            float stagingDistance = dockingStagingDistance;
            Vector3 stagingPosition = engagementPosition + outwardDirection * stagingDistance;

            _dockStaging = CreateDockWaypoint(stagingPosition, Quaternion.identity, "Dock Entrance", Color.yellow,
                true, false, false, true);
            _dockAlignment = CreateDockWaypoint(stagingPosition, dockingRotation, "Dock Alignment", Color.yellow,
                false, true, true, false);
            _dockEngagement = CreateDockWaypoint(engagementPosition, dockingRotation, "Dock Engagement", Color.green,
                false, false, true, false);

            ModDebugLog.LogDebug($"Derived drive-through dock approach: tube centre {dockingEnd.position}, " +
                                 $"inward {inwardDirection}, first trigger pass distance " +
                                 $"{firstTriggerPassDistance:F2}m, penetration {dockingTriggerPenetration:F2}m, " +
                                 $"observed cabin front offset {cabinFrontOffset}, turn radius " +
                                 $"{cabinTurnRadius:F1}m, staging distance {stagingDistance:F1}m, " +
                                 $"staging {stagingPosition}, engagement {engagementPosition}, rotation " +
                                 $"{dockingRotation.eulerAngles}.");

            _instantNavWaypoints.Clear();
            _instantNavWaypoints.Add(_dockEngagement);
        }

        private void CreateFallbackDockApproach()
        {
            Quaternion dockingRotation = Quaternion.LookRotation(-gameObject.transform.right, Vector3.up);
            Vector3 stagingPosition = gameObject.transform.position + Vector3.up * 0.1f -
                                      gameObject.transform.right * dockingStagingDistance;
            Vector3 engagementPosition = gameObject.transform.position + Vector3.up * 0.8f -
                                         gameObject.transform.right * 10.0f;
            _dockStaging = CreateDockWaypoint(stagingPosition, Quaternion.identity, "Dock Entrance", Color.yellow,
                true, false, false, true);
            _dockAlignment = CreateDockWaypoint(stagingPosition, dockingRotation, "Dock Alignment", Color.yellow,
                false, true, true, false);
            _dockEngagement = CreateDockWaypoint(engagementPosition, dockingRotation, "Dock Engagement", Color.green,
                false, true, true, false);
            _instantNavWaypoints.Clear();
            _instantNavWaypoints.Add(_dockEngagement);
        }

        private bool TryGetFirstTriggerPassDistance(Vector3 tubeCentre, Vector3 inwardDirection,
            out float firstTriggerPassDistance)
        {
            firstTriggerPassDistance = 0.0f;
            if (!_dockingManager || !_dockingManager.bay)
            {
                return false;
            }

            BoxCollider[] colliders = _dockingManager.bay.GetComponents<BoxCollider>();
            float closestNearFaceDistance = float.PositiveInfinity;
            bool foundTrigger = false;
            foreach (BoxCollider collider in colliders)
            {
                if (!collider.enabled || !collider.isTrigger)
                {
                    continue;
                }

                Vector3 worldCentre = collider.transform.TransformPoint(collider.center);
                Vector3 worldSize = Vector3.Scale(collider.size, collider.transform.lossyScale);
                Vector3 halfSize = worldSize * 0.5f;
                float projectedRadius = Mathf.Abs(Vector3.Dot(collider.transform.right, inwardDirection)) *
                                        Mathf.Abs(halfSize.x) +
                                        Mathf.Abs(Vector3.Dot(collider.transform.up, inwardDirection)) *
                                        Mathf.Abs(halfSize.y) +
                                        Mathf.Abs(Vector3.Dot(collider.transform.forward, inwardDirection)) *
                                        Mathf.Abs(halfSize.z);
                float centreDistance = Vector3.Dot(worldCentre - tubeCentre, inwardDirection);
                float nearFaceDistance = centreDistance - projectedRadius;
                float farFaceDistance = centreDistance + projectedRadius;
                ModDebugLog.LogDebug($"Dock trigger candidate '{collider.name}' ({collider.GetInstanceID()}): " +
                                     $"local centre {collider.center}, world centre {worldCentre}, size " +
                                     $"{worldSize}, near face distance {nearFaceDistance:F2}m, centre distance " +
                                     $"{centreDistance:F2}m, far face distance {farFaceDistance:F2}m along the tube.");

                if (nearFaceDistance < closestNearFaceDistance)
                {
                    closestNearFaceDistance = nearFaceDistance;
                    firstTriggerPassDistance = farFaceDistance;
                    foundTrigger = true;
                }
            }

            return foundTrigger;
        }

        private static void GetCabinGeometry(Transform seaTruckTransform, out Vector3 frontOffset,
            out float turnRadius)
        {
            Collider[] colliders = seaTruckTransform.GetComponentsInChildren<Collider>(true);
            SeaTruckSegment headSegment = seaTruckTransform.GetComponent<SeaTruckSegment>();
            float minimumX = float.PositiveInfinity;
            float maximumX = float.NegativeInfinity;
            float minimumY = float.PositiveInfinity;
            float maximumY = float.NegativeInfinity;
            float maximumZ = float.NegativeInfinity;
            turnRadius = 0.0f;

            foreach (Collider collider in colliders)
            {
                if (!collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                Bounds bounds = collider.bounds;
                Vector3 localCentre = seaTruckTransform.InverseTransformPoint(bounds.center);
                Vector3 localExtents = seaTruckTransform.InverseTransformVector(bounds.extents);
                localExtents = new Vector3(Mathf.Abs(localExtents.x), Mathf.Abs(localExtents.y),
                    Mathf.Abs(localExtents.z));
                turnRadius = Mathf.Max(turnRadius, (localCentre + localExtents).magnitude,
                    (localCentre - localExtents).magnitude);

                if (collider.GetComponentInParent<SeaTruckSegment>() != headSegment)
                {
                    continue;
                }

                minimumX = Mathf.Min(minimumX, localCentre.x - localExtents.x);
                maximumX = Mathf.Max(maximumX, localCentre.x + localExtents.x);
                minimumY = Mathf.Min(minimumY, localCentre.y - localExtents.y);
                maximumY = Mathf.Max(maximumY, localCentre.y + localExtents.y);
                maximumZ = Mathf.Max(maximumZ, localCentre.z + localExtents.z);
            }

            if (float.IsInfinity(maximumZ))
            {
                frontOffset = Vector3.forward * 10.0f;
                turnRadius = 6.0f;
                return;
            }

            frontOffset = new Vector3((minimumX + maximumX) * 0.5f,
                (minimumY + maximumY) * 0.5f, maximumZ);
            turnRadius += 1.0f;
        }

        private float FindClearStagingDistance(GameObject ignoredSeaTruck, Vector3 triggerCentre,
            Vector3 rotatedFrontOffset, Vector3 outwardDirection, float turnRadius)
        {
            float stagingDistance = dockingStagingDistance;
            while (stagingDistance < maximumDockingStagingDistance)
            {
                Vector3 candidate = triggerCentre + outwardDirection * stagingDistance - rotatedFrontOffset;
                int hitCount = Physics.OverlapSphereNonAlloc(candidate, turnRadius, _stagingClearanceHits,
                    _navGridHelper.NavGridIncludeLayerMask, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
                {
                    Collider collider = _stagingClearanceHits[hitIndex];
                    GameObject entityRoot = collider ? UWE.Utils.GetEntityRoot(collider.gameObject) : null;
                    if (!collider || entityRoot == ignoredSeaTruck || collider.transform.IsChildOf(ignoredSeaTruck.transform) ||
                        NavigationObstacleFilter.IsPlayerCollider(collider, entityRoot) ||
                        (entityRoot && entityRoot.GetComponent<Creature>()))
                    {
                        continue;
                    }
                    blocked = true;
                    break;
                }
                if (!blocked)
                {
                    return stagingDistance;
                }
                stagingDistance += 5.0f;
            }
            return maximumDockingStagingDistance;
        }
        
        /// <summary>
        /// Create a dock waypoint
        /// </summary>
        private Waypoint CreateDockWaypoint(Vector3 position, Quaternion rotation, string waypointName,
            Color debugColor, bool monitorObstacles, bool rotateBeforeMoving, bool useFixedRotation,
            bool allowSkip)
        {
            Waypoint newWaypoint = new Waypoint(position, rotation, rotateBeforeMoving, true, waypointName,
                monitorObstacles, false, useFixedRotation, allowSkip);

            // if (_navGridHelper.NavGridDebug)
            if (true)
            {
                GameObject newWaypointGo = new GameObject(waypointName)
                {
                    transform =
                    {
                        position = position
                    }
                };
                
                /*
                GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.name = waypointName;
                debugSphere.transform.SetParent(_navGridHelper.NavGridDebugContainer);
                debugSphere.transform.position = newWaypointGo.transform.position;
                debugSphere.transform.rotation = newWaypointGo.transform.rotation;
                debugSphere.GetComponent<Renderer>().material.color = debugColor;
                Destroy(debugSphere.GetComponent<Collider>());
                */
            }

            return newWaypoint;
        }
        
        /// <summary>
        /// Returns true if recaller is available
        /// otherwise false
        /// </summary>
        public bool IsDockReady()
        {
            // Allow us to test in the Unity Editor
            if (!_dockingManager)
            {
                return true;
            }
            return _gridReady && !_dockingManager.IsOccupied() && currentAutoPilot == null;
        }
        
        private void SetDockState(DockRecallState newRecallState)
        {
            if (newRecallState == _currentRecallState)
            {
                return;
            }

            ModDebugLog.LogDebug($"SeaTruckRecaller.SetDockState: state changed from {_currentRecallState} to {newRecallState}.");
            _currentRecallState = newRecallState;
            onDockingStateChanged?.Invoke(newRecallState);
        }
        
        // Unity Events to publish DockRecallStatus changes
        [Serializable]
        internal class AutoPilotChangedEvent : UnityEvent<SeaTruckAutoPilot, SeaTruckAutoPilot>
        {
        }
        
        [Serializable]
        internal class DockStateChangedEvent : UnityEvent<DockRecallState>
        {
        }
    }
}
