using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;
using UnityEngine;

namespace UniversalTracker.Samples
{
    public sealed class ProAnimaVisionYoloHumanoidRetargetingDemo : MonoBehaviour
    {
        [SerializeField]
        private float animationSpeed = 1.2f;

        [SerializeField]
        private float rigRadius = 0.045f;

        [SerializeField]
        private Material bodyMaterial;

        [SerializeField]
        private Material predictedMaterial;

        private ProAnimaVisionGeneratedHumanoidRig rig;
        private ProAnimaVisionRetargetingDemoView view;
        private VisionHumanoidRigReceiver receiver;
        private float time;

        private void Awake()
        {
            ConfigureCamera();
            bodyMaterial ??= CreateMaterial(new Color(0.18f, 0.68f, 0.95f));
            predictedMaterial ??= CreateMaterial(new Color(1f, 0.76f, 0.18f));
            rig = ProAnimaVisionGeneratedHumanoidRig.Create(transform, bodyMaterial, predictedMaterial, rigRadius);
            view = new ProAnimaVisionRetargetingDemoView(transform, 1280, 720, 30);
            view.Initialize();

            receiver = gameObject.AddComponent<VisionHumanoidRigReceiver>();
            receiver.poseSpaceRoot = transform;
            receiver.bindings = rig.Bindings;
            receiver.blend = 1f;
            receiver.positionScale = 1f;
            receiver.driveRootPosition = false;
            receiver.driveJointPositions = false;
            receiver.driveBoneRotations = true;
            receiver.retargetingOptions = new VisionPoseRetargetingOptions
            {
                keypointConfidenceThreshold = 0.35f,
                dropoutHoldSeconds = 0.18f,
                smoothing = 0.3f,
                bodyHeightMeters = 1.7f,
                minimumPoseQuality = 0.2f,
                missingJointConfidence = 0.14f
            };
            receiver.Initialize();
        }

        private void Update()
        {
            time += Time.deltaTime * animationSpeed;
            VisionFrameResult result = CreateFrameResult(CreateSyntheticCocoPose(time), view.CurrentTexture, view.SourceSize);
            receiver.ReceiveVisionResult(result, result.sourceTexture);
            rig.UpdateVisuals(receiver.LastHumanoidPose, receiver.HasLastHumanoidPose);
            view.Update(time, result);
        }

        private void OnDestroy()
        {
            view?.Dispose();
        }

        private static VisionFrameResult CreateFrameResult(VisionPose pose, Texture sourceTexture, Vector2Int sourceSize)
        {
            return new VisionFrameResult
            {
                frameIndex = Time.frameCount,
                timestamp = Time.realtimeSinceStartupAsDouble,
                sourceSize = sourceSize,
                sourceTexture = sourceTexture,
                detections = new[] { CreateDetection(pose, sourceSize) },
                poses = new[] { pose }
            };
        }

        private static VisionPose CreateSyntheticCocoPose(float time)
        {
            var keypoints = new VisionKeypoint[17];
            Set(keypoints, 0, "nose", 0.5f, 0.19f, 0.95f);
            Set(keypoints, 5, "left_shoulder", 0.39f, 0.31f, 0.95f);
            Set(keypoints, 6, "right_shoulder", 0.61f, 0.31f, 0.95f);
            Set(keypoints, 11, "left_hip", 0.43f, 0.55f, 0.95f);
            Set(keypoints, 12, "right_hip", 0.57f, 0.55f, 0.95f);

            float wave = Mathf.Sin(time) * 0.09f;
            float step = Mathf.Sin(time * 0.65f) * 0.035f;
            Set(keypoints, 7, "left_elbow", 0.33f, 0.43f + wave, 0.9f);
            Set(keypoints, 8, "right_elbow", 0.67f, 0.43f - wave, 0.9f);
            Set(keypoints, 9, "left_wrist", 0.27f, 0.57f + wave, WristConfidence(time));
            Set(keypoints, 10, "right_wrist", 0.73f, 0.57f - wave, 0.9f);
            Set(keypoints, 13, "left_knee", 0.42f - step, 0.76f, 0.9f);
            Set(keypoints, 14, "right_knee", 0.58f + step, 0.76f, 0.9f);
            Set(keypoints, 15, "left_ankle", 0.41f - step, 0.95f, 0.9f);
            Set(keypoints, 16, "right_ankle", 0.59f + step, 0.95f, AnkleConfidence(time));

            return new VisionPose
            {
                personId = 1,
                confidence = 0.95f,
                keypoints = keypoints,
                normalizedRect = CalculateNormalizedRect(keypoints),
                skeleton = CocoSkeleton
            };
        }

        private static VisionDetection CreateDetection(VisionPose pose, Vector2Int sourceSize)
        {
            Rect normalized = pose.normalizedRect;
            return new VisionDetection
            {
                trackId = pose.personId,
                classId = 0,
                label = "person",
                confidence = pose.confidence,
                normalizedRect = normalized,
                sourceRect = new Rect(
                    normalized.x * sourceSize.x,
                    normalized.y * sourceSize.y,
                    normalized.width * sourceSize.x,
                    normalized.height * sourceSize.y),
                sourceCenter = new Vector2(
                    normalized.center.x * sourceSize.x,
                    normalized.center.y * sourceSize.y),
                trackState = VisionTrackState.Tracking
            };
        }

        private static Rect CalculateNormalizedRect(VisionKeypoint[] keypoints)
        {
            Vector2 min = Vector2.one;
            Vector2 max = Vector2.zero;
            for (int i = 0; i < keypoints.Length; i++)
            {
                if (!keypoints[i].isVisible || keypoints[i].confidence <= 0.01f)
                    continue;

                min = Vector2.Min(min, keypoints[i].normalizedPosition);
                max = Vector2.Max(max, keypoints[i].normalizedPosition);
            }

            const float pad = 0.04f;
            min = Vector2.Max(Vector2.zero, min - Vector2.one * pad);
            max = Vector2.Min(Vector2.one, max + Vector2.one * pad);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static void Set(VisionKeypoint[] keypoints, int index, string name, float x, float y, float confidence)
        {
            keypoints[index] = new VisionKeypoint
            {
                index = index,
                name = name,
                normalizedPosition = new Vector2(x, y),
                confidence = confidence,
                isVisible = confidence > 0.01f
            };
        }

        private static float WristConfidence(float time)
        {
            return Mathf.Repeat(time, 2f) > 1.45f ? 0.05f : 0.9f;
        }

        private static float AnkleConfidence(float time)
        {
            return Mathf.Repeat(time + 0.7f, 3f) > 2.55f ? 0f : 0.88f;
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Standard") ??
                            Shader.Find("Sprites/Default");
            var material = new Material(shader);
            material.color = color;
            return material;
        }

        private static void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            camera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            camera.transform.position = new Vector3(0f, 0.55f, -4.2f);
            camera.transform.rotation = Quaternion.Euler(4f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.043f, 0.052f, 1f);
        }

        private static readonly VisionSkeletonDefinition CocoSkeleton = new VisionSkeletonDefinition
        {
            name = "COCO-17",
            bones = new[]
            {
                Bone(5, 7), Bone(7, 9), Bone(6, 8), Bone(8, 10),
                Bone(5, 6), Bone(5, 11), Bone(6, 12), Bone(11, 12),
                Bone(11, 13), Bone(13, 15), Bone(12, 14), Bone(14, 16)
            }
        };

        private static VisionSkeletonBone Bone(int from, int to)
        {
            return new VisionSkeletonBone { from = from, to = to };
        }
    }
}
