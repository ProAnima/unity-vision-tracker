using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UniversalTracker.Editor
{
    internal static class VisionExperimentalSceneSampleUtility
    {
        private const string PackageJsonPath =
            "Packages/com.proanima.universal-vision-tracker/package.json";
        private const string PackageName =
            "com.proanima.universal-vision-tracker";
        private const string ExperimentalSceneSampleName =
            "Experimental Scene";

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
            error = null;

            Type sampleType = FindType("UnityEditor.PackageManager.UI.Sample");
            if (sampleType == null)
            {
                error = "Unity Package Manager sample API was not found.";
                return false;
            }

            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssetPath(PackageJsonPath);
            string packageVersion = packageInfo != null ? packageInfo.version : null;
            if (string.IsNullOrWhiteSpace(packageVersion))
            {
                error = "Package version could not be resolved.";
                return false;
            }

            MethodInfo findByPackage = sampleType.GetMethod(
                "FindByPackage",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(string) },
                null);
            if (findByPackage == null)
            {
                error = "Sample.FindByPackage API was not found.";
                return false;
            }

            IEnumerable samples;
            try
            {
                samples = findByPackage.Invoke(null, new object[] { PackageName, packageVersion }) as IEnumerable;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            object sample = FindSampleByDisplayName(sampleType, samples, ExperimentalSceneSampleName);
            if (sample == null)
            {
                error = "Experimental Scene sample was not found in the package manifest.";
                return false;
            }

            if (!TryImportSample(sampleType, sample, out error))
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
            bool packageManagerOpened = TryOpenPackageManager();
            string message = packageManagerOpened
                ? "In Package Manager, select ProAnima Universal Vision Tracker, open Samples, and import Experimental Scene."
                : "Open Window > Package Management > Package Manager, select ProAnima Universal Vision Tracker, open Samples, and import Experimental Scene.";

            if (!string.IsNullOrWhiteSpace(importError))
                message += $"\n\nAutomatic import was not available: {importError}";

            EditorUtility.DisplayDialog(
                "Experimental Scene",
                message,
                "OK");
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static object FindSampleByDisplayName(Type sampleType, IEnumerable samples, string displayName)
        {
            if (samples == null)
                return null;

            PropertyInfo displayNameProperty = sampleType.GetProperty("displayName");
            foreach (object sample in samples)
            {
                string sampleName = displayNameProperty?.GetValue(sample) as string;
                if (string.Equals(sampleName, displayName, StringComparison.OrdinalIgnoreCase))
                    return sample;
            }

            return null;
        }

        private static bool TryImportSample(Type sampleType, object sample, out string error)
        {
            error = null;
            Type optionsType = sampleType.GetNestedType("ImportOptions", BindingFlags.Public);
            MethodInfo import = optionsType == null
                ? sampleType.GetMethod("Import", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)
                : sampleType.GetMethod("Import", BindingFlags.Public | BindingFlags.Instance, null, new[] { optionsType }, null);
            if (import == null)
            {
                error = "Sample.Import API was not found.";
                return false;
            }

            try
            {
                object[] args = optionsType == null ? null : new[] { ResolveImportOptions(optionsType) };
                object result = import.Invoke(sample, args);
                return result is not bool imported || imported;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static object ResolveImportOptions(Type optionsType)
        {
            foreach (string name in Enum.GetNames(optionsType))
            {
                if (name.IndexOf("Override", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Enum.Parse(optionsType, name);
            }

            return Enum.ToObject(optionsType, 0);
        }

        private static bool TryOpenPackageManager()
        {
            Type packageManagerWindow = Type.GetType(
                "UnityEditor.PackageManager.UI.Window,UnityEditor.PackageManagerUI.Editor");
            if (packageManagerWindow == null)
                return false;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            MethodInfo openPackage = packageManagerWindow.GetMethod(
                "Open",
                flags,
                null,
                new[] { typeof(string) },
                null);
            if (TryInvoke(openPackage, PackageName))
                return true;

            MethodInfo open = packageManagerWindow.GetMethod(
                "Open",
                flags,
                null,
                Type.EmptyTypes,
                null);
            return TryInvoke(open);
        }

        private static bool TryInvoke(MethodInfo method, params object[] args)
        {
            if (method == null)
                return false;

            try
            {
                method.Invoke(null, args);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ProAnima Vision] Package Manager could not be opened: {exception.Message}");
                return false;
            }
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
