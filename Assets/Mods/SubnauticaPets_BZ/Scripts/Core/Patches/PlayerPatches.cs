using System.Collections;
using DaftAppleGames.SubnauticaPets.BaseParts;
using DaftAppleGames.SubnauticaPets.Pets;
using DaftAppleGames.SubnauticaPets.Utils;
using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Patches
{
    [HarmonyPatch(typeof(Player))] internal class PlayerPatches
    {
        private static readonly float secondsToWaitBeforeCheck = 3.0f;

        [HarmonyPatch(nameof(Player.Start))]
        [HarmonyPrefix]
        private static bool Start_Prefix(Player __instance)
        {
            // FOR DEV ONLY! REMOVE BEFORE BUILD!!
            /*
            Debug.Log("In Player Start");
            ModDebugLog.LogInfo("Added FragmentSpawner to player!");

            __instance.gameObject.AddComponent<FragmentSpawner>();
            */

            __instance.StartCoroutine(CheckUnlockStateSync(__instance));

            return true;
        }

        private static IEnumerator CheckUnlockStateSync(Player player)
        {
            ModDebugLog.LogInfo("In CheckUnlockStateSync");
            yield return new WaitForSeconds(secondsToWaitBeforeCheck);
            ModDebugLog.LogInfo(
                $"Player.Awake (After delay): KnownText UnlockState for 'PetFabricatorPrefab' is: {KnownTech.GetTechUnlockState(PetFabricatorPrefab.Info.TechType)}");
            ModDebugLog.LogInfo(
                $"Player.Awake (After delay): KnownText UnlockState for 'PetConsolePrefab' is: {KnownTech.GetTechUnlockState(PetConsolePrefab.Info.TechType)}");
            UnlockUtils.UnlockAllIfCreativeMode();
        }

        /// <summary>
        ///     Stops base-bound Trivalve pets from following the player out of their home base.
        /// </summary>
        [HarmonyPatch(nameof(Player.SetCurrentSub))]
        [HarmonyPrefix]
        private static void SetCurrentSub_Prefix(Player __instance, SubRoot sub)
        {
            SubRoot exitedSub = __instance.currentSub;
            if (!exitedSub || exitedSub == sub)
            {
                return;
            }

            Base exitedBase = exitedSub.GetComponent<Base>();
            if (!exitedBase || SubnauticaPetsPlugin.PetSaver == null ||
                SubnauticaPetsPlugin.PetSaver.PetList == null)
            {
                return;
            }

            foreach (var pet in SubnauticaPetsPlugin.PetSaver.PetList)
            {
                if (!pet || pet.Base != exitedBase)
                {
                    continue;
                }

                Trivalve trivalve = pet.GetComponent<Trivalve>();
                if (trivalve && trivalve.followingPlayer)
                {
                    ModDebugLog.LogDebug(
                        $"Stopping Trivalve pet '{pet.PetName}' from following player out of base '{pet.BaseId}'.");
                    trivalve.followingPlayer = false;
                }
            }
        }
    }
}
