using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    ///     Adapts Moonpool avoidance to either custom Pet movement or vanilla creature movement.
    /// </summary>
    internal class MoonpoolPetEvader : MonoBehaviour
    {
        private const float VanillaRedirectInterval = 0.2f;
        private const float VanillaRedirectSpeed = 2.0f;

        private PetStateController stateController;
        private SwimBehaviour vanillaMovement;
        private Vector3 safePosition;
        private float redirectTimer;
        private bool redirectingVanillaPet;

        private void Awake()
        {
            stateController = GetComponent<PetStateController>();
            if (stateController) return;

            WalkBehaviour walkBehaviour = GetComponent<WalkBehaviour>();
            if (walkBehaviour)
            {
                vanillaMovement = walkBehaviour;
                return;
            }

            SwimRandom swimRandom = GetComponent<SwimRandom>();
            if (swimRandom && swimRandom.swimBehaviour)
            {
                vanillaMovement = swimRandom.swimBehaviour;
                return;
            }

            vanillaMovement = GetComponent<SwimBehaviour>();
        }

        private void Update()
        {
            if (!redirectingVanillaPet || !vanillaMovement) return;

            redirectTimer -= Time.deltaTime;
            if (redirectTimer > 0.0f) return;

            redirectTimer = VanillaRedirectInterval;
            vanillaMovement.SwimTo(safePosition, VanillaRedirectSpeed);
        }

        internal bool Redirect(Vector3 targetPosition)
        {
            if (stateController)
            {
                stateController.AvoidMoonpool(targetPosition);
                return true;
            }

            if (!vanillaMovement) return false;

            safePosition = targetPosition;
            redirectTimer = 0.0f;
            redirectingVanillaPet = true;
            return true;
        }

        internal void StopRedirecting()
        {
            redirectingVanillaPet = false;
        }
    }
}
