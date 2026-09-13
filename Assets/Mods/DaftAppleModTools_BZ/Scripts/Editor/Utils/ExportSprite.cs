using System.IO;
using UnityEditor;
using UnityEngine;

public static class ExportSprite
{
    [MenuItem("Assets/Export Selected Sprite to PNG", true)]
    private static bool ValidateExport()
    {
        return Selection.activeObject is Sprite;
    }

    [MenuItem("Assets/Export Selected Sprite to PNG")]
    private static void Export()
    {
        Sprite sprite = Selection.activeObject as Sprite;

        if (sprite == null || sprite.texture == null)
        {
            Debug.LogError("The selected Sprite has no texture.");
            return;
        }

        Rect rect = sprite.rect;

        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);

        RenderTexture temporary = RenderTexture.GetTemporary(
            width,
            height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default
        );

        RenderTexture previous = RenderTexture.active;

        try
        {
            // Copy only the Sprite's area from its underlying texture.
            Vector2 scale = new Vector2(
                rect.width / sprite.texture.width,
                rect.height / sprite.texture.height
            );

            Vector2 offset = new Vector2(
                rect.x / sprite.texture.width,
                rect.y / sprite.texture.height
            );

            Graphics.Blit(sprite.texture, temporary, scale, offset);
            RenderTexture.active = temporary;

            Texture2D exportedTexture =
                new Texture2D(width, height, TextureFormat.RGBA32, false);

            exportedTexture.ReadPixels(
                new Rect(0, 0, width, height),
                0,
                0
            );

            exportedTexture.Apply();

            string path = EditorUtility.SaveFilePanel(
                "Export Sprite",
                "",
                sprite.name + ".png",
                "png"
            );

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllBytes(path, exportedTexture.EncodeToPNG());
                Debug.Log("Exported Sprite to: " + path);
            }

            Object.DestroyImmediate(exportedTexture);
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
        }
    }
}