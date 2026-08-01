using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DaftAppleGames.Editor
{
    /// <summary>
    ///     Imports an AssetRipper asset dependency closure and reconnects exported scripts to game DLL types.
    /// </summary>
    public class ReferenceAssetImporterWindow : EditorWindow
    {
        private const string DefaultExportAssetsPath = "GameFiles~/ExportedProject/Assets";
        private const string DefaultDestinationPath = "Assets/SubnauticaRefAssets";
        private const string GameAssemblyPackagePath = "Packages/SubnauticaZero";
        private const int LegacyMaxDirectoryPath = 247;
        private const int LegacyMaxFilePath = 259;
        private const int ErrorAlreadyExists = 183;

        private static readonly Regex GuidRegex = new Regex(
            @"\bguid:\s*([0-9a-fA-F]{32})\b", RegexOptions.Compiled);
        private static readonly Regex MetaGuidRegex = new Regex(
            @"^guid:\s*([0-9a-fA-F]{32})\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex NamespaceRegex = new Regex(
            @"\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)", RegexOptions.Compiled);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateDirectoryW(string path, IntPtr securityAttributes);

        [SerializeField] private string sourceAssetPath = string.Empty;
        [SerializeField] private string exportAssetsPath = DefaultExportAssetsPath;
        [SerializeField] private string destinationPath = DefaultDestinationPath;
        [SerializeField] private bool dryRun = true;
        [SerializeField] private bool overwriteExisting = true;
        [SerializeField] private Vector2 reportScrollPosition;
        [SerializeField] private string report = "Select an AssetRipper asset to begin.";

        [MenuItem("Tools/Subnautica/Import Reference Asset")]
        public static void ShowWindow()
        {
            ReferenceAssetImporterWindow window = GetWindow<ReferenceAssetImporterWindow>();
            window.titleContent = new GUIContent("Reference Asset Importer");
            window.minSize = new Vector2(640.0f, 430.0f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("AssetRipper Reference Asset Importer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Copies the selected asset and its recursive dependencies while preserving AssetRipper GUIDs. " +
                "Exported scripts are remapped to MonoScript types in ThunderKit's imported game DLLs.",
                MessageType.Info);

            DrawPathField("Export Assets Root", ref exportAssetsPath, true);
            DrawSourceAssetField();
            DrawPathField("Destination", ref destinationPath, false);
            dryRun = EditorGUILayout.Toggle("Dry Run", dryRun);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(sourceAssetPath)))
            {
                if (GUILayout.Button("Import Asset and Dependencies", GUILayout.Height(32.0f)))
                    ImportSelectedAsset();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Import Report", EditorStyles.boldLabel);
            reportScrollPosition = EditorGUILayout.BeginScrollView(reportScrollPosition);
            EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void DrawSourceAssetField()
        {
            EditorGUILayout.BeginHorizontal();
            sourceAssetPath = EditorGUILayout.TextField("Source Asset", sourceAssetPath);
            if (GUILayout.Button("Browse", GUILayout.Width(72.0f)))
            {
                string absoluteRoot = GetAbsolutePath(exportAssetsPath);
                string selectedPath = EditorUtility.OpenFilePanel("Select AssetRipper asset", absoluteRoot, string.Empty);
                if (!string.IsNullOrEmpty(selectedPath)) sourceAssetPath = selectedPath;
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawPathField(string label, ref string path, bool allowAbsolutePath)
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("Browse", GUILayout.Width(72.0f)))
            {
                string initialPath = GetAbsolutePath(path);
                string selectedPath = EditorUtility.OpenFolderPanel(label, initialPath, string.Empty);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    string projectRelativePath = GetProjectRelativePath(selectedPath);
                    path = allowAbsolutePath || string.IsNullOrEmpty(projectRelativePath)
                        ? selectedPath
                        : projectRelativePath;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ImportSelectedAsset()
        {
            StringBuilder reportBuilder = new StringBuilder();
            string absoluteExportRoot = NormalizeFullPath(GetAbsolutePath(exportAssetsPath));
            string absoluteSourcePath = NormalizeFullPath(sourceAssetPath);
            string normalizedDestination = destinationPath.Replace('\\', '/').TrimEnd('/');

            if (!Directory.Exists(absoluteExportRoot))
            {
                report = $"Export Assets Root does not exist: {absoluteExportRoot}";
                return;
            }

            if (!FileExists(absoluteSourcePath) || absoluteSourcePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                report = $"Select an asset file, not a folder or .meta file: {absoluteSourcePath}";
                return;
            }

            if (!IsPathWithinRoot(absoluteSourcePath, absoluteExportRoot))
            {
                report = $"The selected asset is outside the configured export root: {absoluteExportRoot}";
                return;
            }

            if (!normalizedDestination.StartsWith("Assets/", StringComparison.Ordinal) &&
                !string.Equals(normalizedDestination, "Assets", StringComparison.Ordinal))
            {
                report = "Destination must be a project-relative path under Assets.";
                return;
            }

            try
            {
                Dictionary<string, string> exportedAssetsByGuid =
                    BuildExportedGuidIndex(absoluteExportRoot, reportBuilder);
                Dictionary<string, MonoScript> gameScriptsByType = BuildGameScriptIndex(reportBuilder);
                HashSet<string> sourceAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> scriptGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                DiscoverDependencies(absoluteSourcePath, exportedAssetsByGuid, sourceAssets, scriptGuids);

                Dictionary<string, string> scriptReferences = BuildScriptReferences(
                    scriptGuids, exportedAssetsByGuid, gameScriptsByType, reportBuilder);
                int copiedCount = CopyAssets(absoluteExportRoot, normalizedDestination, sourceAssets,
                    scriptReferences, reportBuilder);

                if (!dryRun)
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    string rootRelativePath = GetRelativePath(absoluteExportRoot, absoluteSourcePath);
                    string importedRootPath = normalizedDestination + "/" + rootRelativePath.Replace('\\', '/');
                    Object importedAsset = AssetDatabase.LoadMainAssetAtPath(importedRootPath);
                    if (importedAsset) EditorGUIUtility.PingObject(importedAsset);
                }

                reportBuilder.Insert(0,
                    $"{(dryRun ? "Dry run" : "Import")} complete. Discovered {sourceAssets.Count} assets, " +
                    $"{(dryRun ? "would copy or update" : "copied or updated")} {copiedCount}, " +
                    $"and resolved {scriptReferences.Count} game script references.\n\n");
                report = reportBuilder.ToString();
            }
            catch (Exception exception)
            {
                reportBuilder.AppendLine();
                reportBuilder.AppendLine("IMPORT FAILED");
                reportBuilder.AppendLine(exception.ToString());
                report = reportBuilder.ToString();
                Debug.LogException(exception);
            }
        }

        private static Dictionary<string, string> BuildExportedGuidIndex(string exportRoot,
            StringBuilder reportBuilder)
        {
            Dictionary<string, string> assetsByGuid =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] metaPaths = Directory.GetFiles(exportRoot, "*.meta", SearchOption.AllDirectories);

            for (int metaIndex = 0; metaIndex < metaPaths.Length; metaIndex++)
            {
                string metaPath = metaPaths[metaIndex];
                string guid;
                try
                {
                    guid = ReadMetaGuid(metaPath);
                }
                catch (IOException exception)
                {
                    reportBuilder.AppendLine($"UNREADABLE META: {metaPath} ({exception.Message})");
                    continue;
                }
                catch (UnauthorizedAccessException exception)
                {
                    reportBuilder.AppendLine($"UNREADABLE META: {metaPath} ({exception.Message})");
                    continue;
                }

                if (string.IsNullOrEmpty(guid)) continue;

                string assetPath = metaPath.Substring(0, metaPath.Length - ".meta".Length);
                assetsByGuid[guid] = assetPath;
            }

            return assetsByGuid;
        }

        private static Dictionary<string, MonoScript> BuildGameScriptIndex(StringBuilder reportBuilder)
        {
            Dictionary<string, MonoScript> scriptsByType =
                new Dictionary<string, MonoScript>(StringComparer.Ordinal);
            string absoluteAssemblyFolder = GetAbsolutePath(GameAssemblyPackagePath);
            if (!Directory.Exists(absoluteAssemblyFolder))
                throw new DirectoryNotFoundException($"Game assembly package was not found: {absoluteAssemblyFolder}");

            string[] assemblyPaths = Directory.GetFiles(absoluteAssemblyFolder, "*.dll", SearchOption.AllDirectories);
            for (int assemblyIndex = 0; assemblyIndex < assemblyPaths.Length; assemblyIndex++)
            {
                if (Path.GetFileNameWithoutExtension(assemblyPaths[assemblyIndex])
                    .EndsWith("_publicized", StringComparison.OrdinalIgnoreCase)) continue;

                string assetPath = GetProjectRelativePath(assemblyPaths[assemblyIndex]);
                if (string.IsNullOrEmpty(assetPath)) continue;

                Object[] assemblyAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                for (int assetIndex = 0; assetIndex < assemblyAssets.Length; assetIndex++)
                {
                    MonoScript monoScript = assemblyAssets[assetIndex] as MonoScript;
                    if (!monoScript) continue;

                    Type scriptType = monoScript.GetClass();
                    if (scriptType is null) continue;

                    scriptsByType[scriptType.FullName] = monoScript;
                    if (!scriptsByType.ContainsKey(scriptType.Name)) scriptsByType.Add(scriptType.Name, monoScript);
                }
            }

            reportBuilder.AppendLine($"Indexed {scriptsByType.Count} game script type names from {assemblyPaths.Length} DLLs.");
            return scriptsByType;
        }

        private static void DiscoverDependencies(string rootAssetPath, Dictionary<string, string> assetsByGuid,
            HashSet<string> discoveredAssets, HashSet<string> scriptGuids)
        {
            Queue<string> pendingAssets = new Queue<string>();
            pendingAssets.Enqueue(rootAssetPath);

            while (pendingAssets.Count > 0)
            {
                string assetPath = pendingAssets.Dequeue();
                if (!discoveredAssets.Add(assetPath)) continue;

                FindReferencedAssets(assetPath, assetsByGuid, pendingAssets, scriptGuids);
                string metaPath = assetPath + ".meta";
                if (FileExists(metaPath)) FindReferencedAssets(metaPath, assetsByGuid, pendingAssets, scriptGuids);
            }
        }

        private static void FindReferencedAssets(string sourcePath, Dictionary<string, string> assetsByGuid,
            Queue<string> pendingAssets, HashSet<string> scriptGuids)
        {
            if (!IsTextSerializedAsset(sourcePath)) return;

            string contents = ReadAllText(sourcePath);
            MatchCollection matches = GuidRegex.Matches(contents);
            for (int matchIndex = 0; matchIndex < matches.Count; matchIndex++)
            {
                string guid = matches[matchIndex].Groups[1].Value;
                string referencedPath;
                if (!assetsByGuid.TryGetValue(guid, out referencedPath)) continue;

                if (referencedPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    scriptGuids.Add(guid);
                else
                    pendingAssets.Enqueue(referencedPath);
            }
        }

        private static Dictionary<string, string> BuildScriptReferences(HashSet<string> scriptGuids,
            Dictionary<string, string> assetsByGuid, Dictionary<string, MonoScript> gameScriptsByType,
            StringBuilder reportBuilder)
        {
            Dictionary<string, string> references =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string scriptGuid in scriptGuids)
            {
                string scriptPath;
                if (!assetsByGuid.TryGetValue(scriptGuid, out scriptPath)) continue;

                string typeName = Path.GetFileNameWithoutExtension(scriptPath);
                string source = ReadAllText(scriptPath);
                Match namespaceMatch = NamespaceRegex.Match(source);
                string fullTypeName = namespaceMatch.Success
                    ? namespaceMatch.Groups[1].Value + "." + typeName
                    : typeName;

                MonoScript monoScript;
                if (!gameScriptsByType.TryGetValue(fullTypeName, out monoScript) &&
                    !gameScriptsByType.TryGetValue(typeName, out monoScript))
                {
                    reportBuilder.AppendLine($"UNRESOLVED SCRIPT: {fullTypeName} ({scriptPath})");
                    continue;
                }

                string dllGuid;
                long localId;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(monoScript, out dllGuid, out localId))
                {
                    reportBuilder.AppendLine($"UNRESOLVED DLL ID: {fullTypeName}");
                    continue;
                }

                references[scriptGuid] = $"fileID: {localId}, guid: {dllGuid}, type: 3";
                reportBuilder.AppendLine($"SCRIPT: {fullTypeName} -> {AssetDatabase.GetAssetPath(monoScript)}:{localId}");
            }

            return references;
        }

        private int CopyAssets(string exportRoot, string projectDestination, HashSet<string> sourceAssets,
            Dictionary<string, string> scriptReferences, StringBuilder reportBuilder)
        {
            int copiedCount = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string sourcePath in sourceAssets)
                {
                    string sourceMetaPath = sourcePath + ".meta";
                    string sourceGuid = GetMetaGuid(sourceMetaPath);
                    string existingAssetPath = string.IsNullOrEmpty(sourceGuid)
                        ? string.Empty
                        : AssetDatabase.GUIDToAssetPath(sourceGuid);
                    string relativePath = GetRelativePath(exportRoot, sourcePath).Replace('\\', '/');
                    string destinationAssetPath = projectDestination + "/" + relativePath;

                    if (!string.IsNullOrEmpty(existingAssetPath) &&
                        !string.Equals(existingAssetPath, destinationAssetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        reportBuilder.AppendLine($"REUSED: {relativePath} -> {existingAssetPath}");
                        continue;
                    }

                    string absoluteDestinationPath = GetAbsolutePath(destinationAssetPath);
                    if (FileExists(absoluteDestinationPath) && !overwriteExisting)
                    {
                        reportBuilder.AppendLine($"SKIPPED EXISTING: {destinationAssetPath}");
                        continue;
                    }

                    if (dryRun)
                    {
                        copiedCount++;
                        reportBuilder.AppendLine($"WOULD COPY: {destinationAssetPath}");
                        continue;
                    }

                    string destinationDirectory = Path.GetDirectoryName(absoluteDestinationPath);
                    if (!string.IsNullOrEmpty(destinationDirectory)) CreateDirectory(destinationDirectory);

                    if (IsTextSerializedAsset(sourcePath))
                    {
                        string contents = ReadAllText(sourcePath);
                        contents = RemapScriptReferences(contents, scriptReferences);
                        WriteAllText(absoluteDestinationPath, contents);
                    }
                    else
                    {
                        CopyFile(sourcePath, absoluteDestinationPath);
                    }

                    if (FileExists(sourceMetaPath)) CopyFile(sourceMetaPath, absoluteDestinationPath + ".meta");
                    copiedCount++;
                    reportBuilder.AppendLine($"COPIED: {destinationAssetPath}");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return copiedCount;
        }

        private static string RemapScriptReferences(string contents, Dictionary<string, string> scriptReferences)
        {
            foreach (KeyValuePair<string, string> scriptReference in scriptReferences)
            {
                string pattern = @"m_Script:\s*\{\s*fileID:\s*-?\d+\s*,\s*guid:\s*" +
                                 Regex.Escape(scriptReference.Key) + @"\s*,\s*type:\s*3\s*\}";
                contents = Regex.Replace(contents, pattern, "m_Script: {" + scriptReference.Value + "}");
            }

            return contents;
        }

        private static bool IsTextSerializedAsset(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            switch (extension)
            {
                case ".anim":
                case ".asset":
                case ".controller":
                case ".guiskin":
                case ".mask":
                case ".mat":
                case ".meta":
                case ".overridecontroller":
                case ".playable":
                case ".prefab":
                case ".shader":
                case ".spriteatlas":
                case ".unity":
                    return true;
                default:
                    return false;
            }
        }

        private static string GetMetaGuid(string metaPath)
        {
            if (!FileExists(metaPath)) return string.Empty;
            return ReadMetaGuid(metaPath);
        }

        private static string ReadMetaGuid(string metaPath)
        {
            using (StreamReader reader = new StreamReader(ToExtendedLengthPath(metaPath)))
            {
                for (int lineIndex = 0; lineIndex < 20 && !reader.EndOfStream; lineIndex++)
                {
                    string line = reader.ReadLine();
                    Match match = MetaGuidRegex.Match(line ?? string.Empty);
                    if (match.Success) return match.Groups[1].Value;
                }
            }

            return string.Empty;
        }

        private static bool IsPathWithinRoot(string path, string root)
        {
            string rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                       Path.DirectorySeparatorChar;
            return path.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRelativePath(string root, string path)
        {
            Uri rootUri = new Uri(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                  Path.DirectorySeparatorChar);
            Uri pathUri = new Uri(path);
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString())
                .Replace('/', Path.DirectorySeparatorChar);
        }

        private static string GetAbsolutePath(string path)
        {
            if (Path.IsPathRooted(path)) return NormalizeFullPath(path);
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return NormalizeFullPath(Path.Combine(projectRoot, path));
        }

        private static string GetProjectRelativePath(string absolutePath)
        {
            string projectRoot = NormalizeFullPath(Directory.GetParent(Application.dataPath).FullName);
            string normalizedPath = NormalizeFullPath(absolutePath);
            if (!IsPathWithinRoot(normalizedPath, projectRoot) &&
                !string.Equals(normalizedPath, projectRoot, StringComparison.OrdinalIgnoreCase)) return string.Empty;

            return GetRelativePath(projectRoot, normalizedPath).Replace('\\', '/');
        }

        private static string NormalizeFullPath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool FileExists(string path)
        {
            return File.Exists(ToExtendedLengthPath(path));
        }

        private static string ReadAllText(string path)
        {
            return File.ReadAllText(ToExtendedLengthPath(path));
        }

        private static void WriteAllText(string path, string contents)
        {
            File.WriteAllText(ToExtendedLengthPath(path), contents, new UTF8Encoding(false));
        }

        private static void CopyFile(string sourcePath, string destinationPath)
        {
            File.Copy(ToExtendedLengthPath(sourcePath), ToExtendedLengthPath(destinationPath), true);
        }

        private static void CreateDirectory(string path)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor || path.Length <= LegacyMaxDirectoryPath)
            {
                Directory.CreateDirectory(path);
                return;
            }

            CreateLongDirectory(path);
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

        /// <summary>
        ///     Allows Unity 2019's Mono file APIs to access Windows paths beyond the legacy MAX_PATH limit.
        /// </summary>
        private static string ToExtendedLengthPath(string path, int legacyMaxPath = LegacyMaxFilePath)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor || !Path.IsPathRooted(path) ||
                path.Length <= legacyMaxPath || path.StartsWith(@"\\?\", StringComparison.Ordinal)) return path;

            if (path.StartsWith(@"\\", StringComparison.Ordinal))
                return @"\\?\UNC\" + path.Substring(2);

            return @"\\?\" + path;
        }
    }
}
