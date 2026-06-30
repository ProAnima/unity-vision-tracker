using System;
using Unity.InferenceEngine;
using UnityEngine;

namespace UniversalTracker.Core
{
    [CreateAssetMenu(menuName = "ProAnima Vision/Model Profile", fileName = "VisionModelProfile")]
    public sealed class VisionModelProfile : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable machine-readable id for this model profile.")]
        public string profileId;
        [Tooltip("Human-readable model name shown in tools and dashboards.")]
        public string displayName;
        [Tooltip("Model family used to select adapters and parsers.")]
        public VisionModelFamily family = VisionModelFamily.Unknown;
        [Tooltip("Primary task this model is expected to perform.")]
        public VisionTaskType primaryTask = VisionTaskType.Detection;
        [Tooltip("Capabilities exposed by this model to runtime adapters and UI.")]
        public VisionModelCapability capabilities = VisionModelCapability.Detection;

        [Header("Runtime")]
        [Tooltip("Runtime adapter family used to execute this model.")]
        public VisionRuntimeKind runtimeKind = VisionRuntimeKind.UnityInferenceEngine;
        [Tooltip("Source format of the assigned model asset.")]
        public VisionModelSourceFormat sourceFormat = VisionModelSourceFormat.UnityModelAsset;
        [Tooltip("Unity Inference Engine model asset to load at runtime.")]
        public ModelAsset modelAsset;
        [Tooltip("Preferred Unity Inference Engine backend. YOLO samples should use GPU backends by default.")]
        public BackendType backend = BackendType.GPUPixel;

        [Header("Schema")]
        [Tooltip("Expected input tensor schema and preprocessing hints.")]
        public VisionInputSchema input = VisionInputSchema.ImageSquare(640);
        [Tooltip("Expected output tensor schema used by validators and parsers.")]
        public VisionOutputSchema output = VisionOutputSchema.Empty;
        [Tooltip("Optional coordinate transform applied after parsing normalized output.")]
        public VisionOutputCoordinateTransform outputTransform = VisionOutputCoordinateTransform.Identity;
        [Tooltip("Parser id used to convert raw model tensors into VisionFrameResult data.")]
        public string parserId;
        [Tooltip("Optional newline-separated label file used to resolve class names.")]
        public TextAsset labels;

        [Header("Thresholds")]
        [Tooltip("Minimum score required for a parsed candidate to be kept.")]
        [Range(0.01f, 0.99f)] public float confidenceThreshold = 0.5f;
        [Tooltip("Non-maximum suppression threshold used to merge overlapping detections.")]
        [Range(0.01f, 0.99f)] public float nmsThreshold = 0.45f;

        [Header("Governance")]
        [Tooltip("License name or SPDX expression for the model weights.")]
        public string modelLicense;
        [Tooltip("Original model source or documentation URL.")]
        public string modelSourceUrl;

        public bool Supports(VisionModelCapability required) => (capabilities & required) == required;
    }

    [Serializable]
    public struct VisionOutputCoordinateTransform
    {
        [Tooltip("Flip normalized X coordinates after parsing.")]
        public bool flipX;
        [Tooltip("Flip normalized Y coordinates after parsing.")]
        public bool flipY;
        [Tooltip("Scale normalized X coordinates around the image center.")]
        public float scaleX;
        [Tooltip("Scale normalized Y coordinates around the image center.")]
        public float scaleY;
        [Tooltip("Offset normalized X coordinates after scaling and flipping.")]
        public float offsetX;
        [Tooltip("Offset normalized Y coordinates after scaling and flipping.")]
        public float offsetY;
        [Tooltip("Scale rectangle width and height together with position.")]
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
        [Tooltip("Input tensor name expected by the model.")]
        public string inputName;
        [Tooltip("Model input width in pixels.")]
        public int width;
        [Tooltip("Model input height in pixels.")]
        public int height;
        [Tooltip("Number of input image channels.")]
        public int channels;
        [Tooltip("Preserve camera aspect ratio during preprocessing.")]
        public bool preserveAspectRatio;
        [Tooltip("Normalize image values before inference.")]
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
        [Tooltip("Output tensors expected from the model.")]
        public VisionTensorSchema[] tensors;

        public int TensorCount => tensors?.Length ?? 0;
        public bool IsValid => TensorCount > 0;

        public static VisionOutputSchema Empty => new VisionOutputSchema { tensors = Array.Empty<VisionTensorSchema>() };
    }

    [Serializable]
    public struct VisionTensorSchema
    {
        [Tooltip("Output tensor name.")]
        public string name;
        [Tooltip("Expected tensor shape. Use -1 for dynamic dimensions when needed.")]
        public int[] shape;
        [Tooltip("Semantic role of this tensor, for example detection, pose, or mask.")]
        public string semantic;

        public bool IsValid => !string.IsNullOrWhiteSpace(name) && shape != null && shape.Length > 0;
    }
}
