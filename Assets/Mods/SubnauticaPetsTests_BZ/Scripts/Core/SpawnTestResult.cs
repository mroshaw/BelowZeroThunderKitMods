namespace DaftAppleGames.SubnauticaPetsTests
{
    internal enum SpawnTestOutcome
    {
        Passed,
        Failed,
        TimedOut
    }

    internal sealed class SpawnTestResult
    {
        internal readonly SpawnTestCase TestCase;
        internal readonly SpawnTestOutcome Outcome;
        internal readonly string Details;

        internal SpawnTestResult(SpawnTestCase testCase, SpawnTestOutcome outcome, string details)
        {
            TestCase = testCase;
            Outcome = outcome;
            Details = details;
        }
    }
}
