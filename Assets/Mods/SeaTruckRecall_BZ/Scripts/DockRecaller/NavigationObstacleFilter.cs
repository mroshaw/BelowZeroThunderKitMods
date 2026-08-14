using UnityEngine;

namespace DaftAppleGames.SeaTruckRecall_BZ.DockRecaller
{
    /// <summary>
    /// Identifies colliders that should not influence SeaTruck navigation decisions.
    /// </summary>
    internal static class NavigationObstacleFilter
    {
        /// <summary>
        /// Determines whether a collider belongs to the player.
        /// </summary>
        internal static bool IsPlayerCollider(Collider collider, GameObject entityRoot = null)
        {
            if (!collider || !Player.main)
            {
                return false;
            }

            Transform playerTransform = Player.main.transform;
            if (collider.transform == playerTransform || collider.transform.IsChildOf(playerTransform))
            {
                return true;
            }

            return entityRoot == Player.main.gameObject;
        }
    }
}
