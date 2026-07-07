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

    [Serializable]
    public struct VisionHumanoidJointPose
    {
        public VisionHumanoidJoint joint;
        public Vector3 position;
        public float confidence;
        public bool observed;
        public bool predicted;
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

        public static VisionPoseRetargetingOptions Default => new VisionPoseRetargetingOptions();
    }
}
