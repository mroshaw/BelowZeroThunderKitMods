using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    ///     Simple MonoBehaviour class to manage Pet DNA collectible behaviour
    /// </summary>
    internal class PetDna : MonoBehaviour
    {
        private void Start()
        {
            KeepUpright();
        }

        /// <summary>
        ///     Preserve the spawn slot's yaw while removing pitch and roll that could rotate
        ///     the model below terrain around its bottom-centre pivot.
        /// </summary>
        private void KeepUpright()
        {
            float yaw = transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);
        }
    }
}
