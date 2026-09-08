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
        private Constructable constructable;
        private ColoredLabel coloredLabel;
        private Toggle automaticToggle;
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
            coloredLabel = GetComponentInChildren<ColoredLabel>(true);
            LogComponentDiagnostics();
            
            if (!IsValidLocker())
            {
                ModDebugLog.LogError($"LockerController disabled for '{gameObject.name}' because its required locker or label components are invalid.");
                enabled = false;
                return;
            }

            PrefabIdentifier prefabIdentifier = storageContainer.GetComponent<PrefabIdentifier>();
            if (prefabIdentifier == null)
            {
                ModDebugLog.LogError($"LockerController disabled for '{gameObject.name}' because no PrefabIdentifier was found.");
                enabled = false;
                return;
            }

            AutoToggle autoToggle = GetComponentInChildren<AutoToggle>(true);
            automaticToggle = autoToggle == null ? null : autoToggle.Toggle;
            labelInput = coloredLabel.signInput.inputField;
            itemsContainer = storageContainer.container;
            savesCustomLabel = CraftData.GetTechType(storageContainer.gameObject) == TechType.Locker;
           
            lockerId = prefabIdentifier.Id;
            isAutomatic = AutoLockerLabelsPlugin.SaveData.IsAutomatic(lockerId);
            UpdateLabelEditability();

            // Subscribe to toggle state change to toggle automatic
            if (automaticToggle != null)
            {
                automaticToggle.SetIsOnWithoutNotify(isAutomatic);
                automaticToggle.onValueChanged.AddListener(SetAutomatic);
                ModDebugLog.LogDebug($"LockerController subscribed to AutoToggle.onValueChanged for '{gameObject.name}', initial state={isAutomatic}.");
            }
            else
            {
                ModDebugLog.LogError($"LockerController could not subscribe to AutoToggle.onValueChanged for '{gameObject.name}'.");
            }

            if (savesCustomLabel)
            {
                labelInput.onEndEdit.AddListener(OnCustomLabelEdited);
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

        private void OnDestroy()
        {
            CategoryService.CategoriesChanged -= OnCategoriesChanged;

            if (constructable != null &&
                constructable.constructedAmount <= 0f &&
                !string.IsNullOrWhiteSpace(lockerId))
            {
                AutoLockerLabelsPlugin.SaveData.RemoveLocker(lockerId);
            }

            if (automaticToggle != null)
            {
                automaticToggle.onValueChanged.RemoveListener(SetAutomatic);
            }

            if (savesCustomLabel && labelInput != null)
            {
                labelInput.onEndEdit.RemoveListener(OnCustomLabelEdited);
            }
            
            if (itemsContainer is null)
            {
                return;
            }

            itemsContainer.onAddItem -= OnContentsChanged;
            itemsContainer.onRemoveItem -= OnContentsChanged;
        }

        private void LogComponentDiagnostics()
        {
            if (!DetailedLoggingEnabled)
            {
                return;
            }

            ModDebugLog.LogDebug($"LockerController components for '{gameObject.name}': StorageContainer={storageContainer != null}, ItemsContainer={storageContainer != null && storageContainer.container != null}, Constructable={constructable != null}, ColoredLabel={coloredLabel != null}, SignInput={coloredLabel != null && coloredLabel.signInput != null}, InputField={coloredLabel != null && coloredLabel.signInput != null && coloredLabel.signInput.inputField != null}.");
            foreach (ColoredLabel candidate in GetComponentsInChildren<ColoredLabel>(true))
            {
                ModDebugLog.LogDebug($"Found ColoredLabel on: {candidate.gameObject.name}");
            }
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
            return storageContainer != null &&
                   storageContainer.container != null &&
                   coloredLabel != null &&
                   coloredLabel.signInput != null &&
                   coloredLabel.signInput.inputField != null;
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
            if (coloredLabel == null)
            {
                coloredLabel = GetComponentInChildren<ColoredLabel>(true);
            }

            if (coloredLabel != null)
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
