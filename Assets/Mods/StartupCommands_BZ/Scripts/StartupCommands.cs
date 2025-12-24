using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using static DaftAppleGames.StartupCommand.StartupCommandPlugin;

namespace DaftAppleGames.StartupCommand
{
    public static class StartupCommands
    {

        private static DevConsole _devConsole;
        private static bool _isRunning;

        /// <summary>
        /// Run the commands in the configured list
        /// </summary>
        public static void RunCommands(bool runImmediately = false)
        {
            if (_isRunning)
            {
                ErrorMessage.AddMessage("Command list is already running!");
                return;
            }
            
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
            UWE.CoroutineHost.StartCoroutine(RunCommandsAsync(runImmediately));
        }

        /// <summary>
        /// Executes all commands in the list, with appropriate delays
        /// </summary>
        private static IEnumerator RunCommandsAsync(bool runImmediately = false)
        {
            _isRunning = true;
            
            string allCommands = StartupCommandPlugin.ScriptConfigFile.StartupScript;

            // Get all console commands to run
            // Remove UTF-8 BOM from start of command list
            allCommands = allCommands.TrimStart('\uFEFF');
            
            // Split the command list into an array, line by line
            string[] commandArray = allCommands
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Where(line => !line.StartsWith("#"))       // skip comments
                .ToArray();
            
            ModDebugLog.LogDebug($"Found {commandArray.Length} commands to run.");
            
            // Wait for initial start delay
            if (!runImmediately)
            {
                yield return new WaitForSeconds(ConfigFile.StartDelay);
            }

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
            
            _isRunning = false;
        }
    }
}