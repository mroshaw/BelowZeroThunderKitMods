using System.Collections.Generic;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    ///     Redirects pets away from a Moonpool opening without physically blocking other objects.
    /// </summary>
    internal class MoonpoolPetTrigger : MonoBehaviour
    {
        private const float ExitClearance = 0.75f;

        private readonly HashSet<int> petsInside = new HashSet<int>();
        private BoxCollider triggerCollider;

        internal void Init(BoxCollider sourceCollider)
        {
            triggerCollider = sourceCollider;
        }

        private void Awake()
        {
            if (!triggerCollider) triggerCollider = GetComponent<BoxCollider>();
        }

        private void OnDisable()
        {
            petsInside.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            Pet pet = other.GetComponentInParent<Pet>();
            if (!pet || !petsInside.Add(pet.GetInstanceID())) return;

            MoonpoolPetEvader evader = pet.gameObject.GetComponent<MoonpoolPetEvader>();
            if (!evader) evader = pet.gameObject.AddComponent<MoonpoolPetEvader>();

            Vector3 safePosition = GetNearestSafePosition(pet.transform.position);
            if (!evader.Redirect(safePosition))
            {
                Debug.LogWarning($"[SubnauticaPets] Moonpool trigger detected {pet.gameObject.name}, but no " +
                                 "supported custom or vanilla movement component was found.");
                return;
            }

            Debug.Log($"[SubnauticaPets] Moonpool trigger redirecting {pet.gameObject.name} from " +
                      $"{pet.transform.position} to {safePosition}.");
        }

        private void OnTriggerExit(Collider other)
        {
            Pet pet = other.GetComponentInParent<Pet>();
            if (!pet || !petsInside.Remove(pet.GetInstanceID())) return;

            MoonpoolPetEvader evader = pet.GetComponent<MoonpoolPetEvader>();
            if (evader) evader.StopRedirecting();
        }

        private Vector3 GetNearestSafePosition(Vector3 petPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(petPosition);
            Vector3 center = triggerCollider.center;
            Vector3 extents = triggerCollider.size * 0.5f;

            float distanceToXEdge = extents.x - Mathf.Abs(localPosition.x - center.x);
            float distanceToZEdge = extents.z - Mathf.Abs(localPosition.z - center.z);

            if (distanceToXEdge < distanceToZEdge)
            {
                float direction = localPosition.x < center.x ? -1.0f : 1.0f;
                localPosition.x = center.x + direction * (extents.x + ExitClearance);
            }
            else
            {
                float direction = localPosition.z < center.z ? -1.0f : 1.0f;
                localPosition.z = center.z + direction * (extents.z + ExitClearance);
            }

            return transform.TransformPoint(localPosition);
        }
    }
}
