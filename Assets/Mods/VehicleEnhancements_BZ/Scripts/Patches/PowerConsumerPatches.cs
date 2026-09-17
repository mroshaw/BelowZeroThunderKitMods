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

            PowerRelay relay = PowerRelayField(__instance);
            if (!relay)
            {
                relay = segment.relay;
                PowerRelayField(__instance) = relay;
            }

            __result = relay && relay.IsPowered();
            return false;
        }
    }
}
