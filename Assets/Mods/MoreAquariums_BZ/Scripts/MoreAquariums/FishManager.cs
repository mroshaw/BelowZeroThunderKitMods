using System.Collections.Generic;
using UnityEngine;
using static DaftAppleGames.MoreAquariums.MoreAquariumsPlugin;

namespace DaftAppleGames.MoreAquariums
{
    public class FishManager : MonoBehaviour
    {
        [SerializeField] private FishSettings fishSettings;
        [SerializeField] private List<Collider> movementColliders;
        [SerializeField] private List<AquariumFishExt> fishList = new List<AquariumFishExt>();
        
        internal List<AquariumFishExt> FishList => fishList;
        
        private int cullingFrameOffset;
        private int cullingFrameInterval;
        
        private bool _isCulled;
        
        /// <summary>
        /// Get and configure all child fish objects
        /// </summary>
        private void Awake()
        {
            // Derives a small, varied offset for each Fish Manager
            // to avoid all managers checking culling in the same frame
            cullingFrameOffset = (GetInstanceID() & int.MaxValue) % cullingFrameInterval;
        }

        /// <summary>
        /// Public setter for fishSettings
        /// </summary>
        internal void SetFishSettings(FishSettings newFishSettings)
        {
            fishSettings = newFishSettings;
            cullingFrameInterval = Mathf.Max(1, fishSettings.cullingFrameInterval);
        }

        /// <summary>
        /// Public setter for movementColliders
        /// </summary>
        internal void SetMovementColliders(List<Collider> newMovementColliders)
        {
            movementColliders = newMovementColliders;
        }
        
        /// <summary>
        /// Adds procedural movement to an occupied aquarium track.
        /// </summary>
        internal void AddFish(Aquarium.FishTrack fishTrack)
        {
            if (fishTrack == null || !fishTrack.track || !fishTrack.track.transform.parent)
            {
                ModDebugLog.LogError(
                    "Cannot add custom fish movement because the fish track is invalid.");
                return;
            }

            GameObject trackObject = fishTrack.track.transform.parent.gameObject;
            AquariumFishExt fishMovement = trackObject.GetComponent<AquariumFishExt>();
            if (!fishMovement)
            {
                fishMovement = trackObject.AddComponent<AquariumFishExt>();
                fishMovement.Initialize(this, fishSettings, movementColliders);
                fishList.Add(fishMovement);
            }
            else if (!fishList.Contains(fishMovement))
            {
                fishMovement.Initialize(this, fishSettings, movementColliders);
                fishList.Add(fishMovement);
            }

            fishMovement.ActivateMovement();

            if (_isCulled)
            {
                trackObject.SetActive(false);
            }
        }

        /// <summary>
        /// Disables procedural movement on a vacated aquarium track.
        /// </summary>
        internal void RemoveFish(Aquarium.FishTrack fishTrack)
        {
            if (fishTrack == null || !fishTrack.track || !fishTrack.track.transform.parent)
            {
                return;
            }

            AquariumFishExt fishMovement =
                fishTrack.track.transform.parent.GetComponent<AquariumFishExt>();
            if (fishMovement)
            {
                fishMovement.DeactivateMovement();
            }
        }
        
        /// <summary>
        /// Cull / enable fish based on player distance to avoid unnecessary overhead
        /// </summary>
        private void Update()
        {
            if (!fishSettings ||
                !fishSettings.culling ||
                !Player.main ||
                (Time.frameCount + cullingFrameOffset) % cullingFrameInterval != 0)
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
            foreach (AquariumFishExt fish in fishList)
            {
                if (fish)
                {
                    fish.gameObject.SetActive(state);
                }
            }
        }
    }
}
