using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DaftAppleGames.ModTools.Extensions;
using DaftAppleGames.SubnauticaPets.Pets;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.BaseParts
{
    /// <summary>
    ///     Component to manage the Pet Console UI functionality
    ///     Events should be subscribed to by PetConsole
    /// </summary>
    internal class PetConsole : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private GameObject activeScreen;
        [SerializeField] private GameObject inactiveScreen;

        [Header("UI Settings")] [SerializeField] private GameObject petsScrollViewContent;
        [SerializeField] private Button killAllButton;
        [SerializeField] private Button killAllConfirmButton;
        [SerializeField] private Button killButton;
        [SerializeField] private Button killConfirmButton;
        [SerializeField] private Button renameButton;
        [SerializeField] private Button syncButton;
        [SerializeField] private TMP_InputField petNameTextInput;
        [SerializeField] private TMP_Text versionText;

        [Header("Scroll View Settings")] [SerializeField] private GameObject babySnowStalkerTemplate;
        [SerializeField] private GameObject babyPenlingTemplate;
        [SerializeField] private GameObject adultPengwingTemplate;
        [SerializeField] private GameObject pinnacaridTemplate;
        [SerializeField] private GameObject blueTrivalveTemplate;
        [SerializeField] private GameObject yellowTrivalveTemplate;

        [SerializeField] private GameObject catTemplate;
        [SerializeField] private GameObject dogTemplate;
        [SerializeField] private GameObject rabbitTemplate;
        [SerializeField] private GameObject foxTemplate;
        [SerializeField] private GameObject sealTemplate;
        [SerializeField] private GameObject walrusTemplate;
        private readonly string _confirmButtonText = "";

        private FMOD_CustomEmitter _alertEmitter;
        private List<ConsoleScrollViewEntry> _allScrollViewEntries;
        private bool _hasPower = true;
        private bool _inKillCountdown;

        private bool _isConstructed;
        private int _numPetsManaged;
        private string _petNameText = "";
        private PowerConsumer _powerConsumer;
        private bool _powerStateReady;
        private FMOD_CustomEmitter _renameEmitter;

        private Pet _selectedPet;

        // This is the base root of the base n which the console was created
        internal Base Base { get; set; }

        internal string BaseId
        {
            get
            {
                if (Base != null) return Base.GetComponent<PrefabIdentifier>().Id;

                return "NO BASE!";
            }
        }

        private void Awake()
        {
            _powerConsumer = GetComponent<PowerConsumer>();
        }

        private void Start()
        {
            if (transform.parent == null)
                // We're probably in the prefab, so return.
                return;

            UpdateVersionText();
            SetPetButtonsInteractable();
            SetEmitters();
            SetParentBaseObject();
            StartCoroutine(InitializePowerStateAsync());
            // StartCoroutine(CleanupAsync(2.0f));
        }

        /// <summary>
        ///     Continue to check for loss of power and set the state appropriately
        /// </summary>
        private void Update()
        {
            if (!_powerStateReady) return;

            // Check for loss / restoration of power
            bool isPowered = _powerConsumer.IsPowered();
            if (_hasPower != isPowered)
            {
                _hasPower = isPowered;
                ConstructedOrPowerStateChanged();
            }
        }

        private IEnumerator InitializePowerStateAsync()
        {
            if (!_powerConsumer)
            {
                ModDebugLog.LogError("PetConsole has no PowerConsumer component!");
                yield break;
            }

            while (isActiveAndEnabled && !_powerConsumer.GetBaseComp()) yield return null;
            if (!isActiveAndEnabled) yield break;

            _hasPower = _powerConsumer.IsPowered();
            _powerStateReady = true;
            ConstructedOrPowerStateChanged();
            ModDebugLog.LogDebug($"PetConsole initial power state: {_hasPower}");
        }

        // Enable listeners
        private void OnEnable()
        {
            // Add listeners to controls
            renameButton.onClick.AddListener(RenameButtonHandler);
            killButton.onClick.AddListener(KillButtonHandler);
            killAllButton.onClick.AddListener(KillAllButtonHandler);
            killConfirmButton.onClick.AddListener(KillConfirmButtonHandler);
            killAllConfirmButton.onClick.AddListener(KillAllConfirmButtonHandler);
            syncButton.onClick.AddListener(UpdatePetList);
            
            petNameTextInput.onValueChanged.AddListener(RenameTextChangedHandler);

            // Refresh the pet list
            StartCoroutine(UpdatePetListAsync());
            
            // Listen for any changes to Pets list
            SubnauticaPetsPlugin.PetSaver.PetListUpdatedEvent.AddListener(UpdatePetList);
        }

        // Remove listeners
        private void OnDisable()
        {
            // Remove listeners to controls
            renameButton.onClick.RemoveListener(RenameButtonHandler);
            killButton.onClick.RemoveListener(KillButtonHandler);
            killAllButton.onClick.RemoveListener(KillAllButtonHandler);
            killAllConfirmButton.onClick.RemoveListener(KillAllConfirmButtonHandler);
            syncButton.onClick.RemoveListener(UpdatePetList);
            
            petNameTextInput.onValueChanged.RemoveListener(RenameTextChangedHandler);
            
            // Remove Pet Saver listeners
            SubnauticaPetsPlugin.PetSaver.PetListUpdatedEvent.RemoveListener(UpdatePetList);
        }
        
        /// <summary>
        ///     Finds the FMOD Emitters created during prefab configuration
        /// </summary>
        private void SetEmitters()
        {
            var alertEmitterGo = gameObject.transform.Find("AlertEmitter").gameObject;
            _alertEmitter = alertEmitterGo.GetComponent<FMOD_CustomEmitter>();

            var renameEmitterGo = gameObject.transform.Find("RenameEmitter").gameObject;
            _renameEmitter = renameEmitterGo.GetComponent<FMOD_CustomEmitter>();
        }

        private void UpdateVersionText()
        {
            versionText.text = $"v{VersionString}";
        }

        private void ConstructedOrPowerStateChanged()
        {
            activeScreen.SetActive(_isConstructed && _hasPower);
            inactiveScreen.SetActive(_isConstructed && !_hasPower);
        }

        internal void SetConstructedState(bool newState)
        {
            _isConstructed = newState;
            ConstructedOrPowerStateChanged();
        }

        private void SetParentBaseObject()
        {
            Base = GetComponentInParent<Base>();
            if (Base)
                ModDebugLog.LogDebug($"PetConsole Start in Base: {Base.gameObject.name}");
            else
                ModDebugLog.LogDebug("PetConsole Start: Base not found in parent!");
        }

        /// <summary>
        ///     Set the Kill and Rename button interactable state
        /// </summary>
        private void SetPetButtonsInteractable()
        {
            renameButton.interactable = _selectedPet != null && _petNameText.Length > 0 && !_inKillCountdown;
            killButton.interactable = _selectedPet != null && !_inKillCountdown;
            killAllButton.interactable = !_inKillCountdown && _numPetsManaged > 0;
        }

        private void PlayAlert()
        {
            if (!_alertEmitter)
            {
                ModDebugLog.LogDebug("PetConsole alert emitter is null!");
                return;
            }

            _alertEmitter.Play();
        }

        private void PlayRename()
        {
            if (!_renameEmitter)
            {
                ModDebugLog.LogDebug("PetConsole rename emitter is null!");
                return;
            }

            _renameEmitter.Play();
        }

        /// <summary>
        ///     Refresh the PetList UI when pets are added or removed
        /// </summary>
        internal void OnPetsChangedHandler()
        {
            UpdatePetList();
        }

        /// <summary>
        ///     Proxy to the KillAllClickedEvent
        /// </summary>
        private void KillAllButtonHandler()
        {
            PlayAlert();
            StartCoroutine(CountDownButton(killAllConfirmButton.gameObject, killAllButton.gameObject, 5));
        }

        /// <summary>
        ///     Proxy to ConfirmKillAllClickedEvent
        /// </summary>
        private void KillAllConfirmButtonHandler()
        {
            _inKillCountdown = false;
            killAllConfirmButton.gameObject.SetActive(false);
            killAllButton.gameObject.SetActive(true);
            killAllConfirmButton.GetComponentInChildren<TextMeshProUGUI>().text = _confirmButtonText;

            // Iterate over all pets and kill those in this base
            foreach (var currPet in SubnauticaPetsPlugin.PetSaver.PetList.ToArray())
                // Check to see if the Pet is in the same Base as the Console
                if (IsPetInConsoleBase(currPet))
                    currPet.Kill();

            _selectedPet = null;
            SetPetButtonsInteractable();
        }

        private void KillConfirmButtonHandler()
        {
            _inKillCountdown = false;
            killConfirmButton.gameObject.SetActive(false);
            killButton.gameObject.SetActive(true);
            killConfirmButton.GetComponentInChildren<TextMeshProUGUI>().text = _confirmButtonText;

            // ModDebugLog.LogDebug("Kill Button Clicked!");
            if (_selectedPet != null)
            {
                _selectedPet.Kill();
                _selectedPet = null;
            }

            SetPetButtonsInteractable();
        }

        /// <summary>
        ///     Proxy to the KillClickedEvent
        /// </summary>
        private void KillButtonHandler()
        {
            PlayAlert();
            StartCoroutine(CountDownButton(killConfirmButton.gameObject, killButton.gameObject, 5));
        }

        /// <summary>
        /// Cleans up the PictureFrame component, that can be re-added by the UWE Serializer
        /// </summary>
        private IEnumerator CleanupAsync(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.transform.parent.gameObject.DestroyComponentsInChildren<PictureFrame>();
        }
        
        /// <summary>
        ///     Proxy to the RenameClickedEvent
        /// </summary>
        private void RenameButtonHandler()
        {
            if (_selectedPet != null)
            {
                PlayRename();

                _selectedPet.PetName = _petNameText;

                // Tell the Saver to refresh all consoles
                SubnauticaPetsPlugin.PetSaver.ForceRefresh();

                _selectedPet = null;

                // Set the button states
                SetPetButtonsInteractable();
            }
        }

        /// <summary>
        ///     Proxy to the Name Text changed event
        /// </summary>
        private void RenameTextChangedHandler(string nameText)
        {
            // Local name, for changing existing pets
            _petNameText = nameText;

            SetPetButtonsInteractable();
        }

        /// <summary>
        ///     Proxy to Pet List selection
        /// </summary>
        private void PetSelectedProxy(Pet pet)
        {
            ModDebugLog.LogDebug($"Selected Pet Changed: {pet.PetName}");
            _selectedPet = pet;

            // Set the Kill and Rename buttons to interactable
            SetPetButtonsInteractable();
        }

        /// <summary>
        ///     Update PetList once the plugin static has been initialised
        /// </summary>
        private IEnumerator UpdatePetListAsync()
        {
            while (isActiveAndEnabled &&
                   (SubnauticaPetsPlugin.PetSaver == null ||
                    SubnauticaPetsPlugin.PetSaver.PetList == null ||
                    !Base))
                yield return null;

            if (!isActiveAndEnabled) yield break;

            UpdatePetList();
        }

        /// <summary>
        ///     Create the Pet List controls
        /// </summary>
        internal void UpdatePetList()
        {
            // Clear the current UI objects
            ModDebugLog.LogDebug("CreatePetList: Clearing existing buttons...");
            if (_allScrollViewEntries != null)
                foreach (var scrollListEntry in _allScrollViewEntries)
                    Destroy(scrollListEntry.gameObject);

            // Recreate the list of pet buttons
            _allScrollViewEntries = new List<ConsoleScrollViewEntry>();
            var currPetIndex = 0;

            // Check the PetList
            if (SubnauticaPetsPlugin.PetSaver.PetList == null)
            {
                ModDebugLog.LogDebug("PetConsoleUi: The PetList is null, and cannot be sorted.");
                return;
            }

            // Sort by name
            var sortedPetList = SubnauticaPetsPlugin.PetSaver.PetList.OrderBy(pet => pet.PetName).ToList();

            ModDebugLog.LogDebug($"PetConsoleUi: Sorted list into {sortedPetList.Count} pets.");

            // Iterate over all pets and add a button
            foreach (var currPet in sortedPetList)
                // Check to see if the Pet is in the same Base as the Console
                if (IsPetInConsoleBase(currPet))
                {
                    // Get new instance of button template, based on type of pet
                    var newButtonGameObject = GetScrollListInstance(currPet.TechType,
                        currPet.PetNameString, currPetIndex);

                    if (!newButtonGameObject) continue;

                    var scrollViewEntry = newButtonGameObject.GetComponent<ConsoleScrollViewEntry>();

                    // Add button click listeners
                    scrollViewEntry.Button.onClick.AddListener(delegate { PetSelectedProxy(currPet); });
                    scrollViewEntry.Button.onClick.AddListener(delegate { UpdateSelected(scrollViewEntry); });
                    newButtonGameObject.SetActive(true);

                    _allScrollViewEntries.Add(scrollViewEntry);
                    currPetIndex++;
                }

            if (currPetIndex == 0 && sortedPetList.Count > 0)
            {
                ModDebugLog.LogDebug($"PetConsoleUi: No pets matched Console base '{BaseId}'.");
                for (int petIndex = 0; petIndex < sortedPetList.Count; petIndex++)
                {
                    Pet pet = sortedPetList[petIndex];
                    ModDebugLog.LogDebug($"PetConsoleUi: Pet '{pet.PetName}' has base '{pet.BaseId}'.");
                }
            }

            // Enable Kill All if there are any pets
            _numPetsManaged = currPetIndex;
            SetPetButtonsInteractable();
        }

        private bool IsPetInConsoleBase(Pet pet)
        {
            if (!pet || !pet.Base || !Base) return false;
            if (pet.Base == Base) return true;

            string consoleBaseId = BaseId;
            return consoleBaseId != "NO BASE!" && consoleBaseId == pet.BaseId;
        }

        private GameObject GetScrollListInstance(TechType petTechType, string petName, int indexNum)
        {
            return GetScrollListInstance(petTechType.ToString(), petName, indexNum);
        }

        private GameObject GetScrollListInstance(string petType, string petName, int indexNum)
        {
            var templatePrefab = GetScrollListTemplate(petType);
            if (!templatePrefab)
            {
                ModDebugLog.LogError($"GetScrollListInstance: No template is configured for pet type '{petType}'.");
                return null;
            }

            var newButtonObject = Instantiate(templatePrefab, petsScrollViewContent.transform);
            newButtonObject.GetComponentInChildren<TextMeshProUGUI>(true).SetText(petName);
            newButtonObject.name = $"ListButton{indexNum.ToString()}-{petName}-{petType}";

            return newButtonObject;
        }

        /// <summary>
        ///     Retrieve the appropriate template Game Object for the scroll list
        /// </summary>
        private GameObject GetScrollListTemplate(string petType)
        {
            switch (petType)
            {
                case "PenglingBabyPet":
                    return babyPenlingTemplate;
                case "PengwingAdultPet":
                    return adultPengwingTemplate;
                case "PinnacaridPet":
                    return pinnacaridTemplate;
                case "SnowstalkerBabyPet":
                    return babySnowStalkerTemplate;
                case "TrivalveBluePet":
                    return blueTrivalveTemplate;
                case "TrivalveYellowPet":
                    return yellowTrivalveTemplate;
                case "CatPet":
                    return catTemplate;
                case "DogPet":
                    return dogTemplate;
                case "RabbitPet":
                    return rabbitTemplate;
                case "FoxPet":
                    return foxTemplate;
                case "SealPet":
                    return sealTemplate;
                case "WalrusPet":
                    return walrusTemplate;
                default:
                    ModDebugLog.LogError($"GetScrollListTemplate: Unknown pet type: {petType}");
                    return null;
            }
        }

        /// <summary>
        ///     Highlights the selected Pet game object button
        /// </summary>
        private void UpdateSelected(ConsoleScrollViewEntry selected)
        {
            // Reset all backgrounds
            foreach (var scrollViewEntry in _allScrollViewEntries)
                // scrollViewEntry.SetBackgroundColor(Color.cyan);
                scrollViewEntry.SetTextColor(Color.white);

            // Set background on selected
            // selected.SetBackgroundColor(Color.blue);
            selected.SetTextColor(Color.red);
        }


        /// <summary>
        ///     Handles the "count down" button, allowing the user to effectively cancel their choice to kill pets
        /// </summary>
        private IEnumerator CountDownButton(GameObject objectToHide, GameObject objectToShow, int delayInSeconds)
        {
            _inKillCountdown = true;
            SetPetButtonsInteractable();
            objectToHide.SetActive(true);
            objectToShow.SetActive(false);

            var countDownLabel = objectToHide.GetComponentInChildren<TextMeshProUGUI>();
            var labelText = countDownLabel.text;

            var counter = delayInSeconds;
            while (counter > 0)
            {
                countDownLabel.text = $"{labelText} {counter}";
                yield return new WaitForSeconds(1);
                counter--;
            }

            countDownLabel.text = labelText;
            objectToHide.SetActive(false);
            objectToShow.SetActive(true);

            _inKillCountdown = false;
            SetPetButtonsInteractable();
        }
    }
}
