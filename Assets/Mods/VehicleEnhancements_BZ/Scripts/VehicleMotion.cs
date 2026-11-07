using System;
using DaftAppleGames.VehicleEnhancements_BZ.Integration;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;
using UnityEngine;

namespace DaftAppleGames.VehicleEnhancements_BZ
{
    internal enum EnhancedVehicle
    {
        Seatruck,
        PrawnSuit,
        Snowfox,
        CustomVehicle
    }

    internal static class VehicleMotion
    {
        private static Player customPlayer;
        private static Vehicle customVehicle;
        private static Transform customBody;
        private static Rigidbody customRigidbody;
        private static Func<bool> isCustomVehicleControlled;

        internal static void ClearCustomVehicle()
        {
            customPlayer = null;
            customVehicle = null;
            customBody = null;
            customRigidbody = null;
            isCustomVehicleControlled = null;
            ConfigFile.SetCustomVehicleType(null);
        }

        internal static bool TryGetCustomVehicle(out Transform body, out Vector3 velocity)
        {
            body = null;
            velocity = Vector3.zero;
            Player player = Player.main;
            Vehicle candidate = VehicleFrameworkIntegration.IsEnabled && player
                ? VehicleFrameworkIntegration.GetVehicle(player)
                : null;
            if (!candidate)
            {
                ClearCustomVehicle();
                return false;
            }

            if (customPlayer != player || customVehicle != candidate)
            {
                ClearCustomVehicle();
                customPlayer = player;
                customVehicle = candidate;
                customBody = candidate.transform;
                customRigidbody = candidate.useRigidbody;
                isCustomVehicleControlled = VehicleFrameworkIntegration.BindControlPredicate(candidate);
                ConfigFile.SetCustomVehicleType(VehicleFrameworkIntegration.GetVehicleIdentifier(candidate));
            }

            if (!customVehicle || !customBody || !customRigidbody ||
                isCustomVehicleControlled is null || !isCustomVehicleControlled())
            {
                return false;
            }

            body = customBody;
            velocity = customRigidbody.velocity;
            return true;
        }

        internal static bool TryGet(EnhancedVehicle vehicle, out Transform vehicleTransform, out Vector3 velocity)
        {
            vehicleTransform = null;
            velocity = Vector3.zero;

            if (vehicle == EnhancedVehicle.CustomVehicle)
            {
                return VehicleFrameworkIntegration.IsMonitorMode && TryGetCustomVehicle(out vehicleTransform, out velocity);
            }

            Player player = Player.main;
            if (!player)
            {
                return false;
            }

            Rigidbody rigidbody;
            switch (vehicle)
            {
                case EnhancedVehicle.PrawnSuit:
                    Exosuit exosuit = player.GetVehicle() as Exosuit;
                    if (!exosuit)
                    {
                        return false;
                    }

                    vehicleTransform = exosuit.transform;
                    rigidbody = exosuit.useRigidbody;
                    break;

                case EnhancedVehicle.Snowfox:
                    Hoverbike hoverbike = player.GetComponentInParent<Hoverbike>();
                    if (!hoverbike)
                    {
                        return false;
                    }

                    vehicleTransform = hoverbike.transform;
                    rigidbody = hoverbike.rb;
                    break;

                default:
                    SeaTruckMotor seaTruckMotor = player.GetComponentInParent<SeaTruckMotor>();
                    if (!seaTruckMotor)
                    {
                        return false;
                    }

                    vehicleTransform = seaTruckMotor.transform;
                    rigidbody = seaTruckMotor.useRigidbody;
                    break;
            }

            if (!rigidbody)
            {
                return false;
            }

            velocity = rigidbody.velocity;
            return true;
        }
    }
}
