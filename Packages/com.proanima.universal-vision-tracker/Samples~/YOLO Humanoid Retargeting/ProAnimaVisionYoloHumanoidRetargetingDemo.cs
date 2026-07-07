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
        private VisionHumanoidRigReceiver receiver;
        private float time;

        private void Awake()
        {
            bodyMaterial ??= CreateMaterial(new Color(0.18f, 0.68f, 0.95f));
            predictedMaterial ??= CreateMaterial(new Color(1f, 0.76f, 0.18f));
            rig = ProAnimaVisionGeneratedHumanoidRig.Create(transform, bodyMaterial, predictedMaterial, rigRadius);

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
            receiver.ReceiveVisionResult(CreateFrameResult(CreateSyntheticCocoPose(time)));
            rig.UpdateVisuals(receiver.LastHumanoidPose, receiver.HasLastHumanoidPose);
        }

        private static VisionFrameResult CreateFrameResult(VisionPose pose)
        {
            return new VisionFrameResult
            {
                frameIndex = Time.frameCount,
                timestamp = Time.realtimeSinceStartupAsDouble,
                sourceSize = new Vector2Int(1280, 720),
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
                keypoints = keypoints
            };
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
    }
}
