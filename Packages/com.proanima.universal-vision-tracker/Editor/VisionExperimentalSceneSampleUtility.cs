using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UniversalTracker.Editor
{
    internal static class VisionExperimentalSceneSampleUtility
    {
        private const string ExperimentalSceneSampleName = "Experimental Scene";

        public static void ImportAndOpen()
        {
            if (TryImport(out string importedScenePath, out string importError))
            {
                bool openScene = EditorUtility.DisplayDialog(
                    "Experimental Scene Imported",
                    "Experimental Scene was imported into Assets/Samples. Open it now?",
                    "Open Scene",
                    "Later");

                if (openScene)
                    Open(importedScenePath);
                return;
            }

            ShowImportHelp(importError);
        }

        public static bool EnsureImportedAndOpen()
        {
            string scenePath = FindScenePath();
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                if (!TryImport(out scenePath, out string importError))
                {
                    ShowImportHelp(importError);
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                ShowImportHelp("Experimental Scene was not found after import.");
                return false;
            }

            return Open(scenePath);
        }

        private static bool Open(string preferredPath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            string path = string.IsNullOrWhiteSpace(preferredPath) ? FindScenePath() : preferredPath;
            if (!string.IsNullOrWhiteSpace(path))
            {
                EditorSceneManager.OpenScene(path);
                return true;
            }

            EditorUtility.DisplayDialog(
                "Experimental Scene",
                "Import the Experimental Scene sample from Package Manager, then open it from Control Center.",
                "OK");
            return false;
        }

        private static bool TryImport(out string importedScenePath, out string error)
        {
            importedScenePath = null;
            if (!VisionPackageSampleImportUtility.TryImport(ExperimentalSceneSampleName, out error))
                return false;

            AssetDatabase.Refresh();
            importedScenePath = FindScenePath();
            if (!string.IsNullOrWhiteSpace(importedScenePath))
                return true;

            error = "Sample import completed, but the scene was not found under Assets/Samples.";
            return false;
        }

        private static void ShowImportHelp(string importError)
        {
            VisionPackageSampleImportUtility.ShowImportHelp(
                "Experimental Scene",
                ExperimentalSceneSampleName,
                importError);
        }

        private static string FindScenePath()
        {
            string[] guids = AssetDatabase.FindAssets("ProAnimaVisionExperimentalScene t:Scene", new[] { "Assets" });
            if (guids != null && guids.Length > 0)
                return AssetDatabase.GUIDToAssetPath(guids[0]);

            return null;
        }
    }
}
