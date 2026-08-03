using System;
using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.Utilities.Editor;
#endif

namespace DaftAppleGames.Editor
{
    internal static class ReferenceAssetImporterWindowGui
    {
        private const float LabelWidth = 215.0f;
        private const float BrowseButtonWidth = 72.0f;

        public static void Draw(ReferenceAssetImporterWindow window)
        {
            EditorGUIUtility.labelWidth = LabelWidth;
            DrawIntroduction();

            using (new EditorGUI.DisabledScope(window.IsImporting))
            {
                string exportAssetsPath = window.ExportAssetsPath;
                DrawPathField("Export Assets Root", ref exportAssetsPath, true);
                window.ExportAssetsPath = exportAssetsPath;
                DrawSourceAssetField(window);

                string destinationPath = window.DestinationPath;
                DrawPathField("Dependencies Destination", ref destinationPath, false);
                window.DestinationPath = destinationPath;

                window.OverrideSelectedObjectDestination = EditorGUILayout.Toggle(
                    "Override Object Destination", window.OverrideSelectedObjectDestination);
                if (window.OverrideSelectedObjectDestination)
                {
                    string selectedDestinationPath = window.SelectedObjectDestinationPath;
                    DrawPathField("Selected Object Destination", ref selectedDestinationPath, false);
                    window.SelectedObjectDestinationPath = selectedDestinationPath;
                }

                DrawOptionsHeading();
                window.ForceAssetRipperReindex = EditorGUILayout.Toggle(
                    "Force AssetRipper Re-index", window.ForceAssetRipperReindex);
                window.FixShaderENotation = EditorGUILayout.Toggle(
                    "Fix shader E notation", window.FixShaderENotation);
                window.RepairMissingTmpAtlases = EditorGUILayout.Toggle(
                    "Repair missing TMP atlases", window.RepairMissingTmpAtlases);
                window.ReportOnly = EditorGUILayout.Toggle("Report only", window.ReportOnly);
                window.OverwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", window.OverwriteExisting);
            }

            DrawImportControls(window);
            DrawReport(window);
        }

        private static void DrawIntroduction()
        {
#if ODIN_INSPECTOR
            SirenixEditorGUI.Title("AssetRipper Reference Asset Importer",
                "Import an asset and its recursive dependency closure", TextAlignment.Left, true);
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
        }

        private static void DrawOptionsHeading()
        {
#if ODIN_INSPECTOR
            SirenixEditorGUI.Title("Import Options", null, TextAlignment.Left, true);
#else
            EditorGUILayout.Space();
#endif
        }

        private static void DrawImportControls(ReferenceAssetImporterWindow window)
        {
            EditorGUILayout.Space();
            if (window.IsImporting)
            {
                Rect progressRect = EditorGUILayout.GetControlRect(false, 22.0f);
                EditorGUI.ProgressBar(progressRect, window.ProgressValue, window.ImportStatus);
                if (GUILayout.Button("Cancel Import", GUILayout.Height(28.0f))) window.CancelImport();
                return;
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(window.SourceAssetPath)))
            {
                if (GUILayout.Button("Import Asset and Dependencies", GUILayout.Height(32.0f))) window.BeginImport();
            }
        }

        private static void DrawReport(ReferenceAssetImporterWindow window)
        {
            EditorGUILayout.Space();
#if ODIN_INSPECTOR
            SirenixEditorGUI.Title("Import Report", null, TextAlignment.Left, true);
#else
            EditorGUILayout.LabelField("Import Report", EditorStyles.boldLabel);
#endif
            window.ReportScrollPosition = EditorGUILayout.BeginScrollView(window.ReportScrollPosition);
            EditorGUILayout.TextArea(window.Report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private static void DrawSourceAssetField(ReferenceAssetImporterWindow window)
        {
            EditorGUILayout.BeginHorizontal();
            window.SourceAssetPath = EditorGUILayout.TextField("Source Asset", window.SourceAssetPath);
            if (GUILayout.Button("Browse", GUILayout.Width(BrowseButtonWidth)))
            {
                string absoluteRoot = ReferenceAssetImporterFileSystem.GetAbsolutePath(window.ExportAssetsPath);
                string selectedPath = EditorUtility.OpenFilePanel(
                    "Select AssetRipper asset", absoluteRoot, string.Empty);
                if (!string.IsNullOrEmpty(selectedPath)) window.SourceAssetPath = selectedPath;
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawPathField(string label, ref string path, bool allowAbsolutePath)
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("Browse", GUILayout.Width(BrowseButtonWidth)))
            {
                string initialPath = ReferenceAssetImporterFileSystem.GetAbsolutePath(path);
                string selectedPath = EditorUtility.OpenFolderPanel(label, initialPath, string.Empty);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    string projectRelativePath = ReferenceAssetImporterFileSystem.GetProjectRelativePath(selectedPath);
                    path = allowAbsolutePath || string.IsNullOrEmpty(projectRelativePath)
                        ? selectedPath
                        : projectRelativePath;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
