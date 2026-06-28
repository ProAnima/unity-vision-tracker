using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Editor
{
    internal static class VisionProfileAssetCreator
    {
        [MenuItem("Assets/Create/ProAnima Vision/YOLO Detection Model Profile", priority = 10)]
        public static void CreateYoloDetectionProfile()
        {
            var profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            profile.profileId = $"yolo-detection-{Guid.NewGuid():N}";
            profile.displayName = "YOLO Detection";
            profile.family = VisionModelFamily.YOLO;
            profile.primaryTask = VisionTaskType.Detection;
            profile.capabilities = VisionModelCapability.Detection;
            profile.runtimeKind = VisionRuntimeKind.UnityInferenceEngine;
            profile.sourceFormat = VisionModelSourceFormat.UnityModelAsset;
            profile.input = VisionInputSchema.ImageSquare(640);
            profile.output = CreateYoloDetectionOutputSchema();
            profile.parserId = "yolo.detection.rows";
            profile.confidenceThreshold = 0.5f;
            profile.nmsThreshold = 0.45f;
            profile.modelLicense = "TODO: add model license";
            profile.modelSourceUrl = "TODO: add model source URL";

            CreateAsset(profile, "YoloDetectionProfile.asset");
        }

        [MenuItem("Assets/Create/ProAnima Vision/Pipeline Profile From Selected Models", true)]
        private static bool CanCreatePipelineFromSelection()
        {
            return GetSelectedModelProfiles().Count > 0;
        }

        [MenuItem("Assets/Create/ProAnima Vision/Pipeline Profile From Selected Models", priority = 11)]
        public static void CreatePipelineFromSelection()
        {
            List<VisionModelProfile> models = GetSelectedModelProfiles();
            var profile = ScriptableObject.CreateInstance<VisionPipelineProfile>();
            profile.models = models.ToArray();
            profile.defaultModelIndex = 0;
            profile.targetFps = 30;
            profile.enableTracking = true;
            profile.enableDebugOverlay = true;
            profile.maxConsecutiveRecoverableErrors = 10;

            CreateAsset(profile, "VisionPipelineProfile.asset");
        }

        private static VisionOutputSchema CreateYoloDetectionOutputSchema()
        {
            return new VisionOutputSchema
            {
                tensors = new[]
                {
                    new VisionTensorSchema
                    {
                        name = "output0",
                        shape = new[] { 1, 84, 8400 },
                        semantic = "detection.rows"
                    }
                }
            };
        }

        private static List<VisionModelProfile> GetSelectedModelProfiles()
        {
            var profiles = new List<VisionModelProfile>();
            foreach (UnityEngine.Object selected in Selection.objects)
            {
                if (selected is VisionModelProfile profile && !profiles.Contains(profile))
                    profiles.Add(profile);
            }

            return profiles;
        }

        private static void CreateAsset(ScriptableObject asset, string defaultName)
        {
            string folder = ResolveSelectedFolder();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{defaultName}");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static string ResolveSelectedFolder()
        {
            if (Selection.activeObject == null)
                return "Assets";

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrWhiteSpace(path))
                return "Assets";

            return AssetDatabase.IsValidFolder(path)
                ? path
                : System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        }
    }
}
