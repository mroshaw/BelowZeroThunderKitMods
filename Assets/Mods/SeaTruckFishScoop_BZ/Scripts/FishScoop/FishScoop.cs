using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DaftAppleGames.ModUtils;
using UnityEngine;
using static DaftAppleGames.SeaTruckFishScoop_BZ.SeaTruckFishScoopPluginBz;
using Random = UnityEngine.Random;

namespace DaftAppleGames.SeaTruckFishScoop_BZ
{
    internal enum PurgeTarget { BioReactor, Water }
    
    public class FishScoop : MonoBehaviour
    {
        [SerializeField] private Vector3 purgePositionOffset = new Vector3(0.0f, 4.0f, 1.0f);
        [SerializeField] private float purgeVelocity = 2.0f;
        [SerializeField] private float purgeLocationRandomMax = 0.5f;
        [SerializeField] private float bioreactorPurgeRange = 10.0f;
        [SerializeField] private float slotPressedTimeForPurge = 2.0f;         // Number of seconds the slot activation key must be held to purge the aquariums
        [SerializeField] private float audioVolume = 10.0f;
        
        // Static FMODAsset for playing sounds
        private static readonly FMODAsset SoundsToPlay = ScriptableObject.CreateInstance<FMODAsset>();
        private const string FishScoopPowerOnSoundPath = "event:/sub/cyclops/start";
        private const string FishScoopPowerOffSoundPath = "event:/sub/base/power_off";
        private const string AudioBusPath = "bus:/master/SFX_for_pause/PDA_pause/all/SFX/vehicles/SeaTruck";
        
        // Custom sounds
        private const string PurgeAudioAsset = "PurgeSound.wav";
        private const string FishReleasedAudioAsset = "FishReleased.wav";
        
        // Custom Emitter for purge and release sounds
        private FMOD_CustomEmitter _purgeEmitter;
        private FMOD_CustomEmitter _fishReleasedEmitter;

        private SeaTruckUpgrades _SeaTruckUpgrades;

        // Release / purge fish position
        private Vector3 PurgeLocation => _mainMotor.transform.position + purgePositionOffset;
        private Vector3 RandomPurgeLocation => PurgeLocation + (Random.Range(Random.Range(0, purgeLocationRandomMax), purgeLocationRandomMax) * _mainMotor.transform.forward) + (Random.Range(0, purgeLocationRandomMax) * _mainMotor.transform.up);
        private Vector3 PurgeVelocity =>  _mainMotor.transform.up * purgeVelocity;

        // Used to detect nearby BioReactors without generating garbage
        private Collider[] _colliderBuffer = new Collider[50]; // adjust size if needed
        
        internal bool IsOn => _isOn;
        private bool _isOn;
        
        private float _timeSlotPressed;
        private bool _scoopPurging;
        
        // Determines in which slot the scoop is configured
        // -1 means not equipped
        private int _scoopQuickSlotId;
        private SeaTruckMotor _mainMotor;

        private void Awake()
        {
            _SeaTruckUpgrades = GetComponent<SeaTruckUpgrades>();
            if(!_SeaTruckUpgrades)
            {
                ModDebugLog.LogError("FishScoop Awake: Could not find SeaTruckUpgrades!");
            }
            
            _mainMotor = GetComponentInParent<SeaTruckMotor>();
            if(!_mainMotor)
            {
                ModDebugLog.LogError("FishScoop Awake: Could not find SeaTruckMotor!");
            }
        }
        
        /// <summary>
        /// Initialise the scoop
        /// </summary>
        public void Start()
        {
            _isOn = false;
            
            // Set up the purge sound emitter
            _purgeEmitter = gameObject.AddComponent<FMOD_CustomEmitter>();
            _fishReleasedEmitter = gameObject.AddComponent<FMOD_CustomEmitter>();
            ConfigureEmitters();
        }

        // Set up the FMOD emitter for the custom purge sound
        private void ConfigureEmitters()
        {
            ModAudioUtils.ConfigureEmitter(_purgeEmitter, PurgeAudioAsset, AudioBusPath, audioVolume, ModAssetUtils, ModDebugLog);
            ModAudioUtils.ConfigureEmitter(_fishReleasedEmitter, FishReleasedAudioAsset, AudioBusPath, audioVolume, ModAssetUtils, ModDebugLog);
        }
        
