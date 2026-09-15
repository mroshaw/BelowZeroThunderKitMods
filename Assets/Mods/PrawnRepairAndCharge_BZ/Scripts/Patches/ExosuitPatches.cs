using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.PrawnSuitRepairAndCharge_BZ.PrawnSuitRepairAndChargePluginBz;

namespace DaftAppleGames.PrawnSuitRepairAndCharge_BZ
{
    [HarmonyPatch(typeof(Exosuit))]
    internal class ExosuitPatches
    {
        internal static bool IsRestoringSeaTruckDock;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Exosuit.OnDockedChanged))]
        public static void OnDockedChanged_Postfix(Exosuit __instance, bool docked, Vehicle.DockType dockType)
        {
            if (IsRestoringSeaTruckDock)
            {
                return;
            }

            SaveLoadManager saveLoadManager = SaveLoadManager.main;
            if (saveLoadManager && saveLoadManager.isLoading)
            {
                return;
            }

            ModDebugLog.LogDebug("In Exosuit.OnDockedChanged");
            if (!docked)
            {
                return;
            }

            ModDebugLog.LogDebug($"Dock Change at: {dockType}, Docked is: {docked}");

            // If no options are checked, do nothing more
            if (!ConfigFile.EnableInMoonPool && !ConfigFile.EnableInSeaTruck)
            {
                ModDebugLog.LogDebug($"All options disabled. Nothing to do!");
                return;
            }

            Dockable dockable = __instance.GetComponent<Dockable>();
            if (!dockable)
            {
                ModDebugLog.LogDebug($"Couldn't find Dockable!");
                return;
            }

            // Calculate the deficits
            float powerToAdd = CalculatePowerDeficit(__instance);
            float healthToAdd = CalculateHealthDeficit(__instance);

            // MoonPool Dock
            if (dockType == Vehicle.DockType.Base && ConfigFile.EnableInMoonPool)
            {
                ModDebugLog.LogDebug($"Docked in MoonPool...");

                if (ConfigFile.ConsumeBasePower)
                {
                    VehicleDockingBay vehicleDockingBay = dockable.bay as VehicleDockingBay;
                    if (!vehicleDockingBay || !vehicleDockingBay.powerRelay)
                    {
                        ModDebugLog.LogWarning("Could not find the Moonpool docking bay or base power relay.");
                        return;
                    }

                    if (!TryConsumeDockPower(vehicleDockingBay.powerRelay, healthToAdd, powerToAdd, "Base"))
                    {
                        ErrorMessage.AddMessage($"Base has insufficient power to repair and charge the Prawn Suit!");
                        return;
                    }
                }

                RepairVehicle(__instance, healthToAdd);
                ChargeVehicle(__instance, powerToAdd);
                ErrorMessage.AddMessage($"Prawn Suit repaired and charged!");

                ModDebugLog.LogDebug($"Docked in MoonPool... Done!");
            }

            // SeaTruck Dock
            if (dockType == Vehicle.DockType.Seatruck && ConfigFile.EnableInSeaTruck)
            {
                ModDebugLog.LogDebug($"Docked in SeaTruck...");

                // If consuming SeaTruck power to charge, calculate what we can draw and ensure we subtract that from the SeaTruck
                if (ConfigFile.ConsumeSeaTruckPower)
                {
                    // Get the SeaTruckDockingBay that we're docked to
                    SeaTruckDockingBay seaTruckDockingBay = dockable.bay as SeaTruckDockingBay;
                    if (!seaTruckDockingBay || !seaTruckDockingBay.relay)
                    {
                        ModDebugLog.LogWarning("Could not find the SeaTruck docking bay or power relay.");
                        return;
                    }

                    ModDebugLog.LogDebug($"Found SeaTruckDockingBay: {seaTruckDockingBay.name}");

                    if (!TryConsumeDockPower(seaTruckDockingBay.relay, healthToAdd, powerToAdd, "SeaTruck"))
                    {
                        ErrorMessage.AddMessage($"Seatruck has insufficient power to repair and charge the Prawn Suit!");
                        return;
                    }
                }

                // Repair and charge
                RepairVehicle(__instance, healthToAdd);
                ChargeVehicle(__instance, powerToAdd);

                ErrorMessage.AddMessage($"Prawn Suit repaired and charged!");

                ModDebugLog.LogDebug($"Docked in SeaTruck... Done!");
            }
        }

        /// <summary>
        /// Draws the energy needed to repair and charge a docked Prawn Suit.
        /// </summary>
        private static bool TryConsumeDockPower(PowerRelay powerRelay, float healthToAdd, float powerToAdd,
            string powerSourceName)
        {
            float powerToRepair = healthToAdd * ConfigFile.SeaTruckPowerUseRepairModifier;
            float powerToCharge = powerToAdd * ConfigFile.SeaTruckPowerUseChargeModifier;
            float totalPowerRequired = powerToRepair + powerToCharge;
            ModDebugLog.LogDebug(
                $"{powerSourceName} power required: {totalPowerRequired} (repair: {powerToRepair}, charge: {powerToCharge}).");

            if (totalPowerRequired <= 0f)
            {
                return true;
            }

            if (totalPowerRequired > powerRelay.GetPower())
            {
                ErrorMessage.AddMessage($"Prawn Suit not repaired or recharged! Insufficient {powerSourceName} power!");
                return false;
            }

            IPowerInterface powerInterface = powerRelay.GetComponent<IPowerInterface>();
            if (powerInterface == null)
            {
                ModDebugLog.LogWarning($"Could not find the {powerSourceName} power interface.");
                return false;
            }

            powerInterface.ConsumeEnergy(totalPowerRequired, out float amountConsumed);
            ModDebugLog.LogDebug(
                $"{powerSourceName} supplied {amountConsumed} of {totalPowerRequired} required energy.");
            if (amountConsumed + 0.001f < totalPowerRequired)
            {
                ErrorMessage.AddMessage($"Prawn Suit not repaired or recharged! Insufficient {powerSourceName} power!");
                return false;
            }

            ErrorMessage.AddMessage($"Process consumed {(int)amountConsumed} energy.");
            return true;
        }

        /// <summary>
        /// Calculate how much energy is required to "top up"
        /// </summary>
        private static float CalculatePowerDeficit(Vehicle vehicleInstance)
        {
            // Get current charge and max charge
            vehicleInstance.energyInterface.GetValues(out float currentCharge, out float currentCapacity);
            float powerDelta = Mathf.Max(0f, currentCapacity - currentCharge);
            ModDebugLog.LogDebug(
                $"Current Prawn Suit charge: {currentCharge}, Max charge: {currentCapacity}, Charge delta: {powerDelta}");
            return powerDelta;
        }

        /// <summary>
        /// Calculate how much health is needed for maximum
        /// </summary>
        private static float CalculateHealthDeficit(Vehicle vehicleInstance)
        {
            float currentHealth = vehicleInstance.liveMixin.health;
            float maxHealth = vehicleInstance.liveMixin.maxHealth;
            float healthDelta = Mathf.Max(0f, maxHealth - currentHealth);
            ModDebugLog.LogDebug(
                $"Current Prawn Suit health: {currentHealth}, Max health: {maxHealth}, Health delta: {healthDelta}");
            return healthDelta;
        }

        /// <summary>
        /// Repair the PrawnSuit by the specified amount
        /// </summary>
        private static void RepairVehicle(Vehicle vehicleInstance, float healthToAdd)
        {
            // Top up health
            vehicleInstance.liveMixin.AddHealth(healthToAdd);
            ModDebugLog.LogDebug($"Added health to Prawn Suit: {healthToAdd}");
        }

        /// <summary>
        /// Charge the PrawnSuit by the specified amount
        /// </summary>
        private static void ChargeVehicle(Vehicle vehicleInstance, float powerToAdd)
        {
            vehicleInstance.AddEnergy(powerToAdd);
            ModDebugLog.LogDebug($"Added power to Prawn Suit: {powerToAdd}");
        }
    }
}