using System;
using UnityEngine;

namespace UniversalTracker.Core
{
    public enum VisionHumanoidJoint
    {
        Hips,
        Spine,
        Chest,
        Neck,
        Head,
        LeftShoulder,
        LeftUpperArm,
        LeftLowerArm,
        LeftHand,
        RightShoulder,
        RightUpperArm,
        RightLowerArm,
        RightHand,
        LeftUpperLeg,
        LeftLowerLeg,
        LeftFoot,
        RightUpperLeg,
        RightLowerLeg,
        RightFoot
    }

    public static class VisionHumanoidJointUtility
    {
        public const int JointCount = 19;

        public static readonly VisionHumanoidBone[] Bones =
        {
            Bone(VisionHumanoidJoint.Hips, VisionHumanoidJoint.Spine),
            Bone(VisionHumanoidJoint.Spine, VisionHumanoidJoint.Chest),
            Bone(VisionHumanoidJoint.Chest, VisionHumanoidJoint.Neck),
            Bone(VisionHumanoidJoint.Neck, VisionHumanoidJoint.Head),
            Bone(VisionHumanoidJoint.Chest, VisionHumanoidJoint.LeftShoulder),
            Bone(VisionHumanoidJoint.LeftShoulder, VisionHumanoidJoint.LeftUpperArm),
            Bone(VisionHumanoidJoint.LeftUpperArm, VisionHumanoidJoint.LeftLowerArm),
            Bone(VisionHumanoidJoint.LeftLowerArm, VisionHumanoidJoint.LeftHand),
            Bone(VisionHumanoidJoint.Chest, VisionHumanoidJoint.RightShoulder),
            Bone(VisionHumanoidJoint.RightShoulder, VisionHumanoidJoint.RightUpperArm),
            Bone(VisionHumanoidJoint.RightUpperArm, VisionHumanoidJoint.RightLowerArm),
            Bone(VisionHumanoidJoint.RightLowerArm, VisionHumanoidJoint.RightHand),
            Bone(VisionHumanoidJoint.Hips, VisionHumanoidJoint.LeftUpperLeg),
            Bone(VisionHumanoidJoint.LeftUpperLeg, VisionHumanoidJoint.LeftLowerLeg),
            Bone(VisionHumanoidJoint.LeftLowerLeg, VisionHumanoidJoint.LeftFoot),
            Bone(VisionHumanoidJoint.Hips, VisionHumanoidJoint.RightUpperLeg),
            Bone(VisionHumanoidJoint.RightUpperLeg, VisionHumanoidJoint.RightLowerLeg),
            Bone(VisionHumanoidJoint.RightLowerLeg, VisionHumanoidJoint.RightFoot)
        };

