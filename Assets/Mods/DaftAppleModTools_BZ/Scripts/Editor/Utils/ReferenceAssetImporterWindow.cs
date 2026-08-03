using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static DaftAppleGames.Editor.ReferenceAssetImporterFileSystem;
using static DaftAppleGames.Editor.ReferenceAssetImporterRepairs;
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

        private static readonly Regex GuidRegex = new Regex(
            @"\bguid:\s*([0-9a-fA-F]{32})\b", RegexOptions.Compiled);
        private static readonly Regex NamespaceRegex = new Regex(
            @"\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)", RegexOptions.Compiled);
        private static readonly Regex MonoScriptReferenceRegex = new Regex(
            @"m_Script:\s*\{\s*fileID:\s*(-?\d+)\s*,\s*guid:\s*([0-9a-fA-F]{32})\s*,\s*type:\s*3\s*\}",
            RegexOptions.Compiled);

        [SerializeField] private string sourceAssetPath = string.Empty;
        [SerializeField] private string exportAssetsPath = DefaultExportAssetsPath;
        [SerializeField] private string destinationPath = DefaultDestinationPath;
        [SerializeField] private bool overrideSelectedObjectDestination;
        [SerializeField] private string selectedObjectDestinationPath = DefaultDestinationPath;
        [SerializeField] private bool forceAssetRipperReindex;
        [SerializeField] private bool fixShaderExponentNotation = true;
        [SerializeField] private bool repairMissingTmpAtlases = true;
        [SerializeField] private bool reportOnly = false;
        [SerializeField] private bool overwriteExisting = true;
        [SerializeField] private Vector2 reportScrollPosition;
        [SerializeField] private string report = "Select an AssetRipper asset to begin.";

        private CancellationTokenSource importCancellation;
        private bool isImporting;
        private float importProgress;
        private string importStatus = string.Empty;

        internal string SourceAssetPath { get => sourceAssetPath; set => sourceAssetPath = value; }
        internal string ExportAssetsPath { get => exportAssetsPath; set => exportAssetsPath = value; }
        internal string DestinationPath { get => destinationPath; set => destinationPath = value; }
        internal bool OverrideSelectedObjectDestination
        {
            get => overrideSelectedObjectDestination;
            set => overrideSelectedObjectDestination = value;
        }
        internal string SelectedObjectDestinationPath
        {
            get => selectedObjectDestinationPath;
            set => selectedObjectDestinationPath = value;
        }
        internal bool ForceAssetRipperReindex
        {
            get => forceAssetRipperReindex;
            set => forceAssetRipperReindex = value;
        }
        internal bool FixShaderENotation
        {
            get => fixShaderExponentNotation;
            set => fixShaderExponentNotation = value;
        }
        internal bool RepairMissingTmpAtlases
        {
            get => repairMissingTmpAtlases;
            set => repairMissingTmpAtlases = value;
        }
        internal bool ReportOnly { get => reportOnly; set => reportOnly = value; }
        internal bool OverwriteExisting { get => overwriteExisting; set => overwriteExisting = value; }
        internal Vector2 ReportScrollPosition
        {
            get => reportScrollPosition;
            set => reportScrollPosition = value;
        }
        internal string Report => report;
        internal bool IsImporting => isImporting;
        internal float ProgressValue => importProgress;
        internal string ImportStatus => importStatus;

        internal void BeginImport()
        {
            ImportSelectedAssetAsync();
        }

        internal void CancelImport()
        {
            if (importCancellation != null) importCancellation.Cancel();
        }

        [MenuItem("Tools/Import Reference Asset")]
        public static void ShowWindow()
        {
            ReferenceAssetImporterWindow window = GetWindow<ReferenceAssetImporterWindow>();
            window.titleContent = new GUIContent("Reference Asset Importer");
            window.minSize = new Vector2(640.0f, 430.0f);
        }

        private void OnGUI()
        {
            ReferenceAssetImporterWindowGui.Draw(this);
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
                HashSet<ManagedScriptReference> managedScriptReferences = indexResult.ManagedScriptReferences;
                reportBuilder.Append(indexResult.Report);

                cancellationToken.ThrowIfCancellationRequested();
                SetImportProgress(0.55f, "Indexing ThunderKit game scripts...");
                await Task.Yield();
                Dictionary<string, MonoScript> gameScriptsByType = BuildGameScriptIndex(reportBuilder);

                cancellationToken.ThrowIfCancellationRequested();
                SetImportProgress(0.65f, "Resolving game script references...");
                await Task.Yield();
                Dictionary<string, string> scriptReferences = BuildScriptReferences(
                    scriptGuids, exportedAssetsByGuid, gameScriptsByType, reportBuilder);
                Dictionary<ManagedScriptReference, string> managedAssemblyScriptReferences =
                    BuildManagedAssemblyScriptReferences(
                        managedScriptReferences, exportedAssetsByGuid, reportBuilder);
                HashSet<string> tmpFontAssetsToRepair =
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int copiedCount = await CopyAssetsAsync(absoluteExportRoot, absoluteSourcePath,
                    normalizedDestination, normalizedSelectedObjectDestination, sourceAssets, scriptReferences,
                    managedAssemblyScriptReferences, tmpFontAssetsToRepair, reportBuilder, progress,
                    cancellationToken);

                if (!reportOnly)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SetImportProgress(0.96f, "Refreshing the Unity Asset Database...");
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    if (repairMissingTmpAtlases && tmpFontAssetsToRepair.Count > 0)
                    {
                        SetImportProgress(0.98f, "Repairing missing TextMeshPro atlases...");
                        RepairMissingTmpFontAtlases(tmpFontAssetsToRepair, reportBuilder);
                    }

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
                    $"and resolved {scriptReferences.Count + managedAssemblyScriptReferences.Count} " +
                    "script references.\n\n");
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
            HashSet<ManagedScriptReference> managedScriptReferences = new HashSet<ManagedScriptReference>();
            DiscoverDependencies(sourcePath, assetsByGuid, sourceAssets, scriptGuids, managedScriptReferences,
                progress, cancellationToken);
            return new IndexResult(assetsByGuid, sourceAssets, scriptGuids, managedScriptReferences,
                indexReport.ToString());
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
            HashSet<string> discoveredAssets, HashSet<string> scriptGuids,
            HashSet<ManagedScriptReference> managedScriptReferences, IProgress<ImportProgress> progress,
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
                        0.47f, $"Discovering dependencies ({discoveredAssets.Count} found): " +
                               Path.GetFileName(assetPath)));

                FindReferencedAssets(
                    assetPath, assetsByGuid, pendingAssets, scriptGuids, managedScriptReferences);
                string metaPath = assetPath + ".meta";
                if (FileExists(metaPath))
                    FindReferencedAssets(
                        metaPath, assetsByGuid, pendingAssets, scriptGuids, managedScriptReferences);
            }
        }

        private static void FindReferencedAssets(string sourcePath, Dictionary<string, string> assetsByGuid,
            Queue<string> pendingAssets, HashSet<string> scriptGuids,
            HashSet<ManagedScriptReference> managedScriptReferences)
        {
            if (!IsTextSerializedAsset(sourcePath)) return;

            string contents = ReadAllText(sourcePath);
            FindManagedScriptReferences(contents, assetsByGuid, managedScriptReferences);
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

        private static void FindManagedScriptReferences(string contents, Dictionary<string, string> assetsByGuid,
            HashSet<ManagedScriptReference> managedScriptReferences)
        {
            MatchCollection matches = MonoScriptReferenceRegex.Matches(contents);
            for (int matchIndex = 0; matchIndex < matches.Count; matchIndex++)
            {
                long localId;
                if (!long.TryParse(
                        matches[matchIndex].Groups[1].Value, NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out localId)) continue;

                string guid = matches[matchIndex].Groups[2].Value;
                string referencedPath;
                if (assetsByGuid.TryGetValue(guid, out referencedPath) &&
                    IsManagedAssemblyArtifact(referencedPath))
                    managedScriptReferences.Add(new ManagedScriptReference(guid, localId));
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

        private static Dictionary<ManagedScriptReference, string> BuildManagedAssemblyScriptReferences(
            HashSet<ManagedScriptReference> sourceReferences, Dictionary<string, string> assetsByGuid,
            StringBuilder reportBuilder)
        {
            Dictionary<ManagedScriptReference, string> references =
                new Dictionary<ManagedScriptReference, string>();
            if (sourceReferences.Count == 0) return references;

            Dictionary<string, MonoScript> projectScriptsByAssemblyAndLocalId =
                BuildProjectScriptLocalIdIndex(sourceReferences, assetsByGuid, reportBuilder);
            foreach (ManagedScriptReference sourceReference in sourceReferences)
            {
                string sourceAssemblyPath;
                if (!assetsByGuid.TryGetValue(sourceReference.Guid, out sourceAssemblyPath)) continue;

                string assemblyName = Path.GetFileNameWithoutExtension(sourceAssemblyPath);
                string lookupKey = GetManagedScriptLookupKey(assemblyName, sourceReference.LocalId);
                MonoScript targetScript;
                if (!projectScriptsByAssemblyAndLocalId.TryGetValue(lookupKey, out targetScript))
                {
                    reportBuilder.AppendLine(
                        $"UNRESOLVED MANAGED SCRIPT: {assemblyName}:{sourceReference.LocalId}");
                    continue;
                }

                string targetGuid;
                long targetLocalId;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        targetScript, out targetGuid, out targetLocalId))
                {
                    reportBuilder.AppendLine(
                        $"UNRESOLVED MANAGED SCRIPT ID: {targetScript.GetClass()?.FullName}");
                    continue;
                }

                references[sourceReference] =
                    $"fileID: {targetLocalId}, guid: {targetGuid}, type: 3";
                Type targetType = targetScript.GetClass();
                reportBuilder.AppendLine(
                    $"MANAGED SCRIPT: {assemblyName}:{sourceReference.LocalId} -> " +
                    $"{targetType?.FullName} ({AssetDatabase.GetAssetPath(targetScript)})");
            }

            return references;
        }

        private static Dictionary<string, MonoScript> BuildProjectScriptLocalIdIndex(
            HashSet<ManagedScriptReference> sourceReferences, Dictionary<string, string> assetsByGuid,
            StringBuilder reportBuilder)
        {
            Dictionary<string, MonoScript> scriptsByAssemblyAndLocalId =
                new Dictionary<string, MonoScript>(StringComparer.Ordinal);
            Dictionary<string, HashSet<long>> requiredIdsByAssembly =
                BuildRequiredManagedScriptIds(sourceReferences, assetsByGuid);
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int assemblyIndex = 0; assemblyIndex < loadedAssemblies.Length; assemblyIndex++)
            {
                Assembly assembly = loadedAssemblies[assemblyIndex];
                string assemblyName = assembly.GetName().Name;
                HashSet<long> requiredLocalIds;
                if (!requiredIdsByAssembly.TryGetValue(assemblyName, out requiredLocalIds)) continue;

                Type[] types = GetLoadableTypes(assembly);
                for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
                {
                    Type scriptType = types[typeIndex];
                    if (scriptType is null || !typeof(UnityEngine.Object).IsAssignableFrom(scriptType)) continue;

                    long dllLocalId = ComputeManagedScriptLocalId(scriptType);
                    if (!requiredLocalIds.Contains(dllLocalId)) continue;

                    MonoScript monoScript = FindMonoScriptForType(scriptType);
                    if (!monoScript) continue;

                    string lookupKey = GetManagedScriptLookupKey(assemblyName, dllLocalId);
                    scriptsByAssemblyAndLocalId[lookupKey] = monoScript;
                }
            }

            reportBuilder.AppendLine(
                $"Resolved {scriptsByAssemblyAndLocalId.Count} targeted project scripts for " +
                "managed assembly reference remapping.");
            return scriptsByAssemblyAndLocalId;
        }

        private static Dictionary<string, HashSet<long>> BuildRequiredManagedScriptIds(
            HashSet<ManagedScriptReference> sourceReferences, Dictionary<string, string> assetsByGuid)
        {
            Dictionary<string, HashSet<long>> requiredIdsByAssembly =
                new Dictionary<string, HashSet<long>>(StringComparer.Ordinal);
            foreach (ManagedScriptReference sourceReference in sourceReferences)
            {
                string assemblyPath;
                if (!assetsByGuid.TryGetValue(sourceReference.Guid, out assemblyPath)) continue;

                string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                HashSet<long> requiredLocalIds;
                if (!requiredIdsByAssembly.TryGetValue(assemblyName, out requiredLocalIds))
                {
                    requiredLocalIds = new HashSet<long>();
                    requiredIdsByAssembly.Add(assemblyName, requiredLocalIds);
                }

                requiredLocalIds.Add(sourceReference.LocalId);
            }

            return requiredIdsByAssembly;
        }

        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                List<Type> loadableTypes = new List<Type>(exception.Types.Length);
                for (int typeIndex = 0; typeIndex < exception.Types.Length; typeIndex++)
                {
                    Type type = exception.Types[typeIndex];
                    if (type != null) loadableTypes.Add(type);
                }

                return loadableTypes.ToArray();
            }
        }

        private static MonoScript FindMonoScriptForType(Type targetType)
        {
            string[] candidateGuids = AssetDatabase.FindAssets(targetType.Name + " t:MonoScript");
            for (int candidateIndex = 0; candidateIndex < candidateGuids.Length; candidateIndex++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(candidateGuids[candidateIndex]);
                MonoScript monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                if (monoScript && monoScript.GetClass() == targetType) return monoScript;
            }

            return null;
        }

        private static string GetManagedScriptLookupKey(string assemblyName, long localId)
        {
            return assemblyName + ":" + localId.ToString(CultureInfo.InvariantCulture);
        }

        private static long ComputeManagedScriptLocalId(Type scriptType)
        {
            string hashInput = "s\0\0\0" + (scriptType.Namespace ?? string.Empty) + scriptType.Name;
            byte[] hash = ReferenceAssetImporterMd4.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
            uint unsignedValue = (uint)(hash[0] | hash[1] << 8 | hash[2] << 16 | hash[3] << 24);
            return unchecked((int)unsignedValue);
        }

        private async Task<int> CopyAssetsAsync(string exportRoot, string selectedSourcePath, string projectDestination,
            string selectedObjectDestination, HashSet<string> sourceAssets,
            Dictionary<string, string> scriptReferences,
            Dictionary<ManagedScriptReference, string> managedAssemblyScriptReferences,
            HashSet<string> tmpFontAssetsToRepair, StringBuilder reportBuilder,
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
                if (!string.IsNullOrEmpty(existingAssetPath) &&
                    !FileExists(GetAbsolutePath(existingAssetPath)))
                {
                    reportBuilder.AppendLine(
                        $"IGNORED STALE GUID PATH: {sourceGuid} -> {existingAssetPath}");
                    existingAssetPath = string.Empty;
                }

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
                if (repairMissingTmpAtlases && IsDynamicTmpFontAsset(sourcePath))
                {
                    string fontAssetPath = guidExistsAtAnotherPath && !copySelectedAssetWithNewGuid
                        ? existingAssetPath
                        : destinationAssetPath;
                    if (IsProjectAssetPath(fontAssetPath)) tmpFontAssetsToRepair.Add(fontAssetPath);
                }

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
                            contents = RemapManagedAssemblyScriptReferences(
                                contents, managedAssemblyScriptReferences);
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

        private static string RemapManagedAssemblyScriptReferences(string contents,
            Dictionary<ManagedScriptReference, string> scriptReferences)
        {
            foreach (KeyValuePair<ManagedScriptReference, string> scriptReference in scriptReferences)
            {
                ManagedScriptReference sourceReference = scriptReference.Key;
                string pattern = @"m_Script:\s*\{\s*fileID:\s*" +
                                 sourceReference.LocalId.ToString(CultureInfo.InvariantCulture) +
                                 @"\s*,\s*guid:\s*" + Regex.Escape(sourceReference.Guid) +
                                 @"\s*,\s*type:\s*3\s*\}";
                contents = Regex.Replace(
                    contents, pattern, "m_Script: {" + scriptReference.Value + "}");
            }

            return contents;
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

        private sealed class IndexResult
        {
            public readonly Dictionary<string, string> AssetsByGuid;
            public readonly HashSet<ManagedScriptReference> ManagedScriptReferences;
            public readonly string Report;
            public readonly HashSet<string> ScriptGuids;
            public readonly HashSet<string> SourceAssets;

            public IndexResult(Dictionary<string, string> assetsByGuid, HashSet<string> sourceAssets,
                HashSet<string> scriptGuids, HashSet<ManagedScriptReference> managedScriptReferences,
                string report)
            {
                AssetsByGuid = assetsByGuid;
                SourceAssets = sourceAssets;
                ScriptGuids = scriptGuids;
                ManagedScriptReferences = managedScriptReferences;
                Report = report;
            }
        }

        private struct ManagedScriptReference : IEquatable<ManagedScriptReference>
        {
            public readonly string Guid;
            public readonly long LocalId;

            public ManagedScriptReference(string guid, long localId)
            {
                Guid = guid;
                LocalId = localId;
            }

            public bool Equals(ManagedScriptReference other)
            {
                return LocalId == other.LocalId &&
                       string.Equals(Guid, other.Guid, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                return obj is ManagedScriptReference && Equals((ManagedScriptReference)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Guid != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Guid) : 0) * 397) ^
                           LocalId.GetHashCode();
                }
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
