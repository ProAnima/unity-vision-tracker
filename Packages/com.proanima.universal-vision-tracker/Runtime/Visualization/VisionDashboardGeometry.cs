using UnityEngine;

namespace UniversalTracker.Visualization
{
    /// <summary>
    /// Geometry helpers shared by runtime dashboards and tests.
    /// </summary>
    public static class VisionDashboardGeometry
    {
        public static Rect CalculateScaleToFitRect(Vector2 sourceSize, Vector2 viewportSize)
        {
            if (sourceSize.x <= 0f || sourceSize.y <= 0f)
                throw new System.ArgumentOutOfRangeException(nameof(sourceSize), "Source size must be positive.");

            if (viewportSize.x <= 0f || viewportSize.y <= 0f)
                return Rect.zero;

            float scale = Mathf.Min(viewportSize.x / sourceSize.x, viewportSize.y / sourceSize.y);
            Vector2 fittedSize = sourceSize * scale;
            Vector2 offset = (viewportSize - fittedSize) * 0.5f;

            return new Rect(offset, fittedSize);
        }

        public static Rect NormalizedToViewportRect(Rect normalizedRect, Vector2 sourceSize, Vector2 viewportSize)
        {
            Rect contentRect = CalculateScaleToFitRect(sourceSize, viewportSize);
            Rect rect = new Rect(
                contentRect.x + normalizedRect.x * contentRect.width,
                contentRect.y + normalizedRect.y * contentRect.height,
                normalizedRect.width * contentRect.width,
                normalizedRect.height * contentRect.height);

            return ClampRectToViewport(rect, viewportSize);
        }

        public static Vector2 NormalizedToViewportPoint(Vector2 normalizedPoint, Vector2 sourceSize, Vector2 viewportSize)
        {
            Rect contentRect = CalculateScaleToFitRect(sourceSize, viewportSize);
            return new Vector2(
                contentRect.x + normalizedPoint.x * contentRect.width,
                contentRect.y + normalizedPoint.y * contentRect.height);
        }

        public static BoneLine CalculateBoneLine(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            return new BoneLine(
                (from + to) * 0.5f,
                delta.magnitude,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        public static Rect ClampRectToViewport(Rect rect, Vector2 viewportSize)
        {
            if (viewportSize.x <= 0f || viewportSize.y <= 0f)
                return Rect.zero;

            float xMin = Mathf.Clamp(rect.xMin, 0f, viewportSize.x);
            float yMin = Mathf.Clamp(rect.yMin, 0f, viewportSize.y);
            float xMax = Mathf.Clamp(rect.xMax, 0f, viewportSize.x);
            float yMax = Mathf.Clamp(rect.yMax, 0f, viewportSize.y);

            return Rect.MinMaxRect(xMin, yMin, Mathf.Max(xMin, xMax), Mathf.Max(yMin, yMax));
        }

        public static Vector2 ClampLabelPosition(Rect targetRect, Vector2 labelSize, Vector2 viewportSize, float margin = 6f)
        {
            if (viewportSize.x <= 0f || viewportSize.y <= 0f)
                return Vector2.zero;

            float x = Mathf.Clamp(targetRect.x, margin, Mathf.Max(margin, viewportSize.x - labelSize.x - margin));
            float preferredY = targetRect.y - labelSize.y - margin;
            float fallbackY = targetRect.y + margin;
            float y = preferredY >= margin ? preferredY : fallbackY;
            y = Mathf.Clamp(y, margin, Mathf.Max(margin, viewportSize.y - labelSize.y - margin));

            return new Vector2(x, y);
        }

        public static float CalculateAdaptiveStroke(Vector2 viewportSize, float min = 1.5f, float max = 4f)
        {
            float shortestSide = Mathf.Min(Mathf.Abs(viewportSize.x), Mathf.Abs(viewportSize.y));
            if (shortestSide <= 0f)
                return min;

            return Mathf.Clamp(shortestSide / 360f, min, max);
        }

        public static Color StableColor(int stableId, float saturation = 0.72f, float value = 0.95f)
        {
            int id = Mathf.Abs(stableId);
            float hue = (id * 0.61803398875f) % 1f;
            return Color.HSVToRGB(hue, Mathf.Clamp01(saturation), Mathf.Clamp01(value));
        }

        public static Color ConfidenceColor(float confidence)
        {
            confidence = Mathf.Clamp01(confidence);
            if (confidence >= 0.7f)
                return new Color(0.22f, 0.92f, 0.46f, 1f);

            if (confidence >= 0.4f)
                return new Color(0.96f, 0.86f, 0.18f, 1f);

            return new Color(1f, 0.26f, 0.24f, 1f);
        }
    }

    public readonly struct BoneLine
    {
        public readonly Vector2 center;
        public readonly float length;
        public readonly float angleDegrees;

        public BoneLine(Vector2 center, float length, float angleDegrees)
        {
            this.center = center;
            this.length = length;
            this.angleDegrees = angleDegrees;
        }
    }
}
