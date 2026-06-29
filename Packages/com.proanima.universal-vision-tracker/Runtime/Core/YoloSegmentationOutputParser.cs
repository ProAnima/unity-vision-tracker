using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalTracker.Core
{
    public sealed class YoloSegmentationOutputParser : IVisionOutputParser
    {
        private const int MaxContourPoints = 48;
        private const float MaskThreshold = 0.5f;

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

                Rect rawNormalized = isEndToEnd
                    ? ReadRawCornerRect(rows, row, context)
                    : ReadRawCenterRect(rows, row, context);
                Rect normalized = context.coordinateTransform.Apply(rawNormalized);
                int maskOffset = isEndToEnd ? 6 : classOffset + classScoreCount;

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
                        normalizedContour = BuildContour(rows, row, prototypeTensor, maskOffset, maskCoefficientCount, rawNormalized, context.coordinateTransform),
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

        private static Rect ReadRawCenterRect(YoloTensorRows rows, int row, VisionOutputParserContext context)
        {
            return YoloOutputParserUtility.CenterToRect(
                YoloOutputParserUtility.NormalizeCoordinate(rows.Get(row, 0), context.modelInputSize.x),
                YoloOutputParserUtility.NormalizeCoordinate(rows.Get(row, 1), context.modelInputSize.y),
                YoloOutputParserUtility.NormalizeCoordinate(rows.Get(row, 2), context.modelInputSize.x),
                YoloOutputParserUtility.NormalizeCoordinate(rows.Get(row, 3), context.modelInputSize.y));
        }

        private static Rect ReadRawCornerRect(YoloTensorRows rows, int row, VisionOutputParserContext context)
        {
            float x1 = YoloOutputParserUtility.NormalizeCoordinate(rows.Get(row, 0), context.modelInputSize.x);
            float y1 = YoloOutputParserUtility.NormalizeCoordinate(rows.Get(row, 1), context.modelInputSize.y);
            float x2 = YoloOutputParserUtility.NormalizeCoordinate(rows.Get(row, 2), context.modelInputSize.x);
            float y2 = YoloOutputParserUtility.NormalizeCoordinate(rows.Get(row, 3), context.modelInputSize.y);
            return Rect.MinMaxRect(
                Mathf.Clamp01(Mathf.Min(x1, x2)),
                Mathf.Clamp01(Mathf.Min(y1, y2)),
                Mathf.Clamp01(Mathf.Max(x1, x2)),
                Mathf.Clamp01(Mathf.Max(y1, y2)));
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

        private static Vector2[] BuildContour(
            YoloTensorRows rows,
            int row,
            VisionRawTensor prototypeTensor,
            int coefficientOffset,
            int coefficientCount,
            Rect rawNormalizedRect,
            VisionOutputCoordinateTransform transform)
        {
            if (coefficientCount <= 0 || !TryResolvePrototypeShape(prototypeTensor, out int channels, out int height, out int width))
                return Array.Empty<Vector2>();

            int xMin = Mathf.Clamp(Mathf.FloorToInt(rawNormalizedRect.xMin * width), 0, width - 1);
            int yMin = Mathf.Clamp(Mathf.FloorToInt(rawNormalizedRect.yMin * height), 0, height - 1);
            int xMax = Mathf.Clamp(Mathf.CeilToInt(rawNormalizedRect.xMax * width), xMin + 1, width);
            int yMax = Mathf.Clamp(Mathf.CeilToInt(rawNormalizedRect.yMax * height), yMin + 1, height);
            var edgePoints = new List<Vector2>();

            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    if (!IsMaskActive(rows, row, prototypeTensor, coefficientOffset, coefficientCount, channels, height, width, x, y))
                        continue;

                    bool isEdge =
                        x == xMin || y == yMin || x == xMax - 1 || y == yMax - 1 ||
                        !IsMaskActive(rows, row, prototypeTensor, coefficientOffset, coefficientCount, channels, height, width, x - 1, y) ||
                        !IsMaskActive(rows, row, prototypeTensor, coefficientOffset, coefficientCount, channels, height, width, x + 1, y) ||
                        !IsMaskActive(rows, row, prototypeTensor, coefficientOffset, coefficientCount, channels, height, width, x, y - 1) ||
                        !IsMaskActive(rows, row, prototypeTensor, coefficientOffset, coefficientCount, channels, height, width, x, y + 1);

                    if (isEdge)
                        edgePoints.Add(new Vector2((x + 0.5f) / width, (y + 0.5f) / height));
                }
            }

            if (edgePoints.Count < 3)
                return Array.Empty<Vector2>();

            return SimplifyContour(BuildConvexHull(edgePoints), transform);
        }

        private static bool TryResolvePrototypeShape(VisionRawTensor tensor, out int channels, out int height, out int width)
        {
            channels = 0;
            height = 0;
            width = 0;
            if (!tensor.IsValid)
                return false;

            if (tensor.shape.Length == 4 && tensor.shape[0] == 1)
            {
                channels = tensor.shape[1];
                height = tensor.shape[2];
                width = tensor.shape[3];
            }
            else if (tensor.shape.Length == 3)
            {
                channels = tensor.shape[0];
                height = tensor.shape[1];
                width = tensor.shape[2];
            }

            return channels > 0 && width > 1 && height > 1 && channels * width * height <= tensor.ElementCount;
        }

        private static bool IsMaskActive(
            YoloTensorRows rows,
            int row,
            VisionRawTensor prototypeTensor,
            int coefficientOffset,
            int coefficientCount,
            int channels,
            int height,
            int width,
            int x,
            int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;

            float value = 0f;
            int count = Mathf.Min(coefficientCount, channels);
            for (int c = 0; c < count; c++)
                value += rows.Get(row, coefficientOffset + c) * prototypeTensor.data[(c * height + y) * width + x];

            return Sigmoid(value) > MaskThreshold;
        }

        private static float Sigmoid(float value)
        {
            return 1f / (1f + Mathf.Exp(-value));
        }

        private static List<Vector2> BuildConvexHull(List<Vector2> points)
        {
            points.Sort((a, b) =>
            {
                int x = a.x.CompareTo(b.x);
                return x != 0 ? x : a.y.CompareTo(b.y);
            });

            var hull = new List<Vector2>();
            for (int i = 0; i < points.Count; i++)
                AddHullPoint(hull, points[i]);

            int lowerCount = hull.Count;
            for (int i = points.Count - 2; i >= 0; i--)
                AddHullPoint(hull, points[i], lowerCount);

            if (hull.Count > 1)
                hull.RemoveAt(hull.Count - 1);

            return hull;
        }

        private static void AddHullPoint(List<Vector2> hull, Vector2 point, int minCount = 0)
        {
            while (hull.Count > minCount + 1 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f)
                hull.RemoveAt(hull.Count - 1);

            hull.Add(point);
        }

        private static float Cross(Vector2 origin, Vector2 a, Vector2 b)
        {
            return (a.x - origin.x) * (b.y - origin.y) - (a.y - origin.y) * (b.x - origin.x);
        }

        private static Vector2[] SimplifyContour(List<Vector2> contour, VisionOutputCoordinateTransform transform)
        {
            if (contour.Count <= MaxContourPoints)
                return TransformContour(contour, transform);

            var simplified = new List<Vector2>(MaxContourPoints);
            float step = contour.Count / (float)MaxContourPoints;
            for (int i = 0; i < MaxContourPoints; i++)
                simplified.Add(contour[Mathf.Min(contour.Count - 1, Mathf.FloorToInt(i * step))]);

            return TransformContour(simplified, transform);
        }

        private static Vector2[] TransformContour(List<Vector2> contour, VisionOutputCoordinateTransform transform)
        {
            var transformed = new Vector2[contour.Count];
            for (int i = 0; i < contour.Count; i++)
                transformed[i] = transform.Apply(contour[i]);

            return transformed;
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
