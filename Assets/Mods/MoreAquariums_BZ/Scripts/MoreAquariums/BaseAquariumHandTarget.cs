using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Forwards interaction from base-piece geometry to its aquarium storage.
    /// </summary>
    public class BaseAquariumHandTarget : MonoBehaviour, IHandTarget
    {
        private StorageContainer storageContainer;
        private bool hoverLogged;

        /// <summary>
        /// Assigns the storage container that receives player interaction.
        /// </summary>
        internal void Initialize(StorageContainer targetStorageContainer)
        {
            storageContainer = targetStorageContainer;
        }

        /// <summary>
        /// Displays the native aquarium storage hover prompt.
        /// </summary>
        public void OnHandHover(GUIHand hand)
        {
            if (!storageContainer)
            {
                ModDebugLog.LogError(
                    $"Observatory Aquarium hand target on '{gameObject.name}' has no storage container.");
                return;
            }

            if (!hoverLogged)
            {
                ModDebugLog.LogDebug(
                    $"Player targeted Observatory Aquarium interaction on '{gameObject.name}'.");
                hoverLogged = true;
            }

            storageContainer.OnHandHover(hand);
        }

        /// <summary>
        /// Opens the native aquarium storage when clicked by the player.
        /// </summary>
        public void OnHandClick(GUIHand hand)
        {
            if (!storageContainer)
            {
                ModDebugLog.LogError(
                    $"Cannot open Observatory Aquarium '{gameObject.name}': storage is missing.");
                return;
            }

            ModDebugLog.LogDebug(
                $"Player clicked Observatory Aquarium interaction on '{gameObject.name}'.");
            storageContainer.OnHandClick(hand);
        }
    }
}
