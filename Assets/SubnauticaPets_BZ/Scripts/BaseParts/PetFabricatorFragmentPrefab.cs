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
    /// Static class for creating a Pet Fabricator Fragment
    /// </summary>
    internal static class PetFabricatorFragmentPrefab
    {
        internal static PrefabInfo Info;
        private const string ClassId = "PetFabricatorFragment";
        private const string CloneClassId = "8029a9ce-ab75-46d0-a8ab-63138f6f83e4";
        private const string EncKey = "PetFabricator";
        
        internal static void Register()
        {
            Info = PrefabInfo
                .WithTechType("PetFabricatorFragment", null, null, unlockAtStart: false);
            CustomPrefab fabricatorFragmentPrefab = new CustomPrefab(Info);

            // Submarine workbench (damaged)
            CloneTemplate cloneTemplate = new CloneTemplate(Info, CloneClassId)

            {
                ModifyPrefab = obj =>
                {
                    if (!obj)
                    {
                        LogUtils.LogError(LogArea.Prefabs, $"FabricatorFragmentPrefab cloned obj is null!");
                    }
                    LogUtils.LogDebug(LogArea.Prefabs, $"FabricatorFragmentPrefab cloned. Obj is: {obj.name}");
                    obj.SetActive(false);
                    // Add components
                    PrefabUtils.AddBasicComponents(obj, ClassId, Info.TechType, LargeWorldEntity.CellLevel.Medium);
                    PrefabUtils.AddResourceTracker(obj, TechType.Fragment);
                    PetPrefabConfigUtils.ConfigureSkyApplier(obj);
                    PetPrefabConfigUtils.UpdatePickupable(obj, false);
                    PetPrefabConfigUtils.SetRigidBodyKinematic(obj, true);
                    PetPrefabConfigUtils.ResizeCollider(obj, new Vector3(0.0f, 0.61f, 0.24f), new Vector3(1.02f, 1.2f, 0.52f));
                    obj.AddComponent<PetFabricatorFragment>();
                }
            };
            LogUtils.LogDebug(LogArea.Prefabs, "PetFabricatorFragment: SetGameObject...");
            fabricatorFragmentPrefab.SetGameObject(cloneTemplate);
            SpawnLocation[] spawnLocations =
            {
                new SpawnLocation(new Vector3(-388.22f, -149.85f, -837.85f), new Vector3(0.02f, -0.01f, 0.01f)), // warp -388.22 -149.85 -837.85 (goto miningsite)
                new SpawnLocation(new Vector3(548.66f, -210.13f, -1093.88f), new Vector3(-0.06f, 0.03f, 0.08f)), // warp 548.66 -210.13 -1093.88 (goto outpostomega)
                new SpawnLocation(new Vector3(267.22f, -233.91f, -1227.16f), new Vector3(-0.11f, -0.14f, -0.06f)), // warp 267.22 -233.91 -1227.16 goto crashedship2)
                new SpawnLocation(new Vector3(-2.70f, -81.76f, -834.83f), new Vector3(-0.09f, 0.06f, -0.10f)), // warp -2.70 -81.76 -834.83 (goto shipwrecksalvage) 
                new SpawnLocation(new Vector3(520.79f, -833.54f, -686.17f), new Vector3(0.00f, 0.56f, 0.82f)), // warp 520.79 -833.54 -686.17 (goto crystalcastlecache)
                new SpawnLocation(new Vector3(-1027.07f, 6.04f, -385.73f), new Vector3(0.24f, 0.40f, -0.44f)), // warp -1027.07 6.04 -385.73 (goto glacialbasinlandbeacon)
                new SpawnLocation(new Vector3(-317.56f, -196.14f, -332.46f), new Vector3(0.78f, 0.04f, 0.38f)), // warp -317.56 -196.14 -332.46 (goto twistybridgesvalleyfloor)
                new SpawnLocation(new Vector3(-254.23f, -129.07f, -239.95f), new Vector3(0.00f, 0.94f, 0.01f)), // warp -254.23 -129.07 -239.95 (goto twistytechsite2)
                new SpawnLocation(new Vector3(-1004.19f, -46.03f, -318.50f), new Vector3(-0.34f, 0.61f, 0.52f)), // warp -1004.19 -46.03 -318.50 (goto glacialbasindock)
                new SpawnLocation(new Vector3(49.46f, -75.72f, -790.23f), new Vector3(-0.02f, 0.74f, 0.66f)), // warp 49.46 -75.72 -790.23 (goto crashedship1)
                };

            LogUtils.LogDebug(LogArea.Prefabs, "PetFabricatorFragment: SetSpawns...");
            fabricatorFragmentPrefab.SetSpawns(spawnLocations);
            LogUtils.LogDebug(LogArea.Prefabs, "PetFabricatorFragment: CreateFragment...");
            fabricatorFragmentPrefab.CreateFragment(PetFabricatorPrefab.Info.TechType, 5.0f, 3, EncKey, true, true);
            LogUtils.LogDebug(LogArea.Prefabs, "PetFabricatorFragment: Register...");
            fabricatorFragmentPrefab.Register();
        }
    }
}
