using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DaftAppleGames.Editor
{
    public static class EditorShortcuts
    {

        // Generic paths
        private static readonly string DnSpyPath = "D:\\Dev\\dnSpy-net-win64";
        private static readonly string TextPadPath = "C:\\Program Files\\TextPad";
        private static readonly string LogBasePath = Path.Combine($"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)}Low", "Unknown Worlds");
        private static readonly string UnityLogBasePath = Path.Combine($"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)}", "Unity");
        private static readonly string UnityEditorLogPath = Path.Combine(UnityLogBasePath, "Editor\\Editor.log");
        private static readonly string NexusZipFolder = "E:\\Dev\\DAG\\ThunderKitMods\\BelowZeroThunderKitMods\\ThunderKit\\NexusMods\\";
        
        // Mod specific paths
        private static readonly string BzGamePath = "E:\\Games\\Steam\\steamapps\\common\\SubnauticaZero";
        private static readonly string BzBepInExPath = Path.Combine(BzGamePath, "BepInEx\\plugins");
        private static readonly string BzLogPath = Path.Combine(LogBasePath, "Subnautica Below Zero\\Player.log");
        private static readonly string BzGameAssemblyPath = Path.Combine(BzGamePath, "SubnauticaZero_Data\\Managed\\Assembly-CSharp.dll");
        
        [MenuItem("Tools/Run DnSpy (BZ)")]
        private static void RunDnSpyBz()
        {
            LaunchProcess("dnSpy.exe", DnSpyPath, BzGameAssemblyPath);
        }

        [MenuItem("Tools/Open BZ Plugins Folder")]
        private static void OpenBzFolder()
        {
            OpenExplorer(BzBepInExPath);
        }
        
        [MenuItem("Tools/Nexus ZIP Folder")]
        private static void OpenNexusZipFolder()
        {
            OpenExplorer(NexusZipFolder);
        }

        [MenuItem("Tools/Open BZ Player Log")]
        private static void OpenBzLog()
        {
            OpenLog(BzLogPath);            
        }

        [MenuItem("Tools/Open Unity Editor Log")]
        private static void OpenUnityEditorLog()
        {
            OpenLog(UnityEditorLogPath);            
        }

        
        private static void OpenLog(string logPath)
        {
            LaunchProcess("textpad.exe", TextPadPath, logPath, true);
        }
        
        private static void LaunchProcess(string processName, string processPath, string arguments, bool allowMultiple = false)
        {
            if (!allowMultiple)
            {
                // Check if it's already running
                Process[] running = Process.GetProcessesByName("dnSpy");
                if (running.Length > 0)
                {
                    Debug.Log($"Process {processName} is already running.");
                    return;
                }
            }
            
            string fullPath = Path.Combine(processPath, processName);
            ProcessStartInfo newProcess = new ProcessStartInfo
            {
                FileName = fullPath,
                Arguments = arguments
            };
            Process.Start(newProcess);
            Debug.Log($"Process {processName} started.");

        }

        private static void OpenExplorer(string folderPath)
        {
            Debug.Log($"Explorer opening: {folderPath}");
            // Process.Start("explorer.exe","/select," + folderPath);
            Process.Start("explorer.exe", folderPath);
        }
    }
}
