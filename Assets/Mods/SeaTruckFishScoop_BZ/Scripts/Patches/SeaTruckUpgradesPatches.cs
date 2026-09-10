using HarmonyLib;

namespace DaftAppleGames.SeaTruckFishScoop_BZ
{
    /// <summary>
    /// Extends selected SeaTruck upgrade activation to support Fish Scoop toggle and purge actions
    /// </summary>
    public static class SeaTruckUpgradesPatches
    {
        private static bool IsValidSlot(int slotId)
        {
            return slotId >= 0 && slotId < SeaTruckUpgrades.slotIDs.Length;
        }

        private static bool TryGetActiveFishScoop(SeaTruckUpgrades upgrades, out FishScoop fishScoop)
        {
            fishScoop = null;
            int activeSlot = ((IQuickSlots)upgrades).GetActiveSlotID();
            if (!IsValidSlot(activeSlot))
            {
                return false;
            }

            TechType techType = upgrades.modules.GetTechTypeInSlot(SeaTruckUpgrades.slotIDs[activeSlot]);
            if (techType != FishScoopModulePrefab.PrefabInfo.TechType)
            {
                return false;
            }

            fishScoop = upgrades.GetComponent<FishScoop>();
            return fishScoop != null;
        }

        /// <summary>
        /// Starts timing activation for the selected Fish Scoop
        /// </summary>
        [HarmonyPatch] public static class SlotLeftDownPrefix
        {
            static System.Reflection.MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(SeaTruckUpgrades), "IQuickSlots.SlotLeftDown");
            }

            static bool Prefix(SeaTruckUpgrades __instance)
            {
                FishScoop fishScoop;
                if (!TryGetActiveFishScoop(__instance, out fishScoop))
                {
                    return true;
                }

                fishScoop.ActivationPressed();
                return true;
            }
        }

        /// <summary>
        /// Tracks a held activation for the selected Fish Scoop
        /// </summary>
        [HarmonyPatch] public static class SlotLeftHeldPrefix
        {
            static System.Reflection.MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(SeaTruckUpgrades), "IQuickSlots.SlotLeftHeld");
            }

            static bool Prefix(SeaTruckUpgrades __instance)
            {
                FishScoop fishScoop = __instance.GetComponent<FishScoop>();
                if (fishScoop == null || !fishScoop.IsActivationInProgress)
                {
                    return true;
                }

                fishScoop.ActivationHeld();
                return false;
            }
        }

        /// <summary>
        /// Completes a short activation for the selected Fish Scoop
        /// </summary>
        [HarmonyPatch] public static class SlotLeftUpPrefix
        {
            static System.Reflection.MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(SeaTruckUpgrades), "IQuickSlots.SlotLeftUp");
            }

            static bool Prefix(SeaTruckUpgrades __instance)
            {
                FishScoop fishScoop = __instance.GetComponent<FishScoop>();
                if (fishScoop == null || !fishScoop.IsActivationInProgress)
                {
                    return true;
                }

                fishScoop.ActivationReleased();
                return false;
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
                if (!IsValidSlot(slotID))
                {
                    return true;
                }

                TechType techType = __instance.modules.GetTechTypeInSlot(SeaTruckUpgrades.slotIDs[slotID]);

                if (techType == FishScoopModulePrefab.PrefabInfo.TechType)
                {
                    FishScoop fishScoop = __instance.GetComponent<FishScoop>();
                    if (fishScoop == null)
                    {
                        return true;
                    }

                    __result = fishScoop.IsOn;
                    return false;
                }

                return true;
            }
        }
        
    }
}
