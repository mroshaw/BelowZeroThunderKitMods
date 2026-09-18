using HarmonyLib;

namespace DaftAppleGames.VehicleEnhancements_BZ.Patches
{
    /// <summary>
    /// Allows power consumers built in SeaTruck segments to use the segment's relay.
    /// </summary>
    [HarmonyPatch(typeof(PowerConsumer))]
    internal static class PowerConsumerPatches
    {
        private static readonly AccessTools.FieldRef<PowerConsumer, PowerRelay> PowerRelayField =
            AccessTools.FieldRefAccess<PowerConsumer, PowerRelay>("powerRelay");

        /// <summary>
        /// Uses SeaTruck relay power when there is no Base to check for powered cells.
        /// </summary>
        [HarmonyPatch(nameof(PowerConsumer.IsPowered))]
        [HarmonyPrefix]
        private static bool IsPowered_Prefix(PowerConsumer __instance, ref bool __result)
        {
            if (__instance.GetBaseComp())
            {
                return true;
            }

            SeaTruckSegment segment = __instance.GetComponentInParent<SeaTruckSegment>();
            if (!segment)
            {
                return true;
            }

            if (!VehicleEnhancementsPlugin_BZ.ConfigFile.EnableBuildingInside)
            {
                __result = false;
                return false;
            }

            PowerRelay relay = PowerRelayField(__instance);
            if (!relay)
            {
                relay = segment.relay;
                PowerRelayField(__instance) = relay;
            }

            __result = relay && relay.IsPowered();
            return false;
        }

        /// <summary>
        /// Reports no available power to Seatruck construction devices while building inside is disabled.
        /// </summary>
        [HarmonyPatch(nameof(PowerConsumer.HasPower))]
        [HarmonyPrefix]
        private static bool HasPower_Prefix(PowerConsumer __instance, ref bool __result)
        {
            if (VehicleEnhancementsPlugin_BZ.ConfigFile.EnableBuildingInside ||
                __instance.GetBaseComp() ||
                !__instance.GetComponentInParent<SeaTruckSegment>())
            {
                return true;
            }

            __result = false;
            return false;
        }

        /// <summary>
        /// Prevents Seatruck construction devices from drawing power while building inside is disabled.
        /// </summary>
        [HarmonyPatch(nameof(PowerConsumer.ConsumePower))]
        [HarmonyPrefix]
        private static bool ConsumePower_Prefix(PowerConsumer __instance, ref float consumed)
        {
            if (VehicleEnhancementsPlugin_BZ.ConfigFile.EnableBuildingInside ||
                __instance.GetBaseComp() ||
                !__instance.GetComponentInParent<SeaTruckSegment>())
            {
                return true;
            }

            consumed = 0.0f;
            return false;
        }
    }
}
