using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if ODIN_INSPECTOR
using Sirenix.Utilities.Editor;
#endif
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
        private const string GuidIndexCachePath = "Library/DaftAppleModTools/ReferenceAssetImporterGuidIndex.cache";
        private const string GuidIndexCacheVersion = "REFERENCE_ASSET_GUID_INDEX_V1";
        private const float LabelWidth = 205.0f;
        private const int LegacyMaxDirectoryPath = 247;
        private const int LegacyMaxFilePath = 259;
        private const int ErrorAlreadyExists = 183;

        private static readonly Regex GuidRegex = new Regex(
            @"\bguid:\s*([0-9a-fA-F]{32})\b", RegexOptions.Compiled);
        private static readonly Regex MetaGuidRegex = new Regex(
            @"^guid:\s*([0-9a-fA-F]{32})\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex NamespaceRegex = new Regex(
            @"\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)", RegexOptions.Compiled);
        private static readonly Regex ShaderExponentRegex = new Regex(
            @"(?<![A-Za-z0-9_.])[+-]?(?:\d+(?:\.\d*)?|\.\d+)[eE][+-]?\d+(?![A-Za-z0-9_.])",
            RegexOptions.Compiled);
        private static readonly bool IsWindowsEditor = Application.platform == RuntimePlatform.WindowsEditor;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateDirectoryW(string path, IntPtr securityAttributes);

        [SerializeField] private string sourceAssetPath = string.Empty;
        [SerializeField] private string exportAssetsPath = DefaultExportAssetsPath;
        [SerializeField] private string destinationPath = DefaultDestinationPath;
        [SerializeField] private bool overrideSelectedObjectDestination;
        [SerializeField] private string selectedObjectDestinationPath = DefaultDestinationPath;
        [SerializeField] private bool forceAssetRipperReindex;
        [SerializeField] private bool fixShaderExponentNotation = true;
        [SerializeField] private bool reportOnly = false;
        [SerializeField] private bool overwriteExisting = true;
        [SerializeField] private Vector2 reportScrollPosition;
        [SerializeField] private string report = "Select an AssetRipper asset to begin.";

        private CancellationTokenSource importCancellation;
        private bool isImporting;
        private float importProgress;
        private string importStatus = string.Empty;

        [MenuItem("Tools/Import Reference Asset")]
        public static void ShowWindow()
        {
            ReferenceAssetImporterWindow window = GetWindow<ReferenceAssetImporterWindow>();
            window.titleContent = new GUIContent("Reference Asset Importer");
            window.minSize = new Vector2(640.0f, 430.0f);
        }

        private void OnGUI()
        {
            EditorGUIUtility.labelWidth = LabelWidth;

#if ODIN_INSPECTOR
            SirenixEditorGUI.Title(
                "AssetRipper Reference Asset Importer",
                "Import an asset and its recursive dependency closure",
                TextAlignment.Left,
                true);
            SirenixEditorGUI.InfoMessageBox(
                "Copies the selected asset and its recursive dependencies while preserving AssetRipper GUIDs. " +
                "Exported scripts are remapped to MonoScript types in ThunderKit's imported game DLLs.");
            SirenixEditorGUI.Title("Import Paths", null, TextAlignment.Left, true);
#else
            EditorGUILayout.LabelField("AssetRipper Reference Asset Importer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Copies the selected asset and its recursive dependencies while preserving AssetRipper GUIDs. " +
                "Exported scripts are remapped to MonoScript types in ThunderKit's imported game DLLs.",
                MessageType.Info);
#endif

            using (new EditorGUI.DisabledScope(isImporting))
            {
                DrawPathField("Export Assets Root", ref exportAssetsPath, true);
                DrawSourceAssetField();
                DrawPathField("Dependencies Destination", ref destinationPath, false);
                overrideSelectedObjectDestination = EditorGUILayout.Toggle(
                    "Override Object Destination", overrideSelectedObjectDestination);
                if (overrideSelectedObjectDestination)
                    DrawPathField("Selected Object Destination", ref selectedObjectDestinationPath, false);

#if ODIN_INSPECTOR
                SirenixEditorGUI.Title("Import Options", null, TextAlignment.Left, true);
#else
                EditorGUILayout.Space();
#endif
                forceAssetRipperReindex = EditorGUILayout.Toggle(
                    "Force AssetRipper Re-index", forceAssetRipperReindex);
                fixShaderExponentNotation = EditorGUILayout.Toggle(
                    "Fix shader E notation", fixShaderExponentNotation);
                reportOnly = EditorGUILayout.Toggle("Report only", reportOnly);
                overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
            }

            EditorGUILayout.Space();
            if (isImporting)
            {
                Rect progressRect = EditorGUILayout.GetControlRect(false, 22.0f);
                EditorGUI.ProgressBar(progressRect, importProgress, importStatus);
                if (GUILayout.Button("Cancel Import", GUILayout.Height(28.0f))) importCancellation.Cancel();
            }
            else
            {
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(sourceAssetPath)))
                {
                    if (GUILayout.Button("Import Asset and Dependencies", GUILayout.Height(32.0f)))
                        ImportSelectedAssetAsync();
                }
            }

            EditorGUILayout.Space();
