using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nautilus.Json;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    /// <summary>
    /// Stores player replacements, additions, and removals for locker categories.
    /// </summary>
    internal sealed class CategoryOverrideFile : JsonFile
    {
        public override string JsonFilePath => Path.Combine(
            BepInEx.Paths.ConfigPath,
            Assembly.GetExecutingAssembly().GetName().Name,
            "category_overrides.json");

        public List<CategoryOverride> Categories { get; set; } =
            new List<CategoryOverride>();

        public List<string> RemovedCategories { get; set; } =
            new List<string>();

        public List<string> CategoryOrder { get; set; } = new List<string>();
    }
}
