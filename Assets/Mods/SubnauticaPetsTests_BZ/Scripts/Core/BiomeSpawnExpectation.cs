namespace DaftAppleGames.SubnauticaPetsTests
{
    internal sealed class BiomeSpawnExpectation
    {
        internal readonly string ClassId;
        internal readonly int ExpectedBiomeCount;

        internal BiomeSpawnExpectation(string classId, int expectedBiomeCount)
        {
            ClassId = classId;
            ExpectedBiomeCount = expectedBiomeCount;
        }
    }
}
