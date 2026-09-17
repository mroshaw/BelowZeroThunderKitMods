using UnityEngine;

namespace DaftAppleGames.VehicleEnhancements_BZ
{
    internal enum EnhancedVehicle
    {
        Seatruck,
        PrawnSuit,
        Snowfox
    }

    internal static class VehicleMotion
    {
        internal static bool TryGet(EnhancedVehicle vehicle, out Transform vehicleTransform, out Vector3 velocity)
        {
            vehicleTransform = null;
            velocity = Vector3.zero;

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
