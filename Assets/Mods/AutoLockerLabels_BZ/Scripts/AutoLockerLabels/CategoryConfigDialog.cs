using System;
using System.Collections.Generic;
using System.Text;
using DaftAppleGames.ModTools.Extensions;
using Nautilus.Handlers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabelsPlugin;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    /// <summary>
    /// Edits automatic locker label categories using an unapplied working copy.
    /// </summary>
    public sealed class CategoryConfigDialog : MonoBehaviour
    {
        private const string MainMenuOptionsPanelPath = "Panel/Options";
        private const string InGameMenuOptionsPanelPath = "Options";

        [SerializeField] private GameObject mainPanel;
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private Transform categoryContent;
        [SerializeField] private Transform techTypeContent;
        [SerializeField] private CategoryListEntry categoryEntryPrefab;
        [SerializeField] private TechTypeListEntry techTypeEntryPrefab;
        [SerializeField] private TMP_InputField categoryNameInput;
        [SerializeField] private TMP_Text categoryStatusText;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button moveUpButton;
        [SerializeField] private Button moveDownButton;
        [SerializeField] private Button addTechTypeButton;
        [SerializeField] private GameObject pickerPanel;
        [SerializeField] private TMP_InputField pickerSearchInput;
        [SerializeField] private Toggle pickerCustomOnlyToggle;
        [SerializeField] private Transform pickerContent;
        [SerializeField] private TechTypePickerEntry pickerEntryPrefab;
        [SerializeField] private TMP_Text pickerEmptyText;
        [SerializeField] private Button pickerAddButton;

        private readonly List<CategoryListEntry> categoryEntries = new List<CategoryListEntry>();
        private readonly List<TechTypeListEntry> techTypeEntries = new List<TechTypeListEntry>();
        private readonly List<TechTypePickerEntry> pickerEntries = new List<TechTypePickerEntry>();
        private readonly List<TechType> availableTechTypes = new List<TechType>();
        private readonly List<TechType> candidateTechTypes = new List<TechType>();
        private readonly Dictionary<TechType, TechTypeDisplayData> pickerDisplayData =
            new Dictionary<TechType, TechTypeDisplayData>();
        private readonly HashSet<TechType> selectedTechTypes = new HashSet<TechType>();
        private readonly List<TechType> rejectedVanillaTechTypes = new List<TechType>();
        private readonly InventoryTechTypeCatalog inventoryTechTypeCatalog =
            new InventoryTechTypeCatalog();
        private List<EditableCategory> draft;
        private CanvasGroup parentCanvasGroup;
        private int selectedIndex = -1;
        private bool isShowing;
        private Coroutine catalogCoroutine;

        private void Awake()
        {
            versionText.text = $"v{VersionString}";
            Hide();
        }

        internal void Reparent()
        {
            GameObject optionsPanel = GetOptionsPanel();
            if (!optionsPanel)
            {
                ModDebugLog.LogError("Could not find the Options panel for the category dialog.");
                return;
            }

            mainPanel.transform.SetParent(optionsPanel.transform);
            mainPanel.transform.LocalZero();
            parentCanvasGroup = optionsPanel.GetComponentInParent<CanvasGroup>();
        }

        internal void Show()
        {
            if (isShowing)
            {
                return;
            }

            draft = CategoryService.CreateDraft();
            selectedIndex = draft.Count == 0 ? -1 : 0;
            mainPanel.SetActive(true);
            SetParentInteraction(false);
            isShowing = true;
            RefreshAll();
        }

        /// <summary>
        /// Adds a new empty category to the working copy.
        /// </summary>
        public void AddCategory()
        {
            EditableCategory category = new EditableCategory
            {
                Id = "custom-" + Guid.NewGuid().ToString("N"),
                DisplayName = "New Category",
                IsBuiltIn = false,
                IsModified = true
            };
            draft.Add(category);
            selectedIndex = draft.Count - 1;
            RefreshAll();
            categoryNameInput.Select();
        }

        /// <summary>
        /// Removes the selected category from the working copy.
        /// </summary>
        public void RemoveCategory()
        {
            if (selectedIndex < 0 || selectedIndex >= draft.Count)
            {
                return;
            }

            draft.RemoveAt(selectedIndex);
            if (selectedIndex >= draft.Count)
            {
                selectedIndex = draft.Count - 1;
            }

            RefreshAll();
        }

        /// <summary>
        /// Moves the selected category up one position.
        /// </summary>
        public void MoveUp()
        {
            MoveSelected(-1);
        }

        /// <summary>
        /// Moves the selected category down one position.
        /// </summary>
        public void MoveDown()
        {
            MoveSelected(1);
        }

        private void MoveSelected(int offset)
        {
            int destination = selectedIndex + offset;
            if (selectedIndex < 0 || destination < 0 || destination >= draft.Count)
            {
                return;
            }

            EditableCategory category = draft[selectedIndex];
            draft[selectedIndex] = draft[destination];
            draft[destination] = category;
            selectedIndex = destination;
            RefreshAll();
        }

        /// <summary>
        /// Applies an edited name to the selected category.
        /// </summary>
        public void RenameSelectedCategory(string categoryName)
        {
            EditableCategory category = GetSelectedCategory();
            if (category == null || string.IsNullOrWhiteSpace(categoryName))
            {
                RefreshDetails();
                return;
            }

            string normalizedName = categoryName.Trim();
            if (string.Equals(category.DisplayName, normalizedName, StringComparison.Ordinal))
            {
                RefreshDetails();
                return;
            }

            category.DisplayName = normalizedName;
            category.IsModified = true;
            RefreshCategories();
            RefreshDetails();
        }

        private void RemoveTechType(TechType techType)
        {
            EditableCategory category = GetSelectedCategory();
            if (category is null || !category.TechTypes.Remove(techType))
            {
                return;
            }

            category.IsModified = true;
            RefreshTechTypes();
            RefreshCategories();
        }

        /// <summary>
        /// Opens the TechType picker for the selected category.
        /// </summary>
        public void OpenTechTypePicker()
        {
            if (GetSelectedCategory() == null)
            {
                return;
            }

            BuildCandidateTechTypes();
            availableTechTypes.Clear();
            availableTechTypes.AddRange(candidateTechTypes);
            availableTechTypes.Sort(CompareTechTypeNames);
            pickerSearchInput.SetTextWithoutNotify(string.Empty);
            pickerCustomOnlyToggle.SetIsOnWithoutNotify(false);
            selectedTechTypes.Clear();
            pickerAddButton.interactable = false;
            pickerSearchInput.interactable = true;
            pickerPanel.SetActive(true);

            // Prefab inspection is retained for diagnostics, but is too expensive for normal picker use.
            // rejectedVanillaTechTypes.Clear();
            // pickerSearchInput.interactable = false;
            // pickerEmptyText.text = "Checking inventory-compatible items... 0/" + candidateTechTypes.Count;
            // catalogCoroutine = StartCoroutine(inventoryTechTypeCatalog.Filter(
            //     candidateTechTypes,
            //     AcceptInventoryTechType,
            //     CollectRejectedInventoryTechType,
            //     UpdateCatalogProgress,
            //     CompleteInventoryCatalog));

            RefreshTechTypePicker(string.Empty);
            pickerSearchInput.Select();
        }

        /// <summary>
        /// Closes the TechType picker without adding its selection.
        /// </summary>
        public void CloseTechTypePicker()
        {
            if (catalogCoroutine != null)
            {
                StopCoroutine(catalogCoroutine);
                catalogCoroutine = null;
            }

            pickerPanel.SetActive(false);
            ClearEntries(pickerEntries);
            pickerDisplayData.Clear();
            selectedTechTypes.Clear();
            availableTechTypes.Clear();
            candidateTechTypes.Clear();
        }

        /// <summary>
        /// Refreshes the picker after its source filter changes.
        /// </summary>
        public void FilterTechTypePickerBySource(bool customOnly)
        {
            RefreshTechTypePicker(pickerSearchInput.text);
        }

        private void SetTechTypeSelected(TechType techType, bool selected)
        {
            if (selected)
            {
                selectedTechTypes.Add(techType);
            }
            else
            {
                selectedTechTypes.Remove(techType);
            }

            pickerAddButton.interactable = selectedTechTypes.Count > 0;
        }

        /// <summary>
        /// Adds every selected TechType to the current category.
        /// </summary>
        public void AddSelectedTechTypes()
        {
            EditableCategory category = GetSelectedCategory();
            if (category is null || selectedTechTypes.Count == 0)
            {
                return;
            }

            foreach (TechType techType in availableTechTypes)
            {
                if (selectedTechTypes.Contains(techType))
                {
                    category.TechTypes.Add(techType);
                }
            }

            category.IsModified = true;
            CloseTechTypePicker();
            RefreshTechTypes();
            RefreshCategories();
        }

        private void AcceptInventoryTechType(TechType techType)
        {
            availableTechTypes.Add(techType);
        }

        private void CollectRejectedInventoryTechType(TechType techType)
        {
            if (!EnumHandler.TryGetOwnerAssembly(techType, out _))
            {
                rejectedVanillaTechTypes.Add(techType);
            }
        }

        private void UpdateCatalogProgress(int processed, int total)
        {
            pickerEmptyText.text = "Checking inventory-compatible items... " + processed + "/" + total;
        }

        private void CompleteInventoryCatalog()
        {
            catalogCoroutine = null;
            availableTechTypes.Sort(CompareTechTypeNames);
            pickerSearchInput.interactable = true;
            ModDebugLog.LogInfo(
                "TechType pickupable catalogue: candidates=" + candidateTechTypes.Count +
                ", pickupable=" + availableTechTypes.Count +
                ", vanilla non-pickupable=" + rejectedVanillaTechTypes.Count + ".");
            LogVanillaTechTypeExclusionCandidates();
            if (pickerPanel.activeSelf)
            {
                RefreshTechTypePicker(pickerSearchInput.text);
                pickerSearchInput.Select();
            }
        }

        private void LogVanillaTechTypeExclusionCandidates()
        {
            rejectedVanillaTechTypes.Sort(CompareTechTypeIdentifiers);
            StringBuilder output = new StringBuilder();
            output.AppendLine("BEGIN BELOW ZERO VANILLA TECHTYPE EXCLUSIONS");
            foreach (TechType techType in rejectedVanillaTechTypes)
            {
                output.Append("TechType.");
                output.Append(techType);
                output.AppendLine(",");
            }

            output.Append("END BELOW ZERO VANILLA TECHTYPE EXCLUSIONS");
            ModDebugLog.LogInfo(output.ToString());
        }

        private static int CompareTechTypeIdentifiers(TechType first, TechType second)
        {
            return string.Compare(
                first.ToString(),
                second.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        private void BuildCandidateTechTypes()
        {
            candidateTechTypes.Clear();
            pickerDisplayData.Clear();
            EditableCategory category = GetSelectedCategory();
            HashSet<TechType> uniqueTechTypes = new HashSet<TechType>();
            HashSet<TechType> assignedTechTypes = new HashSet<TechType>(category.TechTypes);
            TechType[] values = (TechType[])Enum.GetValues(typeof(TechType));
            int noneCount = 0;
            int duplicateCount = 0;
            int assignedCount = 0;
            int missingClassIdCount = 0;
            int missingNameCount = 0;
            int missingRegisteredIconCount = 0;
            int vanillaExclusionCount = 0;
            int exceptionCount = 0;
            StringBuilder exclusionSamples = DetailedLoggingEnabled ? new StringBuilder() : null;
            foreach (TechType techType in values)
            {
                if (techType == TechType.None)
                {
                    noneCount++;
                    continue;
                }

                if (!uniqueTechTypes.Add(techType))
                {
                    duplicateCount++;
                    continue;
                }

                if (assignedTechTypes.Contains(techType))
                {
                    assignedCount++;
                    continue;
                }

                string exclusionReason;
                if (!IsSelectableTechType(techType, out exclusionReason))
                {
                    if (exclusionReason == "class ID")
                    {
                        missingClassIdCount++;
                    }
                    else if (exclusionReason == "localized name")
                    {
                        missingNameCount++;
                    }
                    else if (exclusionReason == "registered icon")
                    {
                        missingRegisteredIconCount++;
                    }
                    else
                    {
                        exceptionCount++;
                    }

                    if (exclusionSamples != null && exclusionSamples.Length < 1800)
                    {
                        exclusionSamples.Append(techType);
                        exclusionSamples.Append(" [");
                        exclusionSamples.Append(exclusionReason);
                        exclusionSamples.Append("], ");
                    }

                    continue;
                }

                bool isModded = EnumHandler.TryGetOwnerAssembly(techType, out _);
                if (!isModded && VanillaTechTypeExclusions.Contains(techType))
                {
                    vanillaExclusionCount++;
                    continue;
                }

                candidateTechTypes.Add(techType);
                pickerDisplayData.Add(techType, new TechTypeDisplayData(techType));
            }

            if (DetailedLoggingEnabled)
            {
                ModDebugLog.LogDebug(
                    "TechType picker catalogue: enumerated=" + values.Length +
                    ", metadata candidates=" + candidateTechTypes.Count +
                    ", none=" + noneCount +
                    ", duplicates=" + duplicateCount +
                    ", already assigned=" + assignedCount +
                    ", missing class ID=" + missingClassIdCount +
                    ", missing localized name=" + missingNameCount +
                    ", missing registered icon=" + missingRegisteredIconCount +
                    ", known vanilla non-pickupable=" + vanillaExclusionCount +
                    ", exceptions=" + exceptionCount + ".");
                ModDebugLog.LogDebug("TechType picker exclusion samples: " + exclusionSamples);
                LogTechTypeProbe(TechType.Titanium);
                LogTechTypeProbe(TechType.Copper);
                LogTechTypeProbe(TechType.Battery);
            }
        }

        /// <summary>
        /// Filters the TechType picker using item, enum, or source text.
        /// </summary>
        public void RefreshTechTypePicker(string searchText)
        {
            string normalizedSearch = string.IsNullOrWhiteSpace(searchText)
                ? string.Empty
                : searchText.Trim();
            int visibleCount = 0;
            foreach (TechType techType in availableTechTypes)
            {
                TechTypeDisplayData data = pickerDisplayData[techType];
                if (pickerCustomOnlyToggle.isOn && !data.IsModded)
                {
                    continue;
                }

                if (normalizedSearch.Length > 0 &&
                    data.DisplayName.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0 &&
                    data.SourceName.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                TechTypePickerEntry entry = GetOrCreateEntry(pickerEntries, visibleCount, pickerEntryPrefab, pickerContent);
                entry.Bind(data, selectedTechTypes.Contains(techType), SetTechTypeSelected);
                entry.gameObject.SetActive(true);
                visibleCount++;
            }

            HideUnusedEntries(pickerEntries, visibleCount);
            pickerEmptyText.gameObject.SetActive(visibleCount == 0);
        }

        private static bool IsSelectableTechType(TechType techType, out string exclusionReason)
        {
            try
            {
                string classId = CraftData.GetClassIdForTechType(techType);
                if (string.IsNullOrWhiteSpace(classId))
                {
                    exclusionReason = "class ID";
                    return false;
                }

                string localizedName = Language.main == null ? string.Empty : Language.main.Get(techType);
                if (string.IsNullOrWhiteSpace(localizedName))
                {
                    exclusionReason = "localized name";
                    return false;
                }

                Sprite icon = SpriteManager.Get(techType);
                if (icon == null || icon == SpriteManager.defaultSprite)
                {
                    exclusionReason = "registered icon";
                    return false;
                }

                exclusionReason = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                exclusionReason = "exception: " + exception.GetType().Name;
                return false;
            }
        }

        private static void LogTechTypeProbe(TechType techType)
        {
            try
            {
                string techTypeName = techType.ToString();
                string classId = CraftData.GetClassIdForTechType(techType);
                string localizedName = Language.main == null ? string.Empty : Language.main.Get(techType);
                Sprite normalIcon = SpriteManager.Get(techType);
                Sprite registeredIcon = SpriteManager.Get(SpriteManager.Group.None, techTypeName, null);
                ModDebugLog.LogInfo(
                    "TechType picker probe " + techTypeName +
                    ": classId='" + classId +
                    "', localizedName='" + localizedName +
                    "', normalIcon='" + (normalIcon == null ? "null" : normalIcon.name) +
                    "', isDefaultIcon=" + (normalIcon == SpriteManager.defaultSprite) +
                    ", registeredIcon='" + (registeredIcon == null ? "null" : registeredIcon.name) + "'.");
            }
            catch (Exception exception)
            {
                ModDebugLog.LogError("TechType picker probe failed for " + techType + ": " + exception);
            }
        }

        private int CompareTechTypeNames(TechType first, TechType second)
        {
            return string.Compare(
                pickerDisplayData[first].DisplayName,
                pickerDisplayData[second].DisplayName,
                StringComparison.OrdinalIgnoreCase);
        }

        private void SelectCategory(int index)
        {
            selectedIndex = index;
            RefreshDetails();
            RefreshTechTypes();
            UpdateButtons();
        }

        /// <summary>
        /// Applies and saves the current category working copy.
        /// </summary>
        public void Apply()
        {
            RenameSelectedCategory(categoryNameInput.text);

            CategoryService.ApplyDraft(draft);
            draft = CategoryService.CreateDraft();
            selectedIndex = Mathf.Clamp(selectedIndex, 0, draft.Count - 1);
            RefreshAll();
        }

        /// <summary>
        /// Applies the working copy and closes the dialog.
        /// </summary>
        public void Done()
        {
            Apply();
            Hide();
        }

        /// <summary>
        /// Discards the working copy and closes the dialog.
        /// </summary>
        public void Cancel()
        {
            Hide();
        }

        /// <summary>
        /// Replaces the working copy with the built-in categories.
        /// </summary>
        public void RestoreDefaults()
        {
            draft = CategoryService.CreateDefaultDraft();
            selectedIndex = draft.Count == 0 ? -1 : 0;
            RefreshAll();
        }

        /// <summary>
        /// Restores the selected built-in category without changing the other categories.
        /// </summary>
        public void RestoreSelectedCategoryDefault()
        {
            EditableCategory selectedCategory = GetSelectedCategory();
            if (selectedCategory is null)
            {
                return;
            }

            EditableCategory defaultCategory;
            if (!CategoryService.TryCreateDefaultCategory(selectedCategory.Id, out defaultCategory))
            {
                return;
            }

            draft[selectedIndex] = defaultCategory;
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshCategories();
            RefreshDetails();
            RefreshTechTypes();
            UpdateButtons();
        }

        private void RefreshCategories()
        {
            for (int index = 0; index < draft.Count; index++)
            {
                EditableCategory category = draft[index];
                CategoryListEntry entry = GetOrCreateEntry(categoryEntries, index, categoryEntryPrefab, categoryContent);
                entry.Bind(index, category.DisplayName, GetStatus(category), SelectCategory);
                entry.gameObject.SetActive(true);
            }

            HideUnusedEntries(categoryEntries, draft.Count);
        }

        private void RefreshDetails()
        {
            EditableCategory category = GetSelectedCategory();
            categoryNameInput.interactable = category != null;
            categoryNameInput.SetTextWithoutNotify(category == null ? string.Empty : category.DisplayName);
            categoryStatusText.text = category == null ? "No category selected" : GetStatus(category);
        }

        private void RefreshTechTypes()
        {
            EditableCategory category = GetSelectedCategory();
            if (category is null)
            {
                HideUnusedEntries(techTypeEntries, 0);
                return;
            }

            for (int index = 0; index < category.TechTypes.Count; index++)
            {
                TechTypeDisplayData data = new TechTypeDisplayData(category.TechTypes[index]);
                TechTypeListEntry entry = GetOrCreateEntry(techTypeEntries, index, techTypeEntryPrefab, techTypeContent);
                entry.Bind(data, RemoveTechType);
                entry.gameObject.SetActive(true);
            }

            HideUnusedEntries(techTypeEntries, category.TechTypes.Count);
        }

        private void UpdateButtons()
        {
            bool hasSelection = GetSelectedCategory() != null;
            removeButton.interactable = hasSelection;
            moveUpButton.interactable = hasSelection && selectedIndex > 0;
            moveDownButton.interactable = hasSelection && selectedIndex < draft.Count - 1;
            addTechTypeButton.interactable = hasSelection;
        }

        private EditableCategory GetSelectedCategory()
        {
            return selectedIndex >= 0 && selectedIndex < draft.Count ? draft[selectedIndex] : null;
        }

        private static string GetStatus(EditableCategory category)
        {
            if (!category.IsBuiltIn)
            {
                return "CUSTOM";
            }

            return category.IsModified ? "MODIFIED" : "DEFAULT";
        }

        // Reuse prefab rows while editing; release them when their dialog closes.
        private static T GetOrCreateEntry<T>(List<T> entries, int index, T prefab, Transform parent) where T : Component
        {
            if (index == entries.Count)
            {
                entries.Add(Instantiate(prefab, parent));
            }

            return entries[index];
        }

        private static void HideUnusedEntries<T>(List<T> entries, int usedCount) where T : Component
        {
            for (int index = usedCount; index < entries.Count; index++)
            {
                entries[index].gameObject.SetActive(false);
            }
        }

        private static void ClearEntries<T>(List<T> entries) where T : Component
        {
            foreach (T entry in entries)
            {
                // Destroy is deferred; inactive rows must stop participating in layout immediately.
                entry.gameObject.SetActive(false);
                Destroy(entry.gameObject);
            }

            entries.Clear();
        }

        private void Hide()
        {
            CloseTechTypePicker();
            mainPanel.SetActive(false);
            ClearEntries(categoryEntries);
            ClearEntries(techTypeEntries);
            SetParentInteraction(true);
            isShowing = false;
        }

        private void SetParentInteraction(bool state)
        {
            if (parentCanvasGroup != null)
            {
                parentCanvasGroup.interactable = state;
                parentCanvasGroup.blocksRaycasts = state;
            }
        }

        private static GameObject GetOptionsPanel()
        {
            IngameMenu inGameMenu = FindObjectOfType<IngameMenu>();
            if (inGameMenu != null)
            {
                Transform panel = inGameMenu.transform.Find(InGameMenuOptionsPanelPath);
                if (panel != null)
                {
                    return panel.gameObject;
                }
            }

            uGUI_MainMenu mainMenu = FindObjectOfType<uGUI_MainMenu>();
            if (mainMenu != null)
            {
                Transform panel = mainMenu.transform.Find(MainMenuOptionsPanelPath);
                if (panel != null)
                {
                    return panel.gameObject;
                }
            }

            return null;
        }
    }
}
