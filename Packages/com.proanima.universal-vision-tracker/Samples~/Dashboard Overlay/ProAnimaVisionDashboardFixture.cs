using UnityEngine;
using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;

namespace ProAnimaVision.Samples
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VisionToolkitDashboardReceiver))]
    public sealed class ProAnimaVisionDashboardFixture : MonoBehaviour
    {
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
                    new VisionMask
                    {
                        trackId = 12,
                        classId = 0,
                        label = "person",
                        confidence = 0.88f,
                        normalizedRect = personRect
                    }
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
                        Bone(5, 6, "shoulders"),
                        Bone(5, 11, "left_torso"),
                        Bone(6, 12, "right_torso"),
                        Bone(11, 12, "hips")
                    }
                },
                trackState = VisionTrackState.Tracking
            };
        }

        private static VisionKeypoint[] CreateKeypoints(Rect rect)
        {
            var keypoints = new VisionKeypoint[17];
            keypoints[5] = Keypoint(5, "left_shoulder", rect, 0.35f, 0.24f);
            keypoints[6] = Keypoint(6, "right_shoulder", rect, 0.65f, 0.24f);
            keypoints[11] = Keypoint(11, "left_hip", rect, 0.42f, 0.66f);
            keypoints[12] = Keypoint(12, "right_hip", rect, 0.58f, 0.66f);
            return keypoints;
        }

        private static VisionKeypoint Keypoint(int index, string name, Rect rect, float x, float y)
        {
            Vector2 normalized = new Vector2(rect.x + rect.width * x, rect.y + rect.height * y);
            return new VisionKeypoint
            {
                index = index,
                name = name,
                normalizedPosition = normalized,
                sourcePosition = new Vector2(normalized.x * 640f, normalized.y * 480f),
                confidence = 0.92f,
                isVisible = true
            };
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
