using System.Collections.Generic;
using UniversalTracker.Core;
using UnityEngine;

namespace UniversalTracker.Samples
{
    public sealed class ProAnimaVisionYoloHumanoidRetargetingDemo : MonoBehaviour
    {
        [SerializeField]
        private float animationSpeed = 1.2f;

        [SerializeField]
        private float jointRadius = 0.055f;

        [SerializeField]
        private float boneRadius = 0.025f;

        [SerializeField]
        private Material observedMaterial;

        [SerializeField]
        private Material predictedMaterial;

        private readonly Dictionary<VisionHumanoidJoint, Transform> joints = new Dictionary<VisionHumanoidJoint, Transform>();
        private readonly List<BoneView> bones = new List<BoneView>();
        private VisionCocoHumanoidPoseRetargeter retargeter;
        private float time;

        private void Awake()
        {
            retargeter = new VisionCocoHumanoidPoseRetargeter(new VisionPoseRetargetingOptions
            {
                keypointConfidenceThreshold = 0.35f,
                dropoutHoldSeconds = 0.18f,
                smoothing = 0.35f,
                bodyHeightMeters = 1.7f,
                minimumPoseQuality = 0.2f
            });

            observedMaterial ??= CreateMaterial(new Color(0.18f, 0.68f, 0.95f));
            predictedMaterial ??= CreateMaterial(new Color(1f, 0.76f, 0.18f));
            BuildRig();
        }

        private void Update()
        {
            time += Time.deltaTime * animationSpeed;
            VisionPose pose = CreateSyntheticCocoPose(time);
            if (!retargeter.TryRetarget(pose, Time.deltaTime, out VisionHumanoidPose humanoidPose))
                return;

            ApplyPose(humanoidPose);
            UpdateBones();
        }

        private void BuildRig()
        {
            foreach (VisionHumanoidJoint joint in HumanoidJointOrder)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = joint.ToString();
                marker.transform.SetParent(transform, false);
                marker.transform.localScale = Vector3.one * jointRadius;
                marker.GetComponent<Renderer>().sharedMaterial = observedMaterial;
                joints[joint] = marker.transform;
            }

            AddBone(VisionHumanoidJoint.Hips, VisionHumanoidJoint.Spine);
            AddBone(VisionHumanoidJoint.Spine, VisionHumanoidJoint.Chest);
            AddBone(VisionHumanoidJoint.Chest, VisionHumanoidJoint.Neck);
            AddBone(VisionHumanoidJoint.Neck, VisionHumanoidJoint.Head);
            AddBone(VisionHumanoidJoint.Chest, VisionHumanoidJoint.LeftShoulder);
            AddBone(VisionHumanoidJoint.LeftShoulder, VisionHumanoidJoint.LeftLowerArm);
            AddBone(VisionHumanoidJoint.LeftLowerArm, VisionHumanoidJoint.LeftHand);
            AddBone(VisionHumanoidJoint.Chest, VisionHumanoidJoint.RightShoulder);
            AddBone(VisionHumanoidJoint.RightShoulder, VisionHumanoidJoint.RightLowerArm);
            AddBone(VisionHumanoidJoint.RightLowerArm, VisionHumanoidJoint.RightHand);
            AddBone(VisionHumanoidJoint.Hips, VisionHumanoidJoint.LeftLowerLeg);
            AddBone(VisionHumanoidJoint.LeftLowerLeg, VisionHumanoidJoint.LeftFoot);
            AddBone(VisionHumanoidJoint.Hips, VisionHumanoidJoint.RightLowerLeg);
            AddBone(VisionHumanoidJoint.RightLowerLeg, VisionHumanoidJoint.RightFoot);
        }

        private void AddBone(VisionHumanoidJoint from, VisionHumanoidJoint to)
        {
            GameObject bone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bone.name = $"{from}_to_{to}";
            bone.transform.SetParent(transform, false);
            bone.GetComponent<Renderer>().sharedMaterial = observedMaterial;
            bones.Add(new BoneView(from, to, bone.transform));
        }

        private void ApplyPose(VisionHumanoidPose pose)
        {
            for (int i = 0; i < pose.joints.Length; i++)
            {
                VisionHumanoidJointPose jointPose = pose.joints[i];
                if (!joints.TryGetValue(jointPose.joint, out Transform joint))
                    continue;

                joint.localPosition = jointPose.position;
                joint.GetComponent<Renderer>().sharedMaterial = jointPose.predicted ? predictedMaterial : observedMaterial;
            }
        }

        private void UpdateBones()
        {
            for (int i = 0; i < bones.Count; i++)
            {
                BoneView bone = bones[i];
                Vector3 from = joints[bone.from].localPosition;
                Vector3 to = joints[bone.to].localPosition;
                Vector3 midpoint = (from + to) * 0.5f;
                Vector3 direction = to - from;
                float length = direction.magnitude;

                bone.transform.localPosition = midpoint;
                bone.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                bone.transform.localScale = new Vector3(boneRadius, length * 0.5f, boneRadius);
            }
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
            Set(keypoints, 7, "left_elbow", 0.33f, 0.43f + wave, 0.9f);
            Set(keypoints, 8, "right_elbow", 0.67f, 0.43f - wave, 0.9f);
            Set(keypoints, 9, "left_wrist", 0.27f, 0.57f + wave, WristConfidence(time));
            Set(keypoints, 10, "right_wrist", 0.73f, 0.57f - wave, 0.9f);
            Set(keypoints, 13, "left_knee", 0.42f, 0.76f, 0.9f);
            Set(keypoints, 14, "right_knee", 0.58f, 0.76f, 0.9f);
            Set(keypoints, 15, "left_ankle", 0.41f, 0.95f, 0.9f);
            Set(keypoints, 16, "right_ankle", 0.59f, 0.95f, 0.9f);

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

        private static Material CreateMaterial(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            return material;
        }

        private static readonly VisionHumanoidJoint[] HumanoidJointOrder =
        {
            VisionHumanoidJoint.Hips,
            VisionHumanoidJoint.Spine,
            VisionHumanoidJoint.Chest,
            VisionHumanoidJoint.Neck,
            VisionHumanoidJoint.Head,
            VisionHumanoidJoint.LeftShoulder,
            VisionHumanoidJoint.LeftUpperArm,
            VisionHumanoidJoint.LeftLowerArm,
            VisionHumanoidJoint.LeftHand,
            VisionHumanoidJoint.RightShoulder,
            VisionHumanoidJoint.RightUpperArm,
            VisionHumanoidJoint.RightLowerArm,
            VisionHumanoidJoint.RightHand,
            VisionHumanoidJoint.LeftUpperLeg,
            VisionHumanoidJoint.LeftLowerLeg,
            VisionHumanoidJoint.LeftFoot,
            VisionHumanoidJoint.RightUpperLeg,
            VisionHumanoidJoint.RightLowerLeg,
            VisionHumanoidJoint.RightFoot
        };

        private readonly struct BoneView
        {
            public readonly VisionHumanoidJoint from;
            public readonly VisionHumanoidJoint to;
            public readonly Transform transform;

            public BoneView(VisionHumanoidJoint from, VisionHumanoidJoint to, Transform transform)
            {
                this.from = from;
                this.to = to;
                this.transform = transform;
            }
        }
    }
}