        /// <summary>
        /// Record when the Quick Slot was pressed. Use this to determine whether
        /// the player is pressing (toggle) or holding (purge)
        /// </summary>
        internal void QuickSlotPressed(int slotId)
        {
            if (slotId != _scoopQuickSlotId)
            {
                return;
            }
            
            _scoopPurging = false;
            _timeSlotPressed = Time.fixedTime;
        }
        
        internal void QuickSlotReleased(int slotId)
        {
            if (slotId != _scoopQuickSlotId)
            {
                return;
            }
            if (Time.fixedTime < _timeSlotPressed + slotPressedTimeForPurge)
            {
                ToggleScoop();
            }
        }

        /// <summary>
        /// Called every frame while the quick slot button is held down
        /// Use this to call purge once the held time is reached
        /// </summary>
        internal void QuickSlotHeld(int slotId)
        {
            if (slotId != _scoopQuickSlotId)
            {
                return;
            }
            
            if (Time.fixedTime >= _timeSlotPressed + slotPressedTimeForPurge && !_scoopPurging)
            {
                _scoopPurging = true;
                PurgeAquariums();
            }
        }

        /// <summary>
        /// Derives the QuickSlot ID
        /// </summary>
        private void FindQuickSlotId()
        {
            ModDebugLog.LogDebug("Finding quick slot ID...");
            for (int currSlot = 0; currSlot < SeaTruckUpgrades.slotIDs.Length; currSlot++)
            {
                TechType techTypeInSlot = _SeaTruckUpgrades.modules.GetTechTypeInSlot(SeaTruckUpgrades.slotIDs[currSlot]);
                if (techTypeInSlot == FishScoopModulePrefab.PrefabInfo.TechType)
                {
                    _scoopQuickSlotId = currSlot;
                    ModDebugLog.LogDebug($"Found Fish Scoop in {currSlot}");
                    return;
                }
            }
            // Not found
            ModDebugLog.LogDebug($"Could not find Fish Scoop in Quick Slots!");
            _scoopQuickSlotId = -1;
        }
        /// <summary>
        /// Called when the scoop is equipped / moved to a new slot
        /// </summary>
        internal void Equip(int toSlotId)
        {
            _scoopQuickSlotId = toSlotId;
        }

        /// <summary>
        /// Called when the scoop is un-equipped
        /// </summary>
        internal void Unequip(int fromSlotId)
        {
            _scoopQuickSlotId = -1;
            StopScoop();
        }
        
        /// <summary>
        /// Toggle the Fish Scoop on and off
        /// </summary>
        private bool ToggleScoop()
        {
            ModDebugLog.LogDebug($"Toggling fish scoop from: {_isOn}...");

            // If we're not in the SeaTruck, call it a day
            if (!_mainMotor)
            {
                return false;
            }

            // Can only turn on the scoop in the SeaTruck
            if(!_mainMotor.IsPiloted())
            {
                return false;
            }

            // Check if we have any aquarium modules attached
            if (!IsAquariumAttached())
            {
                ShowAlert($"Cannot start scoop, no aquariums attached!");
                return false;
            }

            // Toggle state
            if (_isOn)
            {
                StopScoop();
            }
            else
            {
                StartScoop();
            }

            SetQuickSlotToggleState();
            return _isOn;
        }
        
        /// <summary>
        /// Stop the scoop
        /// </summary>
        private void StopScoop()
        {
            if (_isOn)
            {
                ShowAlert($"Fish Scoop powering down.");
                SoundsToPlay.path = FishScoopPowerOffSoundPath;
                FMODUWE.PlayOneShot(SoundsToPlay, _mainMotor.transform.position);
                _isOn = false;
            }
        }

        /// <summary>
        /// Start the scoop
        /// </summary>
        private void StartScoop()
        {
            if(!_isOn)
            {
                ShowAlert($"Fish Scoop powering up.");
                SoundsToPlay.path = FishScoopPowerOnSoundPath;
                FMODUWE.PlayOneShot(SoundsToPlay, _mainMotor.transform.position);
                _isOn = true;
            }
        }

