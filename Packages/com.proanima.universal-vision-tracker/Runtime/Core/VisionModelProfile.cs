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
        public VisionOutputCoordinateTransform outputTransform = VisionOutputCoordinateTransform.Identity;
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
    public struct VisionOutputCoordinateTransform
    {
        public bool flipX;
        public bool flipY;
        public float scaleX;
        public float scaleY;
        public float offsetX;
        public float offsetY;
        public bool scaleSize;

        public static VisionOutputCoordinateTransform Identity =>
            new VisionOutputCoordinateTransform
            {
                scaleX = 1f,
                scaleY = 1f
            };

        public Rect Apply(Rect normalizedRect)
        {
            float x = normalizedRect.x;
            float y = normalizedRect.y;
            float width = normalizedRect.width;
            float height = normalizedRect.height;

            if (flipX)
                x = 1f - (x + width);
            if (flipY)
                y = 1f - (y + height);

            ApplyAxisScale(ref x, ref width, scaleX);
            ApplyAxisScale(ref y, ref height, scaleY);
            x += offsetX;
            y += offsetY;

            return Rect.MinMaxRect(
                Mathf.Clamp01(x),
                Mathf.Clamp01(y),
                Mathf.Clamp01(x + width),
                Mathf.Clamp01(y + height));
        }

        public Vector2 Apply(Vector2 normalizedPoint)
        {
            float x = flipX ? 1f - normalizedPoint.x : normalizedPoint.x;
            float y = flipY ? 1f - normalizedPoint.y : normalizedPoint.y;
            ApplyPointAxisScale(ref x, scaleX);
            ApplyPointAxisScale(ref y, scaleY);
            return new Vector2(Mathf.Clamp01(x + offsetX), Mathf.Clamp01(y + offsetY));
        }

        private void ApplyAxisScale(ref float position, ref float size, float scale)
        {
            if (scale <= 0f || Mathf.Approximately(scale, 1f))
                return;

            float center = position + size * 0.5f;
            float scaledCenter = 0.5f + (center - 0.5f) * scale;
            if (scaleSize)
                size *= scale;

            position = scaledCenter - size * 0.5f;
        }

        private static void ApplyPointAxisScale(ref float position, float scale)
        {
            if (scale <= 0f || Mathf.Approximately(scale, 1f))
                return;

            position = 0.5f + (position - 0.5f) * scale;
        }
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
