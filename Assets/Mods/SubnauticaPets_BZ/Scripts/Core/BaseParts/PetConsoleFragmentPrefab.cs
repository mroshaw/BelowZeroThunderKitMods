using DaftAppleGames.SubnauticaPets.Pets;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Utility;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.BaseParts
{
    /// <summary>
    ///     Static class for creating a Pet Console Fragment
    /// </summary>
    internal static class PetConsoleFragmentPrefab
    {
        private const string ClassId = "PetConsoleFragment";
        private const string PrefabAssetName = "PetConsoleDamaged.prefab";
        private const string CloneClassId = "7eaf11d3-5b65-4325-a249-d69c7cc838b0";
        private const string EncKey = "PetConsole";
        internal static PrefabInfo Info;

        /// <summary>
        ///     Initialise Pet Console Fragment prefab
        /// </summary>
        internal static void Register()
        {
            Info = PrefabInfo
                .WithTechType(ClassId, null, null, unlockAtStart: false);
            var consoleFragmentPrefab = new CustomPrefab(Info);

            var damagedConsolePrefab =
                ModAssetUtils.GetObjectFromAssetBundle<GameObject>(PrefabAssetName) as GameObject;

            if (!damagedConsolePrefab)
            {
                ModDebugLog.LogError("PetConsole: Could not find prefab asset!");
                return;
            }

            damagedConsolePrefab.SetActive(false);

            // Configure
            MaterialUtils.ApplySNShaders(damagedConsolePrefab);
            PrefabUtils.AddBasicComponents(damagedConsolePrefab, ClassId, Info.TechType,
                LargeWorldEntity.CellLevel.Medium);
            PrefabUtils.AddResourceTracker(damagedConsolePrefab, TechType.Fragment);
            PetPrefabConfigUtils.ConfigureSkyApplier(damagedConsolePrefab);
            PetPrefabConfigUtils.ConfigurePickupable(damagedConsolePrefab, false);
            PetPrefabConfigUtils.SetRigidBodyKinematic(damagedConsolePrefab, true);
            damagedConsolePrefab.AddComponent<PetConsoleFragment>();

            consoleFragmentPrefab.SetGameObject(damagedConsolePrefab);

            ModDebugLog.LogDebug("PetConsoleFragmentPrefab: SetSpawns...");

            SpawnLocation[] spawnLocations =
            {
                new SpawnLocation(new Vector3(98.44f, -384.53f, -930.38f),
                    new Vector3(55.80f, 80.04f, 101.99f)), // warp 98.44 -382.53 -930.38
                new SpawnLocation(new Vector3(94.51f, -392.88f, -918.59f),
                    new Vector3(77.87f, 278.71f, 198.68f)), // warp 94.51 -390.88 -918.59
                new SpawnLocation(new Vector3(-247.83f, 40.48f, -780.01f),
                    new Vector3(79.41f, 296.13f, 67.11f)), // warp -247.83 42.48 -780.01
                new SpawnLocation(new Vector3(-93.28f, 9.55f, 305.32f),
                    new Vector3(53.20f, 303.42f, 88.92f)), // warp -90.27 10.57 305.48
                new SpawnLocation(new Vector3(56.41f, -75.96f, -793.46f),
                    new Vector3(85.17f, 52.20f, 130.39f)), // warp 53.79 -72.21 -795.16
                new SpawnLocation(new Vector3(110.34f, -36.65f, -3.97f),
                    new Vector3(286.28f, 27.70f, 90.40f)), // warp 110.30 -31.89 -2.63
                new SpawnLocation(new Vector3(-140.43f, -59.09f, -178.51f),
                    new Vector3(281.86f, 158.04f, 52.98f)), // warp -142.73 -56.46 -179.24
                new SpawnLocation(new Vector3(-368.36f, -173.40f, -317.65f),
                    new Vector3(270.00f, 284.05f, 0.00f)), // warp -365.50 -171.18 -319.87
                new SpawnLocation(new Vector3(240.94f, -100.89f, -611.45f),
                    new Vector3(270.00f, 166.68f, 0.00f)), // warp 243.49 -99.22 -613.92
                new SpawnLocation(new Vector3(-287.65f, -17.62f, -11.42f),
                    new Vector3(76.07f, 169.37f, 348.79f)), // warp -289.41 -12.63 -15.73
                new SpawnLocation(new Vector3(-539.43f, -204.23f, -492.26f),
                    new Vector3(284.61f, 172.16f, 198.84f)) // warp -541.18 -202.38 -495.66
            };

            consoleFragmentPrefab.SetSpawns(spawnLocations);
            ModDebugLog.LogDebug("PetConsoleFragmentPrefab: CreateFragment...");
            consoleFragmentPrefab.CreateFragment(PetConsolePrefab.Info.TechType, 5.0f, 3, EncKey);
            ModDebugLog.LogDebug("PetConsoleFragmentPrefab: Register...");
            consoleFragmentPrefab.Register();
        }
    }
}
