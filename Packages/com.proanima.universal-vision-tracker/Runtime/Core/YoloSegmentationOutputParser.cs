using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalTracker.Core
{
    public sealed class YoloSegmentationOutputParser : IVisionOutputParser
    {
        public string ParserId => "yolo.segmentation.rows";
        public VisionModelCapability Capabilities => VisionModelCapability.Detection | VisionModelCapability.Segmentation;

        public bool CanParse(VisionModelProfile profile)
        {
            return profile != null &&
                   profile.family == VisionModelFamily.YOLO &&
                   profile.Supports(VisionModelCapability.Segmentation);
        }

        public VisionParsedOutput Parse(VisionRawModelOutput rawOutput, VisionOutputParserContext context)
        {
            if (rawOutput == null || rawOutput.TensorCount < 2 || !rawOutput.tensors[0].IsValid || !rawOutput.tensors[1].IsValid)
                return VisionParsedOutput.Empty;

            VisionRawTensor rowsTensor = rawOutput.tensors[0];
            VisionRawTensor prototypeTensor = rawOutput.tensors[1];
            if (!YoloOutputParserUtility.TryResolveRows(rowsTensor, out YoloTensorRows rows))
                return VisionParsedOutput.Empty;

            bool isEndToEnd = YoloOutputParserUtility.IsEndToEndRows(rows, 38);
            int maskCoefficientCount = ResolveMaskCoefficientCount(prototypeTensor);
            bool hasObjectness = YoloOutputParserUtility.HasObjectness(rows.stride);
            int classOffset = hasObjectness ? 5 : 4;
            int classScoreCount = rows.stride - classOffset - maskCoefficientCount;
            if (!isEndToEnd && (maskCoefficientCount <= 0 || classScoreCount <= 0))
                return VisionParsedOutput.Empty;

            var candidates = new List<SegmentationCandidate>();
            float maxConfidence = 0f;
            for (int row = 0; row < rows.rowCount; row++)
            {
                int classId = ResolveClass(rows, row, classOffset, classScoreCount, isEndToEnd, out float confidence);
                maxConfidence = Mathf.Max(maxConfidence, confidence);
                if (confidence < context.confidenceThreshold)
                    continue;

                Rect normalized = isEndToEnd
                    ? YoloOutputParserUtility.CornersToNormalizedRect(rows, row, context)
                    : YoloOutputParserUtility.CenterToNormalizedRect(rows, row, context);

                string label = YoloOutputParserUtility.ResolveLabel(classId, context.labels);
                VisionDetection detection = YoloOutputParserUtility.CreateDetection(classId, label, confidence, normalized, context.sourceSize);
                candidates.Add(new SegmentationCandidate(
                    detection,
                    new VisionMask
                    {
                        trackId = -1,
                        classId = classId,
                        label = label,
                        confidence = confidence,
                        normalizedRect = normalized,
                        sourceRect = detection.sourceRect,
                        texture = null
                    }));
            }

            ApplyNms(candidates, context.nmsThreshold, out VisionDetection[] detections, out VisionMask[] masks);
            return new VisionParsedOutput
            {
                detections = detections,
                masks = masks,
                stats = VisionPerformanceStats.FromStages(0f, rawOutput.inferenceMs, 0f, 0f),
                diagnostics = new VisionFrameDiagnostics
                {
                    parserId = ParserId,
                    modelOutput = $"{rowsTensor.name} [{FormatShape(rowsTensor.shape)}], {rows.LayoutLabel}",
                    candidateCount = candidates.Count,
                    acceptedCount = detections.Length,
                    maxConfidence = maxConfidence
                }
            };
        }

        private static int ResolveClass(
            YoloTensorRows rows,
            int row,
            int classOffset,
            int classScoreCount,
            bool isEndToEnd,
            out float confidence)
        {
            if (isEndToEnd)
            {
                confidence = Mathf.Clamp01(rows.Get(row, 4));
                return Mathf.Max(0, Mathf.RoundToInt(rows.Get(row, 5)));
            }

            float objectness = YoloOutputParserUtility.HasObjectness(rows.stride) ? Mathf.Clamp01(rows.Get(row, 4)) : 1f;
            int classId = FindBestClass(rows, row, classOffset, classScoreCount, out float classScore);
            confidence = objectness * classScore;
            return classId;
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

        private static int ResolveMaskCoefficientCount(VisionRawTensor prototypeTensor)
        {
            if (prototypeTensor.shape.Length == 4 && prototypeTensor.shape[0] == 1)
                return prototypeTensor.shape[1];

            if (prototypeTensor.shape.Length == 3)
                return prototypeTensor.shape[0];

            return 0;
        }

        private static void ApplyNms(List<SegmentationCandidate> candidates, float threshold, out VisionDetection[] detections, out VisionMask[] masks)
        {
            if (candidates.Count == 0)
            {
                detections = Array.Empty<VisionDetection>();
                masks = Array.Empty<VisionMask>();
                return;
            }

            candidates.Sort((a, b) => b.detection.confidence.CompareTo(a.detection.confidence));
            var keptDetections = new List<VisionDetection>();
            var keptMasks = new List<VisionMask>();
            var suppressed = new bool[candidates.Count];

            for (int i = 0; i < candidates.Count; i++)
            {
                if (suppressed[i])
                    continue;

                SegmentationCandidate current = candidates[i];
                keptDetections.Add(current.detection);
                keptMasks.Add(current.mask);

                for (int j = i + 1; j < candidates.Count; j++)
                {
                    if (suppressed[j] || candidates[j].detection.classId != current.detection.classId)
                        continue;

                    if (YoloOutputParserUtility.CalculateIoU(current.detection.normalizedRect, candidates[j].detection.normalizedRect) > threshold)
                        suppressed[j] = true;
                }
            }

            detections = keptDetections.ToArray();
            masks = keptMasks.ToArray();
        }

        private static string FormatShape(int[] shape)
        {
            if (shape == null || shape.Length == 0)
                return "-";

            return string.Join("x", shape);
        }

        private readonly struct SegmentationCandidate
        {
            public readonly VisionDetection detection;
            public readonly VisionMask mask;

            public SegmentationCandidate(VisionDetection detection, VisionMask mask)
            {
                this.detection = detection;
                this.mask = mask;
            }
        }
    }
}