        /// <summary>
        /// Stop the scoop if we stop piloting, and config is not checked
        /// </summary>
        internal void StopPiloting()
        {
            if (ConfigFile.OnlyScoopWhilePiloting)
            {
                StopScoop();
            }
        }
        
        /// <summary>
        /// Set the state of the QuickSlot occupied by the Fish Scoop
        /// </summary>
        private void SetQuickSlotToggleState()
        {
            ModDebugLog.LogDebug($"Setting quick slot {_scoopQuickSlotId} toggled state to {_isOn}");
            RaiseOnToggle(_SeaTruckUpgrades, _scoopQuickSlotId, _isOn);
        }
        
        /// <summary>
        /// Invoke the onToggle event on SeaTruckUpgrades. In turn this will notify listeners
        /// (including uGUI_QuickSlots) that the state has changed
        /// </summary>
        private static void RaiseOnToggle(SeaTruckUpgrades upgrades, int slotID, bool state)
        {
            FieldInfo evt = typeof(SeaTruckUpgrades).GetField("onToggle",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (evt == null)
            {
                return;
            }
            
            QuickSlots.OnToggle del = evt.GetValue(upgrades) as QuickSlots.OnToggle;
            del?.Invoke(slotID, state);
        }
        
        /// <summary>
        /// Attempt to scoop the given GameObject into an attached aquarium
        /// </summary>
        public bool Scoop(GameObject objectToScoop)
        {
            // Is this thing on?
            if(!_isOn)
            {
                return false;
            }

            // Let's see if what took the damage was a compatible aquarium fish
            if (!IsValidObject(objectToScoop))
            {
                return false;
            }

            // Check if SeaTruck is being piloted and whether or not we're allowed to scoop
            bool isPiloted = _mainMotor.IsPiloted();
            if (!isPiloted && !ConfigFile.OnlyScoopWhilePiloting)
            {
                return false;
            }

            // Check if static against the config options
            float velocityMagnitude = _mainMotor.useRigidbody.velocity.magnitude;
            if ((velocityMagnitude == 0.0f) && !ConfigFile.ScoopWhileStatic)
            {
                 return false;
            }

            // We've passed our checks, now try to add the fish
            ModDebugLog.LogDebug("Taker is a supported fish");
            bool fishAdded = AddFishToFreeAquarium(objectToScoop);
            return fishAdded;
        }

        /// <summary>
        /// Is the object hit valid for inclusion in the Aquarium?
        /// </summary>
        private bool IsValidObject(GameObject takerGameObject)
        {
            ModDebugLog.LogDebug("In IsValidObject");
            if (!takerGameObject.GetComponent<AquariumFish>())
            {
                ModDebugLog.LogDebug("IsValidObject: Not an AquariumFish. No Scoop.");
                return false;
            }
            WaterParkCreature waterParkCreature = takerGameObject.GetComponent<WaterParkCreature>();
            if (waterParkCreature && waterParkCreature.IsInsideWaterPark())
            {
                ModDebugLog.LogDebug("IsValidObject: Target IsInsideWaterPark. No Scoop.");
                return false;
            }
            ModDebugLog.LogDebug("IsValidObject: Target IsInsideWaterPark. No Scoop.");
            return true;
        }

        /// <summary>
        /// Purge all aquariums attached to the main SeaTruck
        /// </summary>
        private void PurgeAquariums()
        {
            // Check if the SeaTruck being piloted is attached to this scoop
            if(!_mainMotor.IsPiloted())
            {
                return;
            }

            // Check if we have any aquarium modules attached
            if (!IsAquariumAttached())
            {
                ModDebugLog.LogDebug($"Couldn't find any Aquariums!");
                ShowAlert($"Cannot purge, no aquariums attached!");
                return;
            }

            // Checks all done, we can purge the modules
            int totalFishInWater = 0;
            int totalFishInBioreactor = 0;
            
            // Check if there are bioreactors in range
            List<BaseBioReactor> bioReactorsInRange = FindNearbyBioReactors();
            PurgeTarget purgeTarget = bioReactorsInRange.Count > 0 ? PurgeTarget.BioReactor : PurgeTarget.Water;
            ModDebugLog.LogDebug($"Attempting to purge to : {purgeTarget}");
            
            // Iterate over attached aquariums
            SeaTruckAquarium[] SeaTruckAquariums = _mainMotor.GetComponentsInChildren<SeaTruckAquarium>();
            ModDebugLog.LogDebug($"Found {SeaTruckAquariums.Length} aquarium modules");
            _purgeEmitter.Play();
            foreach (SeaTruckAquarium SeaTruckAquarium in SeaTruckAquariums)
            {
                (int numFishInWater, int numFixInBioReactor) = PurgeFishFromAquarium(SeaTruckAquarium, purgeTarget, bioReactorsInRange);
                totalFishInWater +=  numFishInWater;
                totalFishInBioreactor +=  numFixInBioReactor;
                ModDebugLog.LogDebug($"Purged aquarium: {SeaTruckAquarium.name}");
            }
            ShowAlert($"All aquariums purged!");
            if (totalFishInWater > 0)
            {
                ShowAlert($"Released {totalFishInWater} fish.");
            }

            if (totalFishInBioreactor > 0)
            {
                ShowAlert($"Moved {totalFishInBioreactor} fish to Bioreactors in range.");
            }
        }

        /// <summary>
        /// Returns true if at least one aquarium module is attached to the SeaTruckMotor
        /// </summary>
        private bool IsAquariumAttached()
        {
            SeaTruckAquarium[] SeaTruckAquariums = _mainMotor.GetComponentsInChildren<SeaTruckAquarium>();
            ModDebugLog.LogDebug($"Found {SeaTruckAquariums.Length} aquarium modules");
            // Check to see if there are any aquariums
            if (SeaTruckAquariums.Length == 0)
            {
                ModDebugLog.LogDebug("No aquariums found.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Checks the state of the Sea Truck if the player has left then entered it again
        /// </summary>
        internal void EvaluateScoopState()
        {
            // Check if the player has disconnected aquariums
            if (!IsAquariumAttached())
            {
                StopScoop();
            }
        }
        
        /// <summary>
        /// Adds the specified fish to an aquarium attached to the SeaTruckMotor
        /// </summary>
        private bool AddFishToFreeAquarium(GameObject fish)
        {
            // We hit a supported fish with our SeaTruck cab. Iterate over all Aquarium modules and add the fish to
            // the first one with space
            SeaTruckAquarium[] SeaTruckAquariums = _mainMotor.GetComponentsInChildren<SeaTruckAquarium>();
            ModDebugLog.LogDebug($"Found {SeaTruckAquariums.Length} aquarium modules");

            // Check to see if there are any aquariums
            if (SeaTruckAquariums.Length == 0)
            {
                ModDebugLog.LogDebug("No aquariums found.");
                return false;
            }

            string friendlyFishName = GetFriendlyName(fish.name);
            
            foreach (SeaTruckAquarium SeaTruckAquarium in SeaTruckAquariums)
            {
                if (AddFishToAquarium(SeaTruckAquarium, fish))
                {
                    ModDebugLog.LogDebug($"Fish successfully added {fish.name} as {friendlyFishName}");
                    ShowAlert($"Scooped {friendlyFishName}!");
                    return true;
                }
                ModDebugLog.LogDebug($"Unable to add fish to this aquarium ({SeaTruckAquarium.name}). Likely full or fish is already in one.");
            }
            
            if (ConfigFile.ReleaseFailedScoopFish)
            {
                ReleaseSingleFish(fish);
                ShowAlert($"Aquariums are full! Releasing {friendlyFishName}!");
                return true;
            }
            ShowAlert("Aquariums are full! Scoop failed!");
            return false;
        }

        /// <summary>
        /// Removes all fish from the specified Aquarium module
        /// </summary>
        private (int numFishInWater, int numFishInBioReactor) PurgeFishFromAquarium(SeaTruckAquarium SeaTruckAquarium, PurgeTarget purgeTarget, List<BaseBioReactor> bioReactors)
        {
            // Get all creatures / aquariumfish
            ItemsContainer container = SeaTruckAquarium.storageContainer.container;

            // Maintain counts
            int numFishInWater = 0;
            int numFishInBioReactor = 0;
            
            // Allows us to amend while iterating
            foreach (InventoryItem fishItem in container.ToList())
            {
                Pickupable fishPickupable = fishItem.item;

                switch (purgeTarget)
                {
                    case PurgeTarget.Water:
                        fishPickupable.Drop(RandomPurgeLocation, PurgeVelocity, false);
                        numFishInWater++;
                        break;
                    
                    case PurgeTarget.BioReactor:
                        ModDebugLog.LogDebug("Attempting to add to BioReactor...");
                        // Try to add to one of the in range bioreactors
                        bool addedToReactor = false;
                        foreach (BaseBioReactor bioReactor in bioReactors)
                        {
                            if (bioReactor._container.HasRoomFor(fishPickupable))
                            {
                                ModDebugLog.LogDebug("Adding to BioReactor...");
                                InventoryItem item = new InventoryItem(fishPickupable);
                                bioReactor._container.UnsafeAdd(item);
                                addedToReactor = true;
                                numFishInBioReactor++;
                                ModDebugLog.LogDebug("Successfully added to BioReactor!");
                                break;
                            }
                        }

                        // If we were unable to add to a bioreactor, drop it in the ocean
                        if (!addedToReactor)
                        {
                            ModDebugLog.LogDebug("All BioReactors in range are full. Purging to ocean...");
                            fishPickupable.Drop(RandomPurgeLocation, PurgeVelocity, false);
                            numFishInWater++;
                        }
                        break;
                }
                


                // Remove from aquarium container
                container.RemoveItem(fishPickupable, true);
                ModDebugLog.LogDebug($"Removed {fishPickupable.name}");
            }
            return (numFishInWater,  numFishInBioReactor);
        }
        
        /// <summary>
        /// Returns a "user friendly" name for the fish caught
        /// </summary>
        private string GetFriendlyName(string fishName)
        {
            return (fishName.Replace("(Clone)", ""));
        }

        /// <summary>
        /// Add our fish to the chosen Aquarium
        /// </summary>
        private static bool AddFishToAquarium(SeaTruckAquarium SeaTruckAquarium, GameObject aquariumFish)
        {
            Pickupable pickupable = aquariumFish.GetComponent<Pickupable>();

            if (SeaTruckAquarium.storageContainer.container.HasRoomFor(pickupable))
            {
                Utils.PlayFMODAsset(SeaTruckAquarium.collectSound, aquariumFish.transform);
                pickupable.Initialize();
                InventoryItem item = new InventoryItem(pickupable);
                SeaTruckAquarium.storageContainer.container.UnsafeAdd(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Eject a fish that couldn't be scooped
        /// </summary>
        private void ReleaseSingleFish(GameObject fishGameObject)
        {
            fishGameObject.GetComponent<LiveMixin>()?.ResetHealth();
            _fishReleasedEmitter.Play();
            fishGameObject.GetComponent<Transform>().position = RandomPurgeLocation;
        }

        /// <summary>
        /// Shows an alert, if the Config is configured for it
        /// </summary>
        private void ShowAlert(string alertMessage)
        {
            if (!ConfigFile.ShowScoopAlerts)
            {
                return;
            }
            ErrorMessage.AddMessage(alertMessage);
        }
        
        /// <summary>
        /// Purges the aquariums to these bio-reactors
        /// </summary>
        private void PurgeToBioreactor(List<BaseBioReactor> bioreactors)
        {
            
        }

        /// <summary>
        /// Finds Bioreactors within range of the Fish Scoop
        /// </summary>
        /// <returns></returns>
        private List<BaseBioReactor> FindNearbyBioReactors()
        {
            List<BaseBioReactor> bioReactorsInRange = new List<BaseBioReactor>();
            BaseBioReactor[] allBioReactors = FindObjectsOfType<BaseBioReactor>();

            foreach (BaseBioReactor bioReactor in allBioReactors)
            {
#if UNITY_EDITOR
                if(Math.Abs(Vector3.Distance(bioReactor.transform.position, transform.position)) < bioreactorPurgeRange)
#else
                if(Math.Abs(Vector3.Distance(bioReactor.transform.position, transform.position)) < ConfigFile.BioreactorRange)
#endif
                {
                    bioReactorsInRange.Add(bioReactor);
                }
            }
            ModDebugLog.LogDebug($"Found {bioReactorsInRange.Count} nearby bio reactors");
            return bioReactorsInRange;
        }
    }
}