#if ODIN_INSPECTOR
            SirenixEditorGUI.Title("Import Report", null, TextAlignment.Left, true);
#else
            EditorGUILayout.LabelField("Import Report", EditorStyles.boldLabel);
#endif
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

        private async void ImportSelectedAssetAsync()
        {
            if (isImporting) return;

            StringBuilder reportBuilder = new StringBuilder();
            string absoluteExportRoot = NormalizeFullPath(GetAbsolutePath(exportAssetsPath));
            string absoluteSourcePath = NormalizeFullPath(sourceAssetPath);
            string normalizedDestination = destinationPath.Replace('\\', '/').TrimEnd('/');
            string normalizedSelectedObjectDestination =
                selectedObjectDestinationPath.Replace('\\', '/').TrimEnd('/');
            string absoluteGuidIndexCachePath = GetAbsolutePath(GuidIndexCachePath);

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

            if (IsManagedAssemblyArtifact(absoluteSourcePath))
            {
                report = "Managed assemblies and their debug symbols cannot be imported as reference assets.";
                return;
            }

            if (!IsPathWithinRoot(absoluteSourcePath, absoluteExportRoot))
            {
                report = $"The selected asset is outside the configured export root: {absoluteExportRoot}";
                return;
            }

            if (!IsValidProjectDestination(normalizedDestination))
            {
                report = "Destination must be a project-relative path under Assets.";
                return;
            }

            if (overrideSelectedObjectDestination &&
                !IsValidProjectDestination(normalizedSelectedObjectDestination))
            {
                report = "Selected Object Destination must be a project-relative path under Assets.";
                return;
            }

            importCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = importCancellation.Token;
            isImporting = true;
            SetImportProgress(0.0f, "Starting import...");

            try
            {
                Progress<ImportProgress> progress = new Progress<ImportProgress>(UpdateImportProgress);
                IndexResult indexResult = await Task.Run(() => BuildExportAndDependencyIndex(
                    absoluteExportRoot, absoluteSourcePath, absoluteGuidIndexCachePath, forceAssetRipperReindex,
                    progress, cancellationToken), cancellationToken);
                Dictionary<string, string> exportedAssetsByGuid = indexResult.AssetsByGuid;
                HashSet<string> sourceAssets = indexResult.SourceAssets;
                HashSet<string> scriptGuids = indexResult.ScriptGuids;
                reportBuilder.Append(indexResult.Report);

                cancellationToken.ThrowIfCancellationRequested();
                SetImportProgress(0.55f, "Indexing ThunderKit game scripts...");
                Dictionary<string, MonoScript> gameScriptsByType = BuildGameScriptIndex(reportBuilder);

                cancellationToken.ThrowIfCancellationRequested();
                SetImportProgress(0.65f, "Resolving game script references...");
                Dictionary<string, string> scriptReferences = BuildScriptReferences(
                    scriptGuids, exportedAssetsByGuid, gameScriptsByType, reportBuilder);
                int copiedCount = await CopyAssetsAsync(absoluteExportRoot, absoluteSourcePath,
                    normalizedDestination, normalizedSelectedObjectDestination, sourceAssets, scriptReferences,
                    reportBuilder, progress, cancellationToken);

                if (!reportOnly)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SetImportProgress(0.96f, "Refreshing the Unity Asset Database...");
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    string rootRelativePath = GetRelativePath(absoluteExportRoot, absoluteSourcePath);
                    string importedRootPath = overrideSelectedObjectDestination
                        ? CombineAssetPath(normalizedSelectedObjectDestination, Path.GetFileName(absoluteSourcePath))
                        : CombineAssetPath(normalizedDestination, rootRelativePath.Replace('\\', '/'));
                    Object importedAsset = AssetDatabase.LoadMainAssetAtPath(importedRootPath);
                    if (importedAsset) EditorGUIUtility.PingObject(importedAsset);
                }

                reportBuilder.Insert(0,
                    $"{(reportOnly ? "Report only" : "Import")} complete. Discovered {sourceAssets.Count} assets, " +
                    $"{(reportOnly ? "would copy or update" : "copied or updated")} {copiedCount}, " +
                    $"and resolved {scriptReferences.Count} game script references.\n\n");
                report = reportBuilder.ToString();
                SetImportProgress(1.0f, "Import complete");
            }
            catch (OperationCanceledException)
            {
                reportBuilder.Insert(0, "Import cancelled. Files already copied during this run were not removed.\n\n");
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
            finally
            {
                isImporting = false;
                forceAssetRipperReindex = false;
                importCancellation.Dispose();
                importCancellation = null;
                Repaint();
            }
        }

        private static IndexResult BuildExportAndDependencyIndex(string exportRoot, string sourcePath,
            string cachePath, bool forceReindex, IProgress<ImportProgress> progress,
            CancellationToken cancellationToken)
        {
            StringBuilder indexReport = new StringBuilder();
            Dictionary<string, string> assetsByGuid;
            if (!forceReindex && TryLoadExportedGuidIndex(
                    exportRoot, cachePath, cancellationToken, out assetsByGuid))
            {
                indexReport.AppendLine($"Loaded {assetsByGuid.Count} AssetRipper GUIDs from the cached index.");
                progress.Report(new ImportProgress(0.42f, $"Loaded {assetsByGuid.Count} cached AssetRipper GUIDs."));
            }
            else
            {
                assetsByGuid = BuildExportedGuidIndex(exportRoot, indexReport, progress, cancellationToken);
                SaveExportedGuidIndex(exportRoot, cachePath, assetsByGuid, cancellationToken);
                indexReport.AppendLine($"Cached {assetsByGuid.Count} AssetRipper GUIDs for future imports.");
            }

            HashSet<string> sourceAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> scriptGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            DiscoverDependencies(sourcePath, assetsByGuid, sourceAssets, scriptGuids, progress, cancellationToken);
            return new IndexResult(assetsByGuid, sourceAssets, scriptGuids, indexReport.ToString());
        }

        private static Dictionary<string, string> BuildExportedGuidIndex(string exportRoot,
            StringBuilder reportBuilder, IProgress<ImportProgress> progress, CancellationToken cancellationToken)
        {
            Dictionary<string, string> assetsByGuid =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] metaPaths = Directory.GetFiles(exportRoot, "*.meta", SearchOption.AllDirectories);

            for (int metaIndex = 0; metaIndex < metaPaths.Length; metaIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (metaIndex % 100 == 0)
                {
                    float phaseProgress = metaPaths.Length == 0 ? 1.0f : (float)metaIndex / metaPaths.Length;
                    progress.Report(new ImportProgress(
                        0.02f + 0.40f * phaseProgress,
                        $"Indexing AssetRipper metadata ({metaIndex}/{metaPaths.Length})..."));
                }

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

        private static bool TryLoadExportedGuidIndex(string exportRoot, string cachePath,
            CancellationToken cancellationToken, out Dictionary<string, string> assetsByGuid)
        {
            assetsByGuid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!FileExists(cachePath)) return false;

            try
            {
                using (StreamReader reader = new StreamReader(ToExtendedLengthPath(cachePath)))
                {
                    string version = reader.ReadLine();
                    string cachedExportRoot = reader.ReadLine();
                    if (!string.Equals(version, GuidIndexCacheVersion, StringComparison.Ordinal) ||
                        !string.Equals(cachedExportRoot, exportRoot, StringComparison.OrdinalIgnoreCase)) return false;

                    while (!reader.EndOfStream)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string line = reader.ReadLine();
                        int separatorIndex = string.IsNullOrEmpty(line) ? -1 : line.IndexOf('\t');
                        if (separatorIndex <= 0 || separatorIndex >= line.Length - 1) return false;

                        string guid = line.Substring(0, separatorIndex);
                        string encodedRelativePath = line.Substring(separatorIndex + 1);
                        string relativePath = Encoding.UTF8.GetString(Convert.FromBase64String(encodedRelativePath));
                        assetsByGuid[guid] = NormalizeFullPath(Path.Combine(exportRoot, relativePath));
                    }
                }

                return assetsByGuid.Count > 0;
            }
            catch (FormatException)
            {
                assetsByGuid.Clear();
                return false;
            }
            catch (IOException)
            {
                assetsByGuid.Clear();
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                assetsByGuid.Clear();
                return false;
            }
        }

        private static void SaveExportedGuidIndex(string exportRoot, string cachePath,
            Dictionary<string, string> assetsByGuid, CancellationToken cancellationToken)
        {
            string cacheDirectory = Path.GetDirectoryName(cachePath);
            if (!string.IsNullOrEmpty(cacheDirectory)) CreateDirectory(cacheDirectory);

            using (StreamWriter writer = new StreamWriter(
                       ToExtendedLengthPath(cachePath), false, new UTF8Encoding(false)))
            {
                writer.WriteLine(GuidIndexCacheVersion);
                writer.WriteLine(exportRoot);
                foreach (KeyValuePair<string, string> assetByGuid in assetsByGuid)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string relativePath = GetRelativePath(exportRoot, assetByGuid.Value);
                    string encodedRelativePath = Convert.ToBase64String(Encoding.UTF8.GetBytes(relativePath));
                    writer.WriteLine(assetByGuid.Key + "\t" + encodedRelativePath);
                }
            }
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
            HashSet<string> discoveredAssets, HashSet<string> scriptGuids, IProgress<ImportProgress> progress,
            CancellationToken cancellationToken)
        {
            Queue<string> pendingAssets = new Queue<string>();
            pendingAssets.Enqueue(rootAssetPath);

            while (pendingAssets.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string assetPath = pendingAssets.Dequeue();
                if (!discoveredAssets.Add(assetPath)) continue;

                if (discoveredAssets.Count % 25 == 0)
                    progress.Report(new ImportProgress(
                        0.47f, $"Discovering dependencies ({discoveredAssets.Count} found)..."));

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
                else if (IsManagedAssemblyArtifact(referencedPath))
                    continue;
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

        private async Task<int> CopyAssetsAsync(string exportRoot, string selectedSourcePath, string projectDestination,
            string selectedObjectDestination, HashSet<string> sourceAssets,
            Dictionary<string, string> scriptReferences, StringBuilder reportBuilder,
            IProgress<ImportProgress> progress, CancellationToken cancellationToken)
        {
            int copiedCount = 0;
            int processedCount = 0;
            foreach (string sourcePath in sourceAssets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (IsManagedAssemblyArtifact(sourcePath))
                {
                    reportBuilder.AppendLine($"EXCLUDED MANAGED ASSEMBLY: {sourcePath}");
                    continue;
                }

                string sourceMetaPath = sourcePath + ".meta";
                string sourceGuid = GetMetaGuid(sourceMetaPath);
                string existingAssetPath = string.IsNullOrEmpty(sourceGuid)
                    ? string.Empty
                    : AssetDatabase.GUIDToAssetPath(sourceGuid);
                string relativePath = GetRelativePath(exportRoot, sourcePath).Replace('\\', '/');
                bool isSelectedAsset = string.Equals(
                    sourcePath, selectedSourcePath, StringComparison.OrdinalIgnoreCase);
                string destinationAssetPath = overrideSelectedObjectDestination && isSelectedAsset
                    ? CombineAssetPath(selectedObjectDestination, Path.GetFileName(sourcePath))
                    : CombineAssetPath(projectDestination, relativePath);
                bool guidExistsAtAnotherPath = !string.IsNullOrEmpty(existingAssetPath) &&
                    !string.Equals(existingAssetPath, destinationAssetPath, StringComparison.OrdinalIgnoreCase);
                bool copySelectedAssetWithNewGuid = guidExistsAtAnotherPath &&
                    overrideSelectedObjectDestination && isSelectedAsset;

                if (guidExistsAtAnotherPath && !copySelectedAssetWithNewGuid)
                {
                    reportBuilder.AppendLine($"REUSED: {relativePath} -> {existingAssetPath}");
                    if (fixShaderExponentNotation && IsShaderAsset(sourcePath) &&
                        IsProjectAssetPath(existingAssetPath))
                    {
                        int replacementCount = FixShaderFile(
                            GetAbsolutePath(existingAssetPath), !reportOnly);
                        if (replacementCount > 0)
                        {
                            copiedCount++;
                            reportBuilder.AppendLine(
                                $"{(reportOnly ? "WOULD FIX" : "FIXED")} SHADER E NOTATION: " +
                                $"{existingAssetPath} ({replacementCount} replacements)");
                        }
                    }
                }
                else
                {
                    string absoluteDestinationPath = GetAbsolutePath(destinationAssetPath);
                    if (FileExists(absoluteDestinationPath) && !overwriteExisting)
                    {
                        reportBuilder.AppendLine($"SKIPPED EXISTING: {destinationAssetPath}");
                    }
                    else if (reportOnly)
                    {
                        copiedCount++;
                        string copyDescription = copySelectedAssetWithNewGuid
                            ? "WOULD COPY WITH NEW GUID"
                            : "WOULD COPY";
                        reportBuilder.AppendLine($"{copyDescription}: {destinationAssetPath}");
                        if (fixShaderExponentNotation && IsShaderAsset(sourcePath))
                        {
                            int replacementCount = CountShaderExponentReplacements(ReadAllText(sourcePath));
                            if (replacementCount > 0)
                                reportBuilder.AppendLine(
                                    $"WOULD FIX SHADER E NOTATION: {destinationAssetPath} " +
                                    $"({replacementCount} replacements)");
                        }
                    }
                    else
                    {
                        string destinationDirectory = Path.GetDirectoryName(absoluteDestinationPath);
                        if (!string.IsNullOrEmpty(destinationDirectory)) CreateDirectory(destinationDirectory);

                        if (IsTextSerializedAsset(sourcePath))
                        {
                            string contents = ReadAllText(sourcePath);
                            contents = RemapScriptReferences(contents, scriptReferences);
                            if (fixShaderExponentNotation && IsShaderAsset(sourcePath))
                            {
                                int replacementCount;
                                contents = FixShaderExponentNotation(contents, out replacementCount);
                                if (replacementCount > 0)
                                    reportBuilder.AppendLine(
                                        $"FIXED SHADER E NOTATION: {destinationAssetPath} " +
                                        $"({replacementCount} replacements)");
                            }

                            WriteAllText(absoluteDestinationPath, contents);
                        }
                        else
                        {
                            CopyFile(sourcePath, absoluteDestinationPath);
                        }

                        if (!copySelectedAssetWithNewGuid && FileExists(sourceMetaPath))
                            CopyFile(sourceMetaPath, absoluteDestinationPath + ".meta");

                        copiedCount++;
                        string copyDescription = copySelectedAssetWithNewGuid
                            ? "COPIED WITH NEW GUID"
                            : "COPIED";
                        reportBuilder.AppendLine($"{copyDescription}: {destinationAssetPath}");
                    }
                }

                processedCount++;
                progress.Report(new ImportProgress(
                    0.70f + 0.24f * processedCount / sourceAssets.Count,
                    $"{(reportOnly ? "Planning" : "Copying")} assets ({processedCount}/{sourceAssets.Count})..."));
                if (processedCount % 10 == 0)
                {
                    Repaint();
                    await Task.Yield();
                }
            }

            return copiedCount;
        }

        private static bool IsValidProjectDestination(string path)
        {
            return IsProjectAssetPath(path) || string.Equals(path, "Assets", StringComparison.Ordinal);
        }

        private static bool IsProjectAssetPath(string path)
        {
            return path.StartsWith("Assets/", StringComparison.Ordinal);
        }

        private static string CombineAssetPath(string directory, string relativePath)
        {
            return directory + "/" + relativePath.TrimStart('/');
        }

        private void UpdateImportProgress(ImportProgress progress)
        {
            SetImportProgress(progress.Value, progress.Status);
        }

        private void SetImportProgress(float value, string status)
        {
            importProgress = value;
            importStatus = status;
            Repaint();
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

        private static int FixShaderFile(string path, bool writeChanges)
        {
            if (!FileExists(path)) return 0;

            string contents = ReadAllText(path);
            int replacementCount;
            string fixedContents = FixShaderExponentNotation(contents, out replacementCount);
            if (writeChanges && replacementCount > 0) WriteAllText(path, fixedContents);
            return replacementCount;
        }

        private static int CountShaderExponentReplacements(string contents)
        {
            int replacementCount;
            FixShaderExponentNotation(contents, out replacementCount);
            return replacementCount;
        }

        private static string FixShaderExponentNotation(string contents, out int replacementCount)
        {
            int convertedCount = 0;
            string fixedContents = ShaderExponentRegex.Replace(contents, match =>
            {
                decimal value;
                if (!decimal.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    return match.Value;

                convertedCount++;
                return value.ToString("0.#############################", CultureInfo.InvariantCulture);
            });
            replacementCount = convertedCount;
            return fixedContents;
        }

        private static bool IsShaderAsset(string path)
        {
            return string.Equals(Path.GetExtension(path), ".shader", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsManagedAssemblyArtifact(string path)
        {
            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".pdb", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".mdb", StringComparison.OrdinalIgnoreCase);
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
            if (!IsWindowsEditor || path.Length <= LegacyMaxDirectoryPath)
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
            if (!IsWindowsEditor || !Path.IsPathRooted(path) ||
                path.Length <= legacyMaxPath || path.StartsWith(@"\\?\", StringComparison.Ordinal)) return path;

            if (path.StartsWith(@"\\", StringComparison.Ordinal))
                return @"\\?\UNC\" + path.Substring(2);

            return @"\\?\" + path;
        }

        private sealed class IndexResult
        {
            public readonly Dictionary<string, string> AssetsByGuid;
            public readonly string Report;
            public readonly HashSet<string> ScriptGuids;
            public readonly HashSet<string> SourceAssets;

            public IndexResult(Dictionary<string, string> assetsByGuid, HashSet<string> sourceAssets,
                HashSet<string> scriptGuids, string report)
            {
                AssetsByGuid = assetsByGuid;
                SourceAssets = sourceAssets;
                ScriptGuids = scriptGuids;
                Report = report;
            }
        }

        private struct ImportProgress
        {
            public readonly string Status;
            public readonly float Value;

            public ImportProgress(float value, string status)
            {
                Value = value;
                Status = status;
            }
        }
    }
}
