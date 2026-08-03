using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DaftAppleGames.SubnauticaPetsTests
{
    internal sealed class BiomeSpawnTestRunner
    {
        private const int ExpectedSpawnCount = 1;
        private const float ProbabilityTolerance = 0.0001f;

        internal string Run()
        {
            List<BiomeSpawnExpectation> expectations = BiomeSpawnTestCatalog.Create();
            LootDistributionData distribution = LootDistributionData.Load(LootDistributionData.dataPath);
            if (distribution == null || distribution.srcDistribution == null)
                return LogAndReturn("BIOME FAIL; the live loot distribution could not be loaded");

            Dictionary<string, int> loadedInstances = CountLoadedInstances();
            int passed = 0;
            int failed = 0;
            int warnings = 0;
            Log($"BIOME RUN; classes={expectations.Count}");

            for (int index = 0; index < expectations.Count; index++)
            {
                BiomeSpawnExpectation expectation = expectations[index];
                string failure = ValidateRegistration(distribution, expectation);
                if (!string.IsNullOrEmpty(failure))
                {
                    failed++;
                    Log($"BIOME FAIL {expectation.ClassId}; {failure}");
                    continue;
                }

                passed++;
                int instanceCount;
                loadedInstances.TryGetValue(expectation.ClassId, out instanceCount);
                Log($"BIOME PASS {expectation.ClassId}; registeredBiomes={expectation.ExpectedBiomeCount}; " +
                    $"loadedInstances={instanceCount}");
                if (instanceCount == 0)
                {
                    warnings++;
                    Log($"BIOME WARN {expectation.ClassId}; registration is valid, but no matching active " +
                        "instance can currently be observed; this is not a deterministic failure");
                }
            }

            return LogAndReturn($"BIOME COMPLETE; passed={passed}; failed={failed}; warnings={warnings}; " +
                                $"classes={expectations.Count}");
        }

        private static string ValidateRegistration(LootDistributionData distribution,
            BiomeSpawnExpectation expectation)
        {
            LootDistributionData.SrcData sourceData;
            if (!distribution.GetPrefabData(expectation.ClassId, out sourceData) || sourceData == null)
                return "ClassId is absent from the live loot distribution";
            if (sourceData.distribution == null)
                return "biome distribution is null";
            if (sourceData.distribution.Count != expectation.ExpectedBiomeCount)
                return $"expected {expectation.ExpectedBiomeCount} biome entries, found {sourceData.distribution.Count}";

            HashSet<BiomeType> seenBiomes = new HashSet<BiomeType>();
            for (int index = 0; index < sourceData.distribution.Count; index++)
            {
                LootDistributionData.BiomeData biomeData = sourceData.distribution[index];
                if (!seenBiomes.Add(biomeData.biome))
                    return $"duplicate biome entry: {biomeData.biome}";
                if (biomeData.count != ExpectedSpawnCount)
                    return $"{biomeData.biome} has count={biomeData.count}, expected {ExpectedSpawnCount}";
                if (biomeData.probability <= ProbabilityTolerance)
                    return $"{biomeData.biome} has a non-positive probability ({biomeData.probability})";

                LootDistributionData.DstData destinationData;
                if (!distribution.GetBiomeLoot(biomeData.biome, out destinationData) || destinationData == null ||
                    !ContainsMatchingDestination(destinationData, expectation.ClassId, biomeData))
                    return $"{biomeData.biome} is missing the matching reverse lookup entry";
            }

            return string.Empty;
        }

        private static bool ContainsMatchingDestination(LootDistributionData.DstData destinationData, string classId,
            LootDistributionData.BiomeData expected)
        {
            if (destinationData.prefabs == null) return false;
            for (int index = 0; index < destinationData.prefabs.Count; index++)
            {
                LootDistributionData.PrefabData prefabData = destinationData.prefabs[index];
                if (string.Equals(prefabData.classId, classId, StringComparison.Ordinal) &&
                    prefabData.count == expected.count &&
                    Mathf.Abs(prefabData.probability - expected.probability) <= ProbabilityTolerance)
                    return true;
            }

            return false;
        }

        private static Dictionary<string, int> CountLoadedInstances()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            PrefabIdentifier[] identifiers = Object.FindObjectsOfType<PrefabIdentifier>();
            for (int index = 0; index < identifiers.Length; index++)
            {
                PrefabIdentifier identifier = identifiers[index];
                if (!identifier || string.IsNullOrEmpty(identifier.ClassId) ||
                    !identifier.ClassId.EndsWith("PetDna", StringComparison.Ordinal))
                    continue;

                int count;
                counts.TryGetValue(identifier.ClassId, out count);
                counts[identifier.ClassId] = count + 1;
            }

            return counts;
        }

        private static string LogAndReturn(string message)
        {
            Log(message);
            return message;
        }

        private static void Log(string message)
        {
            SubnauticaPetsTestsPlugin.Log.LogInfo($"[PetTests] {message}");
        }
    }
}
