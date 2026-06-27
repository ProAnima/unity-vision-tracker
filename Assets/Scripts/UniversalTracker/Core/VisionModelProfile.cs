using System;
using Unity.InferenceEngine;
using UnityEngine;

namespace UniversalTracker.Core
{
    [CreateAssetMenu(menuName = "ProAnima Vision/Model Profile", fileName = "VisionModelProfile")]
    public sealed class VisionModelProfile : ScriptableObject
    {
        [Header("Identity")]
        public string profileId;
        public string displayName;
        public VisionModelFamily family = VisionModelFamily.Unknown;
        public VisionTaskType primaryTask = VisionTaskType.Detection;
        public VisionModelCapability capabilities = VisionModelCapability.Detection;

        [Header("Runtime")]
        public VisionRuntimeKind runtimeKind = VisionRuntimeKind.UnityInferenceEngine;
        public VisionModelSourceFormat sourceFormat = VisionModelSourceFormat.UnityModelAsset;
        public ModelAsset modelAsset;
        public BackendType backend = BackendType.GPUPixel;

        [Header("Schema")]
        public VisionInputSchema input = VisionInputSchema.ImageSquare(640);
        public VisionOutputSchema output = VisionOutputSchema.Empty;
        public string parserId;
        public TextAsset labels;

        [Header("Thresholds")]
        [Range(0.01f, 0.99f)] public float confidenceThreshold = 0.5f;
        [Range(0.01f, 0.99f)] public float nmsThreshold = 0.45f;

        [Header("Governance")]
        public string modelLicense;
        public string modelSourceUrl;

        public bool Supports(VisionModelCapability required) => (capabilities & required) == required;
    }

    [Serializable]
    public struct VisionInputSchema
    {
        public string inputName;
        public int width;
        public int height;
        public int channels;
        public bool preserveAspectRatio;
        public bool normalize;

        public Vector2Int Size => new Vector2Int(width, height);
        public bool IsValid => width > 0 && height > 0 && channels > 0;

        public static VisionInputSchema ImageSquare(int size) =>
            new VisionInputSchema
            {
                inputName = "images",
                width = size,
                height = size,
                channels = 3,
                preserveAspectRatio = true,
                normalize = true
            };
    }

    [Serializable]
    public struct VisionOutputSchema
    {
        public VisionTensorSchema[] tensors;

        public int TensorCount => tensors?.Length ?? 0;
        public bool IsValid => TensorCount > 0;

        public static VisionOutputSchema Empty => new VisionOutputSchema { tensors = Array.Empty<VisionTensorSchema>() };
    }

    [Serializable]
    public struct VisionTensorSchema
    {
        public string name;
        public int[] shape;
        public string semantic;

        public bool IsValid => !string.IsNullOrWhiteSpace(name) && shape != null && shape.Length > 0;
    }
}
