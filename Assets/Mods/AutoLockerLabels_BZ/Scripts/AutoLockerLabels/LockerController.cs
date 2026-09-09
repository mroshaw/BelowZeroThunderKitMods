using UnityEngine;
using UnityEngine.UI;
using static DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabelsPlugin;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    /// <summary>
    /// Handles automatic labels on standard lockers
    /// </summary>
    public class LockerController : MonoBehaviour, IConstructable
    {
        private StorageContainer storageContainer;
        private PrefabIdentifier prefabIdentifier;
        private Constructable constructable;
        private ColoredLabel coloredLabel;
        private Toggle automaticToggle;
        private Button colorSelectorButton;
        private uGUI_InputField labelInput;
        private ItemsContainer itemsContainer;
        private bool isAutomatic;
        private bool savesCustomLabel;
        private string lastGeneratedLabel;
        private string lockerId;

        internal bool IsAutomatic => isAutomatic;

        private void Start()
        {
            ModDebugLog.LogDebug($"LockerController.Start entered for '{gameObject.name}'.");
            storageContainer = GetComponent<StorageContainer>();
            constructable = GetComponent<Constructable>();

            GameObject lockerRoot = storageContainer && storageContainer.prefabRoot
                ? storageContainer.prefabRoot
                : gameObject;
            TechType techType = CraftData.GetTechType(lockerRoot);

            coloredLabel = lockerRoot.GetComponentInChildren<ColoredLabel>(true);
            prefabIdentifier = lockerRoot.GetComponent<PrefabIdentifier>();

            AutoToggle autoToggle = lockerRoot.GetComponentInChildren<AutoToggle>(true);
            automaticToggle = autoToggle == null ? null : autoToggle.Toggle;
            
            if (!IsValidLocker())
            {
                ModDebugLog.LogError($"LockerController disabled for '{gameObject.name}' because its required locker or label components are invalid.");
                enabled = false;
                return;
            }

            labelInput = coloredLabel.signInput.inputField;
            itemsContainer = storageContainer.container;
            savesCustomLabel = CraftData.GetTechType(storageContainer.gameObject) == TechType.Locker;

            if (savesCustomLabel)
            {
                Transform colorSelectorTransform = coloredLabel.signInput.transform.Find("ColorSelector");
                colorSelectorButton = colorSelectorTransform
                    ? colorSelectorTransform.GetComponent<Button>()
                    : null;
            }
           
            lockerId = prefabIdentifier.Id;
            isAutomatic = AutoLockerLabelsPlugin.SaveData.IsAutomatic(lockerId);
            UpdateLabelEditability();
            ApplySavedCustomLabelColor();

            // For a SmallStorage locker, shift the ColorSelector to the left
            if (techType == TechType.SmallStorage)
            {
                RectTransform colorSelectorTransform =
                    coloredLabel.signInput.transform.Find("ColorSelector") as RectTransform;
                RectTransform inputFieldTransform = labelInput.transform as RectTransform;
                MoveSmallStorageUiElement(colorSelectorTransform, "ColorSelector", -40.0f, 0.0f);
                // MoveSmallStorageUiElement(inputFieldTransform, "InputField", 20.0f, -25.0f);
            }

            // Subscribe to toggle state change to toggle automatic
            automaticToggle.SetIsOnWithoutNotify(isAutomatic);
            automaticToggle.onValueChanged.AddListener(SetAutomatic);
            ModDebugLog.LogDebug($"LockerController subscribed to AutoToggle.onValueChanged for '{gameObject.name}', initial state={isAutomatic}.");

            if (savesCustomLabel)
            {
                labelInput.onEndEdit.AddListener(OnCustomLabelEdited);

                if (colorSelectorButton)
                {
                    colorSelectorButton.onClick.AddListener(OnCustomLabelColorChanged);
                }
            }
            
            itemsContainer.onAddItem += OnContentsChanged;
            itemsContainer.onRemoveItem += OnContentsChanged;
            CategoryService.CategoriesChanged += OnCategoriesChanged;

            if (isAutomatic)
            {
                ApplyAutomaticLabel();
            }
            else
            {
                ApplySavedCustomLabel();
            }
        }

        private void MoveSmallStorageUiElement(
            RectTransform controlRectTransform,
            string gameObjectName,
            float xDelta,
            float yDelta)
        {
            if (controlRectTransform)
            {
                ModDebugLog.LogDebug($"Moving {gameObjectName} control...");
                Vector2 position = controlRectTransform.anchoredPosition;
                position.x += xDelta;
                position.y += yDelta;
                controlRectTransform.anchoredPosition = position;
            }
        }

        private void OnDestroy()
        {
            CategoryService.CategoriesChanged -= OnCategoriesChanged;

            if (constructable &&
                constructable.constructedAmount <= 0f &&
                !string.IsNullOrWhiteSpace(lockerId))
            {
                AutoLockerLabelsPlugin.SaveData.RemoveLocker(lockerId);
            }

            if (automaticToggle)
            {
                automaticToggle.onValueChanged.RemoveListener(SetAutomatic);
            }

            if (savesCustomLabel && labelInput)
            {
                labelInput.onEndEdit.RemoveListener(OnCustomLabelEdited);
            }

            if (colorSelectorButton)
            {
                colorSelectorButton.onClick.RemoveListener(OnCustomLabelColorChanged);
            }
            
            if (itemsContainer is null)
            {
                return;
            }

            itemsContainer.onAddItem -= OnContentsChanged;
            itemsContainer.onRemoveItem -= OnContentsChanged;
        }
        private void OnCategoriesChanged()
        {
            if (isAutomatic)
            {
                lastGeneratedLabel = null;
                ApplyAutomaticLabel();
            }
        }

        private bool IsValidLocker()
        {
            return storageContainer && storageContainer.container != null &&
                   coloredLabel &&
                   coloredLabel.signInput != null &&
                   coloredLabel.signInput.inputField != null &&
                   prefabIdentifier && automaticToggle;
        }

        private void OnContentsChanged(InventoryItem item)
        {
            if (isAutomatic)
            {
                ApplyAutomaticLabel();
            }
        }

        private void SetAutomatic(bool state)
        {
            ModDebugLog.LogDebug($"LockerController.SetAutomatic invoked for '{gameObject.name}' with state={state}.");
            if (state)
            {
                EnableAutomatic();
            }
            else
            {
                DisableAutomatic();
            }
        }

        private void OnCustomLabelEdited(string label)
        {
            if (!isAutomatic)
            {
                AutoLockerLabelsPlugin.SaveData.SetCustomLabel(lockerId, label);
            }
        }

        private void OnCustomLabelColorChanged()
        {
            AutoLockerLabelsPlugin.SaveData.SetCustomLabelColor(
                lockerId,
                coloredLabel.signInput.colorIndex);
        }
        
        private void EnableAutomatic()
        {
            if (string.IsNullOrWhiteSpace(lockerId))
            {
                return;
            }

            if (savesCustomLabel)
            {
                AutoLockerLabelsPlugin.SaveData.SetCustomLabel(lockerId, coloredLabel.signInput.text);
            }

            AutoLockerLabelsPlugin.SaveData.EnableAutomatic(lockerId);
            isAutomatic = true;
            ModDebugLog.LogDebug($"Automatic labelling enabled for '{gameObject.name}' with locker ID '{lockerId}'.");
            UpdateLabelEditability();
            ApplyAutomaticLabel();
        }

        private void DisableAutomatic()
        {
            if (string.IsNullOrWhiteSpace(lockerId))
            {
                return;
            }

            AutoLockerLabelsPlugin.SaveData.DisableAutomatic(lockerId);
            isAutomatic = false;
            ModDebugLog.LogDebug($"Automatic labelling disabled for '{gameObject.name}' with locker ID '{lockerId}'.");
            lastGeneratedLabel = null;
            UpdateLabelEditability();
            ApplySavedCustomLabel();
        }

        private void UpdateLabelEditability()
        {
            labelInput.readOnly = isAutomatic;
        }

        private void ApplySavedCustomLabel()
        {
            if (savesCustomLabel && AutoLockerLabelsPlugin.SaveData.TryGetCustomLabel(lockerId, out string customLabel))
            {
                coloredLabel.signInput.text = customLabel;
            }
        }

        private void ApplySavedCustomLabelColor()
        {
            if (savesCustomLabel &&
                AutoLockerLabelsPlugin.SaveData.TryGetCustomLabelColor(lockerId, out int colorIndex))
            {
                coloredLabel.signInput.colorIndex = colorIndex;
            }
        }

        private void ApplyAutomaticLabel()
        {
            string newGeneratedLabel = LabelGenerator.Generate(itemsContainer).ToUpper();

            if (lastGeneratedLabel == newGeneratedLabel)
            {
                return;
            }

            coloredLabel.signInput.text = newGeneratedLabel;
            lastGeneratedLabel = newGeneratedLabel;
        }

        /// <summary>
        /// Shows the label when constructed and hides it while being deconstructed
        /// </summary>
        void IConstructable.OnConstructedChanged(bool constructed)
        {
            if (!coloredLabel)
            {
                coloredLabel = GetComponentInChildren<ColoredLabel>(true);
            }

            if (coloredLabel)
            {
                coloredLabel.gameObject.SetActive(constructed);
            }
        }

        bool IObstacle.IsDeconstructionObstacle()
        {
            return true;
        }

        bool IObstacle.CanDeconstruct(out string reason)
        {
            reason = null;
            return true;
        }
    }
}
