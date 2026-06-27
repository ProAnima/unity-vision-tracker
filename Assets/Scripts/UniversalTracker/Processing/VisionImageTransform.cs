using System;
using UnityEngine;

namespace UniversalTracker.Processing
{
    /// <summary>
    /// Reversible coordinate transform between source image space and model input space.
    /// Keeps resize/letterbox logic testable and independent from model parsers.
    /// </summary>
    public readonly struct VisionImageTransform
    {
        public readonly Vector2Int sourceSize;
        public readonly Vector2Int modelSize;
        public readonly Vector2 scale;
        public readonly Vector2 padding;
        public readonly Vector2 scaledSourceSize;
        public readonly bool preservesAspectRatio;

        private VisionImageTransform(
            Vector2Int sourceSize,
            Vector2Int modelSize,
            Vector2 scale,
            Vector2 padding,
            Vector2 scaledSourceSize,
            bool preservesAspectRatio)
        {
            this.sourceSize = sourceSize;
            this.modelSize = modelSize;
            this.scale = scale;
            this.padding = padding;
            this.scaledSourceSize = scaledSourceSize;
            this.preservesAspectRatio = preservesAspectRatio;
        }

        public static VisionImageTransform Stretch(Vector2Int sourceSize, Vector2Int modelSize)
        {
            ValidateSize(sourceSize, nameof(sourceSize));
            ValidateSize(modelSize, nameof(modelSize));

            var scale = new Vector2(
                modelSize.x / (float)sourceSize.x,
                modelSize.y / (float)sourceSize.y);

            return new VisionImageTransform(
                sourceSize,
                modelSize,
                scale,
                Vector2.zero,
                new Vector2(modelSize.x, modelSize.y),
                false);
        }

        public static VisionImageTransform Letterbox(Vector2Int sourceSize, Vector2Int modelSize)
        {
            ValidateSize(sourceSize, nameof(sourceSize));
            ValidateSize(modelSize, nameof(modelSize));

            float uniformScale = Mathf.Min(
                modelSize.x / (float)sourceSize.x,
                modelSize.y / (float)sourceSize.y);

            var scaled = new Vector2(sourceSize.x * uniformScale, sourceSize.y * uniformScale);
            var padding = new Vector2(
                (modelSize.x - scaled.x) * 0.5f,
                (modelSize.y - scaled.y) * 0.5f);

            return new VisionImageTransform(
                sourceSize,
                modelSize,
                new Vector2(uniformScale, uniformScale),
                padding,
                scaled,
                true);
        }

        public Vector2 SourceToModelPoint(Vector2 sourcePoint)
        {
            return new Vector2(
                sourcePoint.x * scale.x + padding.x,
                sourcePoint.y * scale.y + padding.y);
        }

        public Vector2 ModelToSourcePoint(Vector2 modelPoint)
        {
            return new Vector2(
                (modelPoint.x - padding.x) / scale.x,
                (modelPoint.y - padding.y) / scale.y);
        }

        public Rect SourceToModelRect(Rect sourceRect)
        {
            var min = SourceToModelPoint(sourceRect.min);
            var max = SourceToModelPoint(sourceRect.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public Rect ModelToSourceRect(Rect modelRect)
        {
            var min = ModelToSourcePoint(modelRect.min);
            var max = ModelToSourcePoint(modelRect.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public Vector2 SourceToNormalizedPoint(Vector2 sourcePoint)
        {
            return new Vector2(
                sourcePoint.x / sourceSize.x,
                sourcePoint.y / sourceSize.y);
        }

        public Vector2 NormalizedToSourcePoint(Vector2 normalizedPoint)
        {
            return new Vector2(
                normalizedPoint.x * sourceSize.x,
                normalizedPoint.y * sourceSize.y);
        }

        public Rect SourceToNormalizedRect(Rect sourceRect)
        {
            return new Rect(
                sourceRect.x / sourceSize.x,
                sourceRect.y / sourceSize.y,
                sourceRect.width / sourceSize.x,
                sourceRect.height / sourceSize.y);
        }

        public Rect NormalizedToSourceRect(Rect normalizedRect)
        {
            return new Rect(
                normalizedRect.x * sourceSize.x,
                normalizedRect.y * sourceSize.y,
                normalizedRect.width * sourceSize.x,
                normalizedRect.height * sourceSize.y);
        }

        public Rect ClampSourceRect(Rect sourceRect)
        {
            return ClampRect(sourceRect, new Rect(0, 0, sourceSize.x, sourceSize.y));
        }

        public Rect ClampModelRect(Rect modelRect)
        {
            return ClampRect(modelRect, new Rect(0, 0, modelSize.x, modelSize.y));
        }

        private static Rect ClampRect(Rect rect, Rect bounds)
        {
            return Rect.MinMaxRect(
                Mathf.Clamp(rect.xMin, bounds.xMin, bounds.xMax),
                Mathf.Clamp(rect.yMin, bounds.yMin, bounds.yMax),
                Mathf.Clamp(rect.xMax, bounds.xMin, bounds.xMax),
                Mathf.Clamp(rect.yMax, bounds.yMin, bounds.yMax));
        }

        private static void ValidateSize(Vector2Int size, string paramName)
        {
            if (size.x <= 0 || size.y <= 0)
                throw new ArgumentOutOfRangeException(paramName, size, "Image dimensions must be positive.");
        }
    }
}

