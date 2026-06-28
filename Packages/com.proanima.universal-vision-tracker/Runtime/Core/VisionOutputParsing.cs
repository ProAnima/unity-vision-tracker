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
        public VisionFrameDiagnostics diagnostics;

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
                stats = stats,
                diagnostics = diagnostics
            };
        }
    }

    public readonly struct VisionOutputParserContext
    {
        public readonly Vector2Int sourceSize;
        public readonly float confidenceThreshold;
        public readonly float nmsThreshold;
        public readonly Vector2Int modelInputSize;
        public readonly string[] labels;

        public VisionOutputParserContext(
            Vector2Int sourceSize,
            float confidenceThreshold,
            float nmsThreshold,
            string[] labels = null,
            Vector2Int modelInputSize = default)
        {
            this.sourceSize = sourceSize;
            this.confidenceThreshold = confidenceThreshold;
            this.nmsThreshold = nmsThreshold;
            this.modelInputSize = modelInputSize;
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
            if (rawOutput == null || !rawOutput.HasTensors)
                return CreateEmpty(rawOutput, "No valid model tensor was produced.");

            if (!TryResolveFirstRows(rawOutput, out VisionRawTensor tensor, out YoloTensorRows rows))
                return CreateEmpty(rawOutput, $"No supported YOLO tensor layout found. Outputs: {DescribeTensors(rawOutput)}.");

            List<VisionDetection> candidates = CollectCandidates(rows, context, out float maxConfidence);
            VisionDetection[] accepted = ApplyNms(candidates, context.nmsThreshold);

            return new VisionParsedOutput
            {
                detections = accepted,
                stats = VisionPerformanceStats.FromStages(0f, rawOutput.inferenceMs, 0f, 0f),
                diagnostics = CreateDiagnostics(tensor, rows, candidates.Count, accepted.Length, maxConfidence, context.confidenceThreshold)
            };
        }

        private VisionParsedOutput CreateEmpty(VisionRawModelOutput rawOutput, string message)
        {
            return new VisionParsedOutput
            {
                stats = VisionPerformanceStats.FromStages(0f, rawOutput?.inferenceMs ?? 0f, 0f, 0f),
                diagnostics = new VisionFrameDiagnostics
                {
                    parserId = ParserId,
                    modelOutput = DescribeFirstTensor(rawOutput),
                    message = message
                }
            };
        }

        private static List<VisionDetection> CollectCandidates(
            YoloTensorRows rows,
            VisionOutputParserContext context,
            out float maxConfidence)
        {
            var detections = new List<VisionDetection>();
            maxConfidence = 0f;

            for (int row = 0; row < rows.rowCount; row++)
            {
                if (TryReadDetection(rows, row, context, out VisionDetection detection, out float confidence))
                {
                    detections.Add(detection);
                }

                if (confidence > maxConfidence)
                    maxConfidence = confidence;
            }

            return detections;
        }

        private static bool TryReadDetection(
            YoloTensorRows rows,
            int row,
            VisionOutputParserContext context,
            out VisionDetection detection,
            out float confidence)
        {
            detection = default;
            confidence = 0f;
            if (rows.stride < 6)
                return false;

            int classOffset = rows.hasObjectness ? 5 : 4;
            float objectness = rows.hasObjectness ? Mathf.Clamp01(rows.Get(row, 4)) : 1f;
            int classId = FindBestClass(rows, row, classOffset, rows.stride - classOffset, out float classScore);
            confidence = Mathf.Clamp01(objectness * classScore);
            if (confidence < context.confidenceThreshold)
                return false;

            Rect normalized = CenterToRect(
                NormalizeCoordinate(rows.Get(row, 0), context.modelInputSize.x),
                NormalizeCoordinate(rows.Get(row, 1), context.modelInputSize.y),
                NormalizeCoordinate(rows.Get(row, 2), context.modelInputSize.x),
                NormalizeCoordinate(rows.Get(row, 3), context.modelInputSize.y));

            detection = new VisionDetection
            {
                trackId = -1,
                classId = classId,
                label = ResolveLabel(classId, context.labels),
                confidence = confidence,
                normalizedRect = normalized,
                sourceRect = NormalizedToSourceRect(normalized, context.sourceSize),
                sourceCenter = NormalizedToSourcePoint(normalized.center, context.sourceSize),
                trackState = VisionTrackState.None
            };
            return true;
        }

        private VisionFrameDiagnostics CreateDiagnostics(
            VisionRawTensor tensor,
            YoloTensorRows rows,
            int candidateCount,
            int acceptedCount,
            float maxConfidence,
            float threshold)
        {
            return new VisionFrameDiagnostics
            {
                parserId = ParserId,
                modelOutput = $"{tensor.name} [{FormatShape(tensor.shape)}], {rows.LayoutLabel}",
                candidateCount = candidateCount,
                acceptedCount = acceptedCount,
                maxConfidence = maxConfidence,
                message = candidateCount == 0
                    ? $"Max confidence {maxConfidence:0.00} is below threshold {threshold:0.00}."
                    : null
            };
        }

        private static bool TryResolveRows(VisionRawTensor tensor, out YoloTensorRows rows)
        {
            rows = default;

            if (tensor.shape.Length == 2)
            {
                rows = new YoloTensorRows(tensor, tensor.shape[0], tensor.shape[1], false, HasObjectness(tensor.shape[1]));
            }
            else if (tensor.shape.Length == 3 && tensor.shape[0] == 1)
            {
                int first = tensor.shape[1];
                int second = tensor.shape[2];
                bool channelFirst = IsKnownYoloStride(first) || (first >= 6 && first < second);
                rows = channelFirst
                    ? new YoloTensorRows(tensor, second, first, true, HasObjectness(first))
                    : new YoloTensorRows(tensor, first, second, false, HasObjectness(second));
            }

            return rows.IsValid;
        }

        private static bool TryResolveFirstRows(VisionRawModelOutput rawOutput, out VisionRawTensor tensor, out YoloTensorRows rows)
        {
            tensor = default;
            rows = default;
            if (rawOutput?.tensors == null)
                return false;

            for (int i = 0; i < rawOutput.tensors.Length; i++)
            {
                VisionRawTensor candidate = rawOutput.tensors[i];
                if (!candidate.IsValid || !TryResolveRows(candidate, out YoloTensorRows candidateRows))
                    continue;

                tensor = candidate;
                rows = candidateRows;
                return true;
            }

            return false;
        }

        private static bool HasObjectness(int stride)
        {
            return stride == 85 || stride == 117 || stride == 57 || stride <= 10;
        }

        private static bool IsKnownYoloStride(int stride)
        {
            return stride == 84 || stride == 85 || stride == 56 || stride == 57 || stride == 116 || stride == 117;
        }

        private static int FindBestClass(YoloTensorRows rows, int row, int offset, int count, out float score)
        {
            int bestIndex = 0;
            score = 0f;

            for (int i = 0; i < count; i++)
            {
                float value = Mathf.Clamp01(rows.Get(row, offset + i));
                if (value <= score)
                    continue;

                score = value;
                bestIndex = i;
            }

            return bestIndex;
        }

        private static float NormalizeCoordinate(float value, int modelAxis)
        {
            if (value >= 0f && value <= 1f)
                return value;

            int divisor = modelAxis > 0 ? modelAxis : 640;
            return Mathf.Clamp01(value / divisor);
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

        private static string DescribeFirstTensor(VisionRawModelOutput rawOutput)
        {
            if (rawOutput == null || rawOutput.TensorCount == 0 || rawOutput.tensors == null)
                return "-";

            VisionRawTensor tensor = rawOutput.tensors[0];
            return $"{tensor.name} [{FormatShape(tensor.shape)}]";
        }

        private static string DescribeTensors(VisionRawModelOutput rawOutput)
        {
            if (rawOutput == null || rawOutput.TensorCount == 0 || rawOutput.tensors == null)
                return "-";

            var parts = new string[rawOutput.tensors.Length];
            for (int i = 0; i < rawOutput.tensors.Length; i++)
            {
                VisionRawTensor tensor = rawOutput.tensors[i];
                parts[i] = $"{tensor.name} [{FormatShape(tensor.shape)}]";
            }

            return string.Join(", ", parts);
        }

        private static string FormatShape(int[] shape)
        {
            if (shape == null || shape.Length == 0)
                return "-";

            return string.Join("x", shape);
        }

        private readonly struct YoloTensorRows
        {
            private readonly VisionRawTensor tensor;
            private readonly bool channelFirst;

            public readonly int rowCount;
            public readonly int stride;
            public readonly bool hasObjectness;
            public string LayoutLabel => channelFirst
                ? $"channels-first rows={rowCount} stride={stride}"
                : $"rows={rowCount} stride={stride}";

            public YoloTensorRows(VisionRawTensor tensor, int rowCount, int stride, bool channelFirst, bool hasObjectness)
            {
                this.tensor = tensor;
                this.rowCount = rowCount;
                this.stride = stride;
                this.channelFirst = channelFirst;
                this.hasObjectness = hasObjectness;
            }

            public bool IsValid => rowCount > 0 && stride >= 6 && rowCount * stride <= tensor.ElementCount;

            public float Get(int row, int column)
            {
                if (tensor.data == null || row < 0 || row >= rowCount || column < 0 || column >= stride)
                    return 0f;

                int index = channelFirst
                    ? column * rowCount + row
                    : row * stride + column;
                return index >= 0 && index < tensor.data.Length ? tensor.data[index] : 0f;
            }
        }
    }
}
