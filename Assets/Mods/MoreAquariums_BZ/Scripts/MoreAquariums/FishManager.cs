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
        internal List<AquariumFishPlus> ActiveFishList => _activeFishList;
        
        private readonly List<AquariumFishPlus> _fishList = new List<AquariumFishPlus>();
        private readonly List<AquariumFishPlus> _activeFishList = new List<AquariumFishPlus>();
        private bool _isCulled;
        
        /// <summary>
        /// Get and configure all child fish objects
        /// </summary>
        private void Awake()
        {
            ModDebugLog.LogDebug("FishManager is refreshing attached fish...");
            // Refresh attached fish
            foreach (AquariumFishPlus fish in GetComponentsInChildren<AquariumFishPlus>(true))
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
        /// <param name="newFishPlus"></param>
        internal void AddActiveFish(AquariumFishPlus newFishPlus)
        {
            if (!_fishList.Contains(newFishPlus))
            {
                _fishList.Add(newFishPlus);
            }

            if (!_activeFishList.Contains(newFishPlus))
            {
                _activeFishList.Add(newFishPlus);
            }
            
        }

        /// <summary>
        /// Remove a fish from the manager
        /// </summary>
        internal void RemoveActiveFish(AquariumFishPlus fishPlusToRemove)
        {
            if (_fishList.Contains(fishPlusToRemove))
            {
                _activeFishList.Remove(fishPlusToRemove);
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
            foreach (AquariumFishPlus fish in _fishList)
            {
                fish.gameObject.SetActive(state);
            }
        }
    }
}