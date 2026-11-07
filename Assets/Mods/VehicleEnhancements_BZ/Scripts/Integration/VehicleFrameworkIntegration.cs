using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using DaftAppleGames.VehicleEnhancements_BZ.Patches;
using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ.Integration
{
    internal static class VehicleFrameworkIntegration
    {
        private const string PluginGuid = "com.mikjaw.subnautica.vehicleframework.mod";
        private static readonly Version MinimumPluginVersion = new Version(2, 0, 7);
        private static Func<Player, Vehicle> getVehicle;
        private static MethodInfo isPlayerControlling;
        private static Type hudType;
        private static FieldInfo hudRoot;
        private static FieldInfo vehicleTypes;
        private static FieldInfo entryName;
        private static FieldInfo entryTechType;
        private static PropertyInfo vehicleTechType;

        internal static bool IsEnabled { get; private set; }
        internal static bool IsMonitorMode => !XRSettings.enabled;

        internal static void Initialize(Harmony harmony)
        {
            if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out PluginInfo plugin))
            {
                return;
            }

            if (plugin.Metadata.Version < MinimumPluginVersion)
            {
                ModDebugLog.LogWarning($"Custom vehicles require VehicleFramework {MinimumPluginVersion} or later. Vanilla enhancements remain available.");
                return;
            }

            try
            {
                Assembly assembly = plugin.Instance.GetType().Assembly;
                Type vehicleType = assembly.GetType("VehicleFramework.ModVehicle", true);
                Type extensions = assembly.GetType("VehicleFramework.Extensions.ExtensionMethods", true);
                Type builder = assembly.GetType("VehicleFramework.VehicleBuilding.HUDBuilder", true);
                Type manager = assembly.GetType("VehicleFramework.Admin.VehicleManager", true);
                Type entry = assembly.GetType("VehicleFramework.VehicleBuilding.VehicleEntry", true);
                vehicleTypes = manager.GetField("vehicleTypes", BindingFlags.Static | BindingFlags.NonPublic);
                entryName = entry.GetField("name");
                entryTechType = entry.GetField("techType");
                vehicleTechType = vehicleType.GetProperty("TechType");
                hudType = assembly.GetType("VehicleFramework.MiscComponents.UGUI_VehicleHUD", true);
                hudRoot = hudType.GetField("root");
                isPlayerControlling = vehicleType.GetMethod("IsPlayerControlling", Type.EmptyTypes);
                getVehicle = (Func<Player, Vehicle>)Delegate.CreateDelegate(typeof(Func<Player, Vehicle>),
                    extensions.GetMethod("GetModVehicle", new[] { typeof(Player) }));
                harmony.Patch(builder.GetMethod("DecideBuildHUD", Type.EmptyTypes),
                    postfix: new HarmonyMethod(typeof(VehicleFrameworkIntegration), nameof(ConfigureHudClones)));
                IsEnabled = true;
                ConfigureHudClones();
                ModDebugLog.LogInfo($"VehicleFramework {plugin.Metadata.Version} integration enabled.");
                if (!IsMonitorMode)
                {
                    ModDebugLog.LogInfo("Custom vehicle enhancements are inactive in VR until the VR support phase is complete.");
                }
            }
            catch (Exception exception)
            {
                IsEnabled = false;
                ModDebugLog.LogError($"Could not initialize optional VehicleFramework support: {exception}");
            }
        }

        internal static Vehicle GetVehicle(Player player) => getVehicle(player);

        internal static string GetVehicleIdentifier(Vehicle vehicle) =>
            ((TechType)vehicleTechType.GetValue(vehicle)).AsString();

        internal static List<KeyValuePair<string, string>> GetRegisteredVehicles()
        {
            Dictionary<string, string> names = new Dictionary<string, string>();
            if (IsEnabled)
            {
                foreach (object entry in (IEnumerable)vehicleTypes.GetValue(null))
                {
                    TechType techType = (TechType)entryTechType.GetValue(entry);
                    if (techType != TechType.None)
                    {
                        names[techType.AsString()] = (string)entryName.GetValue(entry);
                    }
                }
            }

            List<KeyValuePair<string, string>> vehicles = new List<KeyValuePair<string, string>>(names);
            vehicles.Sort((left, right) =>
            {
                int nameOrder = StringComparer.OrdinalIgnoreCase.Compare(left.Value, right.Value);
                return nameOrder != 0 ? nameOrder : StringComparer.Ordinal.Compare(left.Key, right.Key);
            });
            return vehicles;
        }

        internal static Func<bool> BindControlPredicate(Vehicle vehicle) =>
            (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), vehicle, isPlayerControlling);

        private static void ConfigureHudClones()
        {
            if (!IsEnabled || !uGUI.main)
            {
                return;
            }

            foreach (Component hud in uGUI.main.GetComponentsInChildren(hudType, true))
            {
                GameObject root = (GameObject)hudRoot.GetValue(hud);
                if (!root)
                {
                    continue;
                }

                Transform indicators = root.transform.Find(UGuiPatches.EnhancedIndicatorsPrefabName);
                if (!indicators)
                {
                    continue;
                }

                indicators.gameObject.SetActive(false);
                if (!UGuiPatches.ConfigureEnhancedIndicators(indicators.gameObject, root.transform, EnhancedVehicle.CustomVehicle))
                {
                    continue;
                }

                VehicleFrameworkCustomHudController customHudController = root.GetComponent<VehicleFrameworkCustomHudController>();
                if (!customHudController)
                {
                    customHudController = root.AddComponent<VehicleFrameworkCustomHudController>();
                }

                customHudController.Configure(indicators.gameObject);
            }
        }
    }
}
