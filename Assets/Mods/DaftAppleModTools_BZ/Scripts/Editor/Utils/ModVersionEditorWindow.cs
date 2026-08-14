using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ThunderKit.Core.Manifests;
using UnityEditor;
using UnityEngine;

namespace DaftAppleGames.Editor
{
    public sealed class ModVersionEditorWindow : EditorWindow
    {
        private enum VersionComponent
        {
            Major,
            Minor,
            Patch
        }

        private const string WindowTitle = "Mod Versions";
        private const string NexusApiKeyEditorPrefsKey = "DaftAppleModTools.NexusMods.ApiKey";
        private const string NexusArchiveFolder = "ThunderKit/NexusMods";
        private const double PersistenceDelaySeconds = 0.75d;
        private const float EditorLabelWidth = 230.0f;
        private const string VersionConstantPattern =
            "(?<prefix>\\b(?:private|internal|public|protected)\\s+const\\s+string\\s+VersionString\\s*=\\s*\")(?<version>[^\"]*)(?<suffix>\"\\s*;)";

        private SerializedObject settingsObject;
        private SerializedProperty modsProperty;
        private Vector2 scrollPosition;
        private string nexusApiKey;
        private int uploadingModIndex = -1;
        private float uploadProgress;
        private string uploadStatus;
        private CancellationTokenSource uploadCancellation;
        private bool settingsSavePending;
        private bool apiKeySavePending;
        private double persistenceDueTime;

        [MenuItem("Tools/Daft Apple Games/Mod Versions")]
        public static void ShowWindow()
        {
            ModVersionEditorWindow window = GetWindow<ModVersionEditorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(820.0f, 360.0f);
        }

        private void OnEnable()
        {
            settingsObject = new SerializedObject(ModVersionSettings.Instance);
            modsProperty = settingsObject.FindProperty("mods");
            nexusApiKey = EditorPrefs.GetString(NexusApiKeyEditorPrefsKey, string.Empty);
            EditorApplication.update -= SavePendingChanges;
            EditorApplication.update += SavePendingChanges;
        }

        private void OnDisable()
        {
            EditorApplication.update -= SavePendingChanges;
            SavePendingChanges(true);

            if (uploadCancellation != null)
            {
                uploadCancellation.Cancel();
            }
        }

        private void OnGUI()
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = EditorLabelWidth;
            settingsObject.Update();

            EditorGUILayout.HelpBox(
                "Bumping updates the master version, plugin VersionString, and Manifest version. Nexus publishing uploads the generated ThunderKit ZIP as a new version of the configured Nexus file.",
                MessageType.Info);

            DrawNexusApiKey();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.PropertyField(modsProperty, true);
            ApplySettingsChanges();
            EditorGUILayout.Space();

            for (int index = 0; index < modsProperty.arraySize; index++)
            {
                DrawVersionButtons(index);
            }

            EditorGUILayout.EndScrollView();

            ApplySettingsChanges();

            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        private void DrawVersionButtons(int index)
        {
            SerializedProperty entryProperty = modsProperty.GetArrayElementAtIndex(index);
            SerializedProperty nameProperty = entryProperty.FindPropertyRelative("name");
            SerializedProperty versionProperty = entryProperty.FindPropertyRelative("version");
            string displayName = string.IsNullOrWhiteSpace(nameProperty.stringValue)
                ? $"Mod {index + 1}"
                : nameProperty.stringValue;
            string version = GetVersionString(versionProperty);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{displayName}  v{version}", EditorStyles.boldLabel, GUILayout.MinWidth(180.0f));

            if (GUILayout.Button("Major +", GUILayout.Width(90.0f)))
            {
                BumpVersion(index, VersionComponent.Major);
            }

            if (GUILayout.Button("Minor +", GUILayout.Width(90.0f)))
            {
                BumpVersion(index, VersionComponent.Minor);
            }

            if (GUILayout.Button("Patch +", GUILayout.Width(90.0f)))
            {
                BumpVersion(index, VersionComponent.Patch);
            }

            EditorGUILayout.EndHorizontal();

            DrawNexusButtons(index, displayName, version);
        }

        private void DrawNexusApiKey()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Nexus Mods", EditorStyles.boldLabel);
            string changedApiKey = EditorGUILayout.PasswordField("Personal API key", nexusApiKey);
            if (changedApiKey != nexusApiKey)
            {
                nexusApiKey = changedApiKey;
                SchedulePersistence(false, true);
            }

