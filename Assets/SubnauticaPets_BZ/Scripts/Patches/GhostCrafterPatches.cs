using DaftAppleGames.SubnauticaPets.BaseParts;
using DaftAppleGames.SubnauticaPets.Pets;
using HarmonyLib;
using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Patches
{

    /// <summary>
    /// Patch the Workbench to do things different when spawning a pet
    /// </summary>
    [HarmonyPatch(typeof(GhostCrafter))]
    internal class GhostCrafterPatches
    {
        /// <summary>
        /// Patches the Craft method, allowing us to set the type of Pet to spawn
        /// </summary>
        [HarmonyPatch(nameof(GhostCrafter.Craft))]
        [HarmonyPrefix]
        public static bool Craft_Prefix(GhostCrafter __instance, TechType techType, float duration)
        {

            if(Pet.IsPetTechType(techType))
            {
                SelectedCreaturePetType = techType;
            }
            return true;
        }

        [HarmonyPatch(nameof(GhostCrafter.OnCraftingEnd))]
        [HarmonyPrefix]
        public static bool OnCraftingEnd_Prefix(GhostCrafter __instance)
        {
            CrafterLogic crafterLogic = __instance.logic;
            TechType techType = crafterLogic.craftingTechType;

            if(Pet.IsPetTechType(techType))
            {
                PetFabricator petFabricator = __instance.GetComponent<PetFabricator>();
                crafterLogic.ResetCrafter();
                petFabricator.SpawnPet(techType);
                return false;
            }
            return true;
        }
    }
}