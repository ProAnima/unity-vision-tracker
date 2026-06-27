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
            return new Rect(
                contentRect.x + normalizedRect.x * contentRect.width,
                contentRect.y + normalizedRect.y * contentRect.height,
                normalizedRect.width * contentRect.width,
                normalizedRect.height * contentRect.height);
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
