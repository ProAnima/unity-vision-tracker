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

            int maskCoefficientCount = ResolveMaskCoefficientCount(prototypeTensor);
            bool hasObjectness = YoloOutputParserUtility.HasObjectness(rows.stride);
            int classOffset = hasObjectness ? 5 : 4;
            int classScoreCount = rows.stride - classOffset - maskCoefficientCount;
            if (maskCoefficientCount <= 0 || classScoreCount <= 0)
                return VisionParsedOutput.Empty;

            var detections = new List<VisionDetection>();
            var masks = new List<VisionMask>();
            float maxConfidence = 0f;
            for (int row = 0; row < rows.rowCount; row++)
            {
                float objectness = hasObjectness ? Mathf.Clamp01(rows.Get(row, 4)) : 1f;
                int classId = FindBestClass(rows, row, classOffset, classScoreCount, out float classScore);
                float confidence = objectness * classScore;
                maxConfidence = Mathf.Max(maxConfidence, confidence);
                if (confidence < context.confidenceThreshold)
                    continue;

                Rect normalized = YoloOutputParserUtility.CenterToNormalizedRect(rows, row, context);

                string label = YoloOutputParserUtility.ResolveLabel(classId, context.labels);
                VisionDetection detection = YoloOutputParserUtility.CreateDetection(classId, label, confidence, normalized, context.sourceSize);
                detections.Add(detection);
                masks.Add(new VisionMask
                {
                    trackId = -1,
                    classId = classId,
                    label = label,
                    confidence = confidence,
                    normalizedRect = normalized,
                    sourceRect = detection.sourceRect,
                    texture = null
                });
            }

            return new VisionParsedOutput
            {
                detections = detections.ToArray(),
                masks = masks.ToArray(),
                stats = VisionPerformanceStats.FromStages(0f, rawOutput.inferenceMs, 0f, 0f),
                diagnostics = new VisionFrameDiagnostics
                {
                    parserId = ParserId,
                    modelOutput = $"{rowsTensor.name} [{FormatShape(rowsTensor.shape)}], {rows.LayoutLabel}",
                    candidateCount = detections.Count,
                    acceptedCount = detections.Count,
                    maxConfidence = maxConfidence
                }
            };
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

        private static string FormatShape(int[] shape)
        {
            if (shape == null || shape.Length == 0)
                return "-";

            return string.Join("x", shape);
        }
    }
}
