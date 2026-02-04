// ============================================
// TMP FONT UPDATER - Editor utility to batch update fonts
// Updates all TMP Text components to use the default font
// ============================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace SpaceCombat.Editor
{
    /// <summary>
    /// Editor utility to update all TextMeshPro components to use the default font.
    /// Access via: Tools → SpaceCombat → Update All TMP Fonts
    /// </summary>
    public static class TMPFontUpdater
    {
        [MenuItem("Tools/SpaceCombat/Update All TMP Fonts")]
        public static void UpdateAllFonts()
        {
            // Get the default TMP font from settings
            var defaultFont = TMP_Settings.defaultFontAsset;

            if (defaultFont == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "No default font set in TMP Settings!\n\n" +
                    "Go to: Edit → Project Settings → TextMeshPro\n" +
                    "Set the Default Font Asset first.", "OK");
                return;
            }

            int sceneCount = 0;
            int prefabCount = 0;
            int totalUpdated = 0;

            // Ask user what to update
            int choice = EditorUtility.DisplayDialogComplex(
                "Update TMP Fonts",
                $"This will update all TMP Text components to use:\n\n" +
                $"Font: {defaultFont.name}\n\n" +
                $"What do you want to update?",
                "Scenes & Prefabs",
                "Cancel",
                "Only Open Scene");

            if (choice == 1) return; // Cancel

            bool updatePrefabs = choice == 0;
            bool updateScenes = choice == 0 || choice == 2;

            // Update current scene
            if (updateScenes)
            {
                int sceneUpdates = UpdateCurrentScene(defaultFont);
                totalUpdated += sceneUpdates;
                if (sceneUpdates > 0) sceneCount++;
            }

            // Update all prefabs
            if (updatePrefabs)
            {
                var prefabUpdates = UpdateAllPrefabs(defaultFont);
                totalUpdated += prefabUpdates.updated;
                prefabCount = prefabUpdates.prefabCount;
            }

            // Show results
            string message = $"Font update complete!\n\n" +
                           $"Font: {defaultFont.name}\n" +
                           $"Total texts updated: {totalUpdated}\n";

            if (updateScenes)
                message += $"Scenes processed: {sceneCount}\n";
            if (updatePrefabs)
                message += $"Prefabs processed: {prefabCount}\n";

            message += "\nDon't forget to save your scenes!";

            EditorUtility.DisplayDialog("TMP Font Updater", message, "OK");

            Debug.Log($"[TMPFontUpdater] Updated {totalUpdated} TMP Text components to {defaultFont.name}");
        }

        private static int UpdateCurrentScene(TMP_FontAsset font)
        {
            int updated = 0;

            // Find all TMP_Text in scene (including inactive)
            var allTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var text in allTexts)
            {
                // Skip if already using the correct font
                if (text.font == font) continue;

                // Record for undo
                Undo.RecordObject(text, "Update TMP Font");

                text.font = font;
                EditorUtility.SetDirty(text);
                updated++;

                Debug.Log($"[TMPFontUpdater] Updated: {GetGameObjectPath(text.gameObject)}");
            }

            if (updated > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            return updated;
        }

        private static (int updated, int prefabCount) UpdateAllPrefabs(TMP_FontAsset font)
        {
            int updated = 0;
            int prefabCount = 0;

            // Find all prefab assets
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Load prefab
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                // Get all TMP_Text components in prefab
                var texts = prefab.GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length == 0) continue;

                bool prefabModified = false;

                foreach (var text in texts)
                {
                    if (text.font == font) continue;

                    // Need to modify prefab
                    if (!prefabModified)
                    {
                        prefabCount++;
                    }

                    // Use SerializedObject for prefab modification
                    var serializedObject = new SerializedObject(text);
                    var fontProperty = serializedObject.FindProperty("m_fontAsset");

                    if (fontProperty != null)
                    {
                        fontProperty.objectReferenceValue = font;
                        serializedObject.ApplyModifiedProperties();
                        updated++;
                        prefabModified = true;

                        Debug.Log($"[TMPFontUpdater] Updated prefab: {path} - {text.gameObject.name}");
                    }
                }

                if (prefabModified)
                {
                    EditorUtility.SetDirty(prefab);
                }
            }

            if (updated > 0)
            {
                AssetDatabase.SaveAssets();
            }

            return (updated, prefabCount);
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        // ============================================
        // ADDITIONAL UTILITIES
        // ============================================

        [MenuItem("Tools/SpaceCombat/Count TMP Texts in Scene")]
        public static void CountTMPTexts()
        {
            var allTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // Group by font
            var fontGroups = allTexts.GroupBy(t => t.font?.name ?? "None");

            string message = $"TMP Text components in current scene: {allTexts.Length}\n\n";

            foreach (var group in fontGroups.OrderByDescending(g => g.Count()))
            {
                message += $"  {group.Key}: {group.Count()}\n";
            }

            EditorUtility.DisplayDialog("TMP Text Count", message, "OK");
            Debug.Log($"[TMPFontUpdater] {message}");
        }
    }
}
#endif
