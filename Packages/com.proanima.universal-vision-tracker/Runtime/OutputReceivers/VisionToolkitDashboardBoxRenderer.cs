using UnityEngine;
using UnityEngine.UIElements;
using UniversalTracker.Core;
using UniversalTracker.Visualization;

namespace UniversalTracker.OutputReceivers
{
    internal static class VisionToolkitDashboardBoxRenderer
    {
        public static void UpdateDetection(
            VisualElement box,
            Label label,
            int index,
            VisionDetection detection,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            float opacity = 1f)
        {
            Rect rect = VisionDashboardGeometry.NormalizedToViewportRect(detection.normalizedRect, sourceSize, viewportSize);
            float boxStroke = VisionDashboardOverlayStyle.DetectionStroke(stroke, detection.confidence);
            Color color = VisionDashboardOverlayStyle.DetectionColor(detection, index);

            ApplyRect(box, rect);
            ApplyBorder(box, boxStroke, VisionDashboardOverlayStyle.WithAlpha(color, 0.98f));
            box.style.backgroundColor = VisionDashboardOverlayStyle.WithAlpha(color, Mathf.Lerp(0.045f, 0.12f, Mathf.Clamp01(detection.confidence)));
            float elementOpacity = detection.trackState == VisionTrackState.Lost ? Mathf.Min(opacity, 0.55f) : opacity;
            box.style.opacity = elementOpacity;
            UpdateDetectionCorners(box, rect, color, boxStroke);

            label.text = VisionDashboardOverlayStyle.DetectionLabelText(detection);
            label.tooltip = VisionDashboardOverlayStyle.DetectionTooltip(detection);
            UpdateLabel(label, rect, viewportSize, color, 86f, 280f);
            label.style.opacity = elementOpacity;
        }

        public static Color UpdateMaskOverlay(
            VisualElement element,
            Label label,
            int index,
            VisionMask mask,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            float maskAlpha,
            float opacity = 1f)
        {
            Rect rect = VisionDashboardGeometry.NormalizedToViewportRect(mask.normalizedRect, sourceSize, viewportSize);
            float borderStroke = mask.HasContour ? 0f : VisionDashboardOverlayStyle.MaskBorderStroke(stroke);
            Color color = VisionDashboardOverlayStyle.MaskColor(mask, index);

            ApplyRect(element, rect);
            ApplyBorder(element, borderStroke, VisionDashboardOverlayStyle.WithAlpha(color, mask.HasContour ? 0f : 0.92f));
            element.style.opacity = opacity;
            element.style.backgroundColor = mask.texture != null
                ? VisionDashboardOverlayStyle.WithAlpha(color, Mathf.Clamp01(maskAlpha * 0.34f))
                : VisionDashboardOverlayStyle.WithAlpha(color, mask.HasContour ? 0f : 0.035f);

            Image image = element.Q<Image>(VisionToolkitDashboardPrimitives.MaskImageName);
            if (image != null)
            {
                image.image = mask.texture;
                image.tintColor = new Color(1f, 1f, 1f, mask.texture != null ? Mathf.Clamp01(maskAlpha + 0.12f) : 0f);
                image.style.display = mask.texture != null ? DisplayStyle.Flex : DisplayStyle.None;
            }

            label.text = VisionDashboardOverlayStyle.MaskLabelText(mask);
            label.tooltip = VisionDashboardOverlayStyle.MaskTooltip(mask);
            UpdateMaskLabel(label, rect, viewportSize, color);
            label.style.opacity = opacity;
            return color;
        }

        private static void ApplyRect(VisualElement element, Rect rect)
        {
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = Mathf.Max(1f, rect.width);
            element.style.height = Mathf.Max(1f, rect.height);
        }

        private static void ApplyBorder(VisualElement element, float width, Color color)
        {
            element.style.borderTopWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            VisionDashboardTheme.SetBorderColor(element, color);
        }

        private static void UpdateDetectionCorners(VisualElement box, Rect rect, Color color, float stroke)
        {
            float length = VisionDashboardOverlayStyle.DetectionCornerLength(rect, stroke);
            float thickness = VisionDashboardOverlayStyle.DetectionCornerThickness(stroke);
            for (int i = 0; i < Mathf.Min(8, box.childCount); i++)
            {
                VisualElement corner = box.ElementAt(i);
                bool horizontal = i % 2 == 0;
                corner.style.width = horizontal ? length : thickness;
                corner.style.height = horizontal ? thickness : length;
                corner.style.backgroundColor = color;
            }
        }

        private static void UpdateLabel(Label label, Rect rect, Vector2 viewportSize, Color color, float minWidth, float maxWidth)
        {
            Vector2 labelSize = VisionDashboardOverlayStyle.LabelSize(label.text, minWidth, maxWidth);
            Vector2 labelPosition = VisionDashboardGeometry.ClampLabelPosition(rect, labelSize, viewportSize);
            ApplyLabelFrame(label, labelPosition, labelSize, color, 0.92f);
        }

        private static void UpdateMaskLabel(Label label, Rect rect, Vector2 viewportSize, Color color)
        {
            Vector2 labelSize = VisionDashboardOverlayStyle.LabelSize(label.text, 92f, 300f);
            Vector2 labelPosition = VisionDashboardGeometry.ClampLabelPosition(
                new Rect(rect.x, rect.yMax + labelSize.y + 6f, rect.width, rect.height),
                labelSize,
                viewportSize);
            ApplyLabelFrame(label, labelPosition, labelSize, color, 0.88f);
        }

        private static void ApplyLabelFrame(Label label, Vector2 position, Vector2 size, Color color, float alpha)
        {
            label.style.left = position.x;
            label.style.top = position.y;
            label.style.width = size.x;
            label.style.height = size.y;
            label.style.backgroundColor = VisionDashboardOverlayStyle.WithAlpha(color, alpha);
            label.style.color = VisionDashboardOverlayStyle.ReadableTextColor(color);
            VisionDashboardTheme.SetBorderColor(label, VisionDashboardOverlayStyle.WithAlpha(Color.black, 0.5f));
        }
    }
}
