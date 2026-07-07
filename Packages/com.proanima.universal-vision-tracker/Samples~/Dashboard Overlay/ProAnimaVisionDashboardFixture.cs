using UnityEngine;
using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;

namespace ProAnimaVision.Samples
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VisionToolkitDashboardReceiver))]
    public sealed class ProAnimaVisionDashboardFixture : MonoBehaviour
    {
        [Tooltip("Synthetic result update rate used by the dashboard sample.")]
        [Range(0.1f, 5f)] public float updateRate = 1f;

        private VisionToolkitDashboardReceiver dashboard;
        private Texture2D previewTexture;
        private float nextUpdateTime;
        private int frameIndex;

        private void Awake()
        {
            dashboard = GetComponent<VisionToolkitDashboardReceiver>();
            dashboard.autoFindManager = false;
            dashboard.subscribeToManagerEvent = false;
            previewTexture = CreatePreviewTexture();
        }

        private void OnDestroy()
        {
            if (previewTexture != null)
                Destroy(previewTexture);
        }

        private void Update()
        {
            if (Time.time < nextUpdateTime)
                return;

            nextUpdateTime = Time.time + 1f / Mathf.Max(0.1f, updateRate);
            dashboard.ReceiveVisionResult(CreateResult(), previewTexture);
        }

        private VisionFrameResult CreateResult()
        {
            float phase = Mathf.PingPong(Time.time * 0.12f, 0.3f);
            Rect personRect = new Rect(0.25f + phase, 0.18f, 0.28f, 0.62f);
            Rect propRect = new Rect(0.62f - phase * 0.4f, 0.55f, 0.18f, 0.16f);

            return new VisionFrameResult
            {
                frameIndex = frameIndex++,
                timestamp = Time.timeAsDouble,
                sourceSize = new Vector2Int(640, 480),
                detections = new[]
                {
                    Detection(12, 0, "person", 0.93f, personRect),
                    Detection(41, 1, "prop", 0.81f, propRect)
                },
                poses = new[]
                {
                    Pose(12, personRect)
                },
                masks = new[]
                {
                    Mask(12, 0, "person", 0.88f, personRect)
                },
                stats = VisionPerformanceStats.FromStages(0.5f, 7.8f, 1.2f, 0.4f)
            };
        }

        private static VisionDetection Detection(int trackId, int classId, string label, float confidence, Rect rect)
        {
            return new VisionDetection
            {
                trackId = trackId,
                classId = classId,
                label = label,
                confidence = confidence,
                normalizedRect = rect,
                sourceRect = new Rect(rect.x * 640f, rect.y * 480f, rect.width * 640f, rect.height * 480f),
                sourceCenter = new Vector2(rect.center.x * 640f, rect.center.y * 480f),
                trackState = VisionTrackState.Tracking
            };
        }

        private static VisionPose Pose(int personId, Rect rect)
        {
            return new VisionPose
            {
                personId = personId,
                confidence = 0.9f,
                normalizedRect = rect,
                keypoints = CreateKeypoints(rect),
                skeleton = new VisionSkeletonDefinition
                {
                    name = "sample-COCO",
                    bones = new[]
                    {
                        Bone(0, 1, "left_face"),
                        Bone(0, 2, "right_face"),
                        Bone(1, 3, "left_ear"),
                        Bone(2, 4, "right_ear"),
                        Bone(5, 7, "left_upper_arm"),
                        Bone(7, 9, "left_lower_arm"),
                        Bone(6, 8, "right_upper_arm"),
                        Bone(8, 10, "right_lower_arm"),
                        Bone(5, 6, "shoulders"),
                        Bone(5, 11, "left_torso"),
                        Bone(6, 12, "right_torso"),
                        Bone(11, 12, "hips"),
                        Bone(11, 13, "left_upper_leg"),
                        Bone(13, 15, "left_lower_leg"),
                        Bone(12, 14, "right_upper_leg"),
                        Bone(14, 16, "right_lower_leg")
                    }
                },
                trackState = VisionTrackState.Tracking
            };
        }

        private static VisionMask Mask(int trackId, int classId, string label, float confidence, Rect rect)
        {
            return new VisionMask
            {
                trackId = trackId,
                classId = classId,
                label = label,
                confidence = confidence,
                normalizedRect = rect,
                sourceRect = new Rect(rect.x * 640f, rect.y * 480f, rect.width * 640f, rect.height * 480f),
                normalizedContour = CreatePersonContour(rect)
            };
        }

        private static VisionKeypoint[] CreateKeypoints(Rect rect)
        {
            var keypoints = new VisionKeypoint[17];
            keypoints[0] = Keypoint(0, "nose", rect, 0.5f, 0.12f, 0.96f);
            keypoints[1] = Keypoint(1, "left_eye", rect, 0.46f, 0.1f, 0.9f);
            keypoints[2] = Keypoint(2, "right_eye", rect, 0.54f, 0.1f, 0.9f);
            keypoints[3] = Keypoint(3, "left_ear", rect, 0.4f, 0.14f, 0.82f);
            keypoints[4] = Keypoint(4, "right_ear", rect, 0.6f, 0.14f, 0.82f);
            keypoints[5] = Keypoint(5, "left_shoulder", rect, 0.35f, 0.25f, 0.94f);
            keypoints[6] = Keypoint(6, "right_shoulder", rect, 0.65f, 0.25f, 0.94f);
            keypoints[7] = Keypoint(7, "left_elbow", rect, 0.28f, 0.43f, 0.88f);
            keypoints[8] = Keypoint(8, "right_elbow", rect, 0.72f, 0.42f, 0.88f);
            keypoints[9] = Keypoint(9, "left_wrist", rect, 0.31f, 0.58f, 0.8f);
            keypoints[10] = Keypoint(10, "right_wrist", rect, 0.76f, 0.55f, 0.8f);
            keypoints[11] = Keypoint(11, "left_hip", rect, 0.42f, 0.66f, 0.92f);
            keypoints[12] = Keypoint(12, "right_hip", rect, 0.58f, 0.66f, 0.92f);
            keypoints[13] = Keypoint(13, "left_knee", rect, 0.39f, 0.84f, 0.86f);
            keypoints[14] = Keypoint(14, "right_knee", rect, 0.61f, 0.84f, 0.86f);
            keypoints[15] = Keypoint(15, "left_ankle", rect, 0.36f, 0.98f, 0.78f);
            keypoints[16] = Keypoint(16, "right_ankle", rect, 0.64f, 0.98f, 0.78f);
            return keypoints;
        }

        private static VisionKeypoint Keypoint(int index, string name, Rect rect, float x, float y, float confidence)
        {
            Vector2 normalized = new Vector2(rect.x + rect.width * x, rect.y + rect.height * y);
            return new VisionKeypoint
            {
                index = index,
                name = name,
                normalizedPosition = normalized,
                sourcePosition = new Vector2(normalized.x * 640f, normalized.y * 480f),
                confidence = confidence,
                isVisible = true
            };
        }

        private static Vector2[] CreatePersonContour(Rect rect)
        {
            return new[]
            {
                ContourPoint(rect, 0.5f, 0.04f),
                ContourPoint(rect, 0.66f, 0.14f),
                ContourPoint(rect, 0.71f, 0.35f),
                ContourPoint(rect, 0.84f, 0.52f),
                ContourPoint(rect, 0.72f, 0.62f),
                ContourPoint(rect, 0.62f, 0.55f),
                ContourPoint(rect, 0.59f, 0.96f),
                ContourPoint(rect, 0.49f, 0.99f),
                ContourPoint(rect, 0.42f, 0.56f),
                ContourPoint(rect, 0.31f, 0.65f),
                ContourPoint(rect, 0.2f, 0.54f),
                ContourPoint(rect, 0.3f, 0.36f),
                ContourPoint(rect, 0.34f, 0.14f)
            };
        }

        private static Vector2 ContourPoint(Rect rect, float x, float y)
        {
            return new Vector2(rect.x + rect.width * x, rect.y + rect.height * y);
        }

        private static VisionSkeletonBone Bone(int from, int to, string name)
        {
            return new VisionSkeletonBone { from = from, to = to, name = name };
        }

        private static Texture2D CreatePreviewTexture()
        {
            var texture = new Texture2D(64, 48, TextureFormat.RGBA32, false);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float t = (float)x / Mathf.Max(1, texture.width - 1);
                    texture.SetPixel(x, y, Color.Lerp(new Color(0.08f, 0.1f, 0.12f), new Color(0.18f, 0.22f, 0.28f), t));
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
