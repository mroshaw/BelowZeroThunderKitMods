using System;
using System.Collections.Generic;
using DaftAppleGames.SubnauticaPets.Pets;
using Nautilus.Json;
using Nautilus.Json.Attributes;

namespace DaftAppleGames.SubnauticaPets
{
    [FileName("SubnauticaPets")] internal class SaveData : SaveDataCache
    {
        private static Version LatestSaveDataVersion = new Version(1, 0, 0, 0);

        public Version LatestSaveVersion = LatestSaveDataVersion;
        public DateTime SaveDateTime = DateTime.Now;
        public Version SaveDataVersion = LatestSaveDataVersion;
        public HashSet<PetSaver.PetDetails> PetDetailsHashSet { get; set; }
    }
}
