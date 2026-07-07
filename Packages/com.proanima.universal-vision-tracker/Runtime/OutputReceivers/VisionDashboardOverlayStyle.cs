using System;
using UnityEngine;
using UniversalTracker.Core;
using UniversalTracker.Visualization;

namespace UniversalTracker.OutputReceivers
{
    internal static class VisionDashboardOverlayStyle
    {
        private static readonly Color LeftPoseColor = new Color(0.2f, 0.78f, 1f, 1f);
        private static readonly Color RightPoseColor = new Color(1f, 0.58f, 0.32f, 1f);
        private static readonly Color CenterPoseColor = new Color(0.92f, 0.96f, 0.44f, 1f);

        public static string DetectionLabelText(VisionDetection detection)
        {
            string name = string.IsNullOrWhiteSpace(detection.label) ? $"#{detection.classId}" : detection.label;
            string id = detection.IsTracked ? $" T{detection.trackId}" : string.Empty;
            return $"{name} {Mathf.Clamp01(detection.confidence) * 100f:F0}%{id}";
        }

        public static string MaskLabelText(VisionMask mask)
        {
            string name = string.IsNullOrWhiteSpace(mask.label) ? $"#{mask.classId}" : mask.label;
            string id = mask.trackId >= 0 ? $" T{mask.trackId}" : string.Empty;
            return $"{name} {Mathf.Clamp01(mask.confidence) * 100f:F0}%{id}";
        }

        public static Vector2 LabelSize(string text, float minWidth, float maxWidth)
        {
            int length = string.IsNullOrEmpty(text) ? 1 : text.Length;
            return new Vector2(Mathf.Clamp(length * 7.4f + 22f, minWidth, maxWidth), 24f);
        }

        public static Color DetectionColor(VisionDetection detection, int index)
        {
            int stableId = detection.IsTracked ? detection.trackId : detection.classId * 997 + index * 37;
            float saturation = detection.IsTracked ? 0.66f : IsHumanDetection(detection) ? 0.6f : 0.72f;
            return VisionDashboardGeometry.StableColor(stableId, saturation, 0.98f);
        }

        public static Color MaskColor(VisionMask mask, int index)
        {
            int stableId = mask.trackId >= 0 ? mask.trackId : mask.classId * 997 + index * 41;
            return VisionDashboardGeometry.StableColor(stableId, 0.68f, 0.9f);
        }

        public static Color BoneColor(VisionSkeletonBone bone, float confidence)
        {
            Color semantic = PoseSemanticColor(bone.name, bone.from + bone.to);
            Color confidenceColor = VisionDashboardGeometry.ConfidenceColor(confidence);
            Color color = Color.Lerp(confidenceColor, semantic, 0.72f);
            color.a = Mathf.Lerp(0.48f, 0.98f, Mathf.Clamp01(confidence));
            return color;
        }

        public static Color KeypointColor(VisionKeypoint keypoint)
        {
            Color semantic = PoseSemanticColor(keypoint.name, keypoint.index);
            Color confidenceColor = VisionDashboardGeometry.ConfidenceColor(keypoint.confidence);
            Color color = Color.Lerp(confidenceColor, semantic, 0.64f);
            color.a = Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(keypoint.confidence));
            return color;
        }

        public static Color ReadableTextColor(Color background)
        {
            float luminance = background.r * 0.299f + background.g * 0.587f + background.b * 0.114f;
            return luminance >= 0.46f ? Color.black : Color.white;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        public static float DetectionStroke(float stroke, float confidence)
        {
            return Mathf.Clamp(stroke * Mathf.Lerp(0.95f, 1.45f, Mathf.Clamp01(confidence)), 1.5f, 6f);
        }

        public static float DetectionCornerLength(Rect rect, float stroke)
        {
            float shortest = Mathf.Max(1f, Mathf.Min(rect.width, rect.height));
            return Mathf.Clamp(shortest * 0.22f, 12f, 34f) + stroke;
        }

        public static float DetectionCornerThickness(float stroke)
        {
            return Mathf.Clamp(stroke * 1.85f, 3f, 8f);
        }

        public static float MaskBorderStroke(float stroke)
        {
            return Mathf.Clamp(stroke * 0.9f, 1.25f, 4f);
        }

        public static float BoneCoreThickness(float stroke, float confidence)
        {
            return Mathf.Clamp(stroke * Mathf.Lerp(1.15f, 2.05f, Mathf.Clamp01(confidence)), 2f, 8f);
        }

        public static float KeypointRadius(float stroke, float confidence)
        {
            return Mathf.Clamp(stroke * Mathf.Lerp(2.3f, 3.05f, Mathf.Clamp01(confidence)), 4.5f, 10f);
        }

        public static float ContourCoreThickness(float stroke)
        {
            return Mathf.Clamp(stroke * 1.55f, 2.4f, 6.5f);
        }

        public static float HaloThickness(float coreThickness)
        {
            return Mathf.Clamp(coreThickness * 1.9f, coreThickness + 2f, 12f);
        }

        public static string DetectionTooltip(VisionDetection detection)
        {
            string state = detection.trackState == VisionTrackState.None ? "untracked" : detection.trackState.ToString();
            return $"class {detection.classId} | confidence {Mathf.Clamp01(detection.confidence):P0} | {state}";
        }

        public static string MaskTooltip(VisionMask mask)
        {
            string contour = mask.HasContour ? "contour" : "bounds";
            string track = mask.trackId >= 0 ? $"track {mask.trackId}" : "untracked";
            return $"class {mask.classId} | confidence {Mathf.Clamp01(mask.confidence):P0} | {track} | {contour}";
        }

        private static bool IsHumanDetection(VisionDetection detection)
        {
            return detection.classId == 0 ||
                   string.Equals(detection.label, "person", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(detection.label, "human", StringComparison.OrdinalIgnoreCase);
        }

        private static Color PoseSemanticColor(string name, int stableId)
        {
            if (ContainsToken(name, "left"))
                return LeftPoseColor;

            if (ContainsToken(name, "right"))
                return RightPoseColor;

            if (ContainsToken(name, "nose") || ContainsToken(name, "eye") || ContainsToken(name, "ear") ||
                ContainsToken(name, "shoulder") || ContainsToken(name, "hip") || ContainsToken(name, "torso"))
            {
                return CenterPoseColor;
            }

            return VisionDashboardGeometry.StableColor(stableId, 0.62f, 0.96f);
        }

        private static bool ContainsToken(string value, string token)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
