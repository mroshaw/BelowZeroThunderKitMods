using System.Collections.Generic;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    /// <summary>
    /// Describes one complete player-owned category definition.
    /// </summary>
    internal sealed class CategoryOverride
    {
        public string Id { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public int Priority { get; set; }

        public List<string> TechTypes { get; set; } = new List<string>();
    }
}
