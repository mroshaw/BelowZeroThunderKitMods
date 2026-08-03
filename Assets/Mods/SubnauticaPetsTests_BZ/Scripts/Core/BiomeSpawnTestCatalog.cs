using System.Collections.Generic;

namespace DaftAppleGames.SubnauticaPetsTests
{
    internal static class BiomeSpawnTestCatalog
    {
        internal static List<BiomeSpawnExpectation> Create()
        {
            return new List<BiomeSpawnExpectation>
            {
                new BiomeSpawnExpectation("PengwingAdultPetDna", 22),
                new BiomeSpawnExpectation("PenglingBabyPetDna", 24),
                new BiomeSpawnExpectation("SnowstalkerBabyPetDna", 15),
                new BiomeSpawnExpectation("TrivalveBluePetDna", 22),
                new BiomeSpawnExpectation("TrivalveYellowPetDna", 20),
                new BiomeSpawnExpectation("PinnacaridPetDna", 33)
            };
        }
    }
}
