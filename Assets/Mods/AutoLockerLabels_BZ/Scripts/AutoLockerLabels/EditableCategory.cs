using System.Collections.Generic;

namespace DaftAppleGames.AutoLockerLabels_BZ.AutoLockerLabels
{
    internal sealed class EditableCategory
    {
        internal string Id { get; set; }
        internal string DisplayName { get; set; }
        internal bool IsBuiltIn { get; set; }
        internal bool IsModified { get; set; }
        internal List<TechType> TechTypes { get; } = new List<TechType>();
    }
}
