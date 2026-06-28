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
            CreateModelProfile(VisionModelProfileTemplate.YoloDetection);
        }

        [MenuItem("Assets/Create/ProAnima Vision/YOLO Pose 2D Model Profile", priority = 11)]
        public static void CreateYoloPoseProfile()
        {
            CreateModelProfile(VisionModelProfileTemplate.YoloPose2D);
        }

        [MenuItem("Assets/Create/ProAnima Vision/YOLO Segmentation Model Profile", priority = 12)]
        public static void CreateYoloSegmentationProfile()
        {
            CreateModelProfile(VisionModelProfileTemplate.YoloSegmentation);
        }

        [MenuItem("Assets/Create/ProAnima Vision/Pipeline Profile", priority = 19)]
        public static void CreatePipelineProfile()
        {
            List<VisionModelProfile> models = GetSelectedModelProfiles();
            VisionPipelineProfile profile = VisionModelProfileTemplateFactory.CreatePipelineProfile(models.ToArray());
            CreateAsset(profile, "VisionPipelineProfile.asset");
        }

        [MenuItem("Assets/Create/ProAnima Vision/Pipeline Profile From Selected Models", true)]
        private static bool CanCreatePipelineFromSelection()
        {
            return GetSelectedModelProfiles().Count > 0;
        }

        [MenuItem("Assets/Create/ProAnima Vision/Pipeline Profile From Selected Models", priority = 20)]
        public static void CreatePipelineFromSelection()
        {
            CreatePipelineProfile();
        }

        public static void CreateModelProfile(VisionModelProfileTemplate template)
        {
            VisionModelProfileTemplateSettings settings = VisionModelProfileTemplateSettings.Defaults(template);
            VisionModelProfile profile = VisionModelProfileTemplateFactory.Create(template, settings);
            CreateAsset(profile, VisionModelProfileTemplateFactory.DefaultAssetName(template));
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

        public static void CreateAsset(ScriptableObject asset, string defaultName)
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
