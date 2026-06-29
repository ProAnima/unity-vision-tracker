using UnityEngine;
using UnityEngine.UIElements;
using UniversalTracker.Core;
using UniversalTracker.Visualization;

namespace UniversalTracker.OutputReceivers
{
    internal static class VisionToolkitDashboardMaskContourRenderer
    {
        public static void Render(
            VisionMask mask,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            Color color,
            ref int segmentsUsed)
        {
            if (mask.normalizedContourSegments != null && mask.normalizedContourSegments.Length > 1)
            {
                RenderSegments(mask.normalizedContourSegments, state, sourceSize, viewportSize, stroke, color, ref segmentsUsed);
                return;
            }

            if (mask.normalizedContour == null || mask.normalizedContour.Length <= 1)
                return;

            for (int i = 0; i < mask.normalizedContour.Length; i++)
            {
                Vector2 from = VisionDashboardGeometry.NormalizedToViewportPoint(mask.normalizedContour[i], sourceSize, viewportSize);
                Vector2 to = VisionDashboardGeometry.NormalizedToViewportPoint(mask.normalizedContour[(i + 1) % mask.normalizedContour.Length], sourceSize, viewportSize);
                VisualElement segment = VisionDashboardElementPool.GetElement(
                    state.maskContourSegments,
                    state.maskLayer,
                    VisionToolkitDashboardPrimitives.CreateBone,
                    segmentsUsed);

                UpdateSegment(segment, from, to, color, stroke);
                segmentsUsed++;
            }
        }

        private static void RenderSegments(
            Vector2[] segments,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            Color color,
            ref int segmentsUsed)
        {
            for (int i = 0; i + 1 < segments.Length; i += 2)
            {
                Vector2 from = VisionDashboardGeometry.NormalizedToViewportPoint(segments[i], sourceSize, viewportSize);
                Vector2 to = VisionDashboardGeometry.NormalizedToViewportPoint(segments[i + 1], sourceSize, viewportSize);
                VisualElement segment = VisionDashboardElementPool.GetElement(
                    state.maskContourSegments,
                    state.maskLayer,
                    VisionToolkitDashboardPrimitives.CreateBone,
                    segmentsUsed);

                UpdateSegment(segment, from, to, color, stroke);
                segmentsUsed++;
            }
        }

        private static void UpdateSegment(VisualElement segment, Vector2 from, Vector2 to, Color color, float stroke)
        {
            BoneLine line = VisionDashboardGeometry.CalculateBoneLine(from, to);
            segment.style.left = line.center.x - line.length * 0.5f;
            segment.style.top = line.center.y - stroke * 0.5f;
            segment.style.width = Mathf.Max(1f, line.length);
            segment.style.height = Mathf.Max(2f, stroke * 1.35f);
            segment.style.rotate = new Rotate(new Angle(line.angleDegrees, AngleUnit.Degree));
            segment.style.backgroundColor = new Color(color.r, color.g, color.b, 0.96f);
        }
    }
}
