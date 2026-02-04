#if UNITY_EDITOR
// ============================================
// AUDIO IMPORT SETTINGS - Batch configure audio for Android
// Fixes common Android audio playback issues
// ============================================

using UnityEditor;
using UnityEngine;
using System.IO;

namespace SpaceCombat.Editor
{
    /// <summary>
    /// Editor utility to configure audio import settings for Android.
    /// Fixes common issues like audio not playing on mobile.
    ///
    /// Access via: Tools → SpaceCombat → Configure Audio for Android
    /// </summary>
    public static class AudioImportSettings
    {
        [MenuItem("Tools/SpaceCombat/Configure Audio for Android")]
        public static void ConfigureAudioForAndroid()
        {
            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Sounds" });

            if (audioGuids.Length == 0)
            {
                Debug.LogWarning("[AudioImportSettings] No audio files found in Assets/Sounds/");
                return;
            }

            int updated = 0;

            foreach (string guid in audioGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;

                if (importer == null) continue;

                bool needsUpdate = false;

                // Get or create Android-specific settings
                AudioImporterSampleSettings androidSettings = importer.GetOverrideSampleSettings("Android");

                // Configure for Android compatibility
                // Vorbis compression works best on Android
                if (androidSettings.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    androidSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                    needsUpdate = true;
                }

                // Quality setting (0.5 is good balance between size and quality)
                if (androidSettings.quality < 0.4f || androidSettings.quality > 0.7f)
                {
                    androidSettings.quality = 0.5f;
                    needsUpdate = true;
                }

                // Load type based on file size
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    // Short clips (< 3 seconds): Decompress on Load for instant playback
                    // Longer clips: Compressed in Memory to save RAM
                    AudioClipLoadType targetLoadType = clip.length < 3f
                        ? AudioClipLoadType.DecompressOnLoad
                        : AudioClipLoadType.CompressedInMemory;

                    if (androidSettings.loadType != targetLoadType)
                    {
                        androidSettings.loadType = targetLoadType;
                        needsUpdate = true;
                    }
                }

                if (needsUpdate)
                {
                    // Apply Android override
                    importer.SetOverrideSampleSettings("Android", androidSettings);

                    // Also update default settings for consistency
                    AudioImporterSampleSettings defaultSettings = importer.defaultSampleSettings;
                    defaultSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                    defaultSettings.quality = 0.5f;
                    if (clip != null)
                    {
                        defaultSettings.loadType = clip.length < 3f
                            ? AudioClipLoadType.DecompressOnLoad
                            : AudioClipLoadType.CompressedInMemory;
                    }
                    importer.defaultSampleSettings = defaultSettings;

                    // Force mono for small SFX to reduce size
                    if (clip != null && clip.length < 2f)
                    {
                        importer.forceToMono = true;
                    }

                    importer.SaveAndReimport();
                    updated++;

                    Debug.Log($"[AudioImportSettings] Updated: {path}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Audio Import Settings",
                $"Android audio configuration complete!\n\n" +
                $"Files checked: {audioGuids.Length}\n" +
                $"Files updated: {updated}\n\n" +
                $"Settings applied:\n" +
                $"• Compression: Vorbis\n" +
                $"• Quality: 50%\n" +
                $"• Short clips: Decompress on Load\n" +
                $"• Long clips: Compressed in Memory\n" +
                $"• Short SFX: Force Mono",
                "OK"
            );

            Debug.Log($"[AudioImportSettings] Configured {updated}/{audioGuids.Length} audio files for Android");
        }

        [MenuItem("Tools/SpaceCombat/Check AudioListener")]
        public static void CheckAudioListener()
        {
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

            if (listeners.Length == 0)
            {
                bool addListener = EditorUtility.DisplayDialog(
                    "AudioListener Missing",
                    "No AudioListener found in the scene!\n\n" +
                    "Audio will not play without an AudioListener.\n" +
                    "Usually it should be on the Main Camera.\n\n" +
                    "Would you like to add one to the Main Camera?",
                    "Yes, Add It",
                    "Cancel"
                );

                if (addListener)
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        Undo.AddComponent<AudioListener>(mainCam.gameObject);
                        Debug.Log("[AudioImportSettings] AudioListener added to Main Camera");
                        EditorUtility.DisplayDialog("Success", "AudioListener added to Main Camera!", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "No Main Camera found! Tag a camera as 'MainCamera' first.", "OK");
                    }
                }
            }
            else if (listeners.Length > 1)
            {
                string listenerList = "";
                foreach (var listener in listeners)
                {
                    listenerList += $"• {listener.gameObject.name}\n";
                }

                EditorUtility.DisplayDialog(
                    "Multiple AudioListeners",
                    $"Found {listeners.Length} AudioListeners!\n\n" +
                    $"Unity only uses ONE AudioListener.\n" +
                    $"Remove extras from:\n{listenerList}",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "AudioListener OK",
                    $"AudioListener found on: {listeners[0].gameObject.name}\n\n" +
                    "Audio setup looks correct!",
                    "OK"
                );
            }
        }

        [MenuItem("Tools/SpaceCombat/List Audio Files Info")]
        public static void ListAudioFilesInfo()
        {
            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Sounds" });

            Debug.Log("=== AUDIO FILES INFO ===");

            foreach (string guid in audioGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;

                if (clip == null || importer == null) continue;

                var androidSettings = importer.GetOverrideSampleSettings("Android");
                bool hasAndroidOverride = importer.ContainsSampleSettingsOverride("Android");

                Debug.Log($"File: {Path.GetFileName(path)}\n" +
                         $"  Length: {clip.length:F2}s | Channels: {clip.channels}\n" +
                         $"  Android Override: {hasAndroidOverride}\n" +
                         $"  Compression: {androidSettings.compressionFormat}\n" +
                         $"  Load Type: {androidSettings.loadType}\n" +
                         $"  Force Mono: {importer.forceToMono}");
            }

            Debug.Log($"=== Total: {audioGuids.Length} audio files ===");
        }
    }
}
#endif
