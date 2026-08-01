using System.Collections.Generic;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    ///     Makes the Moonpool blocker solid for Pets while allowing every other physical collider through.
    /// </summary>
    internal class MoonpoolPetCollisionFilter : MonoBehaviour
    {
        private readonly HashSet<Collider> ignoredColliders = new HashSet<Collider>();
        private BoxCollider blockerCollider;
        private BoxCollider filterTrigger;

        internal void Init(BoxCollider moonpoolBlocker, BoxCollider moonpoolFilterTrigger)
        {
            blockerCollider = moonpoolBlocker;
            filterTrigger = moonpoolFilterTrigger;
        }

        /// <summary>
        ///     Configures collision pairs for objects already near the Moonpool before the blocker is expanded.
        /// </summary>
        internal void PrimeExistingOverlaps()
        {
            if (!blockerCollider || !filterTrigger) return;

            Physics.SyncTransforms();
            Vector3 halfExtents = Vector3.Scale(filterTrigger.size * 0.5f,
                GetAbsoluteVector(filterTrigger.transform.lossyScale));
            Collider[] overlaps = Physics.OverlapBox(filterTrigger.bounds.center, halfExtents,
                filterTrigger.transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int overlapIndex = 0; overlapIndex < overlaps.Length; overlapIndex++)
                IgnoreForNonPet(overlaps[overlapIndex]);
        }

        private void OnTriggerEnter(Collider other)
        {
            IgnoreForNonPet(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other || !ignoredColliders.Remove(other) || !blockerCollider) return;

            Physics.IgnoreCollision(blockerCollider, other, false);
        }

        private void OnDisable()
        {
            RestoreIgnoredCollisions();
        }

        private void OnDestroy()
        {
            RestoreIgnoredCollisions();
        }

        private void IgnoreForNonPet(Collider other)
        {
            if (!other || other == blockerCollider || other == filterTrigger || other.isTrigger) return;
            if (other.GetComponentInParent<Pet>()) return;
            if (!blockerCollider || Physics.GetIgnoreCollision(blockerCollider, other)) return;

            Physics.IgnoreCollision(blockerCollider, other, true);
            ignoredColliders.Add(other);
            Debug.Log($"[SubnauticaPets] Moonpool blocker ignoring non-Pet collider '{other.name}' " +
                      $"({other.GetType().Name}, root={other.transform.root.name}, " +
                      $"layer={LayerMask.LayerToName(other.gameObject.layer)}).");
        }

        private void RestoreIgnoredCollisions()
        {
            if (!blockerCollider)
            {
                ignoredColliders.Clear();
                return;
            }

            foreach (Collider ignoredCollider in ignoredColliders)
            {
                if (ignoredCollider) Physics.IgnoreCollision(blockerCollider, ignoredCollider, false);
            }

            ignoredColliders.Clear();
        }

        private static Vector3 GetAbsoluteVector(Vector3 vector)
        {
            return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
        }
    }
}
