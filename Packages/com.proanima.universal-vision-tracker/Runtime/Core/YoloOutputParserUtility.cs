using System.Collections.Generic;
using UnityEngine;

namespace UniversalTracker.Core
{
    internal static class YoloOutputParserUtility
    {
        public static bool TryResolveRows(VisionRawTensor tensor, out YoloTensorRows rows)
        {
            rows = default;

            if (tensor.shape == null)
                return false;

            if (tensor.shape.Length == 2)
            {
                rows = new YoloTensorRows(tensor, tensor.shape[0], tensor.shape[1], false);
            }
            else if (tensor.shape.Length == 3 && tensor.shape[0] == 1)
            {
                int first = tensor.shape[1];
                int second = tensor.shape[2];
                bool channelFirst = IsKnownYoloStride(first) || (first >= 6 && first < second);
                rows = channelFirst
                    ? new YoloTensorRows(tensor, second, first, true)
                    : new YoloTensorRows(tensor, first, second, false);
            }

            return rows.IsValid;
        }

        public static bool TryResolveRows(VisionRawTensor tensor, out int rowCount, out int stride)
        {
            if (TryResolveRows(tensor, out YoloTensorRows rows))
            {
                rowCount = rows.rowCount;
                stride = rows.stride;
                return true;
            }

            rowCount = 0;
            stride = 0;
            return false;
        }

        public static Rect CenterToRect(float centerX, float centerY, float width, float height)
        {
            float xMin = Mathf.Clamp01(centerX - width * 0.5f);
            float yMin = Mathf.Clamp01(centerY - height * 0.5f);
            float xMax = Mathf.Clamp01(centerX + width * 0.5f);
            float yMax = Mathf.Clamp01(centerY + height * 0.5f);
            return Rect.MinMaxRect(Mathf.Min(xMin, xMax), Mathf.Min(yMin, yMax), Mathf.Max(xMin, xMax), Mathf.Max(yMin, yMax));
        }

        public static Rect CenterToNormalizedRect(
            YoloTensorRows rows,
            int row,
            VisionOutputParserContext context)
        {
            Rect rect = CenterToRect(
                NormalizeCoordinate(rows.Get(row, 0), context.modelInputSize.x),
                NormalizeCoordinate(rows.Get(row, 1), context.modelInputSize.y),
                NormalizeCoordinate(rows.Get(row, 2), context.modelInputSize.x),
                NormalizeCoordinate(rows.Get(row, 3), context.modelInputSize.y));

            return context.coordinateTransform.Apply(RemoveLetterbox(rect, context));
        }

        public static Rect CornersToNormalizedRect(
            YoloTensorRows rows,
            int row,
            VisionOutputParserContext context)
        {
            float x1 = NormalizeCoordinate(rows.Get(row, 0), context.modelInputSize.x);
            float y1 = NormalizeCoordinate(rows.Get(row, 1), context.modelInputSize.y);
            float x2 = NormalizeCoordinate(rows.Get(row, 2), context.modelInputSize.x);
            float y2 = NormalizeCoordinate(rows.Get(row, 3), context.modelInputSize.y);
            Rect rect = Rect.MinMaxRect(
                Mathf.Min(x1, x2),
                Mathf.Min(y1, y2),
                Mathf.Max(x1, x2),
                Mathf.Max(y1, y2));

            return context.coordinateTransform.Apply(RemoveLetterbox(rect, context));
        }

        public static Vector2 ReadNormalizedPoint(
            YoloTensorRows rows,
            int row,
            int xColumn,
            int yColumn,
            VisionOutputParserContext context)
        {
            var point = new Vector2(
                NormalizeCoordinate(rows.Get(row, xColumn), context.modelInputSize.x),
                NormalizeCoordinate(rows.Get(row, yColumn), context.modelInputSize.y));

            return context.coordinateTransform.Apply(RemoveLetterbox(point, context));
        }

        public static float NormalizeCoordinate(float value, int modelAxis)
        {
            if (value >= 0f && value <= 1f)
                return value;

            int divisor = modelAxis > 0 ? modelAxis : 640;
            return Mathf.Clamp01(value / divisor);
        }

