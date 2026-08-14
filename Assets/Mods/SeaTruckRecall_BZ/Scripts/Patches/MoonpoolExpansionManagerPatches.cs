using DaftAppleGames.SeaTruckRecall_BZ.DockRecaller;
using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.Patches
{
    /// <summary>
    /// Harmony patching methods for the SeaTruckDock
    /// </summary>
    [HarmonyPatch(typeof(MoonpoolExpansionManager))] internal class MoonpoolExpansionManagerPatches
    {
        private const string RecallConsoleUiAssetName = "RecallConsoleUI.prefab";
        private const string DockRecallAssetName = "SeaTruckRecaller.prefab";

        /// <summary>
        /// Patch the Start method, adding the new component
        /// and register with the static list.
        /// </summary>
        [HarmonyPatch(nameof(MoonpoolExpansionManager.Start))]
        [HarmonyPostfix]
        internal static void Start_Postfix(MoonpoolExpansionManager __instance)
        {
            // Add the SeaTruckRecall component
            GameObject seaDockInstance = ModAssetUtils.GetPrefabInstanceFromAssetBundle(DockRecallAssetName, false);
            seaDockInstance.transform.SetParent(__instance.transform);
            seaDockInstance.transform.localPosition = Vector3.zero;
            seaDockInstance.transform.localRotation = Quaternion.identity;
            seaDockInstance.transform.localScale = Vector3.one;

            SeaTruckDockRecaller newDockRecaller = seaDockInstance.GetComponent<SeaTruckDockRecaller>();
            newDockRecaller.ConfigureStrategicNavigationGraph(LoadedStrategicNavigationGraph);

            seaDockInstance.SetActive(true);
            AllSeaTruckDockRecallers.AddInstance(newDockRecaller);

            ModDebugLog.LogDebug("Finding terminal...");
            MoonpoolExpansionTerminal terminal = __instance.GetComponentInChildren<MoonpoolExpansionTerminal>();
            if (terminal)
            {
                ModDebugLog.LogDebug("Found terminal...");
                AddConsoleUiPrefab(terminal, newDockRecaller);
                ModDebugLog.LogDebug("Added GUI component!");
            }
            else
            {
                ModDebugLog.LogDebug("No terminal found on MoonpoolExpansion!");
            }
        }

        /// <summary>
        /// Patch the OnDestroy method, removing the instance
        /// from the static list
        /// </summary>
        [HarmonyPatch(nameof(MoonpoolExpansionManager.OnDestroy))]
        [HarmonyPostfix]
        internal static void OnDestroy_Postfix(MoonpoolExpansionManager __instance)
        {
            SeaTruckDockRecaller dockRecaller = __instance.GetComponent<SeaTruckDockRecaller>();
            if (dockRecaller)
            {
                AllSeaTruckDockRecallers.RemoveInstance(dockRecaller);
            }
        }

        /// <summary>
        /// Patch the AllowedToDock method, to allow an un-piloted SeaTruck to dock
        /// </summary>
        [HarmonyPatch(nameof(MoonpoolExpansionManager.AllowedToDock))]
        [HarmonyPrefix]
        internal static bool AllowedToDock_Prefix(MoonpoolExpansionManager __instance, Dockable dockable,
            ref bool __result)
        {
            SeaTruckDockRecaller dockRecaller = __instance.GetComponentInChildren<SeaTruckDockRecaller>();
            SeaTruckAutoPilot autoPilot = dockable ? dockable.GetComponent<SeaTruckAutoPilot>() : null;
            if (!dockRecaller || !autoPilot || !autoPilot.IsBusy)
            {
                return true;
            }

            __result = dockable.truckSegment != null &&
                       !__instance.IsOccupied() &&
                       __instance.exitingTruck == null &&
                       !__instance.DockingBlockersInTheWay() &&
                       (__instance.isLoading || __instance.IsPowered()) &&
                       (__instance.isLoading ||
                        !HasBlockingSeaTruckModule(__instance.tailDockingPosition.position, autoPilot));
            return false;
        }

        /// <summary>
        /// Checks the docking area for SeaTruck modules while ignoring the cab currently under autopilot control.
        /// </summary>
        private static bool HasBlockingSeaTruckModule(Vector3 designatedLocation, SeaTruckAutoPilot activeAutoPilot)
        {
            int num = UWE.Utils.OverlapSphereIntoSharedBuffer(designatedLocation, 3f, 1 << LayerID.Vehicle,
                QueryTriggerInteraction.UseGlobal);
            for (int i = 0; i < num; i++)
            {
                Collider collider = UWE.Utils.sharedColliderBuffer[i];

                GameObject gameObject = UWE.Utils.GetEntityRoot(collider.gameObject);
                if (!gameObject)
                {
                    gameObject = collider.gameObject;
                }

                SeaTruckSegment seaTruckSegment = gameObject.GetComponent<SeaTruckSegment>();
                if (!seaTruckSegment)
                {
                    continue;
                }

                SeaTruckAutoPilot autoPilot = gameObject.GetComponent<SeaTruckAutoPilot>();
                if (seaTruckSegment.isMainCab && autoPilot == activeAutoPilot)
                {
                    continue;
                }

                if (!seaTruckSegment.isMainCab ||
                    (!seaTruckSegment.motor.IsPiloted() &&
                     (seaTruckSegment.motor.dockable == null || !seaTruckSegment.motor.dockable.isInTransition)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Keep the Recall Dock status updated - when docking started
        /// </summary>
        [HarmonyPatch(nameof(MoonpoolExpansionManager.StartDocking))]
        [HarmonyPrefix]
        internal static bool StartDocking_Prefix(MoonpoolExpansionManager __instance)
        {
            // ModDebugLog.LogDebug($"MoonpoolExpansionManager.StartDocking called");

            SeaTruckSegment dockingSeaTruck = __instance.dockedHead;

            if (dockingSeaTruck)
            {
                SeaTruckAutoPilot autoPilot = dockingSeaTruck.GetComponent<SeaTruckAutoPilot>();
                if (autoPilot)
                {
                    autoPilot.BeginDocking();
                }
            }

            return true;
        }

        [HarmonyPatch(nameof(MoonpoolExpansionManager.OnDockingTimelineCompleted))]
        [HarmonyPrefix]
        internal static bool OnDockingTimelineCompleted_Prefix(MoonpoolExpansionManager __instance)
        {
            ModDebugLog.LogDebug($"MoonpoolExpansionManager.OnDockingTimelineCompleted called");

            SeaTruckSegment dockingSeaTruck = __instance.dockedHead;

            if (dockingSeaTruck)
            {
                SeaTruckAutoPilot autoPilot = dockingSeaTruck.GetComponent<SeaTruckAutoPilot>();
                if (autoPilot)
                {
                    autoPilot.DockingComplete();
                }
            }

            return true;
        }


        /// <summary>
        /// Keep the Recall Dock status updated - when un-docking complete
        /// </summary>
        [HarmonyPatch(nameof(MoonpoolExpansionManager.StartUndocking))]
        [HarmonyPostfix]
        internal static void StartUndockingPostfix(MoonpoolExpansionManager __instance)
        {
            SeaTruckDockRecaller dockRecaller = __instance.GetComponentInChildren<SeaTruckDockRecaller>();
            if (!dockRecaller)
            {
                return;
            }

            ModDebugLog.LogDebug("Recall Dock Undocking noted as complete.");
            dockRecaller.ReleaseCurrentlyDocked();
        }

        /// <summary>
        /// Get's the console UI prefab from the Asset Bundle and adds it to the console
        /// </summary>
        private static void AddConsoleUiPrefab(MoonpoolExpansionTerminal terminal, SeaTruckDockRecaller dockRecaller)
        {
            // Zap the old UI
            ModDebugLog.LogDebug("Removing old console UI screen...");
            GameObject editScreenGo = terminal.transform.Find("EditScreen").gameObject;
            GameObject activeScreenGo = editScreenGo.transform.Find("Active").gameObject;
            GameObject inActiveScreenGo = editScreenGo.transform.Find("Inactive").gameObject;

            // Clear out the old screen
            foreach (Transform child in inActiveScreenGo.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            ModDebugLog.LogDebug("Removing old console UI screen... Done!");

            ModDebugLog.LogDebug("Getting new UI screen prefab...");
            GameObject consoleUiGo = ModAssetUtils.GetPrefabInstanceFromAssetBundle(RecallConsoleUiAssetName, false);
            SeaTruckDockRecallerUi recallerUi =  consoleUiGo.GetComponent<SeaTruckDockRecallerUi>();
            
            consoleUiGo.GetComponent<SeaTruckDockRecallerUi>().SetRecaller(dockRecaller);
            ModDebugLog.LogDebug("Assigning new UI screen prefab...");

            // Replace the old with the new UI
            consoleUiGo.gameObject.transform.SetParent(editScreenGo.transform);
            recallerUi.ReparentScreen(inActiveScreenGo.transform);

            consoleUiGo.SetActive(true);

            ModDebugLog.LogDebug("Getting new UI screen prefab... Done!");
        }
    }
}
