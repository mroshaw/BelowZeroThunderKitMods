using DaftAppleGames.SubnauticaPets.Pets;
using DaftAppleGames.SubnauticaPets.Utils;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Utility;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.BaseParts
{
    /// <summary>
    /// Static class for creating a Pet Console Fragment
    /// </summary>
    internal static class PetConsoleFragmentPrefab
    {
        internal static PrefabInfo Info;
        private const string PrefabAssetName = "PetConsoleDamaged.prefab";
        private const string ClassId = "PetConsoleFragment";
        private const string CloneClassId = "7eaf11d3-5b65-4325-a249-d69c7cc838b0";
        private const string EncKey = "PetConsole";
        
        /// <summary>
        /// Initialise Pet Console Fragment prefab
        /// </summary>
        internal static void Register()
        {
            Info = PrefabInfo
                .WithTechType(ClassId, null, null, unlockAtStart: false);
            CustomPrefab consoleFragmentPrefab = new CustomPrefab(Info);

            // Base upgrade console fragment
            CloneTemplate cloneTemplate = new CloneTemplate(Info, CloneClassId)

            {
                ModifyPrefab = obj =>
                {
                    if (!obj)
                    {
                        LogUtils.LogError(LogArea.Prefabs, $"PetConsoleFragmentPrefab cloned obj is null!");
                    }
                    LogUtils.LogDebug(LogArea.Prefabs, $"ConsoleFragmentPrefab cloned. Obj is: {obj.name}");

                    obj.SetActive(false);

                    GameObject damagedConsoleGameObject =
                        CustomAssetBundleUtils.GetPrefabInstanceFromAssetBundle(PrefabAssetName, false);
                    
                    GameObject newModelGameObject = damagedConsoleGameObject.FindChild("newmodel");

                    if (!newModelGameObject)
                    {
                        LogUtils.LogError(LogArea.Prefabs, $"PetConsoleFragmentPrefab: Unable to find 'newmodel' in prefab: {PrefabAssetName}!");
                        return;
                    }

                    // Find old model and replace
                    GameObject oldModelGameObject = obj.FindChild("model");

                    if (!oldModelGameObject)
                    {
                        LogUtils.LogError(LogArea.Prefabs, $"PetConsoleFragmentPrefab: Couldn't find old model! All children:");
                        return;
                    }

                    LogUtils.LogDebug(LogArea.Prefabs, "Found old model!");

                    newModelGameObject.transform.SetParent(oldModelGameObject.transform.parent);
                    newModelGameObject.transform.localPosition = new Vector3(0, 0, 0);
                    newModelGameObject.transform.localRotation = new Quaternion(0, 0, 0, 0);

                    oldModelGameObject.SetActive(false);

                    // Configure
                    MaterialUtils.ApplySNShaders(newModelGameObject);
                    PrefabUtils.AddBasicComponents(obj, ClassId, Info.TechType, LargeWorldEntity.CellLevel.Medium);
                    PrefabUtils.AddResourceTracker(obj, TechType.Fragment);
                    PetPrefabConfigUtils.ConfigureSkyApplier(obj);
                    PetPrefabConfigUtils.UpdatePickupable(obj, false);
                    PetPrefabConfigUtils.SetRigidBodyKinematic(obj, true);
                    obj.AddComponent<PetConsoleFragment>();
                }
            };

            LogUtils.LogDebug(LogArea.Prefabs, "PetConsoleFragmentPrefab: SetGameObject...");
            consoleFragmentPrefab.SetGameObject(cloneTemplate);

            LogUtils.LogDebug(LogArea.Prefabs, "PetConsoleFragmentPrefab: SetSpawns...");

            SpawnLocation[] spawnLocations =
            {
                new SpawnLocation(new Vector3(46.47f, -382.91f, -922.39f), new Vector3(0.04f, -0.03f, 0.17f)), // warp 46.47 -382.91 -922.39 (goto margbase)
                new SpawnLocation(new Vector3(-260.88f, 40.13f, -773.85f), new Vector3(-0.02f, 0.00f, -0.07f)), // warp -260.88 40.13 -773.85 (goto deltabase)
                new SpawnLocation(new Vector3(-94.00f, 9.00f, 302.18f), new Vector3(0.03f, -0.46f, -0.01f)), // warp -94.00 9.00 302.18 (goto outpostzero)
                new SpawnLocation(new Vector3(12.66f, -92.07f, -784.40f), new Vector3(-0.10f, -0.07f, 0.16f)), // warp 12.66 -92.07 -784.40 (goto crashedship1)
                new SpawnLocation(new Vector3(115.28f, -36.56f, -2.23f), new Vector3(0.01f, -0.02f, 0.07f)), // warp 115.28 -36.56 -2.23 (goto kelptechsite1)
                new SpawnLocation(new Vector3(-137.69f, -58.83f, -175.80f), new Vector3(-0.10f, 0.03f, -0.06f)), // warp -137.69 -58.83 -175.80 (goto twistytechsite1)
                new SpawnLocation(new Vector3(-378.69f, -175.21f, -318.37f), new Vector3(-0.10f, 0.02f, 0.16f)), // warp -378.69 -175.21 -318.37 (goto twistytechsite3)
                new SpawnLocation(new Vector3(240.52f, -100.88f, -612.13f), new Vector3(0.00f, 0.91f, 0.00f)), // warp 240.52 -100.88 -612.13 (goto purpleventsalvage)
                new SpawnLocation(new Vector3(-282.83f, -16.89f, -14.45f), new Vector3(0.03f, 0.99f, 0.07f)), // warp -282.83 -16.89 -14.45
                new SpawnLocation(new Vector3(-538.61f, -207.98f, -501.29f), new Vector3(-0.01f, 0.67f, 0.00f)), // warp -538.61 -207.98 -501.29 (goto sanctuary)
                };

            consoleFragmentPrefab.SetSpawns(spawnLocations);
            LogUtils.LogDebug(LogArea.Prefabs, "PetConsoleFragmentPrefab: CreateFragment...");
            consoleFragmentPrefab.CreateFragment(PetConsolePrefab.Info.TechType, 5.0f, 3, EncKey, true, true);
            LogUtils.LogDebug(LogArea.Prefabs, "PetConsoleFragmentPrefab: Register...");
            consoleFragmentPrefab.Register();
        }
    }
}