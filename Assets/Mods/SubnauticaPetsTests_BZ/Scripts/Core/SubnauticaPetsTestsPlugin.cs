using BepInEx;
using BepInEx.Logging;
using Nautilus.Handlers;

namespace DaftAppleGames.SubnauticaPetsTests
{
    [BepInDependency("com.daftapplegames.subnauticapets2")]
    [BepInDependency("com.snmodding.nautilus")]
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class SubnauticaPetsTestsPlugin : BaseUnityPlugin
    {
        private const string PluginGuid = "com.daftapplegames.subnauticapets2.tests";
        private const string PluginName = "SubnauticaPets2Tests";
        private const string PluginVersion = "0.1.0";

        internal static SubnauticaPetsTestsPlugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private SpawnTestRunner testRunner;
        private BiomeSpawnTestRunner biomeTestRunner;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            testRunner = gameObject.AddComponent<SpawnTestRunner>();
            biomeTestRunner = new BiomeSpawnTestRunner();
            ConsoleCommandsHandler.RegisterConsoleCommands(typeof(PetTestCommand));
            Logger.LogInfo("Development test plugin loaded. Run 'pettest run all' to begin.");
        }

        internal SpawnTestRunner GetTestRunner()
        {
            return testRunner;
        }

        internal BiomeSpawnTestRunner GetBiomeTestRunner()
        {
            return biomeTestRunner;
        }
    }
}
