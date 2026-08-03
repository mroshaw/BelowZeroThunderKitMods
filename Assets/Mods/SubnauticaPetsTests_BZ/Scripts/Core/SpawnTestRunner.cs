using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DaftAppleGames.SubnauticaPetsTests
{
    internal sealed class SpawnTestRunner : MonoBehaviour
    {
        private const float TeleportHeightOffset = 2.5f;
        private const float StreamingTimeout = 20.0f;
        private const float SpawnSettleDelay = 1.5f;
        private const float SpawnDiscoveryTimeout = 10.0f;
        private const float SpawnPollInterval = 0.25f;
        private const float StreamingBoundsSize = 4.0f;

        private static readonly WaitForSecondsRealtime SpawnPollWait = new WaitForSecondsRealtime(SpawnPollInterval);

        private readonly List<SpawnTestResult> results = new List<SpawnTestResult>();
        private Coroutine runningCoroutine;
        private bool cancelRequested;
        private int currentTestIndex;
        private int totalTestCount;
        private string currentSuite = "none";

        internal string StartRun(string suite)
        {
            if (runningCoroutine != null) return GetStatus();
            if (Player.main == null || GotoConsoleCommand.main == null)
                return "The player or game's teleport controller is not ready.";

            List<SpawnTestCase> testCases = SpawnTestCatalog.Create(suite);
            if (testCases.Count == 0)
                return "Unknown suite. Use: pettest run all|fragments|dna|biome";

            cancelRequested = false;
            currentSuite = suite.ToLowerInvariant();
            currentTestIndex = 0;
            totalTestCount = testCases.Count;
            results.Clear();
            runningCoroutine = StartCoroutine(RunTests(testCases));
            return $"Started '{currentSuite}' Pet spawn tests ({totalTestCount} cases). Progress is written to Player.log.";
        }

        internal string GetStatus()
        {
            if (runningCoroutine == null)
            {
                if (results.Count == 0) return "No Pet test run is active and no run has completed this session.";
                return BuildSummary("Last run");
            }

            return $"Pet test suite '{currentSuite}' is running: {currentTestIndex + 1}/{totalTestCount}.";
        }

        internal string Cancel()
        {
            if (runningCoroutine == null) return "No Pet test run is active.";
            cancelRequested = true;
            return "Cancellation requested. The player will be returned after the current wait completes.";
        }

        private IEnumerator RunTests(List<SpawnTestCase> testCases)
        {
            Vector3 originalPlayerPosition = Player.main.transform.position;
            Log($"RUN suite={currentSuite}; cases={testCases.Count}; started={DateTime.UtcNow:O}");

            for (currentTestIndex = 0; currentTestIndex < testCases.Count; currentTestIndex++)
            {
                if (cancelRequested) break;

                SpawnTestCase testCase = testCases[currentTestIndex];
                yield return RunTest(testCase);
            }

            if (Player.main != null && GotoConsoleCommand.main != null)
                GotoConsoleCommand.main.GotoPosition(originalPlayerPosition);

            string heading = cancelRequested ? "CANCELLED" : "COMPLETE";
            Log(BuildSummary(heading));
            runningCoroutine = null;
        }

        private IEnumerator RunTest(SpawnTestCase testCase)
        {
            Vector3 teleportPosition = testCase.ExpectedPosition + Vector3.up * TeleportHeightOffset;
            Log($"BEGIN {testCase.Name}; classId={testCase.ClassId}; expected={FormatPosition(testCase.ExpectedPosition)}");
            GotoConsoleCommand.main.GotoPosition(teleportPosition);
            yield return null;

            float timeoutAt = Time.realtimeSinceStartup + StreamingTimeout;
            Bounds streamingBounds = new Bounds(testCase.ExpectedPosition, Vector3.one * StreamingBoundsSize);
            while (!IsRangeReady(streamingBounds) && Time.realtimeSinceStartup < timeoutAt)
            {
                if (cancelRequested) yield break;
                yield return null;
            }

            if (!IsRangeReady(streamingBounds))
            {
                Record(new SpawnTestResult(testCase, SpawnTestOutcome.TimedOut,
                    $"streaming range was not ready after {StreamingTimeout:F1}s"));
                yield break;
            }

            float settleUntil = Time.realtimeSinceStartup + SpawnSettleDelay;
            while (Time.realtimeSinceStartup < settleUntil)
            {
                if (cancelRequested) yield break;
                yield return null;
            }

            float discoveryTimeoutAt = Time.realtimeSinceStartup + SpawnDiscoveryTimeout;
            SpawnTestResult result = Evaluate(testCase);
            while (result.Outcome != SpawnTestOutcome.Passed && Time.realtimeSinceStartup < discoveryTimeoutAt)
            {
                if (cancelRequested) yield break;
                yield return SpawnPollWait;
                result = Evaluate(testCase);
            }

            Record(result);
        }

        private static bool IsRangeReady(Bounds bounds)
        {
            return LargeWorldStreamer.main != null && LargeWorldStreamer.main.IsRangeActiveAndBuilt(bounds);
        }

        private static SpawnTestResult Evaluate(SpawnTestCase testCase)
        {
            PrefabIdentifier[] identifiers = Object.FindObjectsOfType<PrefabIdentifier>();
            PrefabIdentifier nearestIdentifier = null;
            float nearestDistance = float.MaxValue;

            for (int identifierIndex = 0; identifierIndex < identifiers.Length; identifierIndex++)
            {
                PrefabIdentifier identifier = identifiers[identifierIndex];
                if (!identifier || !string.Equals(identifier.ClassId, testCase.ClassId, StringComparison.Ordinal))
                    continue;

                float distance = Vector3.Distance(identifier.transform.position, testCase.ExpectedPosition);
                if (distance >= nearestDistance) continue;
                nearestIdentifier = identifier;
                nearestDistance = distance;
            }

            if (!nearestIdentifier)
                return new SpawnTestResult(testCase, SpawnTestOutcome.Failed,
                    $"no active instance with ClassId '{testCase.ClassId}' was found");

            Vector3 positionDelta = nearestIdentifier.transform.position - testCase.ExpectedPosition;
            float horizontalDistance = new Vector2(positionDelta.x, positionDelta.z).magnitude;
            bool verticalPositionValid = positionDelta.y >= -testCase.MaximumDownwardDistance &&
                                         positionDelta.y <= testCase.MaximumUpwardDistance;
            if (horizontalDistance > testCase.HorizontalTolerance || !verticalPositionValid)
                return new SpawnTestResult(testCase, SpawnTestOutcome.Failed,
                    $"nearest instance was at {FormatPosition(nearestIdentifier.transform.position)}; " +
                    $"horizontalDistance={horizontalDistance:F2}m; verticalDelta={positionDelta.y:F2}m; " +
                    $"horizontalTolerance={testCase.HorizontalTolerance:F2}m; " +
                    $"verticalRange=-{testCase.MaximumDownwardDistance:F2}m..+{testCase.MaximumUpwardDistance:F2}m");

            string missingComponents = GetMissingComponents(nearestIdentifier.gameObject, testCase.RequiredComponentNames);
            if (missingComponents.Length > 0)
                return new SpawnTestResult(testCase, SpawnTestOutcome.Failed,
                    $"instance at {FormatPosition(nearestIdentifier.transform.position)} is missing: {missingComponents}");

            return new SpawnTestResult(testCase, SpawnTestOutcome.Passed,
                $"actual={FormatPosition(nearestIdentifier.transform.position)}; " +
                $"horizontalDistance={horizontalDistance:F2}m; verticalDelta={positionDelta.y:F2}m");
        }

        private static string GetMissingComponents(GameObject gameObject, string[] requiredComponentNames)
        {
            Component[] components = gameObject.GetComponents<Component>();
            StringBuilder missing = new StringBuilder();

            for (int requiredIndex = 0; requiredIndex < requiredComponentNames.Length; requiredIndex++)
            {
                string requiredName = requiredComponentNames[requiredIndex];
                bool found = false;
                for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    Component component = components[componentIndex];
                    if (component && component.GetType().Name == requiredName)
                    {
                        found = true;
                        break;
                    }
                }

                if (found) continue;
                if (missing.Length > 0) missing.Append(", ");
                missing.Append(requiredName);
            }

            return missing.ToString();
        }

        private void Record(SpawnTestResult result)
        {
            results.Add(result);
            Log($"{ToLabel(result.Outcome)} {result.TestCase.Name}; {result.Details}");
        }

        private string BuildSummary(string heading)
        {
            int passed = 0;
            int failed = 0;
            int timedOut = 0;
            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                switch (results[resultIndex].Outcome)
                {
                    case SpawnTestOutcome.Passed:
                        passed++;
                        break;
                    case SpawnTestOutcome.Failed:
                        failed++;
                        break;
                    case SpawnTestOutcome.TimedOut:
                        timedOut++;
                        break;
                }
            }

            return $"{heading} suite={currentSuite}; passed={passed}; failed={failed}; timedOut={timedOut}; " +
                   $"completed={results.Count}/{totalTestCount}";
        }

        private static string ToLabel(SpawnTestOutcome outcome)
        {
            switch (outcome)
            {
                case SpawnTestOutcome.Passed:
                    return "PASS";
                case SpawnTestOutcome.TimedOut:
                    return "TIMEOUT";
                default:
                    return "FAIL";
            }
        }

        private static string FormatPosition(Vector3 position)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:F2},{1:F2},{2:F2})",
                position.x, position.y, position.z);
        }

        private static void Log(string message)
        {
            SubnauticaPetsTestsPlugin.Log.LogInfo($"[PetTests] {message}");
        }
    }
}
