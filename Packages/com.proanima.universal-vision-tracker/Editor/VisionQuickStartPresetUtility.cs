using System.IO;
using UnityEditor;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Editor
{
    internal enum VisionQuickStartPreset
    {
        WebCamPreview,
        YoloDetectionWebCam,
        YoloPoseWebCam,
        YoloSegmentationWebCam
    }

    internal static class VisionQuickStartPresetUtility
    {
        private const string ProfileFolder = "Assets/ProAnima Vision/Profiles";

        public static void Apply(VisionQuickStartPreset preset)
        {
            EnsureFolder(ProfileFolder);

            if (preset == VisionQuickStartPreset.WebCamPreview)
            {
                VisionControlCenterWindow.ImportAndOpenExperimentalScene();
                return;
            }

            VisionModelProfile model = CreateModelProfile(ToTemplate(preset));
            VisionPipelineProfile pipeline = VisionModelProfileTemplateFactory.CreatePipelineProfile(new[] { model });
            pipeline.name = $"{model.displayName} WebCam Pipeline";
            pipeline.targetFps = 30;
            pipeline.enableTracking = true;
            CreateAsset(pipeline, $"{ProfileFolder}/{Sanitize(pipeline.name)}.asset");

            VisionSceneSetupUtility.CreateOrUpdate(new VisionSceneSetupOptions(
                VisionSceneSetupUtility.DefaultObjectName,
                pipeline,
                null,
                VisionSceneSetupSource.WebCam,
                addDashboard: true,
                enableTracking: true,
                autoStart: model.modelAsset != null,
                targetFps: pipeline.targetFps));

            Selection.activeObject = pipeline;
            EditorGUIUtility.PingObject(pipeline);

            EditorUtility.DisplayDialog(
                "Preset Applied",
                model.modelAsset == null
                    ? "Profiles and WebCam scene wiring were created. Assign a ModelAsset in the model profile, then run Profile Validator before pressing Play."
                    : "Profiles and WebCam scene wiring were created. Press Play to start.",
                "OK");
        }

        private static VisionModelProfile CreateModelProfile(VisionModelProfileTemplate template)
        {
            VisionModelProfile profile = VisionModelProfileTemplateFactory.Create(
                template,
                VisionModelProfileTemplateSettings.Defaults(template));
            profile.name = profile.displayName;
            CreateAsset(profile, $"{ProfileFolder}/{VisionModelProfileTemplateFactory.DefaultAssetName(template)}");
            return profile;
        }

        private static VisionModelProfileTemplate ToTemplate(VisionQuickStartPreset preset)
        {
            return preset switch
            {
                VisionQuickStartPreset.YoloPoseWebCam => VisionModelProfileTemplate.YoloPose2D,
                VisionQuickStartPreset.YoloSegmentationWebCam => VisionModelProfileTemplate.YoloSegmentation,
                _ => VisionModelProfileTemplate.YoloDetection
            };
        }

        private static void CreateAsset(Object asset, string path)
        {
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(asset, uniquePath);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '-');
            return value;
        }
    }
}
