using System;
using Nautilus.Commands;

namespace DaftAppleGames.SubnauticaPetsTests
{
    internal static class PetTestCommand
    {
        [ConsoleCommand("pettest")]
        public static string HandleCommand(string action = "status", string suite = "all")
        {
            SubnauticaPetsTestsPlugin plugin = SubnauticaPetsTestsPlugin.Instance;
            if (plugin == null) return "The Subnautica Pets test plugin is not ready.";

            SpawnTestRunner runner = plugin.GetTestRunner();
            if (string.Equals(action, "run", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(suite, "biome", StringComparison.OrdinalIgnoreCase))
                    return plugin.GetBiomeTestRunner().Run();
                if (string.Equals(suite, "all", StringComparison.OrdinalIgnoreCase))
                {
                    string biomeResult = plugin.GetBiomeTestRunner().Run();
                    return biomeResult + " " + runner.StartRun(suite);
                }
                return runner.StartRun(suite);
            }
            if (string.Equals(action, "status", StringComparison.OrdinalIgnoreCase))
                return runner.GetStatus();
            if (string.Equals(action, "cancel", StringComparison.OrdinalIgnoreCase))
                return runner.Cancel();

            return "Usage: pettest run all|fragments|dna|biome | pettest status | pettest cancel";
        }
    }
}
