using HarmonyLib;

using UnityEngine;
using static DaftAppleGames.SeaTruckSpeedMod_BZ.SeaTruckSpeedPluginBz;

namespace DaftAppleGames.SeaTruckSpeedMod_BZ.Patches
{
    class SeaTruckMotorPatches
    {
        /// <summary>
        /// Harmony hooks to modify the SeaTruck drag coefficient and add speed-relative power drain.
        /// </summary>
        /// 
        [HarmonyPatch(typeof(SeaTruckMotor))]
        internal class SeaTruckMotorPatch
        {
            private const float VanillaPropulsionPowerPerSecond = 0.12f;
            private const float DrainCurveSpeed = 20.0f;

            /// <summary>
            /// Registers a Seatruck and applies its configured speed modifier.
            /// </summary>
            [HarmonyPatch(nameof(SeaTruckMotor.Start))]
            [HarmonyPostfix]
            public static void Start_Postfix(SeaTruckMotor __instance)
            {
                SeaTruckHistory.AddSeaTruck(__instance);

                SeaTruckHistoryTracker historyTracker = __instance.GetComponent<SeaTruckHistoryTracker>();
                if (!historyTracker)
                {
                    historyTracker = __instance.gameObject.AddComponent<SeaTruckHistoryTracker>();
                }

                historyTracker.Initialize(__instance);
            }

            /// <summary>
            /// Adds a modest amount of propulsion power consumption based on actual speed.
            /// </summary>
            [HarmonyPatch("FixedUpdate")]
            [HarmonyPostfix]
            public static void FixedUpdate_Postfix(SeaTruckMotor __instance, GameObject ___inputStackDummy)
            {
                float configuredDrain = ConfigFile.PowerDrain;
                float additionalSpeedMultiplier = ConfigFile.DragModifier - 1.0f;
                if (configuredDrain <= 0.0f || additionalSpeedMultiplier <= 0.0f ||
                    !__instance.IsPiloted() || !__instance.relay || !__instance.useRigidbody ||
                    !__instance.relay.IsPowered() || __instance.IsBusyAnimating() ||
                    __instance.transform.position.y >= Ocean.GetOceanLevel())
                {
                    return;
                }

                bool inputEnabled = (AvatarInputHandler.main && AvatarInputHandler.main.IsEnabled()) ||
                                    (___inputStackDummy && ___inputStackDummy.activeInHierarchy);
                bool hasMovementInput = inputEnabled && GameInput.IsMoving();
                if (!hasMovementInput && !__instance.afterBurnerActive)
                {
                    return;
                }

                float speed = __instance.useRigidbody.velocity.magnitude;
                if (speed <= Mathf.Epsilon)
                {
                    return;
                }

                float speedRatio = speed / (speed + DrainCurveSpeed);
                float extraPower = VanillaPropulsionPowerPerSecond * __instance.powerEfficiencyFactor *
                                   Mathf.Clamp01(configuredDrain) * speedRatio * Time.fixedDeltaTime;
                __instance.relay.ConsumeEnergy(extraPower, out float amountConsumed);
            }
        }
    }
}
