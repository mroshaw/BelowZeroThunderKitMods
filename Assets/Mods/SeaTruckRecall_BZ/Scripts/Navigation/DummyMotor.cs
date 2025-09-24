using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.Navigation
{
    internal class DummyMotor : MonoBehaviour
    {
        [SerializeField] internal Rigidbody useRigidbody;
        [SerializeField] internal float steeringMultiplier =  0.03f;
        [SerializeField] internal float pilotingDrag = 5f;
        [SerializeField] internal float acceleration = 45f;
        internal void UpdateDrag()
        {
            useRigidbody.drag = pilotingDrag;
        }

        internal float GetWeight()
        {
            return 10.0f;
        }

        internal void StabilizeRoll()
        {
        }
    }
}