using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace DaftAppleGames.Editor
{
    public static class EditorShortcuts
    {

        private static readonly string DnSpyPath = "D:\\Dev\\dnSpy-net-win64";
        private static readonly string SnGamePath = "E:\\Games\\Steam\\steamapps\\common\\Subnautica";
        private static readonly string BzGamePath = "E:\\Games\\Steam\\steamapps\\common\\SubnauticaZero";
        private static readonly string SnBepInExPath = Path.Combine(SnGamePath, "BepInEx");
        private static readonly string BzBepInExPath = Path.Combine(BzGamePath, "BepInEx");

        private static readonly string TextPadPath = "C:\\Program Files\\TextPad";
        
        private static readonly string LogBasePath = Path.Combine($"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData)}Low", "Unknown Worlds");
        private static readonly string SnLogPath = Path.Combine(LogBasePath, "Subnautica\\Player.log");
        private static readonly string BzLogPath = Path.Combine(LogBasePath, "Subnautica Below Zero\\Player.log");
        
        private static readonly string OldPetsModPath = "E:\\Dev\\DAG\\Subnautica Mods\\SubnauticaPets";
        
        private static readonly string SnGameAssemblyPath = Path.Combine(SnGamePath, "Subnautica_Data\\Managed\\Assembly-CSharp.dll");
        private static readonly string BzGameAssemblyPath = Path.Combine(BzGamePath, "SubnauticaZero_Data\\Managed\\Assembly-CSharp.dll");
        
        [MenuItem("Tools/Run DnSpy (BZ)")]
        private static void RunDnSpyBz()
        {
            LaunchProcess("dnSpy.exe", DnSpyPath, BzGameAssemblyPath);
        }

        [MenuItem("Tools/Open BZ Game Folder")]
        private static void OpenBzFolder()
        {
            OpenExplorer(BzBepInExPath);
        }
        
        [MenuItem("Tools/Open BZ Player Log")]
        private static void OpenBzLog()
        {
            OpenLog(BzLogPath);            
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
            Process.Start("explorer.exe","/select," + folderPath);
        }
    }
}