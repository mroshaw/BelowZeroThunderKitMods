/*
using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.SeaTruckRecall_BZ.SeaTruckDockRecallPlugin;

namespace DaftAppleGames.SeaTruckRecall_BZ.Patches
{
    /// <summary>
    /// Harmony patching methods for the SeaTruckDock
    /// </summary>
    [HarmonyPatch(typeof(VehicleDockingBay))]
    internal class VehicleDockingBayPatches
    {
        [HarmonyPatch(nameof(VehicleDockingBay.OnTriggerEnter))]
        [HarmonyPrefix]
        internal static bool OnTriggerEnterPrefix(VehicleDockingBay __instance, Collider other)
        {
            ModDebugLog.LogDebug("VehicleDockingBay.OnTriggerEnter");
            ModDebugLog.LogDebug($"fullConstructed: {__instance.fullyConstructed}");
            ModDebugLog.LogDebug($"isPowered: {__instance.powerConsumer.IsPowered()}");
            ModDebugLog.LogDebug($"TechnologyRequiredPower: {GameModeManager.GetOption<bool>(GameOption.TechnologyRequiresPower)}");
            
            if (!__instance.fullyConstructed || (GameModeManager.GetOption<bool>(GameOption.TechnologyRequiresPower) && !__instance.powerConsumer.IsPowered()))
            {
                ModDebugLog.LogDebug("VehicleDockingBay.OnTriggerEnter: Dropped out of CLAUSE 1");
                return true;
            }
            GameObject gameObject = UWE.Utils.GetEntityRoot(other.gameObject);
            if (!gameObject)
            {
                gameObject = other.gameObject;
            }
            ModDebugLog.LogDebug($"gameObject: {gameObject}");
            ModDebugLog.LogDebug($"TechType: {CraftData.GetTechType(gameObject)}");
            
            if (CraftData.GetTechType(gameObject) == TechType.SeaTruck)
            {
                SeaTruckSegment component = gameObject.GetComponent<SeaTruckSegment>();
                if (component.colliderToAvoidDockingWith == other)
                {
                    ModDebugLog.LogDebug("VehicleDockingBay.OnTriggerEnter: Dropped out of CLAUSE 2");
                    return true;
                }
                
                ModDebugLog.LogDebug($"IsLocked: {__instance.seaTruckRedockLock.IsLocked()}");
                if (__instance.seaTruckRedockLock.IsLocked())
                {
                    if (component == __instance.seaTruckRedockLock.GetLockedSeaTruck())
                    {
                        ModDebugLog.LogDebug("VehicleDockingBay.OnTriggerEnter: Dropped out of CLAUSE 3");
                        return true;
                    }
                    __instance.seaTruckRedockLock.Unlock();
                }
            }
            Dockable component2 = gameObject.GetComponent<Dockable>();
            if (component2)
            {
                ModDebugLog.LogDebug($"AllowedToDock: {((IDockingBay)__instance).AllowedToDock(component2)}");
                if (!((IDockingBay)__instance).AllowedToDock(component2))
                {
                    ModDebugLog.LogDebug("VehicleDockingBay.OnTriggerEnter: Dropped out of CLAUSE 4");
                    return true;
                }
                UWE.Utils.GetEntityRoot(component2.gameObject);
                __instance.dockPlayer = component2.GetPlayer() != null;
                __instance.timeDockingStarted = Time.time;
                __instance.interpolatingDockable = component2;
                __instance.startPosition = __instance.interpolatingDockable.transform.position;
                __instance.startRotation = __instance.interpolatingDockable.transform.rotation;
                component2.OnDockingStart(__instance.disableDockableCollisionInProcess, !__instance.MoonpoolExpansionEnabled());
                
                ModDebugLog.LogDebug($"MoonpoolExpansionEnabled: {__instance.MoonpoolExpansionEnabled()}");
                if (__instance.MoonpoolExpansionEnabled())
                {
                    ModDebugLog.LogDebug("VehicleDockingBay.OnTriggerEnter: Prepping Dock");
                    __instance.expansionManager.PrepDocking(component2);
                }
                Player.allowSaving = false;
            }
            __instance.UpdateDocking();
            return true;
        }
    }
}
*/