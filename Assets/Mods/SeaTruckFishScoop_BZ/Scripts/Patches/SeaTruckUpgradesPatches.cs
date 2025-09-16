using HarmonyLib;

namespace DaftAppleGames.SeaTruckFishScoop_BZ
{
    /// <summary>
    /// Patch SeaTruckUpgradesPatches to implement "toggle" QuickSlot functionality and
    /// allow holding the QuickSlot key to purge attached aquariums
    /// </summary>
    public static class SeaTruckUpgradesPatches
    {
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
    }
}