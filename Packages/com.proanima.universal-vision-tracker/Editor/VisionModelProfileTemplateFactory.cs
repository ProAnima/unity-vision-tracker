using System;
using Unity.InferenceEngine;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Editor
{
    public enum VisionModelProfileTemplate
    {
        YoloDetection,
        YoloPose2D,
        YoloSegmentation
    }

    public readonly struct VisionModelProfileTemplateSettings
    {
        public readonly string profileId;
        public readonly string displayName;
        public readonly ModelAsset modelAsset;
        public readonly TextAsset labels;
        public readonly BackendType backend;
        public readonly int inputSize;
        public readonly float confidenceThreshold;
        public readonly float nmsThreshold;
        public readonly string modelLicense;
        public readonly string modelSourceUrl;

        public VisionModelProfileTemplateSettings(
            string profileId,
            string displayName,
            ModelAsset modelAsset,
            TextAsset labels,
            BackendType backend,
            int inputSize,
            float confidenceThreshold,
            float nmsThreshold,
            string modelLicense,
            string modelSourceUrl)
        {
            this.profileId = profileId;
            this.displayName = displayName;
            this.modelAsset = modelAsset;
            this.labels = labels;
            this.backend = backend;
            this.inputSize = inputSize;
            this.confidenceThreshold = confidenceThreshold;
            this.nmsThreshold = nmsThreshold;
            this.modelLicense = modelLicense;
            this.modelSourceUrl = modelSourceUrl;
        }

        public static VisionModelProfileTemplateSettings Defaults(VisionModelProfileTemplate template)
        {
            return new VisionModelProfileTemplateSettings(
                CreateStableIdPrefix(template) + "-" + Guid.NewGuid().ToString("N"),
                DefaultDisplayName(template),
                null,
                null,
                BackendType.CPU,
                640,
                0.25f,
                0.45f,
                "Add model license before production",
                "Add model source URL before production");
        }

        private static string CreateStableIdPrefix(VisionModelProfileTemplate template)
        {
            return template switch
            {
                VisionModelProfileTemplate.YoloPose2D => "yolo-pose2d",
                VisionModelProfileTemplate.YoloSegmentation => "yolo-segmentation",
                _ => "yolo-detection"
            };
        }

        private static string DefaultDisplayName(VisionModelProfileTemplate template)
        {
            return template switch
            {
                VisionModelProfileTemplate.YoloPose2D => "YOLO Pose 2D",
                VisionModelProfileTemplate.YoloSegmentation => "YOLO Segmentation",
                _ => "YOLO Detection"
            };
        }
    }

    public static class VisionModelProfileTemplateFactory
    {
        public static VisionModelProfile Create(VisionModelProfileTemplate template, VisionModelProfileTemplateSettings settings)
        {
            VisionModelProfile profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            ApplyCommon(profile, settings);

            switch (template)
            {
                case VisionModelProfileTemplate.YoloPose2D:
                    ApplyYoloPose(profile);
                    break;
                case VisionModelProfileTemplate.YoloSegmentation:
                    ApplyYoloSegmentation(profile);
                    break;
                default:
                    ApplyYoloDetection(profile);
                    break;
            }

            return profile;
        }

        public static VisionPipelineProfile CreatePipelineProfile(VisionModelProfile[] models)
        {
            VisionPipelineProfile profile = ScriptableObject.CreateInstance<VisionPipelineProfile>();
            profile.models = models ?? Array.Empty<VisionModelProfile>();
            profile.defaultModelIndex = 0;
            profile.targetFps = 30;
            profile.enableTracking = true;
            profile.enableDebugOverlay = true;
            profile.maxConsecutiveRecoverableErrors = 10;
            return profile;
        }

        public static string DefaultAssetName(VisionModelProfileTemplate template)
        {
            return template switch
            {
                VisionModelProfileTemplate.YoloPose2D => "YoloPose2DProfile.asset",
                VisionModelProfileTemplate.YoloSegmentation => "YoloSegmentationProfile.asset",
                _ => "YoloDetectionProfile.asset"
            };
        }

        private static void ApplyCommon(VisionModelProfile profile, VisionModelProfileTemplateSettings settings)
        {
            profile.profileId = settings.profileId;
            profile.displayName = settings.displayName;
            profile.runtimeKind = VisionRuntimeKind.UnityInferenceEngine;
            profile.sourceFormat = VisionModelSourceFormat.UnityModelAsset;
            profile.modelAsset = settings.modelAsset;
            profile.backend = settings.backend;
            profile.input = VisionInputSchema.ImageSquare(Mathf.Max(1, settings.inputSize));
            profile.labels = settings.labels;
            profile.confidenceThreshold = Mathf.Clamp(settings.confidenceThreshold, 0.01f, 0.99f);
            profile.nmsThreshold = Mathf.Clamp(settings.nmsThreshold, 0.01f, 0.99f);
            profile.modelLicense = settings.modelLicense;
            profile.modelSourceUrl = settings.modelSourceUrl;
        }

        private static void ApplyYoloDetection(VisionModelProfile profile)
        {
            profile.family = VisionModelFamily.YOLO;
            profile.primaryTask = VisionTaskType.Detection;
            profile.capabilities = VisionModelCapability.Detection;
            profile.output = CreateSingleTensorSchema("output0", "detection.rows", 1, 84, 8400);
            profile.outputTransform = CreateYolo11Transform();
            profile.parserId = "yolo.detection.rows";
        }

        private static void ApplyYoloPose(VisionModelProfile profile)
        {
            profile.family = VisionModelFamily.YOLO;
            profile.primaryTask = VisionTaskType.Pose;
            profile.capabilities = VisionModelCapability.Detection | VisionModelCapability.HumanDetection | VisionModelCapability.Pose2D;
            profile.output = CreateSingleTensorSchema("output0", "pose.rows", 1, 56, 8400);
            profile.outputTransform = CreateYolo11Transform();
            profile.parserId = "yolo.pose2d.rows";
        }

        private static void ApplyYoloSegmentation(VisionModelProfile profile)
        {
            profile.family = VisionModelFamily.YOLO;
            profile.primaryTask = VisionTaskType.Segmentation;
            profile.capabilities = VisionModelCapability.Detection | VisionModelCapability.Segmentation;
            profile.output = new VisionOutputSchema
            {
                tensors = new[]
                {
                    new VisionTensorSchema { name = "output0", shape = new[] { 1, 116, 8400 }, semantic = "segmentation.detection.rows" },
                    new VisionTensorSchema { name = "proto", shape = new[] { 1, 32, 160, 160 }, semantic = "segmentation.prototype" }
                }
            };
            profile.outputTransform = CreateYolo11Transform();
            profile.parserId = "yolo.segmentation.rows";
        }

        private static VisionOutputCoordinateTransform CreateYolo11Transform()
        {
            VisionOutputCoordinateTransform transform = VisionOutputCoordinateTransform.Identity;
            transform.flipY = true;
            return transform;
        }

        private static VisionOutputSchema CreateSingleTensorSchema(string name, string semantic, params int[] shape)
        {
            return new VisionOutputSchema
            {
                tensors = new[]
                {
                    new VisionTensorSchema
                    {
                        name = name,
                        shape = shape,
                        semantic = semantic
                    }
                }
            };
        }
    }
}
