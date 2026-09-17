using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using DaftAppleGames.VehicleEnhancements_BZ.Building;
using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ.Patches
{
    [HarmonyPatch(typeof(Builder))]
    internal static class BuilderPatches
    {
        private static readonly MethodInfo FilterPlacementBlockerMethod = AccessTools.Method(
            typeof(SeaTruckBuildingContext),
            nameof(SeaTruckBuildingContext.FilterPlacementBlocker));

        private static readonly MethodInfo InstantiateForPlacementMethod = AccessTools.Method(
            typeof(SeaTruckBuildingContext),
            nameof(SeaTruckBuildingContext.InstantiateForPlacement));

        [HarmonyPatch(nameof(Builder.Begin))]
        [HarmonyPrefix]
        private static void BeginPrefix(GameObject modulePrefab)
        {
            SeaTruckBuildingContext.SetSelectedPrefab(modulePrefab);
        }

        [HarmonyPatch(nameof(Builder.End))]
        [HarmonyPostfix]
        private static void EndPostfix()
        {
            SeaTruckBuildingContext.ResetPlacement();
        }

        [HarmonyPatch(nameof(Builder.Update))]
        [HarmonyPrefix]
        private static void UpdatePrefix()
        {
            SeaTruckBuildingContext.ResetPlacement();
        }

        [HarmonyPatch("SetPlaceOnSurface")]
        [HarmonyPostfix]
        private static void SetPlaceOnSurfacePostfix(RaycastHit hit, ref Quaternion rotation)
        {
            SeaTruckBuildingContext.AlignPlacementWithSegment(hit, ref rotation);
        }

        [HarmonyPatch(nameof(Builder.GetSurfaceType))]
        [HarmonyPostfix]
        private static void GetSurfaceTypePostfix(Vector3 hitNormal, ref SurfaceType __result)
        {
            __result = SeaTruckBuildingContext.GetSurfaceTypeRelativeToSegment(hitNormal, __result);
        }

        [HarmonyPatch("CheckTag")]
        [HarmonyPostfix]
        private static void CheckTagPostfix(Collider c, ref bool __result)
        {
            if (!__result && SeaTruckBuildingContext.IsTaggedPlacementSurfaceAllowed(c))
            {
                __result = true;
            }
        }

        [HarmonyPatch("CheckAsSubModule")]
        [HarmonyPrefix]
        private static void CheckAsSubModulePrefix()
        {
            SeaTruckBuildingContext.BeginSubModuleCheck();
        }

        [HarmonyPatch("CheckAsSubModule")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> CheckAsSubModuleTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> patchedInstructions = new List<CodeInstruction>();
            bool patched = false;

            foreach (CodeInstruction instruction in instructions)
            {
                patchedInstructions.Add(instruction);
                MethodInfo calledMethod = instruction.operand as MethodInfo;
                if (patched || calledMethod is null ||
                    calledMethod.Name != nameof(GameObject.GetComponentInParent) ||
                    !calledMethod.IsGenericMethod)
                {
                    continue;
                }

                System.Type[] genericArguments = calledMethod.GetGenericArguments();
                if (genericArguments.Length == 1 && genericArguments[0] == typeof(SeaTruckSegment))
                {
                    patchedInstructions.Add(new CodeInstruction(OpCodes.Call, FilterPlacementBlockerMethod));
                    patched = true;
                }
            }

            if (!patched)
            {
                ModDebugLog.LogError("Could not patch the SeaTruck placement check in Builder.CheckAsSubModule.");
            }

            return patchedInstructions;
        }

        [HarmonyPatch("CheckAsSubModule")]
        [HarmonyFinalizer]
        private static System.Exception CheckAsSubModuleFinalizer(System.Exception __exception)
        {
            SeaTruckBuildingContext.EndSubModuleCheck();
            return __exception;
        }

        [HarmonyPatch(nameof(Builder.TryPlace))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TryPlaceTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> patchedInstructions = new List<CodeInstruction>();
            bool patched = false;

            foreach (CodeInstruction instruction in instructions)
            {
                MethodInfo calledMethod = instruction.operand as MethodInfo;
                if (!patched && IsGameObjectInstantiateMethod(calledMethod))
                {
                    CodeInstruction replacement = new CodeInstruction(OpCodes.Call, InstantiateForPlacementMethod);
                    replacement.labels.AddRange(instruction.labels);
                    replacement.blocks.AddRange(instruction.blocks);
                    patchedInstructions.Add(replacement);
                    patched = true;
                    continue;
                }

                patchedInstructions.Add(instruction);
            }

            if (!patched)
            {
                ModDebugLog.LogError("Could not patch object creation in Builder.TryPlace.");
            }

            return patchedInstructions;
        }

        [HarmonyPatch(nameof(Builder.TryPlace))]
        [HarmonyPostfix]
        private static void TryPlacePostfix(bool __result)
        {
            SeaTruckBuildingContext.CompletePlacement(__result);
        }

        [HarmonyPatch(
            nameof(Builder.CheckSpace),
            typeof(Vector3),
            typeof(Quaternion),
            typeof(List<OrientedBounds>),
            typeof(int),
            typeof(Collider),
            typeof(List<GameObject>))]
        [HarmonyPrefix]
        private static void CheckSpacePrefix()
        {
            SeaTruckBuildingContext.BeginPlacementSpaceCheck();
        }

        [HarmonyPatch(
            nameof(Builder.CheckSpace),
            typeof(Vector3),
            typeof(Quaternion),
            typeof(List<OrientedBounds>),
            typeof(int),
            typeof(Collider),
            typeof(List<GameObject>))]
        [HarmonyFinalizer]
        private static System.Exception CheckSpaceFinalizer(System.Exception __exception)
        {
            SeaTruckBuildingContext.EndPlacementSpaceCheck();
            return __exception;
        }

        [HarmonyPatch(
            nameof(Builder.GetOverlappedColliders),
            typeof(Vector3),
            typeof(Quaternion),
            typeof(Vector3),
            typeof(int),
            typeof(QueryTriggerInteraction),
            typeof(List<Collider>))]
        [HarmonyPostfix]
        private static void GetOverlappedCollidersPostfix(List<Collider> results)
        {
            SeaTruckBuildingContext.RemoveOwningSegmentColliders(results);
        }

        private static bool IsGameObjectInstantiateMethod(MethodInfo method)
        {
            return method != null &&
                   method.DeclaringType == typeof(Object) &&
                   method.Name == nameof(Object.Instantiate) &&
                   method.GetParameters().Length == 1 &&
                   method.ReturnType == typeof(GameObject);
        }
    }
}
