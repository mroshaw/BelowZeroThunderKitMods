using DaftAppleGames.SubnauticaPets.Pets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DaftAppleGames.SubnauticaPets.Utils;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace DaftAppleGames.SubnauticaPets.BaseParts
{
    /// <summary>
    /// Component to manage the Pet Console UI functionality
    /// Events should be subscribed to by PetConsole
    /// </summary>
    internal class PetConsole : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject activeScreen;
        [SerializeField] private GameObject inactiveScreen;
        
        [Header("UI Settings")]
        [SerializeField] private GameObject petsScrollViewContent;
        [SerializeField] private Button killAllButton;
        [SerializeField] private Button killAllConfirmButton;
        [SerializeField] private Button killButton;
        [SerializeField] private Button killConfirmButton;
        [SerializeField] private Button renameButton;
        [SerializeField] private TMP_InputField petNameTextInput;
        [SerializeField] private TMP_Text versionText;

        [Header("Scroll View Settings")]
        [SerializeField] private GameObject babySnowStalkerTemplate;
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
        
        // This is the base root of the base n which the console was created
        internal Base Base { get; set; }

        internal string BaseId
        {
            get
            {
                if (Base != null)
                {
                    return Base.GetComponent<PrefabIdentifier>().Id;
                }
                else
                {
                    return "NO BASE!";
                }
            }
        }

        private FMOD_CustomEmitter _alertEmitter;
        private FMOD_CustomEmitter _renameEmitter;
        private PowerConsumer _powerConsumer;
        
        private Pet _selectedPet;
        private string _petNameText = "";
        private string _confirmButtonText = "";
        private List<ConsoleScrollViewEntry> _allScrollViewEntries;
        private bool _inKillCountdown;
        private bool _hasPower = true;
        private int _numPetsManaged;

        private bool _isConstructed;
        
        private void Awake()
        {
            _powerConsumer = GetComponent<PowerConsumer>();
        }

        private void Start()
        {
            if (transform.parent == null)
            {
                // We're probably in the prefab, so return.
                return;
            }

            UpdateVersionText();
            SetPetButtonsInteractable();
            SetParentBaseObject();
            StartCoroutine(UpdatePetListAsync());
            SetEmitters();
            
            // Set initial screen state
            _hasPower = _powerConsumer.IsPowered();
            
            // Clean up, as the UWE serializer has a habit of adding stuff back in when loading a save
            Invoke(nameof(CleanUp), 5.0f);
        }

        /// <summary>
        /// Enable listeners
        /// </summary>
        private void OnEnable()
        {
            // Add listeners to controls
            renameButton.onClick.AddListener(RenameButtonHandler);
            killButton.onClick.AddListener(KillButtonHandler);
            killAllButton.onClick.AddListener(KillAllButtonHandler);
            killConfirmButton.onClick.AddListener(KillConfirmButtonHandler);
            killAllConfirmButton.onClick.AddListener(KillAllConfirmButtonHandler);
            petNameTextInput.onValueChanged.AddListener(RenameTextChangedHandler);
        }

        // Remove listeners
        private void OnDisable()
        {
            // Remove Pet Saver listeners
            SubnauticaPetsPlugin.PetSaver.PetListUpdatedEvent.RemoveListener(PetListUpdatedHandler);

            // Remove listeners to controls
            renameButton.onClick.RemoveListener(RenameButtonHandler);
            killButton.onClick.RemoveListener(KillButtonHandler);
            killAllButton.onClick.RemoveListener(KillAllButtonHandler);
            killAllConfirmButton.onClick.RemoveListener(KillAllConfirmButtonHandler);
            petNameTextInput.onValueChanged.RemoveListener(RenameTextChangedHandler);
        }

        /// <summary>
        /// Continue to check for loss of power and set the state appropriately
        /// </summary>
        private void Update()
        {
            // Check for loss / restoration of power
            if (_hasPower == _powerConsumer.IsPowered())
            {
                return;
            }
            
            if (_hasPower && !_powerConsumer.IsPowered())
            {
                _hasPower = false;
                ConstructedOrPowerStateChanged();
            }

            if (!_hasPower && _powerConsumer.IsPowered())
            {
                _hasPower = true;
                ConstructedOrPowerStateChanged();
            }
        }

        private void CleanUp()
        {
            // Listen for changes to the Pet List
            SubnauticaPetsPlugin.PetSaver.PetListUpdatedEvent.AddListener(PetListUpdatedHandler);
        }
        
        /// <summary>
        /// Finds the FMOD Emitters created during prefab configuration
        /// </summary>
        private void SetEmitters()
        {
            GameObject alertEmitterGo = gameObject.transform.Find("AlertEmitter").gameObject;
            _alertEmitter = alertEmitterGo.GetComponent<FMOD_CustomEmitter>();
            
            GameObject renameEmitterGo = gameObject.transform.Find("RenameEmitter").gameObject;
            _renameEmitter = renameEmitterGo.GetComponent<FMOD_CustomEmitter>();
        }
        
        private void UpdateVersionText()
        {
            versionText.text = $"v{SubnauticaPetsPlugin.VersionString}";
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
            // Get the BasePart transform
            Base = transform.parent.parent.GetComponent<Base>();
            if (Base)
            {
                LogUtils.LogDebug(LogArea.MonoBaseParts, $"PetConsole Start in Base: {Base.gameObject.name}");
            }
            else
            {
                LogUtils.LogDebug(LogArea.MonoBaseParts, $"PetConsole Start: Base not found in parent!");
            }
        }
        
        /// <summary>
        /// Set the Kill and Rename button interactable state
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
                LogUtils.LogDebug(LogArea.MonoBaseParts, $"PetConsole alert emitter is null!");
                return;
            }
            _alertEmitter.Play();
        }

        private void PlayRename()
        {
            if (!_renameEmitter)
            {
                LogUtils.LogDebug(LogArea.MonoBaseParts, $"PetConsole rename emitter is null!");
                return;
            }
            _renameEmitter.Play();

        }
        
        /// <summary>
        /// Refresh the PetList UI when pets are added or removed
        /// </summary>
        internal void OnPetsChangedHandler()
        {
            UpdatePetList();
        }

        /// <summary>
        /// Proxy to the KillAllClickedEvent
        /// </summary>
        private void KillAllButtonHandler()
        {
            PlayAlert();
            StartCoroutine(CountDownButton(killAllConfirmButton.gameObject, killAllButton.gameObject, 5));
        }

        /// <summary>
        /// Proxy to ConfirmKillAllClickedEvent
        /// </summary>
        private void KillAllConfirmButtonHandler()
        {
            _inKillCountdown = false;
            killAllConfirmButton.gameObject.SetActive(false);
            killAllButton.gameObject.SetActive(true);
            killAllConfirmButton.GetComponentInChildren<TextMeshProUGUI>().text = _confirmButtonText;

            // Iterate over all pets and kill those in this base
            foreach (Pet currPet in SubnauticaPetsPlugin.PetSaver.PetList.ToArray())
            {
                // Check to see if the Pet is in the same Base as the Console
                if (currPet.Base == Base)
                {
                    currPet.Kill();
                }
            }

            _selectedPet = null;
            SetPetButtonsInteractable();
        }

        private void KillConfirmButtonHandler()
        {
            _inKillCountdown = false;
            killConfirmButton.gameObject.SetActive(false);
            killButton.gameObject.SetActive(true);
            killConfirmButton.GetComponentInChildren<TextMeshProUGUI>().text = _confirmButtonText;

            // LogUtils.LogDebug(LogArea.MonoBaseParts, "Kill Button Clicked!");
            if (_selectedPet != null)
            {
                _selectedPet.Kill();
                _selectedPet = null;
            }
            
            SetPetButtonsInteractable();
        }

        /// <summary>
        /// Proxy to the KillClickedEvent
        /// </summary>
        private void KillButtonHandler()
        {
            PlayAlert();
            StartCoroutine(CountDownButton(killConfirmButton.gameObject, killButton.gameObject, 5));
        }

        /// <summary>
        /// Proxy to the RenameClickedEvent
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
        /// Proxy to the Name Text changed event
        /// </summary>
        private void RenameTextChangedHandler(string nameText)
        {
            // Local name, for changing existing pets
            _petNameText = nameText;

            SetPetButtonsInteractable();
        }

        /// <summary>
        /// Proxy to Pet List selection
        /// </summary>
        private void PetSelectedProxy(Pet pet)
        {
            LogUtils.LogDebug(LogArea.MonoBaseParts, $"Selected Pet Changed: {pet.PetName}");
            _selectedPet = pet;

            // Set the Kill and Rename buttons to interactable
            SetPetButtonsInteractable();
        }

        /// <summary>
        /// Update PetList once the plugin static has been initialised
        /// </summary>
        private IEnumerator UpdatePetListAsync()
        {
            while (SubnauticaPetsPlugin.PetSaver.PetList == null)
            {
                yield return null;
            }

            UpdatePetList();
        }

        private void PetListUpdatedHandler()
        {
            UpdatePetList();
        }

        /// <summary>
        /// Create the Pet List controls
        /// </summary>
        internal void UpdatePetList()
        {
            // Get button background
            Sprite backgroundSprite = CustomAssetBundleUtils.GetObjectFromAssetBundle<Sprite>(UiUtils.CustomButtonTexture) as Sprite;

            // Clear the current UI objects
            LogUtils.LogDebug(LogArea.MonoBaseParts, "CreatePetList: Clearing existing buttons...");
            if (_allScrollViewEntries != null)
            {
                foreach (ConsoleScrollViewEntry scrollListEntry in _allScrollViewEntries)
                {
                    Destroy(scrollListEntry.gameObject);
                }
            }

            // Recreate the list of pet buttons
            _allScrollViewEntries = new List<ConsoleScrollViewEntry>();
            int currPetIndex = 0;

            // Check the PetList
            if (SubnauticaPetsPlugin.PetSaver.PetList == null)
            {
                LogUtils.LogDebug(LogArea.MonoBaseParts, $"PetConsoleUi: The PetList is null, and cannot be sorted.");
                return;
            }

            // Sort by name
            List<Pet> sortedPetList = SubnauticaPetsPlugin.PetSaver.PetList.OrderBy(pet => pet.PetName).ToList();

            LogUtils.LogDebug(LogArea.MonoBaseParts, $"PetConsoleUi: Sorted list into {sortedPetList.Count} pets.");

            // Iterate over all pets and add a button
            foreach (Pet currPet in sortedPetList)
            {
                // Check to see if the Pet is in the same Base as the Console
                if (currPet.Base == Base)
                {
                    // Get new instance of button template, based on type of pet
                    GameObject newButtonGameObject = GetScrollListInstance(currPet.TechType,
                        currPet.PetNameString, currPetIndex);

                    if (!newButtonGameObject)
                    {
                        continue;
                    }
                    
                    ConsoleScrollViewEntry scrollViewEntry = newButtonGameObject.GetComponent<ConsoleScrollViewEntry>();

                    // Add button click listeners
                    scrollViewEntry.Button.onClick.AddListener(delegate { PetSelectedProxy(currPet); });
                    scrollViewEntry.Button.onClick.AddListener(delegate { UpdateSelected(scrollViewEntry); });                    
                    newButtonGameObject.SetActive(true);
                    
                    _allScrollViewEntries.Add(scrollViewEntry);
                    currPetIndex++;
                }

            }

            // Enable Kill All if there are any pets
            _numPetsManaged = sortedPetList.Count;
            SetPetButtonsInteractable();
        }
        
        private GameObject GetScrollListInstance(TechType petTechType, string petName, int indexNum)
        {
            return GetScrollListInstance(petTechType.ToString(), petName, indexNum);
        }
        private GameObject GetScrollListInstance(string petType, string petName, int indexNum)
        {
            GameObject templatePrefab = GetScrollListTemplate(petType);
            
            GameObject newButtonObject = Instantiate(templatePrefab, petsScrollViewContent.transform);
            newButtonObject.GetComponentInChildren<TextMeshProUGUI>(true).SetText(petName, true);
            newButtonObject.name = $"ListButton{indexNum.ToString()}-{petName}-{petType}";

            return newButtonObject;
        }
        
        /// <summary>
        /// Retrieve the appropriate template Game Object for the scroll list
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
                    LogUtils.LogError(LogArea.MonoBaseParts, $"GetScrollListTemplate: Unknown pet type: {petType}");
                    return null;
            }
        }
        
        /// <summary>
        /// Highlights the selected Pet game object button
        /// </summary>
        private void UpdateSelected(ConsoleScrollViewEntry selected)
        {
            // Reset all backgrounds
            foreach (ConsoleScrollViewEntry scrollViewEntry in _allScrollViewEntries)
            {
                // scrollViewEntry.SetBackgroundColor(Color.cyan);
                scrollViewEntry.SetTextColor(Color.white);
            }

            // Set background on selected
            // selected.SetBackgroundColor(Color.blue);
            selected.SetTextColor(Color.red);
        }


        /// <summary>
        /// Handles the "count down" button, allowing the user to effectively cancel their choice to kill pets
        /// </summary>
        private IEnumerator CountDownButton(GameObject objectToHide, GameObject objectToShow, int delayInSeconds)
        {
            _inKillCountdown = true;
            SetPetButtonsInteractable();
            objectToHide.SetActive(true);
            objectToShow.SetActive(false);

            TextMeshProUGUI countDownLabel = objectToHide.GetComponentInChildren<TextMeshProUGUI>();
            string labelText = countDownLabel.text;

            int counter = delayInSeconds;
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