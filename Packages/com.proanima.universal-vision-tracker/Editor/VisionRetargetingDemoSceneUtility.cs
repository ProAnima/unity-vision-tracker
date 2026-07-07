using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UniversalTracker.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalTracker.Editor
{
    internal static class VisionRetargetingDemoSceneUtility
    {
        private const string SampleName = "YOLO Humanoid Retargeting";
        private const string DemoScriptTypeName =
            "UniversalTracker.Samples.ProAnimaVisionYoloHumanoidRetargetingDemo";
        private const string SceneName = "ProAnimaVisionYoloHumanoidRetargetingDemo";
        private const string PendingOpenKey = "ProAnimaVision.PendingRetargetingDemoSceneOpen";

        public static bool EnsureImportedAndOpen()
        {
            if (!EnsureSampleImported())
                return false;

            string scenePath = FindScenePath();
            if (!string.IsNullOrWhiteSpace(scenePath))
                return Open(scenePath);

            if (TryCreateConfiguredScene(out scenePath))
                return Open(scenePath);

            SessionState.SetBool(PendingOpenKey, true);
            EditorUtility.DisplayDialog(
                "Retargeting Demo",
                "The retargeting sample was imported. Unity is compiling the sample scripts and will open the configured demo scene automatically.",
                "OK");
            AssetDatabase.Refresh();
            return true;
        }

        [DidReloadScripts]
        private static void CompletePendingSceneOpen()
        {
            if (!SessionState.GetBool(PendingOpenKey, false))
                return;

            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(PendingOpenKey, false))
                    return;

                if (!TryCreateConfiguredScene(out string scenePath))
                    return;

                SessionState.SetBool(PendingOpenKey, false);
                Open(scenePath);
            };
        }

        private static bool EnsureSampleImported()
        {
            string sampleRoot = FindImportedSampleRoot();
            if (sampleRoot != null && IsImportedSampleCurrent(sampleRoot))
                return true;

            if (VisionPackageSampleImportUtility.TryImport(SampleName, out string importError))
            {
                AssetDatabase.Refresh();
                return true;
            }

            VisionPackageSampleImportUtility.ShowImportHelp("Retargeting Demo", SampleName, importError);
            return false;
        }

        private static bool IsImportedSampleCurrent(string sampleRoot)
        {
            string sourceControllerPath = Path.Combine(sampleRoot, "ProAnimaVisionRetargetingSourceController.cs");
            string viewPath = Path.Combine(sampleRoot, "ProAnimaVisionRetargetingDemoView.cs");
            if (!File.Exists(sourceControllerPath) || !File.Exists(viewPath))
                return false;

            string view = File.ReadAllText(viewPath);
            return view.Contains("UIDocument") && !view.Contains("RawImage");
        }

        private static bool TryCreateConfiguredScene(out string scenePath)
        {
            scenePath = FindScenePath();
            if (!string.IsNullOrWhiteSpace(scenePath))
                return true;

            Type demoType = VisionPackageSampleImportUtility.FindType(DemoScriptTypeName);
            if (demoType == null)
                return false;

            string sampleRoot = FindImportedSampleRoot();
            if (string.IsNullOrWhiteSpace(sampleRoot))
                return false;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = SceneName;

            CreateCamera();
            CreateLight();
            CreateDemoRoot(demoType);

            scenePath = Path.Combine(sampleRoot, $"{SceneName}.unity").Replace('\\', '/');
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                scenePath = null;
                return false;
            }

            AssetDatabase.ImportAsset(scenePath);
            return true;
        }

        private static bool Open(string scenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            EditorSceneManager.OpenScene(scenePath);
            GameObject demo = GameObject.Find("Yolo Humanoid Retargeting Demo");
            ConfigureDemoObject(demo);
            Selection.activeGameObject = demo;
            return true;
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0.75f, -4.2f);
            cameraObject.transform.rotation = Quaternion.Euler(4f, 0f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.043f, 0.052f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 50f;
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Key Light");
            lightObject.transform.rotation = Quaternion.Euler(45f, -25f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
        }

        private static void CreateDemoRoot(Type demoType)
        {
            var demoObject = new GameObject("Yolo Humanoid Retargeting Demo");
            demoObject.transform.position = new Vector3(0f, 0.15f, 0f);
            Component demo = demoObject.AddComponent(demoType);
            ConfigureDemoObject(demoObject);
            if (demo == null)
                Debug.LogWarning("[ProAnima Vision] Retargeting demo component could not be configured.");
        }

        private static void ConfigureDemoObject(GameObject demoObject)
        {
            if (demoObject == null)
                return;

            Component demo = demoObject.GetComponent(DemoScriptTypeName);
            if (demo == null)
                return;

            VisionModelProfile profile = ResolvePoseProfile();
            if (profile == null)
                return;

            var serialized = new SerializedObject(demo);
            SerializedProperty profileProperty = serialized.FindProperty("poseModelProfile");
            if (profileProperty != null && profileProperty.objectReferenceValue == null)
                profileProperty.objectReferenceValue = profile;

            SerializedProperty runPipeline = serialized.FindProperty("runYoloPosePipeline");
            if (runPipeline != null)
                runPipeline.boolValue = true;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(demo);
        }

        private static VisionModelProfile ResolvePoseProfile()
        {
            const string preferredPath = "Assets/ProAnima Vision/Profiles/YoloPose2DProfile.asset";
            VisionModelProfile profile = AssetDatabase.LoadAssetAtPath<VisionModelProfile>(preferredPath);
            if (profile != null)
                return profile;

            string[] guids = AssetDatabase.FindAssets("t:VisionModelProfile", new[] { "Assets" });
            if (guids == null)
                return null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                profile = AssetDatabase.LoadAssetAtPath<VisionModelProfile>(path);
                if (profile != null && profile.Supports(VisionModelCapability.Pose2D))
                    return profile;
            }

            return null;
        }

        private static string FindScenePath()
        {
            string[] guids = AssetDatabase.FindAssets($"{SceneName} t:Scene", new[] { "Assets" });
            if (guids != null && guids.Length > 0)
                return AssetDatabase.GUIDToAssetPath(guids[0]);

            return null;
        }

        private static string FindImportedSampleRoot()
        {
            string[] guids = AssetDatabase.FindAssets("ProAnimaVisionYoloHumanoidRetargetingDemo t:Script", new[] { "Assets" });
            if (guids == null || guids.Length == 0)
                return null;

            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return string.IsNullOrWhiteSpace(scriptPath) ? null : Path.GetDirectoryName(scriptPath)?.Replace('\\', '/');
        }
    }
}
