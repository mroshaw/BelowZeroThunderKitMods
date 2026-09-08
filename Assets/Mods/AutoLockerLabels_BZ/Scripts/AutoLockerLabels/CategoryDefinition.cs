using System.Collections.Generic;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    /// <summary>
    /// Defines an ordered automatic locker label category.
    /// </summary>
    internal class CategoryDefinition
    {
        private readonly List<TechType> itemTypes;
        private readonly HashSet<TechType> itemTypeLookup;

        internal string Id { get; }

        internal string LanguageKey { get; }

        internal string FallbackLabel { get; }

        internal int Priority { get; }

        internal IEnumerable<TechType> ItemTypes => itemTypes;

        internal CategoryDefinition(
            string id,
            string languageKey,
            string fallbackLabel,
            int priority,
            IEnumerable<TechType> itemTypes)
        {
            Id = id;
            LanguageKey = languageKey;
            FallbackLabel = fallbackLabel;
            Priority = priority;
            this.itemTypes = new List<TechType>();
            itemTypeLookup = new HashSet<TechType>();
            foreach (TechType itemType in itemTypes)
            {
                if (itemTypeLookup.Add(itemType))
                {
                    this.itemTypes.Add(itemType);
                }
            }
        }

        internal bool ContainsAll(List<TechType> contents)
        {
            foreach (TechType techType in contents)
            {
                if (!itemTypeLookup.Contains(techType))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
