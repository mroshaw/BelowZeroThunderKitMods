using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace DaftAppleGames.Editor
{
    /// <summary>
    ///     Provides project path conversion and Windows extended-length file operations for reference imports.
    /// </summary>
    internal static class ReferenceAssetImporterFileSystem
    {
        private const int LegacyMaxDirectoryPath = 247;
        private const int LegacyMaxFilePath = 259;
        private const int ErrorAlreadyExists = 183;

        private static readonly bool IsWindowsEditor = Application.platform == RuntimePlatform.WindowsEditor;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateDirectoryW(string path, IntPtr securityAttributes);

        public static string GetMetaGuid(string metaPath)
        {
            return FileExists(metaPath) ? ReadMetaGuid(metaPath) : string.Empty;
        }

        public static string ReadMetaGuid(string metaPath)
        {
            using (StreamReader reader = new StreamReader(ToExtendedLengthPath(metaPath)))
            {
                for (int lineIndex = 0; lineIndex < 20 && !reader.EndOfStream; lineIndex++)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("guid:", StringComparison.Ordinal)) continue;

                    string guid = line.Substring("guid:".Length).Trim();
                    if (guid.Length == 32) return guid;
                }
            }

            return string.Empty;
        }

        public static bool IsPathWithinRoot(string path, string root)
        {
            string rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                       Path.DirectorySeparatorChar;
            return path.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetRelativePath(string root, string path)
        {
            Uri rootUri = new Uri(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                  Path.DirectorySeparatorChar);
            Uri pathUri = new Uri(path);
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString())
                .Replace('/', Path.DirectorySeparatorChar);
        }

        public static string GetAbsolutePath(string path)
        {
            if (Path.IsPathRooted(path)) return NormalizeFullPath(path);

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return NormalizeFullPath(Path.Combine(projectRoot, path));
        }

        public static string GetProjectRelativePath(string absolutePath)
        {
            string projectRoot = NormalizeFullPath(Directory.GetParent(Application.dataPath).FullName);
            string normalizedPath = NormalizeFullPath(absolutePath);
            if (!IsPathWithinRoot(normalizedPath, projectRoot) &&
                !string.Equals(normalizedPath, projectRoot, StringComparison.OrdinalIgnoreCase)) return string.Empty;

            return GetRelativePath(projectRoot, normalizedPath).Replace('\\', '/');
        }

        public static string NormalizeFullPath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static bool FileExists(string path)
        {
            return File.Exists(ToExtendedLengthPath(path));
        }

        public static string ReadAllText(string path)
        {
            return File.ReadAllText(ToExtendedLengthPath(path));
        }

        public static void WriteAllText(string path, string contents)
        {
            File.WriteAllText(ToExtendedLengthPath(path), contents, new UTF8Encoding(false));
        }

        public static void CopyFile(string sourcePath, string destinationPath)
        {
            File.Copy(ToExtendedLengthPath(sourcePath), ToExtendedLengthPath(destinationPath), true);
        }

        public static void CreateDirectory(string path)
        {
            if (!IsWindowsEditor || path.Length <= LegacyMaxDirectoryPath)
            {
                Directory.CreateDirectory(path);
                return;
            }

            CreateLongDirectory(path);
        }

        public static string ToExtendedLengthPath(string path, int legacyMaxPath = LegacyMaxFilePath)
        {
            if (!IsWindowsEditor || !Path.IsPathRooted(path) || path.Length <= legacyMaxPath ||
                path.StartsWith(@"\\?\", StringComparison.Ordinal)) return path;

            return path.StartsWith(@"\\", StringComparison.Ordinal)
                ? @"\\?\UNC\" + path.Substring(2)
                : @"\\?\" + path;
        }

        private static void CreateLongDirectory(string path)
        {
            if (Directory.Exists(ToExtendedLengthPath(path, LegacyMaxDirectoryPath))) return;

            string parentPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parentPath) &&
                !Directory.Exists(ToExtendedLengthPath(parentPath, LegacyMaxDirectoryPath)))
                CreateDirectory(parentPath);

            if (CreateDirectoryW(ToExtendedLengthPath(path, LegacyMaxDirectoryPath), IntPtr.Zero)) return;

            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode != ErrorAlreadyExists)
                throw new IOException($"Could not create directory '{path}'. Windows error: {errorCode}.");
        }
    }
}
