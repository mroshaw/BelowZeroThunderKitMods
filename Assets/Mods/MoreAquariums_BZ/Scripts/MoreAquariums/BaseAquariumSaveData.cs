using System.Collections.Generic;
using Nautilus.Json;
using Nautilus.Json.Attributes;

namespace DaftAppleGames.MoreAquariums
{
    /// <summary>
    /// Persists aquarium base-cell identities that the vanilla base grid cannot store.
    /// </summary>
    [FileName("MoreAquariumsBasePieces")]
    internal class BaseAquariumSaveData : SaveDataCache
    {
        public List<BaseAquariumLocation> Locations =
            new List<BaseAquariumLocation>();
    }
}
