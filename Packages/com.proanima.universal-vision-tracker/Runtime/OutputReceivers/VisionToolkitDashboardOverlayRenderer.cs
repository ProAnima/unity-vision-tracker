using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalTracker.Core;
using UniversalTracker.Visualization;

namespace UniversalTracker.OutputReceivers
{
    internal static class VisionDashboardElementPool
    {
        public static VisualElement GetElement(List<VisualElement> pool, VisualElement parent, Func<VisualElement> factory, int index)
        {
            while (pool.Count <= index)
            {
                VisualElement element = factory();
                parent.Add(element);
                pool.Add(element);
            }

            pool[index].style.display = DisplayStyle.Flex;
            return pool[index];
        }

        public static Label GetLabel(List<Label> pool, VisualElement parent, int index)
        {
            while (pool.Count <= index)
            {
                Label label = VisionToolkitDashboardPrimitives.CreateOverlayLabel();
                parent.Add(label);
                pool.Add(label);
            }

            pool[index].style.display = DisplayStyle.Flex;
            return pool[index];
        }

        public static void SetPoolActive(List<VisualElement> pool, int activeCount)
        {
            for (int i = 0; i < pool.Count; i++)
                pool[i].style.display = i < activeCount ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void SetLabelsActive(List<Label> pool, int activeCount)
        {
            for (int i = 0; i < pool.Count; i++)
                pool[i].style.display = i < activeCount ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    internal sealed class VisionDashboardOverlayState
    {
        public VisualElement maskLayer;
        public VisualElement detectionLayer;
        public VisualElement boneLayer;
        public VisualElement keypointLayer;
        public VisualElement labelLayer;
        public readonly List<VisualElement> masks = new List<VisualElement>();
        public readonly List<VisualElement> detections = new List<VisualElement>();
        public readonly List<Label> labels = new List<Label>();
        public readonly List<VisualElement> keypoints = new List<VisualElement>();
        public readonly List<VisualElement> bones = new List<VisualElement>();
    }

    internal static class VisionToolkitDashboardOverlayRenderer
    {
        public static void Clear(VisionDashboardOverlayState state)
        {
            VisionDashboardElementPool.SetPoolActive(state.masks, 0);
            VisionDashboardElementPool.SetPoolActive(state.detections, 0);
            VisionDashboardElementPool.SetLabelsActive(state.labels, 0);
            VisionDashboardElementPool.SetPoolActive(state.keypoints, 0);
            VisionDashboardElementPool.SetPoolActive(state.bones, 0);
        }

        public static void Render(
            VisionFrameResult result,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            bool showMasks,
            bool showDetections,
            bool showPoses,
            float maskAlpha,
            float keypointConfidenceThreshold)
        {
            int masksUsed = RenderMasks(result, state, sourceSize, viewportSize, stroke, showMasks, maskAlpha);
            int detectionsUsed = RenderDetections(result, state, sourceSize, viewportSize, stroke, showDetections);
            RenderPoses(
                result,
                state,
                sourceSize,
                viewportSize,
                stroke,
                showPoses,
                keypointConfidenceThreshold,
                out int keypointsUsed,
                out int bonesUsed);

            VisionDashboardElementPool.SetPoolActive(state.masks, masksUsed);
            VisionDashboardElementPool.SetPoolActive(state.detections, detectionsUsed);
            VisionDashboardElementPool.SetLabelsActive(state.labels, detectionsUsed);
            VisionDashboardElementPool.SetPoolActive(state.keypoints, keypointsUsed);
            VisionDashboardElementPool.SetPoolActive(state.bones, bonesUsed);
        }

        private static int RenderMasks(
            VisionFrameResult result,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            bool showMasks,
            float maskAlpha)
        {
            int used = 0;
            if (!showMasks || result.masks == null)
                return used;

            for (int i = 0; i < result.masks.Length; i++)
            {
                if (result.masks[i].texture == null)
                    continue;

                VisualElement element = VisionDashboardElementPool.GetElement(
                    state.masks,
                    state.maskLayer,
                    VisionToolkitDashboardPrimitives.CreateMaskOverlay,
                    used);
                UpdateMask(element, used++, result.masks[i], sourceSize, viewportSize, stroke, maskAlpha);
            }

            return used;
        }

        private static int RenderDetections(
            VisionFrameResult result,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            bool showDetections)
        {
            int used = 0;
            if (!showDetections || result.detections == null)
                return used;

            for (int i = 0; i < result.detections.Length; i++)
            {
                VisualElement box = VisionDashboardElementPool.GetElement(
                    state.detections,
                    state.detectionLayer,
                    VisionToolkitDashboardPrimitives.CreateDetectionBox,
                    used);
                Label label = VisionDashboardElementPool.GetLabel(state.labels, state.labelLayer, used);
                UpdateDetectionBox(box, label, used++, result.detections[i], sourceSize, viewportSize, stroke);
            }

            return used;
        }

        private static void RenderPoses(
            VisionFrameResult result,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            bool showPoses,
            float keypointConfidenceThreshold,
            out int keypointsUsed,
            out int bonesUsed)
        {
            keypointsUsed = 0;
            bonesUsed = 0;
            if (!showPoses || result.poses == null)
                return;

            for (int i = 0; i < result.poses.Length; i++)
                UpdatePose(result.poses[i], state, sourceSize, viewportSize, stroke, keypointConfidenceThreshold, ref keypointsUsed, ref bonesUsed);
        }

        private static void UpdateDetectionBox(VisualElement box, Label label, int index, VisionDetection detection, Vector2 sourceSize, Vector2 viewportSize, float stroke)
        {
            Rect rect = VisionDashboardGeometry.NormalizedToViewportRect(detection.normalizedRect, sourceSize, viewportSize);
            box.style.left = rect.x;
            box.style.top = rect.y;
            box.style.width = Mathf.Max(1f, rect.width);
            box.style.height = Mathf.Max(1f, rect.height);
            box.style.borderTopWidth = stroke;
            box.style.borderRightWidth = stroke;
            box.style.borderBottomWidth = stroke;
            box.style.borderLeftWidth = stroke;

            string name = string.IsNullOrWhiteSpace(detection.label) ? $"#{detection.classId}" : detection.label;
            string id = detection.IsTracked ? $" T{detection.trackId}" : string.Empty;
            label.text = $"{name} {(detection.confidence * 100f):F0}%{id}";

            int stableId = detection.IsTracked ? detection.trackId : detection.classId + index * 31;
            Color color = VisionDashboardGeometry.StableColor(stableId);
            VisionDashboardTheme.SetBorderColor(box, color);
            box.style.backgroundColor = new Color(color.r, color.g, color.b, 0.06f);

            Vector2 labelSize = new Vector2(Mathf.Clamp(label.text.Length * 7.5f + 18f, 86f, 260f), 24f);
            Vector2 labelPosition = VisionDashboardGeometry.ClampLabelPosition(rect, labelSize, viewportSize);
            label.style.left = labelPosition.x;
            label.style.top = labelPosition.y;
            label.style.width = labelSize.x;
            label.style.height = labelSize.y;
            label.style.backgroundColor = new Color(color.r, color.g, color.b, 0.9f);
            label.style.color = Color.black;
        }

        private static void UpdatePose(
            VisionPose pose,
            VisionDashboardOverlayState state,
            Vector2 sourceSize,
            Vector2 viewportSize,
            float stroke,
            float keypointConfidenceThreshold,
            ref int keypointsUsed,
            ref int bonesUsed)
        {
            if (pose.keypoints == null)
                return;

            if (pose.skeleton.bones != null)
            {
                for (int i = 0; i < pose.skeleton.bones.Length; i++)
                {
                    VisionSkeletonBone bone = pose.skeleton.bones[i];
                    if (!TryGetVisibleKeypoint(pose, bone.from, keypointConfidenceThreshold, out VisionKeypoint from) ||
                        !TryGetVisibleKeypoint(pose, bone.to, keypointConfidenceThreshold, out VisionKeypoint to))
                    {
                        continue;
                    }

                    Vector2 fromPoint = VisionDashboardGeometry.NormalizedToViewportPoint(from.normalizedPosition, sourceSize, viewportSize);
                    Vector2 toPoint = VisionDashboardGeometry.NormalizedToViewportPoint(to.normalizedPosition, sourceSize, viewportSize);
                    VisualElement boneElement = VisionDashboardElementPool.GetElement(
                        state.bones,
                        state.boneLayer,
                        VisionToolkitDashboardPrimitives.CreateBone,
                        bonesUsed);
                    UpdateBone(boneElement, bonesUsed++, fromPoint, toPoint, from.confidence, to.confidence, stroke);
                }
            }

            for (int i = 0; i < pose.keypoints.Length; i++)
            {
                if (!IsVisibleKeypoint(pose.keypoints[i], keypointConfidenceThreshold))
                    continue;

                Vector2 point = VisionDashboardGeometry.NormalizedToViewportPoint(pose.keypoints[i].normalizedPosition, sourceSize, viewportSize);
                VisualElement keypoint = VisionDashboardElementPool.GetElement(
                    state.keypoints,
                    state.keypointLayer,
                    VisionToolkitDashboardPrimitives.CreateKeypoint,
                    keypointsUsed);
                UpdateKeypoint(keypoint, point, pose.keypoints[i], stroke);
                keypointsUsed++;
            }
        }

        private static void UpdateBone(VisualElement bone, int index, Vector2 from, Vector2 to, float fromConfidence, float toConfidence, float stroke)
        {
            BoneLine line = VisionDashboardGeometry.CalculateBoneLine(from, to);
            float confidence = Mathf.Clamp01(Mathf.Min(fromConfidence, toConfidence));
            bone.style.left = line.center.x - line.length * 0.5f;
            bone.style.top = line.center.y - stroke * 0.5f;
            bone.style.width = Mathf.Max(1f, line.length);
            bone.style.height = Mathf.Max(2f, stroke);
            bone.style.rotate = new Rotate(new Angle(line.angleDegrees, AngleUnit.Degree));
            Color color = index % 2 == 0 ? VisionDashboardTheme.PoseColor : VisionDashboardTheme.Accent;
            bone.style.backgroundColor = new Color(color.r, color.g, color.b, Mathf.Lerp(0.38f, 0.95f, confidence));
        }

        private static void UpdateKeypoint(VisualElement keypoint, Vector2 point, VisionKeypoint data, float stroke)
        {
            float radius = Mathf.Clamp(stroke * 2.2f, 4f, 8f);
            keypoint.style.left = point.x - radius;
            keypoint.style.top = point.y - radius;
            keypoint.style.width = radius * 2f;
            keypoint.style.height = radius * 2f;
            keypoint.style.backgroundColor = data.confidence >= 0.5f ? VisionDashboardTheme.PoseColor : VisionDashboardTheme.Warning;
            VisionDashboardTheme.SetBorderColor(keypoint, new Color(0f, 0f, 0f, 0.8f));
        }

        private static void UpdateMask(VisualElement element, int index, VisionMask mask, Vector2 sourceSize, Vector2 viewportSize, float stroke, float maskAlpha)
        {
            Rect rect = VisionDashboardGeometry.NormalizedToViewportRect(mask.normalizedRect, sourceSize, viewportSize);
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = Mathf.Max(1f, rect.width);
            element.style.height = Mathf.Max(1f, rect.height);
            element.style.borderTopWidth = stroke;
            element.style.borderRightWidth = stroke;
            element.style.borderBottomWidth = stroke;
            element.style.borderLeftWidth = stroke;

            int stableId = mask.trackId >= 0 ? mask.trackId : mask.classId + index * 37;
            Color color = VisionDashboardGeometry.StableColor(stableId, 0.68f, 0.88f);
            VisionDashboardTheme.SetBorderColor(element, new Color(color.r, color.g, color.b, 0.95f));
            element.style.backgroundColor = new Color(color.r, color.g, color.b, maskAlpha);

            var image = element.Q<Image>();
            if (image != null)
            {
                image.image = mask.texture;
                image.tintColor = new Color(1f, 1f, 1f, mask.texture != null ? Mathf.Clamp01(maskAlpha + 0.12f) : 0f);
                image.style.display = mask.texture != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static bool TryGetVisibleKeypoint(VisionPose pose, int index, float threshold, out VisionKeypoint keypoint)
        {
            keypoint = default;
            if (pose.keypoints == null || index < 0 || index >= pose.keypoints.Length)
                return false;

            keypoint = pose.keypoints[index];
            return IsVisibleKeypoint(keypoint, threshold);
        }

        private static bool IsVisibleKeypoint(VisionKeypoint keypoint, float threshold)
        {
            return keypoint.isVisible && keypoint.confidence >= threshold;
        }
    }
}
