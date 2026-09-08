using System;
using DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DaftAppleGames.AutoLockerLabels_BZ.Editor
{
    internal static class CategoryUiPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Mods/AutoLockerLabels_BZ/Prefabs";
        private static readonly Color PanelColor = new Color(0.035f, 0.43f, 0.70f, 0.98f);
        private static readonly Color SectionColor = new Color(0.025f, 0.31f, 0.50f, 0.97f);
        private static readonly Color ButtonColor = new Color(0.035f, 0.55f, 0.82f, 1f);
        private static readonly Color ContentColor = new Color(0.015f, 0.22f, 0.34f, 0.96f);
        private static readonly Color RowColor = new Color(0.025f, 0.40f, 0.61f, 0.95f);
        private static readonly Color AccentColor = new Color(1f, 0.68f, 0.05f, 1f);
        private static Scene previewScene;

        /// <summary>
        /// Rebuilds the serialized category configuration UI prefab assets.
        /// </summary>
        [MenuItem("Daft Apple Games/Auto Locker Labels/Rebuild Category UI Prefabs")]
        public static void Rebuild()
        {
            previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                CategoryListEntry categoryEntry = BuildCategoryEntry();
                TechTypeListEntry techTypeEntry = BuildTechTypeEntry();
                TechTypePickerEntry pickerEntry = BuildTechTypePickerEntry();
                BuildDialog(categoryEntry, techTypeEntry, pickerEntry);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        /// <summary>
        /// Rebuilds only the reusable TechType picker row prefab.
        /// </summary>
        [MenuItem("Daft Apple Games/Auto Locker Labels/Rebuild TechType Picker Row")]
        public static void RebuildTechTypePickerRow()
        {
            previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                BuildTechTypePickerEntry();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        /// <summary>
        /// Converts the existing category dialog prefab to the layout-driven hierarchy without replacing its root.
        /// </summary>
        [MenuItem("Daft Apple Games/Auto Locker Labels/Convert Category UI To Layout Groups")]
        public static void ConvertToLayoutGroups()
        {
            string prefabPath = PrefabFolder + "/CategoryConfigUi.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform panel = root.transform.Find("CategoryConfigPanel");
                if (panel.Find("ContentRow") != null)
                {
                    Debug.Log("Auto Locker Labels category UI already uses layout groups.");
                    return;
                }

                Transform title = panel.Find("Title");
                Transform version = panel.Find("Version");
                Transform help = panel.Find("Help");
                Transform left = panel.Find("CategoryPane");
                Transform right = panel.Find("DetailPane");
                Transform pickerOverlay = panel.Find("TechTypePickerOverlay");

                VerticalLayoutGroup mainLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
                mainLayout.padding = new RectOffset(40, 40, 24, 24);
                mainLayout.spacing = 12f;
                mainLayout.childAlignment = TextAnchor.UpperCenter;
                mainLayout.childControlWidth = true;
                mainLayout.childControlHeight = true;
                mainLayout.childForceExpandWidth = true;
                mainLayout.childForceExpandHeight = false;

                GameObject header = CreateUiObject("Header", panel);
                AddLayoutElement(header, -1f, 64f, 1f, 0f);
                title.SetParent(header.transform, false);
                version.SetParent(header.transform, false);
                SetRect((RectTransform)title, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                SetRect((RectTransform)version, new Vector2(0.85f, 0f), Vector2.one, Vector2.zero, Vector2.zero);

                AddLayoutElement(help.gameObject, -1f, 42f, 1f, 0f);

                GameObject contentRow = CreateUiObject("ContentRow", panel);
                HorizontalLayoutGroup contentLayout = contentRow.AddComponent<HorizontalLayoutGroup>();
                contentLayout.spacing = 24f;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = false;
                contentLayout.childForceExpandHeight = true;
                AddLayoutElement(contentRow, -1f, -1f, 1f, 1f);
                left.SetParent(contentRow.transform, false);
                right.SetParent(contentRow.transform, false);

                ConfigureExistingCategoryPane(left.gameObject);
                ConfigureExistingDetailPane(right.gameObject);

                GameObject footer = CreateUiObject("Footer", panel);
                ConfigureRowLayout(footer, 10f);
                AddLayoutElement(footer, -1f, 74f, 1f, 0f);
                MoveFooterButton(panel, footer.transform, "AddCategory", 180f);
                MoveFooterButton(panel, footer.transform, "RemoveCategory", 140f);
                MoveFooterButton(panel, footer.transform, "MoveUp", 140f);
                MoveFooterButton(panel, footer.transform, "MoveDown", 140f);
                GameObject spacer = CreateUiObject("Spacer", footer.transform);
                AddLayoutElement(spacer, -1f, -1f, 1f, 1f);
                MoveFooterButton(panel, footer.transform, "RestoreDefaults", 220f);
                MoveFooterButton(panel, footer.transform, "Apply", 150f);
                MoveFooterButton(panel, footer.transform, "Done", 150f);
                MoveFooterButton(panel, footer.transform, "Cancel", 150f);

                LayoutElement overlayLayout = pickerOverlay.gameObject.AddComponent<LayoutElement>();
                overlayLayout.ignoreLayout = true;
                SetRect((RectTransform)pickerOverlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                header.transform.SetSiblingIndex(0);
                help.SetSiblingIndex(1);
                contentRow.transform.SetSiblingIndex(2);
                footer.transform.SetSiblingIndex(3);
                pickerOverlay.SetSiblingIndex(4);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("Auto Locker Labels category UI converted to layout groups.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Adds the custom-TechType filter to an existing category dialog prefab without replacing it.
        /// </summary>
        [MenuItem("Daft Apple Games/Auto Locker Labels/Add Custom Only Picker Filter")]
        public static void AddCustomOnlyPickerFilter()
        {
            string prefabPath = PrefabFolder + "/CategoryConfigUi.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform panel = root.transform.Find("CategoryConfigPanel");
                Transform picker = panel.Find("TechTypePickerOverlay/TechTypePicker");
                if (picker.Find("SearchRow/CustomOnly") != null)
                {
                    Debug.Log("Auto Locker Labels custom-only picker filter already exists.");
                    return;
                }

                Transform search = picker.Find("Search");
                GameObject searchRow = CreateUiObject("SearchRow", picker);
                ConfigureRowLayout(searchRow, 12f);
                SetRect((RectTransform)searchRow.transform, new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.88f), Vector2.zero, Vector2.zero);
                search.SetParent(searchRow.transform, false);
                AddLayoutElement(search.gameObject, -1f, -1f, 1f, 1f);
                Toggle customOnly = CreateToggle("CustomOnly", searchRow.transform, "Custom Only");
                AddLayoutElement(customOnly.gameObject, 210f, -1f, 0f, 1f);

                CategoryConfigDialog dialog = panel.GetComponent<CategoryConfigDialog>();
                SetReference(dialog, "pickerCustomOnlyToggle", customOnly);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("Auto Locker Labels custom-only picker filter added.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Configures serialized dialog events and upgrades the TechType picker for multi-selection.
        /// </summary>
        [MenuItem("Daft Apple Games/Auto Locker Labels/Configure Category UI Events And Multi-Select")]
        public static void ConfigureEventsAndMultiSelect()
        {
            ConfigureCategoryEntryPrefab();
            ConfigureTechTypeEntryPrefab();
            ConfigurePickerEntryPrefab();

            string prefabPath = PrefabFolder + "/CategoryConfigUi.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                CategoryConfigDialog dialog = root.GetComponentInChildren<CategoryConfigDialog>(true);
                Transform panel = dialog.transform;
                Transform picker = panel.Find("TechTypePickerOverlay/TechTypePicker");
                Transform footer = picker.Find("PickerFooter");
                Button addSelectedButton;
                if (footer == null)
                {
                    Button closeButton = picker.Find("Close").GetComponent<Button>();
                    GameObject footerObject = CreateUiObject("PickerFooter", picker);
                    ConfigureRowLayout(footerObject, 12f);
                    SetRect((RectTransform)footerObject.transform, new Vector2(0.05f, 0.035f), new Vector2(0.95f, 0.115f), Vector2.zero, Vector2.zero);
                    GameObject spacer = CreateUiObject("Spacer", footerObject.transform);
                    AddLayoutElement(spacer, -1f, -1f, 1f, 1f);
                    addSelectedButton = CreateLayoutButton("AddSelected", footerObject.transform, "Add", 20, 180f);
                    closeButton.transform.SetParent(footerObject.transform, false);
                    AddLayoutElement(closeButton.gameObject, 180f, -1f, 0f, 1f);
                }
                else
                {
                    addSelectedButton = footer.Find("AddSelected").GetComponent<Button>();
                }

                TMP_Text newButtonLabel = panel.Find("Footer/AddCategory/Label").GetComponent<TMP_Text>();
                newButtonLabel.text = "New";
                SetReference(dialog, "pickerAddButton", addSelectedButton);
                ConfigureDialogEvents(dialog);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("Auto Locker Labels dialog events and TechType multi-select configured.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Validates the generated hierarchy and every required serialized reference.
        /// </summary>
        [MenuItem("Daft Apple Games/Auto Locker Labels/Validate Category UI Prefabs")]
        public static void Validate()
        {
            GameObject dialogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/CategoryConfigUi.prefab");
            CategoryConfigDialog dialog = dialogPrefab == null ? null : dialogPrefab.GetComponentInChildren<CategoryConfigDialog>(true);
            if (dialog == null)
            {
                throw new InvalidOperationException("CategoryConfigDialog is missing.");
            }

            SerializedObject serializedDialog = new SerializedObject(dialog);
            string[] requiredProperties =
            {
                "mainPanel", "versionText", "categoryContent", "techTypeContent",
                "categoryEntryPrefab", "techTypeEntryPrefab", "categoryNameInput",
                "categoryStatusText", "removeButton", "moveUpButton", "moveDownButton",
                "addTechTypeButton", "pickerPanel",
                "pickerSearchInput", "pickerCustomOnlyToggle", "pickerContent", "pickerEntryPrefab",
                "pickerEmptyText", "pickerAddButton"
            };
            foreach (string propertyName in requiredProperties)
            {
                SerializedProperty property = serializedDialog.FindProperty(propertyName);
                if (property == null || property.objectReferenceValue == null)
                {
                    throw new InvalidOperationException($"Category UI reference '{propertyName}' is missing.");
                }
            }

            RectTransform panel = dialog.transform as RectTransform;
            if (panel == null || panel.sizeDelta != new Vector2(1600f, 920f))
            {
                throw new InvalidOperationException("Category dialog dimensions are incorrect.");
            }

            ValidateRow<CategoryListEntry>("CategoryListEntry.prefab", "selectButton", "nameText", "statusText");
            ValidateRow<TechTypeListEntry>("TechTypeListEntry.prefab", "nameText", "iconImage", "sourceText", "removeButton");
            ValidateRow<TechTypePickerEntry>("TechTypePickerEntry.prefab", "nameText", "iconImage", "sourceText", "selectionToggle");
            ValidateScrollView(dialog.transform, "ContentRow/CategoryPane/CategoryScrollView");
            ValidateScrollView(dialog.transform, "ContentRow/DetailPane/TechTypeScrollView");
            ValidateScrollView(dialog.transform, "TechTypePickerOverlay/TechTypePicker/PickerScrollView");
            Debug.Log("Auto Locker Labels category UI prefabs validated successfully.");
        }

        private static void ValidateScrollView(Transform dialog, string path)
        {
            Transform scrollTransform = dialog.Find(path);
            ScrollRect scrollRect = scrollTransform == null ? null : scrollTransform.GetComponent<ScrollRect>();
            if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null ||
                scrollRect.verticalScrollbar == null || scrollRect.verticalScrollbar.handleRect == null)
            {
                throw new InvalidOperationException(path + " has no usable vertical scrollbar.");
            }

            VerticalLayoutGroup layout = scrollRect.content.GetComponent<VerticalLayoutGroup>();
            if (layout == null || !layout.childControlWidth || !layout.childControlHeight)
            {
                throw new InvalidOperationException(path + " does not control its row dimensions.");
            }
        }

        private static void ValidateRow<T>(string fileName, params string[] properties) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + fileName);
            T component = prefab == null ? null : prefab.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(fileName + " has no " + typeof(T).Name + ".");
            }

            SerializedObject serializedObject = new SerializedObject(component);
            foreach (string propertyName in properties)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyName);
                if (property == null || property.objectReferenceValue == null)
                {
                    throw new InvalidOperationException(fileName + " reference '" + propertyName + "' is missing.");
                }
            }
        }

        private static CategoryListEntry BuildCategoryEntry()
        {
            GameObject root = CreateUiObject("CategoryListEntry", null);
            Image image = root.AddComponent<Image>();
            image.color = RowColor;
            Button button = root.AddComponent<Button>();
            button.targetGraphic = image;
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredHeight = 58f;
            layout.flexibleWidth = 1f;
            TMP_Text name = CreateText("Name", root.transform, "Category", 24, TextAlignmentOptions.MidlineLeft);
            SetRect(name.rectTransform, new Vector2(0f, 0f), new Vector2(0.72f, 1f), new Vector2(14f, 0f), new Vector2(-5f, 0f));
            TMP_Text status = CreateText("Status", root.transform, "DEFAULT", 16, TextAlignmentOptions.MidlineRight);
            status.color = AccentColor;
            SetRect(status.rectTransform, new Vector2(0.72f, 0f), Vector2.one, new Vector2(4f, 0f), new Vector2(-12f, 0f));
            CategoryListEntry entry = root.AddComponent<CategoryListEntry>();
            SetReference(entry, "selectButton", button);
            SetReference(entry, "nameText", name);
            SetReference(entry, "statusText", status);
            UnityEventTools.AddPersistentListener(button.onClick, entry.Select);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/CategoryListEntry.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<CategoryListEntry>();
        }

        private static TechTypeListEntry BuildTechTypeEntry()
        {
            GameObject root = CreateUiObject("TechTypeListEntry", null);
            Image image = root.AddComponent<Image>();
            image.color = RowColor;
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredHeight = 48f;
            layout.flexibleWidth = 1f;
            Image icon = CreateIcon("Icon", root.transform);
            SetRect(icon.rectTransform, new Vector2(0.012f, 0.10f), new Vector2(0.085f, 0.90f), Vector2.zero, Vector2.zero);
            TMP_Text name = CreateText("Name", root.transform, "Titanium (Titanium)", 20, TextAlignmentOptions.MidlineLeft);
            SetRect(name.rectTransform, new Vector2(0.10f, 0f), new Vector2(0.64f, 1f), Vector2.zero, new Vector2(-4f, 0f));
            TMP_Text source = CreateText("Source", root.transform, "Below Zero", 15, TextAlignmentOptions.MidlineRight);
            source.color = new Color(0.42f, 0.84f, 1f, 1f);
            SetRect(source.rectTransform, new Vector2(0.64f, 0f), new Vector2(0.87f, 1f), new Vector2(2f, 0f), new Vector2(-4f, 0f));
            Button remove = CreateButton("Remove", root.transform, "−", 36);
            SetRect((RectTransform)remove.transform, new Vector2(0.89f, 0.08f), new Vector2(0.99f, 0.92f), Vector2.zero, Vector2.zero);
            TechTypeListEntry entry = root.AddComponent<TechTypeListEntry>();
            SetReference(entry, "nameText", name);
            SetReference(entry, "iconImage", icon);
            SetReference(entry, "sourceText", source);
            SetReference(entry, "removeButton", remove);
            UnityEventTools.AddPersistentListener(remove.onClick, entry.Remove);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/TechTypeListEntry.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<TechTypeListEntry>();
        }

        private static TechTypePickerEntry BuildTechTypePickerEntry()
        {
            GameObject root = CreateUiObject("TechTypePickerEntry", null);
            Image image = root.AddComponent<Image>();
            image.color = RowColor;
            Toggle toggle = root.AddComponent<Toggle>();
            toggle.targetGraphic = image;
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredHeight = 52f;
            layout.flexibleWidth = 1f;
            Image icon = CreateIcon("Icon", root.transform);
            SetRect(icon.rectTransform, new Vector2(0.012f, 0.10f), new Vector2(0.08f, 0.90f), Vector2.zero, Vector2.zero);
            TMP_Text name = CreateText("Name", root.transform, "Titanium  (Titanium)", 20, TextAlignmentOptions.MidlineLeft);
            SetRect(name.rectTransform, new Vector2(0.095f, 0f), new Vector2(0.72f, 1f), Vector2.zero, new Vector2(-8f, 0f));
            TMP_Text source = CreateText("Source", root.transform, "Below Zero", 16, TextAlignmentOptions.MidlineRight);
            source.color = new Color(0.42f, 0.84f, 1f, 1f);
            SetRect(source.rectTransform, new Vector2(0.72f, 0f), new Vector2(0.91f, 1f), new Vector2(4f, 0f), new Vector2(-8f, 0f));
            Graphic checkmark = CreateSelectionCheckbox(root.transform);
            toggle.graphic = checkmark;
            TechTypePickerEntry entry = root.AddComponent<TechTypePickerEntry>();
            SetReference(entry, "nameText", name);
            SetReference(entry, "iconImage", icon);
            SetReference(entry, "sourceText", source);
            SetReference(entry, "selectionToggle", toggle);
            UnityEventTools.AddPersistentListener(toggle.onValueChanged, entry.SetSelected);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/TechTypePickerEntry.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<TechTypePickerEntry>();
        }

        private static void BuildDialog(
            CategoryListEntry categoryPrefab,
            TechTypeListEntry techTypePrefab,
            TechTypePickerEntry pickerEntryPrefab)
        {
            GameObject canvasObject = CreateUiObject("CategoryConfigUi", null);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<CanvasGroup>();

            GameObject panel = CreatePanel("CategoryConfigPanel", canvasObject.transform, PanelColor);
            CanvasGroup panelCanvasGroup = panel.AddComponent<CanvasGroup>();
            panelCanvasGroup.ignoreParentGroups = true;
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1600f, 920f);
            panelRect.anchoredPosition = Vector2.zero;
            CategoryConfigDialog dialog = panel.AddComponent<CategoryConfigDialog>();

            VerticalLayoutGroup mainLayout = panel.AddComponent<VerticalLayoutGroup>();
            mainLayout.padding = new RectOffset(40, 40, 24, 24);
            mainLayout.spacing = 12f;
            mainLayout.childAlignment = TextAnchor.UpperCenter;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = true;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;

            GameObject header = CreateUiObject("Header", panel.transform);
            AddLayoutElement(header, -1f, 64f, 1f, 0f);
            TMP_Text title = CreateText("Title", header.transform, "AUTOMATIC LOCKER LABEL CATEGORIES", 40, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TMP_Text version = CreateText("Version", header.transform, "v1.0.2", 18, TextAlignmentOptions.MidlineRight);
            SetRect(version.rectTransform, new Vector2(0.85f, 0f), Vector2.one, Vector2.zero, Vector2.zero);

            TMP_Text help = CreateText("Help", panel.transform, "Categories are evaluated from top to bottom. The first matching category supplies the locker label.", 20, TextAlignmentOptions.Center);
            help.color = new Color(0.80f, 0.93f, 1f);
            AddLayoutElement(help.gameObject, -1f, 42f, 1f, 0f);

            GameObject contentRow = CreateUiObject("ContentRow", panel.transform);
            HorizontalLayoutGroup contentLayout = contentRow.AddComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = 24f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = true;
            AddLayoutElement(contentRow, -1f, -1f, 1f, 1f);

            GameObject left = CreatePanel("CategoryPane", contentRow.transform, SectionColor);
            ConfigurePaneLayout(left);
            AddLayoutElement(left, 550f, -1f, 0f, 1f);
            CreatePaneTitle(left.transform, "CATEGORIES");
            Transform categoryContent = CreateLayoutScrollView("CategoryScrollView", left.transform);

            GameObject right = CreatePanel("DetailPane", contentRow.transform, SectionColor);
            ConfigurePaneLayout(right);
            AddLayoutElement(right, -1f, -1f, 1f, 1f);
            CreatePaneTitle(right.transform, "CATEGORY DETAILS");

            GameObject nameRow = CreateUiObject("NameRow", right.transform);
            ConfigureRowLayout(nameRow, 16f);
            AddLayoutElement(nameRow, -1f, 70f, 1f, 0f);
            TMP_InputField nameInput = CreateInput("CategoryName", nameRow.transform, "Category name");
            AddLayoutElement(nameInput.gameObject, -1f, -1f, 1f, 1f);
            TMP_Text status = CreateText("CategoryStatus", nameRow.transform, "DEFAULT", 18, TextAlignmentOptions.Center);
            status.color = AccentColor;
            AddLayoutElement(status.gameObject, 210f, -1f, 0f, 1f);

            GameObject techHeader = CreateUiObject("TechHeader", right.transform);
            ConfigureRowLayout(techHeader, 16f);
            AddLayoutElement(techHeader, -1f, 44f, 1f, 0f);
            TMP_Text itemTitle = CreateText("ItemTitle", techHeader.transform, "ASSIGNED TECH TYPES", 20, TextAlignmentOptions.MidlineLeft);
            AddLayoutElement(itemTitle.gameObject, -1f, -1f, 1f, 1f);
            Button addTechType = CreateButton("AddTechType", techHeader.transform, "Add TechType", 18);
            AddLayoutElement(addTechType.gameObject, 220f, -1f, 0f, 1f);
            Transform techContent = CreateLayoutScrollView("TechTypeScrollView", right.transform);

            GameObject footer = CreateUiObject("Footer", panel.transform);
            ConfigureRowLayout(footer, 10f);
            AddLayoutElement(footer, -1f, 74f, 1f, 0f);
            CreateLayoutButton("AddCategory", footer.transform, "New", 20, 180f);
            Button remove = CreateLayoutButton("RemoveCategory", footer.transform, "Remove", 20, 140f);
            Button up = CreateLayoutButton("MoveUp", footer.transform, "Move Up", 20, 140f);
            Button down = CreateLayoutButton("MoveDown", footer.transform, "Move Down", 20, 140f);
            GameObject footerSpacer = CreateUiObject("Spacer", footer.transform);
            AddLayoutElement(footerSpacer, -1f, -1f, 1f, 1f);
            CreateLayoutButton("RestoreDefault", footer.transform, "Restore Defaults", 20, 220f);
            CreateLayoutButton("RestoreAllDefaults", footer.transform, "Reset all defaults", 20, 220f);
            CreateLayoutButton("Apply", footer.transform, "Apply", 20, 150f);
            CreateLayoutButton("Done", footer.transform, "Done", 20, 150f);
            CreateLayoutButton("Cancel", footer.transform, "Cancel", 20, 150f);

            GameObject pickerOverlay = CreatePanel("TechTypePickerOverlay", panel.transform, new Color(0.01f, 0.10f, 0.16f, 0.94f));
            LayoutElement pickerOverlayLayout = pickerOverlay.AddComponent<LayoutElement>();
            pickerOverlayLayout.ignoreLayout = true;
            SetRect((RectTransform)pickerOverlay.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject picker = CreatePanel("TechTypePicker", pickerOverlay.transform, SectionColor);
            SetRect((RectTransform)picker.transform, new Vector2(0.14f, 0.08f), new Vector2(0.86f, 0.92f), Vector2.zero, Vector2.zero);
            TMP_Text pickerTitle = CreateText("Title", picker.transform, "ADD TECH TYPE", 30, TextAlignmentOptions.Center);
            SetRect(pickerTitle.rectTransform, new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
            GameObject searchRow = CreateUiObject("SearchRow", picker.transform);
            ConfigureRowLayout(searchRow, 12f);
            SetRect((RectTransform)searchRow.transform, new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.88f), Vector2.zero, Vector2.zero);
            TMP_InputField pickerSearch = CreateInput("Search", searchRow.transform, "Search by item or TechType name...");
            AddLayoutElement(pickerSearch.gameObject, -1f, -1f, 1f, 1f);
            Toggle pickerCustomOnly = CreateToggle("CustomOnly", searchRow.transform, "Custom Only");
            AddLayoutElement(pickerCustomOnly.gameObject, 210f, -1f, 0f, 1f);
            Transform pickerContent = CreateScrollView("PickerScrollView", picker.transform, 0.14f, 0.76f);
            TMP_Text pickerEmpty = CreateText("Empty", picker.transform, "No matching TechTypes", 22, TextAlignmentOptions.Center);
            pickerEmpty.color = new Color(0.80f, 0.93f, 1f);
            SetRect(pickerEmpty.rectTransform, new Vector2(0.08f, 0.40f), new Vector2(0.92f, 0.55f), Vector2.zero, Vector2.zero);
            GameObject pickerFooter = CreateUiObject("PickerFooter", picker.transform);
            ConfigureRowLayout(pickerFooter, 12f);
            SetRect((RectTransform)pickerFooter.transform, new Vector2(0.05f, 0.035f), new Vector2(0.95f, 0.115f), Vector2.zero, Vector2.zero);
            GameObject pickerFooterSpacer = CreateUiObject("Spacer", pickerFooter.transform);
            AddLayoutElement(pickerFooterSpacer, -1f, -1f, 1f, 1f);
            Button pickerAdd = CreateLayoutButton("AddSelected", pickerFooter.transform, "Add", 20, 180f);
            CreateLayoutButton("Close", pickerFooter.transform, "Cancel", 20, 180f);

            SetReference(dialog, "mainPanel", panel);
            SetReference(dialog, "versionText", version);
            SetReference(dialog, "categoryContent", categoryContent);
            SetReference(dialog, "techTypeContent", techContent);
            SetReference(dialog, "categoryEntryPrefab", categoryPrefab);
            SetReference(dialog, "techTypeEntryPrefab", techTypePrefab);
            SetReference(dialog, "categoryNameInput", nameInput);
            SetReference(dialog, "categoryStatusText", status);
            SetReference(dialog, "removeButton", remove);
            SetReference(dialog, "moveUpButton", up); SetReference(dialog, "moveDownButton", down);
            SetReference(dialog, "addTechTypeButton", addTechType);
            SetReference(dialog, "pickerPanel", pickerOverlay);
            SetReference(dialog, "pickerSearchInput", pickerSearch);
            SetReference(dialog, "pickerCustomOnlyToggle", pickerCustomOnly);
            SetReference(dialog, "pickerContent", pickerContent);
            SetReference(dialog, "pickerEntryPrefab", pickerEntryPrefab);
            SetReference(dialog, "pickerEmptyText", pickerEmpty);
            SetReference(dialog, "pickerAddButton", pickerAdd);
            ConfigureDialogEvents(dialog);

            pickerOverlay.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(canvasObject, PrefabFolder + "/CategoryConfigUi.prefab");
            Object.DestroyImmediate(canvasObject);
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject value = new GameObject(name, typeof(RectTransform));
            value.transform.SetParent(parent, false);
            if (parent == null && previewScene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(value, previewScene);
            }
            return value;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject value = CreateUiObject(name, parent);
            Image image = value.AddComponent<Image>(); image.color = color;
            return value;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment)
        {
            GameObject value = CreateUiObject(name, parent);
            TextMeshProUGUI label = value.AddComponent<TextMeshProUGUI>();
            label.text = text; label.fontSize = size; label.alignment = alignment; label.color = Color.white;
            return label;
        }

        private static Image CreateIcon(string name, Transform parent)
        {
            GameObject value = CreateUiObject(name, parent);
            Image image = value.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Button CreateButton(string name, Transform parent, string text, int size)
        {
            GameObject value = CreatePanel(name, parent, ButtonColor);
            Button button = value.AddComponent<Button>(); button.targetGraphic = value.GetComponent<Image>();
            TMP_Text label = CreateText("Label", value.transform, text, size, TextAlignmentOptions.Center);
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static Button CreateLayoutButton(string name, Transform parent, string text, int size, float preferredWidth)
        {
            Button button = CreateButton(name, parent, text, size);
            AddLayoutElement(button.gameObject, preferredWidth, -1f, 0f, 1f);
            return button;
        }

        private static Toggle CreateToggle(string name, Transform parent, string labelText)
        {
            GameObject root = CreateUiObject(name, parent);
            HorizontalLayoutGroup layout = ConfigureRowLayout(root, 10f);
            layout.padding = new RectOffset(4, 4, 4, 4);

            GameObject box = CreatePanel("Box", root.transform, ContentColor);
            AddLayoutElement(box, 38f, 38f, 0f, 0f);
            TMP_Text checkmark = CreateText("Checkmark", box.transform, "✓", 28, TextAlignmentOptions.Center);
            checkmark.color = AccentColor;
            SetRect(checkmark.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TMP_Text label = CreateText("Label", root.transform, labelText, 20, TextAlignmentOptions.MidlineLeft);
            AddLayoutElement(label.gameObject, -1f, -1f, 1f, 1f);

            Toggle toggle = root.AddComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = checkmark;
            toggle.isOn = false;
            return toggle;
        }

        private static Graphic CreateSelectionCheckbox(Transform parent)
        {
            GameObject box = CreatePanel("SelectionBox", parent, ContentColor);
            SetRect((RectTransform)box.transform, new Vector2(0.935f, 0.18f), new Vector2(0.985f, 0.82f), Vector2.zero, Vector2.zero);
            TMP_Text checkmark = CreateText("Checkmark", box.transform, "✓", 24, TextAlignmentOptions.Center);
            checkmark.color = AccentColor;
            SetRect(checkmark.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return checkmark;
        }

        private static void ConfigurePickerEntryPrefab()
        {
            string prefabPath = PrefabFolder + "/TechTypePickerEntry.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Button button = root.GetComponent<Button>();
                if (button != null)
                {
                    Object.DestroyImmediate(button, true);
                }

                Toggle toggle = root.GetComponent<Toggle>();
                if (toggle == null)
                {
                    toggle = root.AddComponent<Toggle>();
                }

                toggle.targetGraphic = root.GetComponent<Image>();
                Transform existingBox = root.transform.Find("SelectionBox");
                Graphic checkmark = existingBox == null
                    ? CreateSelectionCheckbox(root.transform)
                    : existingBox.Find("Checkmark").GetComponent<TMP_Text>();
                toggle.graphic = checkmark;
                TMP_Text source = root.transform.Find("Source").GetComponent<TMP_Text>();
                SetRect(source.rectTransform, new Vector2(0.72f, 0f), new Vector2(0.91f, 1f), new Vector2(4f, 0f), new Vector2(-8f, 0f));

                TechTypePickerEntry entry = root.GetComponent<TechTypePickerEntry>();
                SetReference(entry, "selectionToggle", toggle);
                ClearPersistentListeners(toggle.onValueChanged);
                UnityEventTools.AddPersistentListener(toggle.onValueChanged, entry.SetSelected);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureCategoryEntryPrefab()
        {
            string prefabPath = PrefabFolder + "/CategoryListEntry.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                CategoryListEntry entry = root.GetComponent<CategoryListEntry>();
                Button button = root.GetComponent<Button>();
                ClearPersistentListeners(button.onClick);
                UnityEventTools.AddPersistentListener(button.onClick, entry.Select);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureTechTypeEntryPrefab()
        {
            string prefabPath = PrefabFolder + "/TechTypeListEntry.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                TechTypeListEntry entry = root.GetComponent<TechTypeListEntry>();
                Button button = root.transform.Find("Remove").GetComponent<Button>();
                ClearPersistentListeners(button.onClick);
                UnityEventTools.AddPersistentListener(button.onClick, entry.Remove);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureDialogEvents(CategoryConfigDialog dialog)
        {
            Transform panel = dialog.transform;
            WireButton(panel, "Footer/AddCategory", dialog.AddCategory);
            WireButton(panel, "Footer/RemoveCategory", dialog.RemoveCategory);
            WireButton(panel, "Footer/MoveUp", dialog.MoveUp);
            WireButton(panel, "Footer/MoveDown", dialog.MoveDown);
            WireButton(panel, "Footer/RestoreDefault", dialog.RestoreSelectedCategoryDefault);
            WireButton(panel, "Footer/RestoreAllDefaults", dialog.RestoreDefaults);
            WireButton(panel, "Footer/Apply", dialog.Apply);
            WireButton(panel, "Footer/Done", dialog.Done);
            WireButton(panel, "Footer/Cancel", dialog.Cancel);
            WireButton(panel, "ContentRow/DetailPane/TechHeader/AddTechType", dialog.OpenTechTypePicker);
            WireButton(panel, "TechTypePickerOverlay/TechTypePicker/PickerFooter/AddSelected", dialog.AddSelectedTechTypes);
            WireButton(panel, "TechTypePickerOverlay/TechTypePicker/PickerFooter/Close", dialog.CloseTechTypePicker);

            TMP_InputField categoryName = panel.Find("ContentRow/DetailPane/NameRow/CategoryName").GetComponent<TMP_InputField>();
            ClearPersistentListeners(categoryName.onEndEdit);
            UnityEventTools.AddPersistentListener(categoryName.onEndEdit, dialog.RenameSelectedCategory);
            TMP_InputField search = panel.Find("TechTypePickerOverlay/TechTypePicker/SearchRow/Search").GetComponent<TMP_InputField>();
            ClearPersistentListeners(search.onValueChanged);
            UnityEventTools.AddPersistentListener(search.onValueChanged, dialog.RefreshTechTypePicker);
            Toggle customOnly = panel.Find("TechTypePickerOverlay/TechTypePicker/SearchRow/CustomOnly").GetComponent<Toggle>();
            ClearPersistentListeners(customOnly.onValueChanged);
            UnityEventTools.AddPersistentListener(customOnly.onValueChanged, dialog.FilterTechTypePickerBySource);
        }

        private static void WireButton(Transform root, string path, UnityAction action)
        {
            Button button = root.Find(path).GetComponent<Button>();
            ClearPersistentListeners(button.onClick);
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static void ClearPersistentListeners(UnityEventBase unityEvent)
        {
            for (int index = unityEvent.GetPersistentEventCount() - 1; index >= 0; index--)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, index);
            }
        }

        private static TMP_InputField CreateInput(string name, Transform parent, string placeholderText)
        {
            GameObject root = CreatePanel(name, parent, ContentColor);
            TMP_InputField input = root.AddComponent<TMP_InputField>();
            TMP_Text text = CreateText("Text", root.transform, string.Empty, 25, TextAlignmentOptions.MidlineLeft);
            TMP_Text placeholder = CreateText("Placeholder", root.transform, placeholderText, 25, TextAlignmentOptions.MidlineLeft);
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, 0f));
            SetRect(placeholder.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, 0f));
            input.textComponent = text; input.placeholder = placeholder;
            return input;
        }

        private static Transform CreateScrollView(string name, Transform parent, float bottom, float top = 0.88f, float bottomOffset = 0f)
        {
            GameObject root = CreatePanel(name, parent, ContentColor);
            SetRect((RectTransform)root.transform, new Vector2(0.03f, bottom), new Vector2(0.97f, top), new Vector2(0f, bottomOffset), Vector2.zero);
            ScrollRect scroll = root.AddComponent<ScrollRect>(); scroll.horizontal = false;
            GameObject viewport = CreateUiObject("Viewport", root.transform); viewport.AddComponent<RectMask2D>();
            SetRect((RectTransform)viewport.transform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-34f, -8f));
            GameObject content = CreateUiObject("Content", viewport.transform);
            RectTransform contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f); contentRect.anchorMax = Vector2.one; contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Scrollbar scrollbar = CreateVerticalScrollbar(root.transform);
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = contentRect;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.verticalScrollbarSpacing = 6f;
            return content.transform;
        }

        private static Transform CreateLayoutScrollView(string name, Transform parent)
        {
            Transform content = CreateScrollView(name, parent, 0f, 1f);
            AddLayoutElement(content.parent.parent.gameObject, -1f, -1f, 1f, 1f);
            return content;
        }

        private static Scrollbar CreateVerticalScrollbar(Transform parent)
        {
            GameObject root = CreatePanel("Scrollbar", parent, new Color(0.01f, 0.16f, 0.25f, 0.95f));
            SetRect((RectTransform)root.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-26f, 0f), new Vector2(-8f, -8f));

            GameObject slidingArea = CreateUiObject("Sliding Area", root.transform);
            SetRect((RectTransform)slidingArea.transform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));

            GameObject handle = CreatePanel("Handle", slidingArea.transform, new Color(0.05f, 0.82f, 0.96f, 1f));
            SetRect((RectTransform)handle.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Scrollbar scrollbar = root.AddComponent<Scrollbar>();
            scrollbar.handleRect = (RectTransform)handle.transform;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            return scrollbar;
        }

        private static void CreatePaneTitle(Transform parent, string text)
        {
            TMP_Text title = CreateText("PaneTitle", parent, text, 25, TextAlignmentOptions.Center);
            AddLayoutElement(title.gameObject, -1f, 52f, 1f, 0f);
        }

        private static VerticalLayoutGroup ConfigurePaneLayout(GameObject pane)
        {
            VerticalLayoutGroup layout = pane.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        private static void ConfigureExistingCategoryPane(GameObject pane)
        {
            Transform title = pane.transform.Find("PaneTitle");
            Transform scrollView = pane.transform.Find("CategoryScrollView");
            ConfigurePaneLayout(pane);
            AddLayoutElement(pane, 550f, -1f, 0f, 1f);
            AddLayoutElement(title.gameObject, -1f, 52f, 1f, 0f);
            AddLayoutElement(scrollView.gameObject, -1f, -1f, 1f, 1f);
            title.SetSiblingIndex(0);
            scrollView.SetSiblingIndex(1);
        }

        private static void ConfigureExistingDetailPane(GameObject pane)
        {
            Transform title = pane.transform.Find("PaneTitle");
            Transform nameInput = pane.transform.Find("CategoryName");
            Transform status = pane.transform.Find("CategoryStatus");
            Transform itemTitle = pane.transform.Find("ItemTitle");
            Transform addTechType = pane.transform.Find("AddTechType");
            Transform scrollView = pane.transform.Find("TechTypeScrollView");

            ConfigurePaneLayout(pane);
            AddLayoutElement(pane, -1f, -1f, 1f, 1f);
            AddLayoutElement(title.gameObject, -1f, 52f, 1f, 0f);

            GameObject nameRow = CreateUiObject("NameRow", pane.transform);
            ConfigureRowLayout(nameRow, 16f);
            AddLayoutElement(nameRow, -1f, 70f, 1f, 0f);
            nameInput.SetParent(nameRow.transform, false);
            status.SetParent(nameRow.transform, false);
            AddLayoutElement(nameInput.gameObject, -1f, -1f, 1f, 1f);
            AddLayoutElement(status.gameObject, 210f, -1f, 0f, 1f);

            GameObject techHeader = CreateUiObject("TechHeader", pane.transform);
            ConfigureRowLayout(techHeader, 16f);
            AddLayoutElement(techHeader, -1f, 44f, 1f, 0f);
            itemTitle.SetParent(techHeader.transform, false);
            addTechType.SetParent(techHeader.transform, false);
            AddLayoutElement(itemTitle.gameObject, -1f, -1f, 1f, 1f);
            AddLayoutElement(addTechType.gameObject, 220f, -1f, 0f, 1f);
            AddLayoutElement(scrollView.gameObject, -1f, -1f, 1f, 1f);

            title.SetSiblingIndex(0);
            nameRow.transform.SetSiblingIndex(1);
            techHeader.transform.SetSiblingIndex(2);
            scrollView.SetSiblingIndex(3);
        }

        private static void MoveFooterButton(Transform panel, Transform footer, string name, float preferredWidth)
        {
            Transform button = panel.Find(name);
            button.SetParent(footer, false);
            AddLayoutElement(button.gameObject, preferredWidth, -1f, 0f, 1f);
        }

        private static HorizontalLayoutGroup ConfigureRowLayout(GameObject row, float spacing)
        {
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return layout;
        }

        private static LayoutElement AddLayoutElement(GameObject target, float preferredWidth, float preferredHeight, float flexibleWidth, float flexibleHeight)
        {
            LayoutElement element = target.AddComponent<LayoutElement>();
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            return element;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
