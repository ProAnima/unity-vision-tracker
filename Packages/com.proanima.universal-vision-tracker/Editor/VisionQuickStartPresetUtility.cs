using System.IO;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Editor
{
    internal static class VisionQuickStartPresetUtility
    {
        private const string ProfileFolder = "Assets/ProAnima Vision/Profiles";
        private const string ExperimentalSceneObjectName = "ProAnima Vision Experimental Demo";

        public static void Apply(VisionQuickStartPreset preset)
        {
            EnsureFolder(ProfileFolder);

            if (preset == VisionQuickStartPreset.WebCamPreview)
            {
                VisionControlCenterWindow.EnsureExperimentalSceneImportedAndOpen();
                return;
            }

            if (!VisionControlCenterWindow.EnsureExperimentalSceneImportedAndOpen())
                return;

            VisionModelProfile model = CreateModelProfile(ToTemplate(preset));
            VisionPipelineProfile pipeline = VisionModelProfileTemplateFactory.CreatePipelineProfile(new[] { model });
            pipeline.name = $"{model.displayName} WebCam Pipeline";
            pipeline.targetFps = 30;
            pipeline.enableTracking = true;
            pipeline = CreateOrUpdateAsset(pipeline, $"{ProfileFolder}/{Sanitize(pipeline.name)}.asset");

            string sceneObjectName = ResolveSceneObjectName();
            VisionSceneSetupResult result = VisionSceneSetupUtility.CreateOrUpdate(new VisionSceneSetupOptions(
                sceneObjectName,
                pipeline,
                null,
                VisionSceneSetupSource.WebCam,
                addDashboard: true,
                enableTracking: true,
                autoStart: model.modelAsset != null,
                targetFps: pipeline.targetFps));
            ConfigureExperimentalBootstrap(result.root, pipeline, model.modelAsset != null);

            Selection.activeObject = pipeline;
            EditorGUIUtility.PingObject(pipeline);

            EditorUtility.DisplayDialog(
                "Preset Applied",
                model.modelAsset == null
                    ? "The demo scene and profiles are ready. Assign a ModelAsset in the model profile, then run Profile Validator before pressing Play."
                    : "The demo scene and profiles are ready. Press Play to start.",
                "OK");
        }

        private static VisionModelProfile CreateModelProfile(VisionModelProfileTemplate template)
        {
            VisionModelProfileTemplateSettings defaults = VisionModelProfileTemplateSettings.Defaults(template);
            ModelAsset modelAsset = ResolveModelAsset(template);
            TextAsset labels = ResolveLabels(template);
            var settings = new VisionModelProfileTemplateSettings(
                defaults.profileId,
                defaults.displayName,
                modelAsset,
                labels,
                defaults.backend,
                defaults.inputSize,
                defaults.confidenceThreshold,
                defaults.nmsThreshold,
                "Repository model asset - verify upstream license before release",
                modelAsset != null ? AssetDatabase.GetAssetPath(modelAsset) : defaults.modelSourceUrl);

            VisionModelProfile profile = VisionModelProfileTemplateFactory.Create(template, settings);
            profile.name = profile.displayName;
            return CreateOrUpdateAsset(profile, $"{ProfileFolder}/{VisionModelProfileTemplateFactory.DefaultAssetName(template)}");
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

        private static T CreateOrUpdateAsset<T>(T asset, string path)
            where T : Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                existing.name = asset.name;
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(asset);
                AssetDatabase.SaveAssets();
                return existing;
            }

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static string ResolveSceneObjectName()
        {
            return GameObject.Find(ExperimentalSceneObjectName) != null
                ? ExperimentalSceneObjectName
                : VisionSceneSetupUtility.DefaultObjectName;
        }

        private static void ConfigureExperimentalBootstrap(GameObject root, VisionPipelineProfile pipeline, bool autoStart)
        {
            Component bootstrap = FindExperimentalBootstrap(root);
            if (bootstrap == null)
                return;

            var serialized = new SerializedObject(bootstrap);
            SetBool(serialized, "runWebCamPreview", false);
            SetBool(serialized, "configureRealPipeline", true);
            SetObject(serialized, "pipelineProfile", pipeline);
            SetObject(serialized, "modelProfile", null);
            SetBool(serialized, "autoStartRealPipeline", autoStart);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        private static Component FindExperimentalBootstrap(GameObject root)
        {
            if (root == null)
                return null;

            foreach (Component component in root.GetComponents<Component>())
            {
                if (component != null && component.GetType().Name == "ProAnimaVisionExperimentalSceneBootstrap")
                    return component;
            }

            return null;
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.boolValue = value;
        }

        private static void SetObject(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
        }

        private static ModelAsset ResolveModelAsset(VisionModelProfileTemplate template)
        {
            foreach (string path in CandidateModelPaths(template))
            {
                ModelAsset asset = AssetDatabase.LoadAssetAtPath<ModelAsset>(path);
                if (asset != null)
                    return asset;
            }

            return null;
        }

        private static string[] CandidateModelPaths(VisionModelProfileTemplate template)
        {
            return template switch
            {
                VisionModelProfileTemplate.YoloPose2D => new[]
                {
                    "Assets/Models/yolo26n-pose.onnx",
                    "Assets/Models/yolo11n-pose.onnx",
                    "Assets/Models/yolo26s-pose.onnx"
                },
                VisionModelProfileTemplate.YoloSegmentation => new[]
                {
                    "Assets/Models/yolo26n-seg.onnx"
                },
                _ => new[]
                {
                    "Assets/Models/yolo26n.onnx",
                    "Assets/Models/yolo11n.onnx",
                    "Assets/Models/yolo26s.onnx",
                    "Assets/Models/yolo11s.onnx"
                }
            };
        }

        private static TextAsset ResolveLabels(VisionModelProfileTemplate template)
        {
            string path = template == VisionModelProfileTemplate.YoloPose2D
                ? "Packages/com.proanima.universal-vision-tracker/Samples~/YOLO Model Profiles/person.labels.txt"
                : "Packages/com.proanima.universal-vision-tracker/Samples~/YOLO Model Profiles/coco-80.labels.txt";
            return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
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
