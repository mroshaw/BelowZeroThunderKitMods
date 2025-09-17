using HarmonyLib;

namespace DaftAppleGames.SeaTruckFishScoop_BZ
{
    /// <summary>
    /// Patch SeaTruckUpgradesPatches to implement "toggle" QuickSlot functionality and
    /// allow holding the QuickSlot key to purge attached aquariums
    /// </summary>
    public static class SeaTruckUpgradesPatches
    {
        /// <summary>
        /// Captures the slot "Key Down" - use this to either toggle the scoop (single press)
        /// on purge (hold down)
        /// </summary>
        [HarmonyPatch] public static class SlotKeyDown_Prefix
        {
            static System.Reflection.MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(SeaTruckUpgrades), "IQuickSlots.SlotKeyDown");
            }
            
            static bool Prefix(SeaTruckUpgrades __instance, int slotID)
            {
                TechType techType = __instance.modules.GetTechTypeInSlot(SeaTruckUpgrades.slotIDs[slotID]);

                if (techType == FishScoopModulePrefab.PrefabInfo.TechType)
                {
                    __instance.GetComponent<FishScoop>().QuickSlotPressed(slotID);
                    
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Used to determine if the press was a single one or a "hold"
        /// </summary>
        [HarmonyPatch] public static class SlotKeyUp_Prefix
        {
            static System.Reflection.MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(SeaTruckUpgrades), "IQuickSlots.SlotKeyUp");
            }
            
            static bool Prefix(SeaTruckUpgrades __instance, int slotID)
            {
                TechType techType = __instance.modules.GetTechTypeInSlot(SeaTruckUpgrades.slotIDs[slotID]);

                if (techType == FishScoopModulePrefab.PrefabInfo.TechType)
                {
                    __instance.GetComponent<FishScoop>().QuickSlotReleased(slotID);
                    return false;
                }
                return true;
            }
        }
        
        /// <summary>
        /// Used to determine how long the key was held down for
        /// </summary>
        [HarmonyPatch] public static class SlotKeyHeld_Prefix
        {
            static System.Reflection.MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(SeaTruckUpgrades), "IQuickSlots.SlotKeyHeld");
            }
            
            static bool Prefix(SeaTruckUpgrades __instance, int slotID)
            {
                TechType techType = __instance.modules.GetTechTypeInSlot(SeaTruckUpgrades.slotIDs[slotID]);

                if (techType == FishScoopModulePrefab.PrefabInfo.TechType)
                {
                    __instance.GetComponent<FishScoop>().QuickSlotHeld(slotID);
                    return false;
                }
                return true;
            }
        }
        
        /// <summary>
        /// As SeaTruckUpgrades has no "toggled" state of it's own, we must patch in our
        /// FishScoop state to return it's toggled state
        /// </summary>
        [HarmonyPatch] public static class IsToggled_Prefix
        {
            static System.Reflection.MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(SeaTruckUpgrades), "IQuickSlots.IsToggled");
            }
            
            static bool Prefix(SeaTruckUpgrades __instance, int slotID, ref bool __result)
            {
                TechType techType = __instance.modules.GetTechTypeInSlot(SeaTruckUpgrades.slotIDs[slotID]);

                if (techType == FishScoopModulePrefab.PrefabInfo.TechType)
                {
                    FishScoop fishScoop = __instance.GetComponent<FishScoop>();
                    __result = fishScoop.IsOn;
                    return false;
                }

                return true;
            }
        }
        
    }
}