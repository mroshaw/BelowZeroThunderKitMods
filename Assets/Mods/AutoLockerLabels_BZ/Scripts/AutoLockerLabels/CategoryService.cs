using System;
using System.Collections.Generic;
using static DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabelsPlugin;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    /// <summary>
    /// Combines built-in categories with player overrides and exposes the applied set.
    /// </summary>
    internal static class CategoryService
    {
        private static readonly List<CategoryDefinition> activeCategories =
            new List<CategoryDefinition>();

        private static CategoryDefinition[] builtInCategories;
        private static readonly HashSet<string> builtInIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal static event Action CategoriesChanged;

        internal static IReadOnlyList<CategoryDefinition> ActiveCategories =>
            activeCategories;

        internal static CategoryOverrideFile OverrideFile { get; private set; }

        internal static void Initialize(CategoryDefinition[] defaults)
        {
            builtInCategories = defaults;
            builtInIds.Clear();
            foreach (CategoryDefinition category in defaults)
            {
                builtInIds.Add(category.Id);
            }

            OverrideFile = new CategoryOverrideFile();

            try
            {
                OverrideFile.Load();
            }
            catch (Exception exception)
            {
                ModDebugLog.LogError($"Could not load category overrides. Built-in categories will be used. {exception}");
                OverrideFile = new CategoryOverrideFile();
            }

            ApplyOverrides(false);
        }

        private static void ApplyOverrides(bool save)
        {
            if (builtInCategories == null)
            {
                return;
            }

            Dictionary<string, CategoryDefinition> effectiveCategories =
                new Dictionary<string, CategoryDefinition>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> removedCategories = CreateRemovedCategorySet();

            int defaultPriority = 10;
            foreach (CategoryDefinition category in builtInCategories)
            {
                if (!removedCategories.Contains(category.Id))
                {
                    effectiveCategories[category.Id] = new CategoryDefinition(
                        category.Id,
                        category.LanguageKey,
                        category.FallbackLabel,
                        defaultPriority,
                        category.ItemTypes);
                }

                defaultPriority += 10;
            }

            List<CategoryOverride> categoryOverrides = OverrideFile.Categories ??
                new List<CategoryOverride>();
            foreach (CategoryOverride categoryOverride in categoryOverrides)
            {
                CategoryDefinition category;
                if (TryCreateCategory(categoryOverride, out category))
                {
                    effectiveCategories[category.Id] = category;
                }
            }

            activeCategories.Clear();
            activeCategories.AddRange(effectiveCategories.Values);

            activeCategories.Sort(ComparePriority);
            ApplyCategoryOrder();

            if (save)
            {
                OverrideFile.Save();
            }

            CategoriesChanged?.Invoke();
        }

        internal static List<EditableCategory> CreateDraft()
        {
            List<EditableCategory> draft = new List<EditableCategory>(activeCategories.Count);
            foreach (CategoryDefinition category in activeCategories)
            {
                draft.Add(CreateEditableCategory(category, builtInIds.Contains(category.Id), category.LanguageKey is null));
            }

            return draft;
        }

        internal static List<EditableCategory> CreateDefaultDraft()
        {
            List<EditableCategory> draft = new List<EditableCategory>(builtInCategories.Length);
            foreach (CategoryDefinition category in builtInCategories)
            {
                draft.Add(CreateEditableCategory(category, true, false));
            }

            return draft;
        }

        internal static bool TryCreateDefaultCategory(string categoryId, out EditableCategory category)
        {
            foreach (CategoryDefinition builtInCategory in builtInCategories)
            {
                if (string.Equals(builtInCategory.Id, categoryId, StringComparison.OrdinalIgnoreCase))
                {
                    category = CreateEditableCategory(builtInCategory, true, false);
                    return true;
                }
            }

            category = null;
            return false;
        }

        private static EditableCategory CreateEditableCategory(CategoryDefinition category, bool isBuiltIn, bool isModified)
        {
            EditableCategory editableCategory = new EditableCategory
            {
                Id = category.Id,
                DisplayName = LabelGenerator.GetLocalizedLabel(category.LanguageKey, category.FallbackLabel),
                IsBuiltIn = isBuiltIn,
                IsModified = isModified
            };
            editableCategory.TechTypes.AddRange(category.ItemTypes);
            return editableCategory;
        }

        internal static void ApplyDraft(List<EditableCategory> draft)
        {
            List<CategoryOverride> overrides = new List<CategoryOverride>();
            HashSet<string> draftIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int priority = 10;
            foreach (EditableCategory category in draft)
            {
                draftIds.Add(category.Id);
                if (!category.IsBuiltIn || category.IsModified)
                {
                    CategoryOverride categoryOverride = new CategoryOverride
                    {
                        Id = category.Id,
                        DisplayName = category.DisplayName,
                        Priority = priority
                    };
                    foreach (TechType techType in category.TechTypes)
                    {
                        categoryOverride.TechTypes.Add(techType.ToString());
                    }

                    overrides.Add(categoryOverride);
                }

                priority += 10;
            }

            List<string> removedCategories = new List<string>();
            foreach (CategoryDefinition category in builtInCategories)
            {
                if (!draftIds.Contains(category.Id))
                {
                    removedCategories.Add(category.Id);
                }
            }

            OverrideFile.Categories = overrides;
            OverrideFile.RemovedCategories = removedCategories;
            OverrideFile.CategoryOrder = new List<string>(draftIds.Count);
            foreach (EditableCategory category in draft)
            {
                OverrideFile.CategoryOrder.Add(category.Id);
            }
            ApplyOverrides(true);
        }

        private static void ApplyCategoryOrder()
        {
            if (OverrideFile.CategoryOrder == null || OverrideFile.CategoryOrder.Count == 0)
            {
                return;
            }

            Dictionary<string, CategoryDefinition> categoriesById =
                new Dictionary<string, CategoryDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (CategoryDefinition category in activeCategories)
            {
                categoriesById[category.Id] = category;
            }

            List<CategoryDefinition> orderedCategories = new List<CategoryDefinition>();
            foreach (string categoryId in OverrideFile.CategoryOrder)
            {
                if (string.IsNullOrWhiteSpace(categoryId))
                {
                    continue;
                }

                CategoryDefinition category;
                if (categoriesById.TryGetValue(categoryId, out category))
                {
                    orderedCategories.Add(category);
                    categoriesById.Remove(categoryId);
                }
            }

            foreach (CategoryDefinition category in activeCategories)
            {
                if (categoriesById.ContainsKey(category.Id))
                {
                    orderedCategories.Add(category);
                }
            }

            activeCategories.Clear();
            activeCategories.AddRange(orderedCategories);
        }

        private static HashSet<string> CreateRemovedCategorySet()
        {
            HashSet<string> removedCategories =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            List<string> removedOverrides = OverrideFile.RemovedCategories ??
                new List<string>();
            foreach (string categoryId in removedOverrides)
            {
                if (!string.IsNullOrWhiteSpace(categoryId))
                {
                    removedCategories.Add(categoryId);
                }
            }

            return removedCategories;
        }

        private static bool TryCreateCategory(
            CategoryOverride categoryOverride,
            out CategoryDefinition category)
        {
            category = null;
            if (categoryOverride == null ||
                string.IsNullOrWhiteSpace(categoryOverride.Id) ||
                string.IsNullOrWhiteSpace(categoryOverride.DisplayName))
            {
                return false;
            }

            List<TechType> itemTypes = new List<TechType>();
            HashSet<TechType> uniqueItemTypes = new HashSet<TechType>();
            List<string> techTypeNames = categoryOverride.TechTypes ??
                new List<string>();
            foreach (string techTypeName in techTypeNames)
            {
                TechType techType;
                if (!TechTypeExtensions.FromString(techTypeName, out techType, false) ||
                    techType == TechType.None ||
                    !uniqueItemTypes.Add(techType))
                {
                    continue;
                }

                itemTypes.Add(techType);
            }

            category = new CategoryDefinition(
                categoryOverride.Id,
                null,
                categoryOverride.DisplayName,
                categoryOverride.Priority,
                itemTypes);
            return true;
        }

        private static int ComparePriority(
            CategoryDefinition first,
            CategoryDefinition second)
        {
            int priorityComparison = first.Priority.CompareTo(second.Priority);
            return priorityComparison != 0
                ? priorityComparison
                : string.Compare(first.Id, second.Id, StringComparison.OrdinalIgnoreCase);
        }
    }
}
