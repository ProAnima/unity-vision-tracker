using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalTracker.Core
{
    public interface IVisionOutputParser
    {
        string ParserId { get; }
        VisionModelCapability Capabilities { get; }
        bool CanParse(VisionModelProfile profile);
        VisionParsedOutput Parse(VisionRawModelOutput rawOutput, VisionOutputParserContext context);
    }

    [Serializable]
    public sealed class VisionRawModelOutput
    {
        public VisionRawTensor[] tensors = Array.Empty<VisionRawTensor>();
        public float inferenceMs;

        public int TensorCount => tensors?.Length ?? 0;
        public bool HasTensors => TensorCount > 0;

        public static VisionRawModelOutput Single(string name, float[] data, params int[] shape)
        {
            return new VisionRawModelOutput
            {
                tensors = new[]
                {
                    new VisionRawTensor(name, data, shape)
                }
            };
        }
    }

    [Serializable]
    public readonly struct VisionRawTensor
    {
        public readonly string name;
        public readonly float[] data;
        public readonly int[] shape;

        public VisionRawTensor(string name, float[] data, int[] shape)
        {
            this.name = name;
            this.data = data;
            this.shape = shape;
        }

        public int ElementCount => data?.Length ?? 0;
        public bool IsValid => data != null && data.Length > 0 && shape != null && shape.Length > 0;
    }

    [Serializable]
    public sealed class VisionParsedOutput
    {
        public VisionDetection[] detections = Array.Empty<VisionDetection>();
        public VisionPose[] poses = Array.Empty<VisionPose>();
        public VisionMask[] masks = Array.Empty<VisionMask>();
        public VisionClassification[] classifications = Array.Empty<VisionClassification>();
        public VisionPerformanceStats stats;

        public int TotalResultCount =>
            (detections?.Length ?? 0) +
            (poses?.Length ?? 0) +
            (masks?.Length ?? 0) +
            (classifications?.Length ?? 0);

        public static VisionParsedOutput Empty => new VisionParsedOutput();

        public VisionFrameResult ToFrameResult(int frameIndex, double timestamp, Vector2Int sourceSize)
        {
            return new VisionFrameResult
            {
                frameIndex = frameIndex,
                timestamp = timestamp,
                sourceSize = sourceSize,
                detections = detections ?? Array.Empty<VisionDetection>(),
                poses = poses ?? Array.Empty<VisionPose>(),
                masks = masks ?? Array.Empty<VisionMask>(),
                classifications = classifications ?? Array.Empty<VisionClassification>(),
                stats = stats
            };
        }
    }

    public readonly struct VisionOutputParserContext
    {
        public readonly Vector2Int sourceSize;
        public readonly float confidenceThreshold;
        public readonly float nmsThreshold;
        public readonly string[] labels;

        public VisionOutputParserContext(
            Vector2Int sourceSize,
            float confidenceThreshold,
            float nmsThreshold,
            string[] labels = null)
        {
            this.sourceSize = sourceSize;
            this.confidenceThreshold = confidenceThreshold;
            this.nmsThreshold = nmsThreshold;
            this.labels = labels ?? Array.Empty<string>();
        }
    }

    public sealed class YoloDetectionOutputParser : IVisionOutputParser
    {
        public string ParserId => "yolo.detection.rows";
        public VisionModelCapability Capabilities => VisionModelCapability.Detection;

        public bool CanParse(VisionModelProfile profile)
        {
            return profile != null &&
                   profile.family == VisionModelFamily.YOLO &&
                   profile.Supports(VisionModelCapability.Detection);
        }

        public VisionParsedOutput Parse(VisionRawModelOutput rawOutput, VisionOutputParserContext context)
        {
            if (rawOutput == null || !rawOutput.HasTensors || !rawOutput.tensors[0].IsValid)
                return VisionParsedOutput.Empty;

            VisionRawTensor tensor = rawOutput.tensors[0];
            if (!TryResolveRows(tensor, out int rowCount, out int stride))
                return VisionParsedOutput.Empty;

            var detections = new List<VisionDetection>();
            for (int row = 0; row < rowCount; row++)
            {
                int offset = row * stride;
                if (stride < 6 || offset + stride > tensor.data.Length)
                    continue;

                float objectness = Mathf.Clamp01(tensor.data[offset + 4]);
                int classId = FindBestClass(tensor.data, offset + 5, stride - 5, out float classScore);
                float confidence = objectness * classScore;
                if (confidence < context.confidenceThreshold)
                    continue;

                Rect normalized = CenterToRect(
                    tensor.data[offset],
                    tensor.data[offset + 1],
                    tensor.data[offset + 2],
                    tensor.data[offset + 3]);

                detections.Add(new VisionDetection
                {
                    trackId = -1,
                    classId = classId,
                    label = ResolveLabel(classId, context.labels),
                    confidence = confidence,
                    normalizedRect = normalized,
                    sourceRect = NormalizedToSourceRect(normalized, context.sourceSize),
                    sourceCenter = NormalizedToSourcePoint(normalized.center, context.sourceSize),
                    trackState = VisionTrackState.None
                });
            }

            return new VisionParsedOutput
            {
                detections = ApplyNms(detections, context.nmsThreshold),
                stats = VisionPerformanceStats.FromStages(0f, rawOutput.inferenceMs, 0f, 0f)
            };
        }

        private static bool TryResolveRows(VisionRawTensor tensor, out int rowCount, out int stride)
        {
            rowCount = 0;
            stride = 0;

            if (tensor.shape.Length == 2)
            {
                rowCount = tensor.shape[0];
                stride = tensor.shape[1];
            }
            else if (tensor.shape.Length == 3 && tensor.shape[0] == 1)
            {
                rowCount = tensor.shape[1];
                stride = tensor.shape[2];
            }

            return rowCount > 0 && stride >= 6 && rowCount * stride <= tensor.ElementCount;
        }

        private static int FindBestClass(float[] data, int offset, int count, out float score)
        {
            int bestIndex = 0;
            score = 0f;

            for (int i = 0; i < count; i++)
            {
                float value = Mathf.Clamp01(data[offset + i]);
                if (value <= score)
                    continue;

                score = value;
                bestIndex = i;
            }

            return bestIndex;
        }

        private static Rect CenterToRect(float centerX, float centerY, float width, float height)
        {
            float xMin = Mathf.Clamp01(centerX - width * 0.5f);
            float yMin = Mathf.Clamp01(centerY - height * 0.5f);
            float xMax = Mathf.Clamp01(centerX + width * 0.5f);
            float yMax = Mathf.Clamp01(centerY + height * 0.5f);
            return Rect.MinMaxRect(Mathf.Min(xMin, xMax), Mathf.Min(yMin, yMax), Mathf.Max(xMin, xMax), Mathf.Max(yMin, yMax));
        }

        private static VisionDetection[] ApplyNms(List<VisionDetection> detections, float threshold)
        {
            if (detections == null || detections.Count == 0)
                return Array.Empty<VisionDetection>();

            detections.Sort((a, b) => b.confidence.CompareTo(a.confidence));
            var kept = new List<VisionDetection>();
            var suppressed = new bool[detections.Count];

            for (int i = 0; i < detections.Count; i++)
            {
                if (suppressed[i])
                    continue;

                VisionDetection current = detections[i];
                kept.Add(current);

                for (int j = i + 1; j < detections.Count; j++)
                {
                    if (suppressed[j] || detections[j].classId != current.classId)
                        continue;

                    if (CalculateIoU(current.normalizedRect, detections[j].normalizedRect) > threshold)
                        suppressed[j] = true;
                }
            }

            return kept.ToArray();
        }

        private static float CalculateIoU(Rect a, Rect b)
        {
            float x1 = Mathf.Max(a.xMin, b.xMin);
            float y1 = Mathf.Max(a.yMin, b.yMin);
            float x2 = Mathf.Min(a.xMax, b.xMax);
            float y2 = Mathf.Min(a.yMax, b.yMax);
            if (x2 <= x1 || y2 <= y1)
                return 0f;

            float intersection = (x2 - x1) * (y2 - y1);
            float union = a.width * a.height + b.width * b.height - intersection;
            return union > 0f ? intersection / union : 0f;
        }

        private static Rect NormalizedToSourceRect(Rect normalized, Vector2Int sourceSize)
        {
            if (sourceSize.x <= 0 || sourceSize.y <= 0)
                return default;

            return new Rect(
                normalized.x * sourceSize.x,
                normalized.y * sourceSize.y,
                normalized.width * sourceSize.x,
                normalized.height * sourceSize.y);
        }

        private static Vector2 NormalizedToSourcePoint(Vector2 normalized, Vector2Int sourceSize)
        {
            if (sourceSize.x <= 0 || sourceSize.y <= 0)
                return Vector2.zero;

            return new Vector2(normalized.x * sourceSize.x, normalized.y * sourceSize.y);
        }

        private static string ResolveLabel(int classId, string[] labels)
        {
            if (labels != null && classId >= 0 && classId < labels.Length && !string.IsNullOrWhiteSpace(labels[classId]))
                return labels[classId];

            return $"class_{classId}";
        }
    }
}
