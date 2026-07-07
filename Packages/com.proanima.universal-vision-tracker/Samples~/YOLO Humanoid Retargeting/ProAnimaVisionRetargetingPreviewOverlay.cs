using UniversalTracker.Core;
using UnityEngine;
using UnityEngine.UI;

namespace UniversalTracker.Samples
{
    public sealed class ProAnimaVisionRetargetingPreviewOverlay : MaskableGraphic
    {
        [SerializeField]
        private float lineThickness = 4f;

        [SerializeField]
        private float keypointSize = 8f;

        [SerializeField]
        private float minimumConfidence = 0.05f;

        [SerializeField]
        private Color boneColor = new Color(0.04f, 1f, 0.72f, 0.95f);

        [SerializeField]
        private Color predictedColor = new Color(1f, 0.78f, 0.15f, 0.95f);

        [SerializeField]
        private Color boxColor = new Color(0.2f, 0.8f, 1f, 0.85f);

        private VisionPose pose;
        private VisionDetection detection;
        private bool hasPose;
        private bool hasDetection;

        public void SetResult(VisionFrameResult result)
        {
            hasPose = result?.poses != null && result.poses.Length > 0;
            pose = hasPose ? result.poses[0] : default;
            hasDetection = result?.detections != null && result.detections.Length > 0;
            detection = hasDetection ? result.detections[0] : default;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();

            if (hasDetection)
                DrawDetection(vh, rect, detection.normalizedRect);

            if (!hasPose || pose.keypoints == null)
                return;

            VisionSkeletonBone[] bones = pose.skeleton.bones ?? FallbackCocoBones;
            for (int i = 0; i < bones.Length; i++)
                DrawBone(vh, rect, bones[i]);

            for (int i = 0; i < pose.keypoints.Length; i++)
                DrawKeypoint(vh, rect, pose.keypoints[i]);
        }

        private void DrawDetection(VertexHelper vh, Rect rect, Rect normalized)
        {
            Vector2 min = ToLocal(rect, new Vector2(normalized.xMin, normalized.yMin));
            Vector2 max = ToLocal(rect, new Vector2(normalized.xMax, normalized.yMax));
            Vector2 topLeft = new Vector2(min.x, min.y);
            Vector2 topRight = new Vector2(max.x, min.y);
            Vector2 bottomRight = new Vector2(max.x, max.y);
            Vector2 bottomLeft = new Vector2(min.x, max.y);

            DrawLine(vh, topLeft, topRight, lineThickness, boxColor);
            DrawLine(vh, topRight, bottomRight, lineThickness, boxColor);
            DrawLine(vh, bottomRight, bottomLeft, lineThickness, boxColor);
            DrawLine(vh, bottomLeft, topLeft, lineThickness, boxColor);
        }

        private void DrawBone(VertexHelper vh, Rect rect, VisionSkeletonBone bone)
        {
            if (!TryGetKeypoint(bone.from, out VisionKeypoint from) ||
                !TryGetKeypoint(bone.to, out VisionKeypoint to))
            {
                return;
            }

            Color color = from.confidence < 0.35f || to.confidence < 0.35f ? predictedColor : boneColor;
            DrawLine(vh, ToLocal(rect, from.normalizedPosition), ToLocal(rect, to.normalizedPosition), lineThickness, color);
        }

        private void DrawKeypoint(VertexHelper vh, Rect rect, VisionKeypoint keypoint)
        {
            if (!IsVisible(keypoint))
                return;

            Color color = keypoint.confidence < 0.35f ? predictedColor : Color.white;
            Vector2 center = ToLocal(rect, keypoint.normalizedPosition);
            float half = keypointSize * 0.5f;
            AddQuad(vh, center + new Vector2(-half, -half), center + new Vector2(half, half), color);
        }

        private bool TryGetKeypoint(int index, out VisionKeypoint keypoint)
        {
            if (index >= 0 && index < pose.keypoints.Length && IsVisible(pose.keypoints[index]))
            {
                keypoint = pose.keypoints[index];
                return true;
            }

            keypoint = default;
            return false;
        }

        private bool IsVisible(VisionKeypoint keypoint)
        {
            return keypoint.isVisible && keypoint.confidence >= minimumConfidence;
        }

        private static Vector2 ToLocal(Rect rect, Vector2 normalized)
        {
            return new Vector2(
                rect.xMin + normalized.x * rect.width,
                rect.yMin + (1f - normalized.y) * rect.height);
        }

        private static void DrawLine(VertexHelper vh, Vector2 from, Vector2 to, float thickness, Color color)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < 0.001f)
                return;

            Vector2 normal = new Vector2(-delta.y, delta.x).normalized * (thickness * 0.5f);
            AddQuad(vh, from - normal, from + normal, to + normal, to - normal, color);
        }

        private static void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Color color)
        {
            AddQuad(
                vh,
                new Vector2(min.x, min.y),
                new Vector2(min.x, max.y),
                new Vector2(max.x, max.y),
                new Vector2(max.x, min.y),
                color);
        }

        private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int index = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero);
            vh.AddVert(c, color, Vector2.zero);
            vh.AddVert(d, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
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
