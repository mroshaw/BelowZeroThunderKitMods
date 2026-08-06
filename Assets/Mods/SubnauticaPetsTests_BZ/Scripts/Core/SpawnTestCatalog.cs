using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPetsTests
{
    internal static class SpawnTestCatalog
    {
        private const float FragmentTolerance = 1.0f;
        private const float DnaTolerance = 1.26f;
        private const float FragmentVerticalTolerance = 1.0f;
        private const float DnaDownwardSettlementTolerance = 12.0f;
        private const float DnaUpwardTolerance = 2.0f;

        internal static List<SpawnTestCase> Create(string suite)
        {
            List<SpawnTestCase> testCases = new List<SpawnTestCase>();
            bool includeFragments = string.Equals(suite, "all", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(suite, "fragments", StringComparison.OrdinalIgnoreCase);
            bool includeDna = string.Equals(suite, "all", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(suite, "dna", StringComparison.OrdinalIgnoreCase);

            if (includeFragments) AddFragmentTests(testCases);
            if (includeDna) AddDnaTests(testCases);
            return testCases;
        }

        private static void AddFragmentTests(List<SpawnTestCase> testCases)
        {
            AddFragment(testCases, "ConsoleFragment[0]", "PetConsoleFragment", 98.44f, -384.53f, -930.38f);
            AddFragment(testCases, "ConsoleFragment[1]", "PetConsoleFragment", 94.51f, -392.88f, -918.59f);
            AddFragment(testCases, "ConsoleFragment[2]", "PetConsoleFragment", -247.83f, 40.48f, -780.01f);
            AddFragment(testCases, "ConsoleFragment[3]", "PetConsoleFragment", -93.28f, 9.55f, 305.32f);
            AddFragment(testCases, "ConsoleFragment[4]", "PetConsoleFragment", 56.41f, -75.96f, -793.46f);
            AddFragment(testCases, "ConsoleFragment[5]", "PetConsoleFragment", 110.34f, -36.65f, -3.97f);
            AddFragment(testCases, "ConsoleFragment[6]", "PetConsoleFragment", -140.43f, -59.09f, -178.51f);
            AddFragment(testCases, "ConsoleFragment[7]", "PetConsoleFragment", -368.36f, -173.40f, -317.65f);
            AddFragment(testCases, "ConsoleFragment[8]", "PetConsoleFragment", 240.94f, -100.89f, -611.45f);
            AddFragment(testCases, "ConsoleFragment[9]", "PetConsoleFragment", -287.65f, -17.62f, -11.42f);
            AddFragment(testCases, "ConsoleFragment[10]", "PetConsoleFragment", -539.43f, -204.23f, -492.26f);

            AddFragment(testCases, "FabricatorFragment[0]", "PetFabricatorFragment", 54.17f, -381.63f, -893.97f);
            AddFragment(testCases, "FabricatorFragment[1]", "PetFabricatorFragment", 545.30f, -210.05f, -1093.87f);
            AddFragment(testCases, "FabricatorFragment[2]", "PetFabricatorFragment", 267.75f, -233.41f, -1225.20f);
            AddFragment(testCases, "FabricatorFragment[3]", "PetFabricatorFragment", 116.66f, -101.49f, -838.96f);
            AddFragment(testCases, "FabricatorFragment[4]", "PetFabricatorFragment", 514.53f, -833.15f, -691.35f);
            AddFragment(testCases, "FabricatorFragment[5]", "PetFabricatorFragment", -1029.30f, 5.70f, -384.70f);
            AddFragment(testCases, "FabricatorFragment[6]", "PetFabricatorFragment", -317.42f, -195.69f, -330.86f);
            AddFragment(testCases, "FabricatorFragment[7]", "PetFabricatorFragment", -251.25f, -128.73f, -239.23f);
            AddFragment(testCases, "FabricatorFragment[8]", "PetFabricatorFragment", -257.13f, -128.71f, -245.16f);
            AddFragment(testCases, "FabricatorFragment[9]", "PetFabricatorFragment", -1000.18f, -46.95f, -316.54f);
            AddFragment(testCases, "FabricatorFragment[10]", "PetFabricatorFragment", 48.86f, -75.44f, -787.47f);
        }

        private static void AddDnaTests(List<SpawnTestCase> testCases)
        {
            AddDnaCluster(testCases, "PenglingBabyDna[0]", "PenglingBabyPetDna", -90.27f, 10.57f, 305.48f, 2);
            AddDnaCluster(testCases, "PenglingBabyDna[1]", "PenglingBabyPetDna", 110.30f, -31.89f, -2.63f, 3);
            AddDnaCluster(testCases, "PenglingBabyDna[2]", "PenglingBabyPetDna", 47.44f, -73.60f, -789.15f, 4);

            AddDnaCluster(testCases, "PengwingAdultDna[0]", "PengwingAdultPetDna", 53.79f, -72.21f, -795.16f, 2);
            AddDnaCluster(testCases, "PengwingAdultDna[1]", "PengwingAdultPetDna", -142.73f, -56.46f, -179.24f, 3);
            AddDnaCluster(testCases, "PengwingAdultDna[2]", "PengwingAdultPetDna", -289.41f, -12.63f, -15.73f, 4);
            AddDnaCluster(testCases, "PengwingAdultDna[3]", "PengwingAdultPetDna", 118.61f, -98.51f, -839.25f, 5);

            AddDnaCluster(testCases, "SnowstalkerBabyDna[0]", "SnowstalkerBabyPetDna", -245.70f, 41.95f, -779.69f, 3);
            AddDnaCluster(testCases, "SnowstalkerBabyDna[1]", "SnowstalkerBabyPetDna", -1032.35f, 7.57f, -383.36f, 4);
            AddDnaCluster(testCases, "SnowstalkerBabyDna[2]", "SnowstalkerBabyPetDna", -1001.00f, -43.32f, -319.54f, 2, 2);

            AddDnaCluster(testCases, "TrivalveBlueDna[0]", "TrivalveBluePetDna", 97.62f, -383.40f, -929.72f, 2);
            AddDnaCluster(testCases, "TrivalveBlueDna[1]", "TrivalveBluePetDna", 243.49f, -99.22f, -613.92f, 3);
            AddDnaCluster(testCases, "TrivalveBlueDna[2]", "TrivalveBluePetDna", 52.56f, -379.21f, -893.41f, 4);
            AddDnaCluster(testCases, "TrivalveBlueDna[3]", "TrivalveBluePetDna", -318.58f, -194.50f, -331.79f, 2);

            AddDnaCluster(testCases, "TrivalveYellowDna[0]", "TrivalveYellowPetDna", 95.17f, -388.81f, -919.84f, 3);
            AddDnaCluster(testCases, "TrivalveYellowDna[1]", "TrivalveYellowPetDna", 268.03f, -231.77f, -1226.99f, 4);
            AddDnaCluster(testCases, "TrivalveYellowDna[2]", "TrivalveYellowPetDna", 514.48f, -831.69f, -693.87f, 3);
            AddDnaCluster(testCases, "TrivalveYellowDna[3]", "TrivalveYellowPetDna", -255.338f, -127.287f, -245.725f, 2);

            AddDnaCluster(testCases, "PinnacaridDna[0]", "PinnacaridPetDna", -365.50f, -171.18f, -319.87f, 2);
            AddDnaCluster(testCases, "PinnacaridDna[1]", "PinnacaridPetDna", -541.18f, -202.38f, -495.66f, 3);
            AddDnaCluster(testCases, "PinnacaridDna[2]", "PinnacaridPetDna", 547.11f, -206.15f, -1092.51f, 4);
            AddDnaCluster(testCases, "PinnacaridDna[3]", "PinnacaridPetDna", -252.56f, -126.35f, -238.21f, 4);
        }

        private static void AddDnaCluster(List<SpawnTestCase> testCases, string clusterName, string classId,
            float x, float y, float z, int count)
        {
            AddDnaCluster(testCases, clusterName, classId, x, y, z, count, 0);
        }

        private static void AddDnaCluster(List<SpawnTestCase> testCases, string clusterName, string classId,
            float x, float y, float z, int count, int firstOffsetIndex)
        {
            Vector3 center = new Vector3(x, y, z);
            Vector3[] offsets =
            {
                Vector3.zero,
                new Vector3(3.0f, 0.0f, 0.0f),
                new Vector3(-1.5f, 0.0f, 2.6f),
                new Vector3(-1.5f, 0.0f, -2.6f),
                new Vector3(0.0f, 0.0f, 4.5f)
            };

            for (int sampleIndex = 0; sampleIndex < count; sampleIndex++)
            {
                Vector3 position = center + offsets[firstOffsetIndex + sampleIndex];
                AddDna(testCases, $"{clusterName}.{sampleIndex}", classId, position.x, position.y, position.z);
            }
        }

        private static void AddFragment(List<SpawnTestCase> testCases, string name, string classId, float x, float y,
            float z)
        {
            string fragmentComponentName = classId == "PetConsoleFragment"
                ? "PetConsoleFragment"
                : "PetFabricatorFragment";
            testCases.Add(new SpawnTestCase(name, classId, new Vector3(x, y, z), FragmentTolerance,
                FragmentVerticalTolerance, FragmentVerticalTolerance,
                "ResourceTracker", "LargeWorldEntity", fragmentComponentName));
        }

        private static void AddDna(List<SpawnTestCase> testCases, string name, string classId, float x, float y,
            float z)
        {
            testCases.Add(new SpawnTestCase(name, classId, new Vector3(x, y, z), DnaTolerance,
                DnaDownwardSettlementTolerance, DnaUpwardTolerance,
                "Pickupable", "ResourceTracker", "LargeWorldEntity", "PetDna"));
        }
    }
}
