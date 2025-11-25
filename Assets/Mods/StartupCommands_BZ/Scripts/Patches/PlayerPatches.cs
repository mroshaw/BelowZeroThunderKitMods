using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using static DaftAppleGames.StartupCommand.StartupCommandPlugin;

namespace DaftAppleGames.StartupCommand
{
    [HarmonyPatch(typeof(Player))]
    public class PlayerPatches
    {
        private static DevConsole _devConsole;
        
        /// <summary>
        /// Run the command list when the Player starts
        /// </summary>
        [HarmonyPatch(nameof(Player.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(Player __instance)
        {
            if (!_devConsole)
            {
                _devConsole = GameObject.FindObjectOfType<DevConsole>();
            }

            if (!_devConsole)
            {
                ModDebugLog.LogError($"Could not find DevConsole instance! Aborting!");
                return;
            }

            // Run commands asynchronously so we can easily factor in the delays
            UWE.CoroutineHost.StartCoroutine(RunCommandsAsync());
        }

        /// <summary>
        /// Executes all commands in the list, with appropriate delays
        /// </summary>
        private static IEnumerator RunCommandsAsync()
        {
            string allCommands = StartupCommandPlugin.ScriptConfigFile.StartupScript;
            // Get all console commands to run
            string[] commandArray = allCommands.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.None
            );
            
            ModDebugLog.LogDebug($"Found {commandArray.Length} commands to run.");
            
            // Wait for initial start delay
            yield return new WaitForSeconds(ConfigFile.StartDelay);

            if (!ConfigFile.DisableAlerts)
            {
                ErrorMessage.AddMessage("Running startup commands...");
            }
            
            ModDebugLog.LogDebug("Running startup commands...");
            foreach (string command in commandArray)
            {
                ModDebugLog.LogDebug($"Submitting command: {command}");
                _devConsole.Submit(command);
                yield return new WaitForSeconds(ConfigFile.CommandDelay);
            }
            
            ModDebugLog.LogDebug("Done running startup commands!");
            if (!ConfigFile.DisableAlerts)
            {
                ErrorMessage.AddMessage("Done running startup commands!");
            }
        }
    }
}