        public static Vector2 RemoveLetterbox(Vector2 normalizedPoint, VisionOutputParserContext context)
        {
            if (!context.preserveAspectRatio ||
                context.sourceSize.x <= 0 ||
                context.sourceSize.y <= 0 ||
                context.modelInputSize.x <= 0 ||
                context.modelInputSize.y <= 0)
            {
                return normalizedPoint;
            }

            float scale = Mathf.Min(
                (float)context.modelInputSize.x / context.sourceSize.x,
                (float)context.modelInputSize.y / context.sourceSize.y);
            if (scale <= 0f)
                return normalizedPoint;

            Vector2 contentSize = new Vector2(context.sourceSize.x * scale, context.sourceSize.y * scale);
            Vector2 padding = (new Vector2(context.modelInputSize.x, context.modelInputSize.y) - contentSize) * 0.5f;
            Vector2 modelPoint = new Vector2(
                normalizedPoint.x * context.modelInputSize.x,
                normalizedPoint.y * context.modelInputSize.y);
            return new Vector2(
                Mathf.Clamp01((modelPoint.x - padding.x) / contentSize.x),
                Mathf.Clamp01((modelPoint.y - padding.y) / contentSize.y));
        }

        public static Rect RemoveLetterbox(Rect normalizedRect, VisionOutputParserContext context)
        {
            if (!context.preserveAspectRatio)
                return normalizedRect;

            Vector2 min = RemoveLetterbox(new Vector2(normalizedRect.xMin, normalizedRect.yMin), context);
            Vector2 max = RemoveLetterbox(new Vector2(normalizedRect.xMax, normalizedRect.yMax), context);
            return Rect.MinMaxRect(
                Mathf.Min(min.x, max.x),
                Mathf.Min(min.y, max.y),
                Mathf.Max(min.x, max.x),
                Mathf.Max(min.y, max.y));
        }

        public static bool HasObjectness(int stride)
        {
            return stride == 85 || stride == 117 || stride == 57 || stride <= 10;
        }

        public static Rect NormalizedToSourceRect(Rect normalized, Vector2Int sourceSize)
        {
            if (sourceSize.x <= 0 || sourceSize.y <= 0)
                return default;

            return new Rect(
                normalized.x * sourceSize.x,
                normalized.y * sourceSize.y,
                normalized.width * sourceSize.x,
                normalized.height * sourceSize.y);
        }

        public static Vector2 NormalizedToSourcePoint(Vector2 normalized, Vector2Int sourceSize)
        {
            if (sourceSize.x <= 0 || sourceSize.y <= 0)
                return Vector2.zero;

            return new Vector2(normalized.x * sourceSize.x, normalized.y * sourceSize.y);
        }

        public static int FindBestClass(float[] data, int offset, int count, out float score)
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

        public static string ResolveLabel(int classId, string[] labels)
        {
            if (labels != null && classId >= 0 && classId < labels.Length && !string.IsNullOrWhiteSpace(labels[classId]))
                return labels[classId];

            return CocoClassLabels.Resolve(classId);
        }

        public static VisionDetection CreateDetection(
            int classId,
            string label,
            float confidence,
            Rect normalizedRect,
            Vector2Int sourceSize)
        {
            return new VisionDetection
            {
                trackId = -1,
                classId = classId,
                label = label,
                confidence = confidence,
                normalizedRect = normalizedRect,
                sourceRect = NormalizedToSourceRect(normalizedRect, sourceSize),
                sourceCenter = NormalizedToSourcePoint(normalizedRect.center, sourceSize),
                trackState = VisionTrackState.None
            };
        }

        public static VisionDetection[] ApplyNms(List<VisionDetection> detections, float threshold, List<int> keptIndices = null)
        {
            keptIndices?.Clear();
            if (detections == null || detections.Count == 0)
                return System.Array.Empty<VisionDetection>();

            detections.Sort((a, b) => b.confidence.CompareTo(a.confidence));
            var kept = new List<VisionDetection>();
            var suppressed = new bool[detections.Count];

            for (int i = 0; i < detections.Count; i++)
            {
                if (suppressed[i])
                    continue;

                VisionDetection current = detections[i];
                kept.Add(current);
                keptIndices?.Add(i);

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

        public static bool IsEndToEndRows(YoloTensorRows rows, int stride)
        {
            return rows.rowCount > 16 && rows.rowCount <= 300 && rows.stride == stride;
        }

        public static float CalculateIoU(Rect a, Rect b)
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

        private static bool IsKnownYoloStride(int stride)
        {
            return stride == 6 || stride == 38 || stride == 56 || stride == 57 || stride == 84 || stride == 85 || stride == 116 || stride == 117;
        }
    }

    internal readonly struct YoloTensorRows
    {
        private readonly VisionRawTensor tensor;
        private readonly bool channelFirst;

        public readonly int rowCount;
        public readonly int stride;
        public string LayoutLabel => channelFirst
            ? $"channels-first rows={rowCount} stride={stride}"
            : $"rows={rowCount} stride={stride}";

        public YoloTensorRows(VisionRawTensor tensor, int rowCount, int stride, bool channelFirst)
        {
            this.tensor = tensor;
            this.rowCount = rowCount;
            this.stride = stride;
            this.channelFirst = channelFirst;
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
