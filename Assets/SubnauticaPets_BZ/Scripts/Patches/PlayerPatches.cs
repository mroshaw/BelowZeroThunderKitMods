using HarmonyLib;
using System.Collections;
using DaftAppleGames.SubnauticaPets.BaseParts;
using DaftAppleGames.SubnauticaPets.Utils;
using UnityEngine;

namespace DaftAppleGames.SubnauticaPets.Patches
{
    [HarmonyPatch(typeof(Player))]
    internal class PlayerPatches
    {
        private static float secondsToWaitBeforeCheck = 3.0f;

        [HarmonyPatch(nameof(Player.Start))]
        [HarmonyPrefix]
        static bool Start_Prefix (Player __instance)
        {
            // FOR DEV ONLY! REMOVE BEFORE BUILD!!
            Debug.Log("In Player Start");
            LogUtils.LogInfo("Added FragmentSpawner to player!");
            __instance.gameObject.AddComponent<FragmentSpawner>();
            
            __instance.StartCoroutine(CheckUnlockStateSync(__instance));

            return true;
        }

        private static IEnumerator CheckUnlockStateSync(Player player)
        {
            LogUtils.LogInfo("In CheckUnlockStateSync");
            yield return new WaitForSeconds(secondsToWaitBeforeCheck);
            LogUtils.LogInfo($"Player.Awake (After delay): KnownText UnlockState for 'PetFabricatorPrefab' is: {KnownTech.GetTechUnlockState(PetFabricatorPrefab.Info.TechType)}");
            LogUtils.LogInfo($"Player.Awake (After delay): KnownText UnlockState for 'PetConsolePrefab' is: {KnownTech.GetTechUnlockState(PetConsolePrefab.Info.TechType)}");
            UnlockUtils.UnlockAllIfCreativeMode();
        }
    }
}
