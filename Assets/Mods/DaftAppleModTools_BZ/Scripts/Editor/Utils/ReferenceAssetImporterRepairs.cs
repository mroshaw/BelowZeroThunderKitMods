using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static DaftAppleGames.Editor.ReferenceAssetImporterFileSystem;

namespace DaftAppleGames.Editor
{
    internal static class ReferenceAssetImporterRepairs
    {
        private static readonly Regex ShaderExponentRegex = new Regex(
            @"(?<![A-Za-z0-9_.])[+-]?(?:\d+(?:\.\d*)?|\.\d+)[eE][+-]?\d+(?![A-Za-z0-9_.])",
            RegexOptions.Compiled);

        public static void RepairMissingTmpFontAtlases(HashSet<string> fontAssetPaths,
            StringBuilder reportBuilder)
        {
            List<string> repairedAssetPaths = new List<string>();
            foreach (string fontAssetPath in fontAssetPaths)
            {
                Object fontAsset = AssetDatabase.LoadMainAssetAtPath(fontAssetPath);
                if (!fontAsset || !string.Equals(
                        fontAsset.GetType().FullName, "TMPro.TMP_FontAsset", StringComparison.Ordinal)) continue;

                SerializedObject serializedFontAsset = new SerializedObject(fontAsset);
                serializedFontAsset.Update();
                SerializedProperty populationMode = serializedFontAsset.FindProperty("m_AtlasPopulationMode");
                SerializedProperty atlasTextures = serializedFontAsset.FindProperty("m_AtlasTextures");
                if (populationMode is null || populationMode.intValue != 1 || atlasTextures is null) continue;
                if (atlasTextures.arraySize > 0 && atlasTextures.GetArrayElementAtIndex(0).objectReferenceValue) continue;

                SerializedProperty sourceFontFile = serializedFontAsset.FindProperty("m_SourceFontFile");
                if (sourceFontFile is null || !sourceFontFile.objectReferenceValue)
                {
                    reportBuilder.AppendLine($"UNRESOLVED TMP SOURCE FONT: {fontAssetPath}");
                    continue;
                }

                int atlasWidth = GetPositiveSerializedInt(serializedFontAsset, "m_AtlasWidth", 1024);
                int atlasHeight = GetPositiveSerializedInt(serializedFontAsset, "m_AtlasHeight", 1024);
                Texture2D atlasTexture = FindAtlasTextureSubAsset(fontAssetPath);
                if (!atlasTexture)
                {
                    atlasTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.Alpha8, false, true)
                    {
                        name = fontAsset.name + " Atlas",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.HideInHierarchy
                    };
                    atlasTexture.LoadRawTextureData(new byte[atlasWidth * atlasHeight]);
                    atlasTexture.Apply(false, false);
                    AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
                }

                atlasTextures.arraySize = 1;
                atlasTextures.GetArrayElementAtIndex(0).objectReferenceValue = atlasTexture;
                SerializedProperty atlasTextureIndex = serializedFontAsset.FindProperty("m_AtlasTextureIndex");
                if (atlasTextureIndex != null) atlasTextureIndex.intValue = 0;
                serializedFontAsset.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(fontAsset);

                SerializedProperty materialProperty = serializedFontAsset.FindProperty("material") ??
                                                      serializedFontAsset.FindProperty("m_Material");
                Material fontMaterial = materialProperty != null
                    ? materialProperty.objectReferenceValue as Material
                    : null;
                if (fontMaterial)
                {
                    fontMaterial.mainTexture = atlasTexture;
                    if (fontMaterial.HasProperty("_TextureWidth")) fontMaterial.SetFloat("_TextureWidth", atlasWidth);
                    if (fontMaterial.HasProperty("_TextureHeight")) fontMaterial.SetFloat("_TextureHeight", atlasHeight);
                    EditorUtility.SetDirty(fontMaterial);
                }

                repairedAssetPaths.Add(fontAssetPath);
                reportBuilder.AppendLine($"REPAIRED TMP ATLAS: {fontAssetPath} ({atlasWidth}x{atlasHeight})");
            }

            if (repairedAssetPaths.Count == 0) return;
            AssetDatabase.SaveAssets();
            for (int assetIndex = 0; assetIndex < repairedAssetPaths.Count; assetIndex++)
                AssetDatabase.ImportAsset(repairedAssetPaths[assetIndex], ImportAssetOptions.ForceUpdate);
        }

        public static bool IsDynamicTmpFontAsset(string path)
        {
            if (!string.Equals(Path.GetExtension(path), ".asset", StringComparison.OrdinalIgnoreCase)) return false;
            string contents = ReadAllText(path);
            return contents.IndexOf("m_AtlasPopulationMode: 1", StringComparison.Ordinal) >= 0 &&
                   contents.IndexOf("m_AtlasTextures:", StringComparison.Ordinal) >= 0 &&
                   contents.IndexOf("m_SourceFontFile:", StringComparison.Ordinal) >= 0;
        }

        public static int FixShaderFile(string path, bool writeChanges)
        {
            if (!FileExists(path)) return 0;
            string contents = ReadAllText(path);
            int replacementCount;
            string fixedContents = FixShaderExponentNotation(contents, out replacementCount);
            if (writeChanges && replacementCount > 0) WriteAllText(path, fixedContents);
            return replacementCount;
        }

        public static int CountShaderExponentReplacements(string contents)
        {
            int replacementCount;
            FixShaderExponentNotation(contents, out replacementCount);
            return replacementCount;
        }

        public static bool IsShaderAsset(string path)
        {
            return string.Equals(Path.GetExtension(path), ".shader", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetPositiveSerializedInt(SerializedObject serializedObject, string propertyName,
            int defaultValue)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null && property.intValue > 0 ? property.intValue : defaultValue;
        }

        private static Texture2D FindAtlasTextureSubAsset(string fontAssetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fontAssetPath);
            for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
            {
                Texture2D texture = assets[assetIndex] as Texture2D;
                if (texture) return texture;
            }
            return null;
        }

        public static string FixShaderExponentNotation(string contents, out int replacementCount)
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
    }
}