        public static bool TryGetHumanBodyBone(VisionHumanoidJoint joint, out HumanBodyBones bone)
        {
            switch (joint)
            {
                case VisionHumanoidJoint.Hips:
                    bone = HumanBodyBones.Hips;
                    return true;
                case VisionHumanoidJoint.Spine:
                    bone = HumanBodyBones.Spine;
                    return true;
                case VisionHumanoidJoint.Chest:
                    bone = HumanBodyBones.Chest;
                    return true;
                case VisionHumanoidJoint.Neck:
                    bone = HumanBodyBones.Neck;
                    return true;
                case VisionHumanoidJoint.Head:
                    bone = HumanBodyBones.Head;
                    return true;
                case VisionHumanoidJoint.LeftShoulder:
                    bone = HumanBodyBones.LeftShoulder;
                    return true;
                case VisionHumanoidJoint.LeftUpperArm:
                    bone = HumanBodyBones.LeftUpperArm;
                    return true;
                case VisionHumanoidJoint.LeftLowerArm:
                    bone = HumanBodyBones.LeftLowerArm;
                    return true;
                case VisionHumanoidJoint.LeftHand:
                    bone = HumanBodyBones.LeftHand;
                    return true;
                case VisionHumanoidJoint.RightShoulder:
                    bone = HumanBodyBones.RightShoulder;
                    return true;
                case VisionHumanoidJoint.RightUpperArm:
                    bone = HumanBodyBones.RightUpperArm;
                    return true;
                case VisionHumanoidJoint.RightLowerArm:
                    bone = HumanBodyBones.RightLowerArm;
                    return true;
                case VisionHumanoidJoint.RightHand:
                    bone = HumanBodyBones.RightHand;
                    return true;
                case VisionHumanoidJoint.LeftUpperLeg:
                    bone = HumanBodyBones.LeftUpperLeg;
                    return true;
                case VisionHumanoidJoint.LeftLowerLeg:
                    bone = HumanBodyBones.LeftLowerLeg;
                    return true;
                case VisionHumanoidJoint.LeftFoot:
                    bone = HumanBodyBones.LeftFoot;
                    return true;
                case VisionHumanoidJoint.RightUpperLeg:
                    bone = HumanBodyBones.RightUpperLeg;
                    return true;
                case VisionHumanoidJoint.RightLowerLeg:
                    bone = HumanBodyBones.RightLowerLeg;
                    return true;
                case VisionHumanoidJoint.RightFoot:
                    bone = HumanBodyBones.RightFoot;
                    return true;
                default:
                    bone = HumanBodyBones.LastBone;
                    return false;
            }
        }

        private static VisionHumanoidBone Bone(VisionHumanoidJoint from, VisionHumanoidJoint to)
        {
            return new VisionHumanoidBone { from = from, to = to };
        }
    }

    [Serializable]
    public struct VisionHumanoidBone
    {
        public VisionHumanoidJoint from;
        public VisionHumanoidJoint to;
    }

    [Serializable]
    public struct VisionHumanoidJointPose
    {
        public VisionHumanoidJoint joint;
        public Vector3 position;
        public Quaternion rotation;
        public float confidence;
        public bool observed;
        public bool predicted;
        public bool hasRotation;
    }

    [Serializable]
    public struct VisionHumanoidPose
    {
        public int personId;
        public float confidence;
        public float trackingQuality;
        public VisionHumanoidJointPose[] joints;

        public bool TryGetJoint(VisionHumanoidJoint joint, out VisionHumanoidJointPose pose)
        {
            if (joints != null)
            {
                for (int i = 0; i < joints.Length; i++)
                {
                    if (joints[i].joint == joint)
                    {
                        pose = joints[i];
                        return true;
                    }
                }
            }

            pose = default;
            return false;
        }
    }

    [Serializable]
    public sealed class VisionPoseRetargetingOptions
    {
        [Range(0f, 1f)]
        public float keypointConfidenceThreshold = 0.35f;

        [Range(0f, 0.5f)]
        public float dropoutHoldSeconds = 0.12f;

        [Range(0f, 1f)]
        public float smoothing = 0.45f;

        [Min(0.01f)]
        public float bodyHeightMeters = 1.7f;

        [Min(0.01f)]
        public float minimumPoseQuality = 0.25f;

        [Min(0.01f)]
        public float missingJointConfidence = 0.15f;

        [Range(0f, 90f)]
        public float maxTorsoRollDegrees = 25f;

        [Range(0f, 1f)]
        public float headKeypointInfluence = 0.55f;

        [Range(0f, 1f)]
        public float legKeypointInfluence = 0.75f;

        public static VisionPoseRetargetingOptions Default => new VisionPoseRetargetingOptions();
    }

    [Serializable]
    public sealed class VisionHumanoidRigJointBinding
    {
        public VisionHumanoidJoint joint;
        public Transform transform;
        public Transform child;
        public Vector3 localAimAxis = Vector3.up;

        [NonSerialized]
        public Quaternion bindWorldRotation = Quaternion.identity;

        [NonSerialized]
        public Vector3 bindWorldDirection = Vector3.up;

        public bool HasTransform => transform != null;
    }
}
