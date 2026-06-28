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
            if (!YoloOutputParserUtility.TryResolveRows(rowsTensor, out int rowCount, out int stride))
                return VisionParsedOutput.Empty;

            int maskCoefficientCount = ResolveMaskCoefficientCount(prototypeTensor);
            int classScoreCount = stride - 5 - maskCoefficientCount;
            if (maskCoefficientCount <= 0 || classScoreCount <= 0)
                return VisionParsedOutput.Empty;

            var detections = new List<VisionDetection>();
            var masks = new List<VisionMask>();
            for (int row = 0; row < rowCount; row++)
            {
                int offset = row * stride;
                float objectness = Mathf.Clamp01(rowsTensor.data[offset + 4]);
                int classId = YoloOutputParserUtility.FindBestClass(rowsTensor.data, offset + 5, classScoreCount, out float classScore);
                float confidence = objectness * classScore;
                if (confidence < context.confidenceThreshold)
                    continue;

                Rect normalized = YoloOutputParserUtility.CenterToRect(
                    rowsTensor.data[offset],
                    rowsTensor.data[offset + 1],
                    rowsTensor.data[offset + 2],
                    rowsTensor.data[offset + 3]);

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
                stats = VisionPerformanceStats.FromStages(0f, rawOutput.inferenceMs, 0f, 0f)
            };
        }

        private static int ResolveMaskCoefficientCount(VisionRawTensor prototypeTensor)
        {
            if (prototypeTensor.shape.Length == 4 && prototypeTensor.shape[0] == 1)
                return prototypeTensor.shape[1];

            if (prototypeTensor.shape.Length == 3)
                return prototypeTensor.shape[0];

            return 0;
        }
    }
}