            EditorGUILayout.HelpBox(
                "The API key is stored in this Windows user's Unity Editor preferences and is not written to the project asset.",
                MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private void DrawNexusButtons(
            int index,
            string displayName,
            string version)
        {
            if (index < 0 || index >= ModVersionSettings.Instance.Mods.Count)
            {
                return;
            }

            ModVersionEntry entry = ModVersionSettings.Instance.Mods[index];
            string generatedZipPath = GetGeneratedZipPath(entry);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(18.0f);
            EditorGUILayout.LabelField(
                File.Exists(generatedZipPath)
                    ? $"Archive: {Path.GetFileName(generatedZipPath)}"
                    : $"Archive not built: {Path.GetFileName(generatedZipPath)}",
                GUILayout.MinWidth(300.0f));

            GUI.enabled = uploadingModIndex < 0;
            if (GUILayout.Button($"Publish {displayName} v{version}", GUILayout.Width(280.0f)))
            {
                PublishToNexus(index);
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (uploadingModIndex == index)
            {
                Rect progressRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.ProgressBar(progressRect, uploadProgress, uploadStatus);
                if (GUILayout.Button("Cancel upload", GUILayout.Width(120.0f)))
                {
                    uploadCancellation.Cancel();
                }
            }

            EditorGUILayout.Space();
        }

        private void ApplySettingsChanges()
        {
            if (!settingsObject.ApplyModifiedProperties())
            {
                return;
            }

            EditorUtility.SetDirty(ModVersionSettings.Instance);
            SchedulePersistence(true, false);
        }

        private void SchedulePersistence(bool saveSettings, bool saveApiKey)
        {
            settingsSavePending |= saveSettings;
            apiKeySavePending |= saveApiKey;
            persistenceDueTime = EditorApplication.timeSinceStartup + PersistenceDelaySeconds;
        }

        private void SavePendingChanges()
        {
            SavePendingChanges(false);
        }

        private void SavePendingChanges(bool force)
        {
            if (!settingsSavePending && !apiKeySavePending)
            {
                return;
            }

            if (!force && EditorApplication.timeSinceStartup < persistenceDueTime)
            {
                return;
            }

            if (settingsSavePending)
            {
                ModVersionSettings.Instance.SaveSettings();
                settingsSavePending = false;
            }

            if (apiKeySavePending)
            {
                EditorPrefs.SetString(NexusApiKeyEditorPrefsKey, nexusApiKey);
                apiKeySavePending = false;
            }
        }

        private async void PublishToNexus(int index)
        {
            settingsObject.ApplyModifiedProperties();
            ModVersionEntry entry = ModVersionSettings.Instance.Mods[index];
            if (!TryValidateNexusUpload(entry, out string error))
            {
                EditorUtility.DisplayDialog(WindowTitle, error, "OK");
                return;
            }

            string version = entry.Version.ToString();
            string generatedZipPath = GetGeneratedZipPath(entry);
            bool confirmed = EditorUtility.DisplayDialog(
                "Publish to Nexus Mods",
                $"Are you sure?\n\nUpload '{generatedZipPath}' as version {version} of Nexus file group {entry.NexusMods.FileGroupId}?\n\nThe description and changelog will be cleared after a successful publish.",
                "Yes, Publish",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            uploadingModIndex = index;
            uploadProgress = 0.0f;
            uploadStatus = "Starting upload...";
            CancellationTokenSource cancellation = new CancellationTokenSource();
            uploadCancellation = cancellation;
            Progress<NexusUploadProgress> progress = new Progress<NexusUploadProgress>(UpdateUploadProgress);

            try
            {
                using (NexusModsApiClient client = new NexusModsApiClient(nexusApiKey))
                {
                    string versionId = await client.UploadNewVersionAsync(
                        entry.NexusMods,
                        generatedZipPath,
                        version,
                        progress,
                        cancellation.Token);
                    ClearPublishedReleaseText(index);
                    EditorUtility.DisplayDialog(
                        WindowTitle,
                        $"Published {entry.Name} {version} successfully. Nexus version ID: {versionId}",
                        "OK");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"Nexus Mods upload for {entry.Name} was cancelled.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(WindowTitle, $"Nexus upload failed:\n\n{exception.Message}", "OK");
            }
            finally
            {
                cancellation.Dispose();
                if (uploadCancellation == cancellation)
                {
                    uploadCancellation = null;
                }

                uploadingModIndex = -1;
                Repaint();
            }
        }

        private void ClearPublishedReleaseText(int index)
        {
            settingsObject.Update();
            SerializedProperty entryProperty = modsProperty.GetArrayElementAtIndex(index);
            SerializedProperty nexusProperty = entryProperty.FindPropertyRelative("nexusMods");
            nexusProperty.FindPropertyRelative("description").stringValue = string.Empty;
            nexusProperty.FindPropertyRelative("changelog").stringValue = string.Empty;
            settingsObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(ModVersionSettings.Instance);
            SchedulePersistence(true, false);
        }

        private void UpdateUploadProgress(NexusUploadProgress progress)
        {
            uploadProgress = progress.Progress;
            uploadStatus = progress.Status;
            Repaint();
        }

        private bool TryValidateNexusUpload(ModVersionEntry entry, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(nexusApiKey))
            {
                error = "Enter your Nexus Mods personal API key.";
                return false;
            }

            if (entry.NexusMods == null || string.IsNullOrWhiteSpace(entry.NexusMods.FileGroupId))
            {
                error = "Enter the Nexus file Group ID shown in the file's API Info dialog.";
                return false;
            }

            if (entry.Manifest == null || entry.Manifest.Identity == null ||
                string.IsNullOrWhiteSpace(entry.Manifest.Identity.Name))
            {
                error = "Assign a valid ThunderKit Manifest so the generated Nexus archive can be located.";
                return false;
            }

            string generatedZipPath = GetGeneratedZipPath(entry);
            if (!File.Exists(generatedZipPath))
            {
                error = $"The generated Nexus archive does not exist:\n{generatedZipPath}\n\nRun the ThunderKit build/deploy pipeline first.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(entry.NexusMods.Changelog) &&
                string.IsNullOrWhiteSpace(entry.NexusMods.GameScopedModId))
            {
                error = "Enter the Nexus game-scoped mod ID from the mod page URL when providing a changelog.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(entry.NexusMods.Changelog) &&
                string.IsNullOrWhiteSpace(entry.NexusMods.GameDomain))
            {
                error = "Enter the Nexus game domain when providing a changelog.";
                return false;
            }

            return true;
        }

        private static string GetGeneratedZipPath(ModVersionEntry entry)
        {
            string manifestName = entry.Manifest == null || entry.Manifest.Identity == null
                ? entry.Name
                : entry.Manifest.Identity.Name;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, NexusArchiveFolder, manifestName + ".zip"));
        }

        private void BumpVersion(int index, VersionComponent component)
        {
            settingsObject.ApplyModifiedProperties();
            ModVersionEntry entry = ModVersionSettings.Instance.Mods[index];

            if (!TryValidateEntry(entry, out string pluginPath, out string error))
            {
                EditorUtility.DisplayDialog(WindowTitle, error, "OK");
                return;
            }

            Undo.RecordObject(ModVersionSettings.Instance, $"Bump {entry.Name} {component} version");
            Undo.RecordObject(entry.Manifest.Identity, $"Update {entry.Name} manifest version");

            IncrementVersion(entry.Version, component);
            string newVersion = entry.Version.ToString();
            string source = File.ReadAllText(pluginPath);
            string updatedSource = Regex.Replace(
                source,
                VersionConstantPattern,
                match => match.Groups["prefix"].Value + newVersion + match.Groups["suffix"].Value,
                RegexOptions.CultureInvariant);

            File.WriteAllText(pluginPath, updatedSource);
            entry.Manifest.Identity.Version = newVersion;
            EditorUtility.SetDirty(entry.Manifest.Identity);
            ModVersionSettings.Instance.SaveSettings();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(entry.PluginScript));
            AssetDatabase.SaveAssets();
            settingsObject.Update();

            Debug.Log($"Updated {entry.Name} to version {newVersion}.");
        }

