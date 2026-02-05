using UnityEngine;
using UnityEditor;
using System.IO;

public class CircleSpriteGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Circle Sprite")]
    public static void GenerateCircle()
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        float radius = size / 2f - 1;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance <= radius)
                {
                    // Smooth edge (anti-aliasing)
                    float alpha = Mathf.Clamp01(radius - distance + 1);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        // Save to file
        string folderPath = "Assets/Sprites/UI";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string path = folderPath + "/Circle256.png";
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);

        DestroyImmediate(texture);

        AssetDatabase.Refresh();

        // Set import settings
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        Debug.Log($"Circle sprite created at: {path}");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Sprite>(path));
    }
}
