using DaftAppleGames.SubnauticaPets.Pets;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Utility;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.BaseParts
{
    /// <summary>
    ///     Static class for creating a Pet Fabricator Fragment
    /// </summary>
    internal static class PetFabricatorFragmentPrefab
    {
        private const string ClassId = "PetFabricatorFragment";
        private const string PrefabAssetName = "PetFabricatorDamaged.prefab";
        // private const string CloneClassId = "8029a9ce-ab75-46d0-a8ab-63138f6f83e4";
        private const string EncKey = "PetFabricator";
        internal static PrefabInfo Info;

        internal static void Register()
        {
            Info = PrefabInfo
                .WithTechType("PetFabricatorFragment", null, null, unlockAtStart: false);
            var fabricatorFragmentPrefab = new CustomPrefab(Info);

            var damagedFabPrefab =
                ModAssetUtils.GetObjectFromAssetBundle<GameObject>(PrefabAssetName) as GameObject;

            if (!damagedFabPrefab)
            {
                ModDebugLog.LogError("PetFabricator: Could not find prefab asset!");
                return;
            }

            damagedFabPrefab.SetActive(false);

            // Add components
            PrefabUtils.AddBasicComponents(damagedFabPrefab, ClassId, Info.TechType, LargeWorldEntity.CellLevel.Medium);
            PrefabUtils.AddResourceTracker(damagedFabPrefab, TechType.Fragment);
            PetPrefabConfigUtils.ConfigureSkyApplier(damagedFabPrefab);
            PetPrefabConfigUtils.ConfigurePickupable(damagedFabPrefab, false);
            PetPrefabConfigUtils.SetRigidBodyKinematic(damagedFabPrefab, true);
            damagedFabPrefab.AddComponent<PetFabricatorFragment>();

            ModDebugLog.LogDebug("PetFabricatorFragment: SetGameObject...");
            fabricatorFragmentPrefab.SetGameObject(damagedFabPrefab);
            SpawnLocation[] spawnLocations =
            {
                new SpawnLocation(new Vector3(54.17f, -381.63f, -893.97f),
                    new Vector3(301.21f, 39.03f, 154.60f)), // warp 52.56 -379.21 -893.41
                new SpawnLocation(new Vector3(545.30f, -210.05f, -1093.87f),
                    new Vector3(278.35f, 39.87f, 149.11f)), // warp 547.11 -206.15 -1092.51
                new SpawnLocation(new Vector3(267.75f, -233.41f, -1225.20f),
                    new Vector3(346.60f, 330.40f, 179.27f)), // warp 268.03 -231.77 -1226.99
                new SpawnLocation(new Vector3(116.66f, -101.49f, -838.96f),
                    new Vector3(359.60f, 302.26f, 184.42f)), // warp 118.61 -98.51 -839.25
                new SpawnLocation(new Vector3(514.53f, -833.15f, -691.35f),
                    new Vector3(359.55f, 246.30f, 179.84f)), // warp 514.48 -831.69 -693.87
                new SpawnLocation(new Vector3(-1029.30f, 5.70f, -384.70f),
                    new Vector3(279.82f, 58.46f, 243.36f)), // warp -1032.35 7.57 -383.36
                new SpawnLocation(new Vector3(-317.42f, -195.69f, -330.86f),
                    new Vector3(326.91f, 334.81f, 175.76f)), // warp -318.58 -194.50 -331.79
                new SpawnLocation(new Vector3(-251.25f, -128.73f, -239.23f),
                    new Vector3(321.87f, 25.74f, 181.46f)), // warp -252.56 -126.35 -238.21
                new SpawnLocation(new Vector3(-257.13f, -128.71f, -245.16f),
                    new Vector3(272.22f, 329.60f, 136.21f)), // warp -255.338 -127.287 -245.725
                new SpawnLocation(new Vector3(-1000.18f, -46.95f, -316.54f),
                    new Vector3(13.67f, 103.79f, 184.40f)), // warp -1001.00 -43.32 -319.54
                new SpawnLocation(new Vector3(48.86f, -75.44f, -787.47f),
                    new Vector3(282.88f, 129.13f, 66.03f)) // warp 47.44 -73.60 -789.15
            };

            ModDebugLog.LogDebug("PetFabricatorFragment: SetSpawns...");
            fabricatorFragmentPrefab.SetSpawns(spawnLocations);
            ModDebugLog.LogDebug("PetFabricatorFragment: CreateFragment...");
            fabricatorFragmentPrefab.CreateFragment(PetFabricatorPrefab.Info.TechType, 5.0f, 3, EncKey);
            ModDebugLog.LogDebug("PetFabricatorFragment: Register...");
            fabricatorFragmentPrefab.Register();
        }
    }
}