        private static bool TryValidateEntry(ModVersionEntry entry, out string pluginPath, out string error)
        {
            pluginPath = null;
            error = null;

            if (entry.PluginScript == null)
            {
                error = "Assign the plugin script before changing its version.";
                return false;
            }

            Type pluginType = entry.PluginScript.GetClass();
            if (pluginType == null || !InheritsFromBaseUnityPlugin(pluginType))
            {
                error = "The assigned script must contain a class derived from BaseUnityPlugin.";
                return false;
            }

            if (entry.Manifest == null || entry.Manifest.Identity == null)
            {
                error = "Assign a valid ThunderKit Manifest with an identity before changing its version.";
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(entry.PluginScript);
            pluginPath = Path.GetFullPath(assetPath);
            string source = File.ReadAllText(pluginPath);
            MatchCollection matches = Regex.Matches(source, VersionConstantPattern, RegexOptions.CultureInvariant);
            if (matches.Count != 1)
            {
                error = matches.Count == 0
                    ? "The plugin script does not contain a VersionString constant."
                    : "The plugin script contains more than one VersionString constant.";
                return false;
            }

            return true;
        }

        private static bool InheritsFromBaseUnityPlugin(Type pluginType)
        {
            Type currentType = pluginType.BaseType;
            while (currentType != null)
            {
                if (currentType.FullName == "BepInEx.BaseUnityPlugin")
                {
                    return true;
                }

                currentType = currentType.BaseType;
            }

            return false;
        }

        private static void IncrementVersion(ModVersion version, VersionComponent component)
        {
            switch (component)
            {
                case VersionComponent.Major:
                    version.IncrementMajor();
                    break;
                case VersionComponent.Minor:
                    version.IncrementMinor();
                    break;
                case VersionComponent.Patch:
                    version.IncrementPatch();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(component), component, null);
            }
        }

        private static string GetVersionString(SerializedProperty versionProperty)
        {
            int major = versionProperty.FindPropertyRelative("major").intValue;
            int minor = versionProperty.FindPropertyRelative("minor").intValue;
            int patch = versionProperty.FindPropertyRelative("patch").intValue;
            return $"{major}.{minor}.{patch}";
        }
    }
}
