using System.Collections.Generic;
using UniversalTracker.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalTracker.Samples
{
    public sealed class ProAnimaVisionRetargetingPreviewOverlay : VisualElement
    {
        private const float BoneThickness = 4f;
        private const float KeypointSize = 8f;
        private const float MinimumConfidence = 0.05f;
        private readonly VisualElement contentGuide;
        private readonly VisualElement detectionLayer;
        private readonly VisualElement boneLayer;
        private readonly VisualElement keypointLayer;
        private readonly List<VisualElement> detections = new List<VisualElement>();
        private readonly List<VisualElement> bones = new List<VisualElement>();
        private readonly List<VisualElement> keypoints = new List<VisualElement>();
        private VisionFrameResult result;

        public ProAnimaVisionRetargetingPreviewOverlay()
        {
            name = "RetargetingPoseOverlay";
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.right = 0;
            style.top = 0;
            style.bottom = 0;

            contentGuide = CreateLayer("SourceContentGuide");
            contentGuide.style.borderTopWidth = 1;
            contentGuide.style.borderRightWidth = 1;
            contentGuide.style.borderBottomWidth = 1;
            contentGuide.style.borderLeftWidth = 1;
            SetBorder(contentGuide, new Color(1f, 1f, 1f, 0.16f));
            Add(contentGuide);

            detectionLayer = CreateLayer("SourceDetections");
            boneLayer = CreateLayer("SourceBones");
            keypointLayer = CreateLayer("SourceKeypoints");
            Add(detectionLayer);
            Add(boneLayer);
            Add(keypointLayer);

            RegisterCallback<GeometryChangedEvent>(_ => Redraw());
        }

        public void SetResult(VisionFrameResult value)
        {
            result = value;
            Redraw();
        }

        private void Redraw()
        {
            float width = resolvedStyle.width;
            float height = resolvedStyle.height;
            if (width <= 1f || height <= 1f)
                return;

            Rect content = CalculateContentRect(new Vector2(width, height), result?.sourceSize ?? Vector2Int.zero);
            ApplyRect(contentGuide, content);
            ApplyRect(detectionLayer, content);
            ApplyRect(boneLayer, content);
            ApplyRect(keypointLayer, content);

            int detectionCount = DrawDetections(content);
            int boneCount = DrawBones(content);
            int keypointCount = DrawKeypoints(content);
            SetPoolActive(detections, detectionCount);
            SetPoolActive(bones, boneCount);
            SetPoolActive(keypoints, keypointCount);
        }

        private int DrawDetections(Rect content)
        {
            if (result?.detections == null)
                return 0;

            int count = 0;
            for (int i = 0; i < result.detections.Length; i++)
            {
                VisionDetection detection = result.detections[i];
                VisualElement element = GetElement(detections, detectionLayer, CreateDetectionBox, count++);
                Rect box = ToRect(content, detection.normalizedRect);
                ApplyRect(element, box);
            }

            return count;
        }

        private int DrawBones(Rect content)
        {
            VisionPose pose = ResolvePose();
            if (pose.keypoints == null)
                return 0;

            VisionSkeletonBone[] skeletonBones = pose.skeleton.bones ?? FallbackCocoBones;
            int count = 0;
            for (int i = 0; i < skeletonBones.Length; i++)
            {
                VisionSkeletonBone bone = skeletonBones[i];
                if (!TryGetKeypoint(pose, bone.from, out VisionKeypoint from) ||
                    !TryGetKeypoint(pose, bone.to, out VisionKeypoint to))
                {
                    continue;
                }

                VisualElement element = GetElement(bones, boneLayer, CreateBone, count++);
                Color color = from.confidence < 0.35f || to.confidence < 0.35f
                    ? new Color(1f, 0.78f, 0.15f, 0.96f)
                    : new Color(0.04f, 1f, 0.72f, 0.96f);
                ApplyLine(element, ToLocal(content, from.normalizedPosition), ToLocal(content, to.normalizedPosition), color);
            }

            return count;
        }

        private int DrawKeypoints(Rect content)
        {
            VisionPose pose = ResolvePose();
            if (pose.keypoints == null)
                return 0;

            int count = 0;
            for (int i = 0; i < pose.keypoints.Length; i++)
            {
                VisionKeypoint keypoint = pose.keypoints[i];
                if (!IsVisible(keypoint))
                    continue;

                VisualElement element = GetElement(keypoints, keypointLayer, CreateKeypoint, count++);
                Color color = keypoint.confidence < 0.35f ? new Color(1f, 0.78f, 0.15f, 1f) : Color.white;
                element.style.backgroundColor = color;
                Vector2 center = ToLocal(content, keypoint.normalizedPosition);
                ApplyRect(element, new Rect(center.x - KeypointSize * 0.5f, center.y - KeypointSize * 0.5f, KeypointSize, KeypointSize));
            }

            return count;
        }

        private VisionPose ResolvePose()
        {
            return result?.poses != null && result.poses.Length > 0 ? result.poses[0] : default;
        }

        private static bool TryGetKeypoint(VisionPose pose, int index, out VisionKeypoint keypoint)
        {
            if (index >= 0 && index < pose.keypoints.Length && IsVisible(pose.keypoints[index]))
            {
                keypoint = pose.keypoints[index];
                return true;
            }

            keypoint = default;
            return false;
        }

        private static bool IsVisible(VisionKeypoint keypoint)
        {
            return keypoint.isVisible && keypoint.confidence >= MinimumConfidence;
        }

        private static Rect CalculateContentRect(Vector2 container, Vector2Int sourceSize)
        {
            if (sourceSize.x <= 0 || sourceSize.y <= 0)
                return new Rect(0f, 0f, container.x, container.y);

            float scale = Mathf.Min(container.x / sourceSize.x, container.y / sourceSize.y);
            float width = sourceSize.x * scale;
            float height = sourceSize.y * scale;
            return new Rect((container.x - width) * 0.5f, (container.y - height) * 0.5f, width, height);
        }

        private static Rect ToRect(Rect content, Rect normalized)
        {
            Vector2 min = ToLocal(content, new Vector2(normalized.xMin, normalized.yMin));
            Vector2 max = ToLocal(content, new Vector2(normalized.xMax, normalized.yMax));
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Vector2 ToLocal(Rect content, Vector2 normalized)
        {
            return new Vector2(
                content.xMin + normalized.x * content.width,
                content.yMin + (1f - normalized.y) * content.height);
        }

        private static void ApplyLine(VisualElement element, Vector2 from, Vector2 to, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length <= 0.001f)
            {
                element.style.display = DisplayStyle.None;
                return;
            }

            element.style.display = DisplayStyle.Flex;
            element.style.left = from.x;
            element.style.top = from.y - BoneThickness * 0.5f;
            element.style.width = length;
            element.style.height = BoneThickness;
            element.style.backgroundColor = color;
            element.style.rotate = new Rotate(Angle.Degrees(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg));
            element.style.transformOrigin = new TransformOrigin(0f, 50f, 0f);
        }

        private static void ApplyRect(VisualElement element, Rect rect)
        {
            element.style.display = DisplayStyle.Flex;
            element.style.position = Position.Absolute;
            element.style.left = rect.xMin;
            element.style.top = rect.yMin;
            element.style.width = Mathf.Max(0f, rect.width);
            element.style.height = Mathf.Max(0f, rect.height);
        }

        private static VisualElement GetElement(List<VisualElement> pool, VisualElement parent, System.Func<VisualElement> factory, int index)
        {
            while (pool.Count <= index)
            {
                VisualElement element = factory();
                pool.Add(element);
                parent.Add(element);
            }

            return pool[index];
        }

        private static void SetPoolActive(List<VisualElement> pool, int activeCount)
        {
            for (int i = 0; i < pool.Count; i++)
                pool[i].style.display = i < activeCount ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static VisualElement CreateLayer(string name)
        {
            var layer = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            layer.style.position = Position.Absolute;
            return layer;
        }

        private static VisualElement CreateDetectionBox()
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.style.borderTopWidth = 2;
            box.style.borderRightWidth = 2;
            box.style.borderBottomWidth = 2;
            box.style.borderLeftWidth = 2;
            SetBorder(box, new Color(0.2f, 0.8f, 1f, 0.88f));
            return box;
        }

        private static VisualElement CreateBone()
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            return element;
        }

        private static VisualElement CreateKeypoint()
        {
            var point = new VisualElement { pickingMode = PickingMode.Ignore };
            point.style.borderTopLeftRadius = KeypointSize * 0.5f;
            point.style.borderTopRightRadius = KeypointSize * 0.5f;
            point.style.borderBottomLeftRadius = KeypointSize * 0.5f;
            point.style.borderBottomRightRadius = KeypointSize * 0.5f;
            return point;
        }

        private static void SetBorder(VisualElement element, Color color)
        {
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
        }

        private static readonly VisionSkeletonBone[] FallbackCocoBones =
        {
            Bone(5, 7), Bone(7, 9), Bone(6, 8), Bone(8, 10),
            Bone(5, 6), Bone(5, 11), Bone(6, 12), Bone(11, 12),
            Bone(11, 13), Bone(13, 15), Bone(12, 14), Bone(14, 16)
        };

        private static VisionSkeletonBone Bone(int from, int to)
        {
            return new VisionSkeletonBone { from = from, to = to };
        }
    }
}
