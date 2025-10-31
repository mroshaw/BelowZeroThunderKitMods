using System.Collections.Generic;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    public class FishManager : MonoBehaviour
    {
        [SerializeField] private FishSettings fishSettings;
        [SerializeField] private List<Collider> movementColliders;

        internal FishSettings FishSettings => fishSettings;
        internal List<Collider> MovementColliders => movementColliders;
        internal List<AquariumFishExt> ActiveFishList => _activeFishList;
        
        private readonly List<AquariumFishExt> _fishList = new List<AquariumFishExt>();
        private readonly List<AquariumFishExt> _activeFishList = new List<AquariumFishExt>();
        private bool _isCulled;
        
        /// <summary>
        /// Get and configure all child fish objects
        /// </summary>
        private void Awake()
        {
            ModDebugLog.LogDebug("FishManager is refreshing attached fish...");
            // Refresh attached fish
            foreach (AquariumFishExt fish in GetComponentsInChildren<AquariumFishExt>(true))
            {
                ModDebugLog.LogDebug($"Updating {fish.name}...");
                fish.SetFishManager(this);
            }
        }

        /// <summary>
        /// Public setter for fishSettings
        /// </summary>
        internal void SetFishSettings(FishSettings newFishSettings)
        {
            fishSettings = newFishSettings;
        }

        /// <summary>
        /// Public setter for movementColliders
        /// </summary>
        internal void SetMovementColliders(List<Collider> newMovementColliders)
        {
            movementColliders = newMovementColliders;
        }
        
        /// <summary>
        /// Add a new fish to the manager
        /// </summary>
        /// <param name="newFishExt"></param>
        internal void AddActiveFish(AquariumFishExt newFishExt)
        {
            if (!_fishList.Contains(newFishExt))
            {
                _fishList.Add(newFishExt);
            }

            if (!_activeFishList.Contains(newFishExt))
            {
                _activeFishList.Add(newFishExt);
            }
            
        }

        /// <summary>
        /// Remove a fish from the manager
        /// </summary>
        internal void RemoveActiveFish(AquariumFishExt fishExtToRemove)
        {
            if (_fishList.Contains(fishExtToRemove))
            {
                _activeFishList.Remove(fishExtToRemove);
            }
        }

        /// <summary>
        /// Cull / enable fish based on player distance to avoid unnecessary overhead
        /// </summary>
        private void Update()
        {
            if (!fishSettings.culling)
            {
                return;
            }

            float distanceFromPlayer = Vector3.Distance(transform.position, Player.main.transform.position);
            if (distanceFromPlayer < fishSettings.cullingDistanceFromPlayer && _isCulled)
            {
                ModDebugLog.LogDebug("FishManager: Enabling fish...");
                _isCulled = false;
                SetFishActiveState(true);
            }

            if (distanceFromPlayer >= fishSettings.cullingDistanceFromPlayer && !_isCulled)
            {
                ModDebugLog.LogDebug("FishManager: Disabling fish...");
                _isCulled = true;
                SetFishActiveState(false);
            }
        }

        /// <summary>
        /// Sets the active state of all fish in the manager
        /// </summary>
        private void SetFishActiveState(bool state)
        {
            foreach (AquariumFishExt fish in _fishList)
            {
                fish.gameObject.SetActive(state);
            }
        }
    }
}