using System;
using UniversalTracker.Core;
using UnityEngine;

namespace UniversalTracker.OutputReceivers
{
    public sealed class VisionHumanoidRigReceiver : MonoBehaviour, IOutputReceiver, IVisionFrameResultReceiver
    {
        [Header("Input")]
        public UniversalTrackerManager trackerManager;
        public int poseIndex = -1;
        public int targetPersonId = -1;

        [Header("Rig")]
        public Animator animator;
        public Transform poseSpaceRoot;
        public bool autoBindHumanoidAnimator = true;
        public VisionHumanoidRigJointBinding[] bindings = Array.Empty<VisionHumanoidRigJointBinding>();

        [Header("Retargeting")]
        public VisionPoseRetargetingOptions retargetingOptions = VisionPoseRetargetingOptions.Default;
        [Range(0f, 1f)]
        public float blend = 0.85f;
        public float positionScale = 1f;
        public bool driveRootPosition;
        public bool driveJointPositions;
        public bool driveBoneRotations = true;

        [SerializeField, Tooltip("Enable this receiver.")]
        private bool isEnabled = true;

        private readonly VisionHumanoidRigJointBinding[] bindingLookup =
            new VisionHumanoidRigJointBinding[VisionHumanoidJointUtility.JointCount];

        private readonly VisionHumanoidJointPose[] poseLookup =
            new VisionHumanoidJointPose[VisionHumanoidJointUtility.JointCount];

        private readonly bool[] hasPose = new bool[VisionHumanoidJointUtility.JointCount];
        private VisionCocoHumanoidPoseRetargeter retargeter;
        private bool subscribed;

        public bool IsEnabled { get => isEnabled; set => isEnabled = value; }
        public bool HasLastHumanoidPose { get; private set; }
        public VisionHumanoidPose LastHumanoidPose { get; private set; }

        private void OnEnable()
        {
            Initialize();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Initialize()
        {
            retargeter ??= new VisionCocoHumanoidPoseRetargeter(retargetingOptions);
            retargeter.Options = retargetingOptions;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (autoBindHumanoidAnimator && (bindings == null || bindings.Length == 0))
                bindings = CreateAnimatorBindings(animator);

            poseSpaceRoot ??= transform;
            RebuildBindingLookup();
            CaptureBindPose();
        }

        public void ReceiveVisionResult(VisionFrameResult result, Texture sourceTexture = null)
        {
            if (!isEnabled || result == null || !TrySelectPose(result.poses, out VisionPose pose))
                return;

            retargeter ??= new VisionCocoHumanoidPoseRetargeter(retargetingOptions);
            retargeter.Options = retargetingOptions;
            if (!retargeter.TryRetarget(pose, Time.deltaTime, out VisionHumanoidPose humanoidPose))
                return;

            LastHumanoidPose = humanoidPose;
            HasLastHumanoidPose = true;
            ApplyHumanoidPose(humanoidPose);
        }

        public void ApplyHumanoidPose(VisionHumanoidPose pose)
        {
            BuildPoseLookup(pose);
            ApplyPositions();
            ApplyRotations();
        }

        public void Clear()
        {
            retargeter?.Reset();
            HasLastHumanoidPose = false;
            LastHumanoidPose = default;
        }

        public void Release()
        {
            Unsubscribe();
            Clear();
        }

        public void Rebind()
        {
            RebuildBindingLookup();
            CaptureBindPose();
        }

        private void Subscribe()
        {
            if (subscribed || trackerManager == null)
                return;

            trackerManager.OnVisionFrameResult += ReceiveTrackedVisionResult;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || trackerManager == null)
                return;

            trackerManager.OnVisionFrameResult -= ReceiveTrackedVisionResult;
            subscribed = false;
        }

        private void ReceiveTrackedVisionResult(VisionFrameResult result)
        {
            ReceiveVisionResult(result, result?.sourceTexture);
        }

        private bool TrySelectPose(VisionPose[] poses, out VisionPose pose)
        {
            pose = default;
            if (poses == null || poses.Length == 0)
                return false;

            if (targetPersonId >= 0)
            {
                for (int i = 0; i < poses.Length; i++)
                {
                    if (poses[i].personId == targetPersonId)
                    {
                        pose = poses[i];
                        return true;
                    }
                }
            }

            if (poseIndex >= 0 && poseIndex < poses.Length)
            {
                pose = poses[poseIndex];
                return true;
            }

            int best = 0;
            for (int i = 1; i < poses.Length; i++)
            {
                if (poses[i].confidence > poses[best].confidence)
                    best = i;
            }

            pose = poses[best];
            return true;
        }

        private static VisionHumanoidRigJointBinding[] CreateAnimatorBindings(Animator sourceAnimator)
        {
            if (sourceAnimator == null || !sourceAnimator.isHuman)
                return Array.Empty<VisionHumanoidRigJointBinding>();

            var result = new VisionHumanoidRigJointBinding[VisionHumanoidJointUtility.JointCount];
            int count = 0;
            for (int i = 0; i < VisionHumanoidJointUtility.JointCount; i++)
            {
                VisionHumanoidJoint joint = (VisionHumanoidJoint)i;
                if (!VisionHumanoidJointUtility.TryGetHumanBodyBone(joint, out HumanBodyBones bone))
                    continue;

                Transform transform = sourceAnimator.GetBoneTransform(bone);
                if (transform == null)
                    continue;

                result[count++] = new VisionHumanoidRigJointBinding { joint = joint, transform = transform };
            }

            Array.Resize(ref result, count);
            return result;
        }

        private void RebuildBindingLookup()
        {
            Array.Clear(bindingLookup, 0, bindingLookup.Length);
            if (bindings == null)
                return;

            for (int i = 0; i < bindings.Length; i++)
            {
                VisionHumanoidRigJointBinding binding = bindings[i];
                if (binding == null || !binding.HasTransform)
                    continue;

                bindingLookup[(int)binding.joint] = binding;
            }
        }

        private void CaptureBindPose()
        {
            if (bindings == null)
                return;

            for (int i = 0; i < bindings.Length; i++)
            {
                VisionHumanoidRigJointBinding binding = bindings[i];
                if (binding == null || !binding.HasTransform)
                    continue;

                binding.child ??= ResolveChild(binding.joint);
                Vector3 direction = binding.child != null
                    ? binding.child.position - binding.transform.position
                    : binding.transform.TransformDirection(binding.localAimAxis);
                if (direction.sqrMagnitude < 0.000001f)
                    direction = binding.transform.up;

                binding.bindWorldDirection = direction.normalized;
                binding.bindWorldRotation = binding.transform.rotation;
            }
        }

        private Transform ResolveChild(VisionHumanoidJoint joint)
        {
            VisionHumanoidBone[] bones = VisionHumanoidJointUtility.Bones;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i].from != joint)
                    continue;

                VisionHumanoidRigJointBinding child = bindingLookup[(int)bones[i].to];
                if (child != null && child.HasTransform)
                    return child.transform;
            }

            return null;
        }

        private void BuildPoseLookup(VisionHumanoidPose pose)
        {
            Array.Clear(hasPose, 0, hasPose.Length);
            if (pose.joints == null)
                return;

            for (int i = 0; i < pose.joints.Length; i++)
            {
                int index = (int)pose.joints[i].joint;
                if (index < 0 || index >= poseLookup.Length)
                    continue;

                poseLookup[index] = pose.joints[i];
                hasPose[index] = true;
            }
        }

        private void ApplyPositions()
        {
            if (!driveRootPosition && !driveJointPositions)
                return;

            for (int i = 0; i < bindingLookup.Length; i++)
            {
                VisionHumanoidRigJointBinding binding = bindingLookup[i];
                if (binding == null || !binding.HasTransform || !hasPose[i])
                    continue;

                bool isRoot = (VisionHumanoidJoint)i == VisionHumanoidJoint.Hips;
                if (!driveJointPositions && !(driveRootPosition && isRoot))
                    continue;

                Vector3 target = poseSpaceRoot.TransformPoint(poseLookup[i].position * positionScale);
                float weight = Mathf.Clamp01(blend * Mathf.Max(0.05f, poseLookup[i].confidence));
                binding.transform.position = Vector3.Lerp(binding.transform.position, target, weight);
            }
        }

        private void ApplyRotations()
        {
            if (!driveBoneRotations)
                return;

            for (int i = 0; i < bindingLookup.Length; i++)
            {
                VisionHumanoidRigJointBinding binding = bindingLookup[i];
                if (binding == null || !binding.HasTransform || !hasPose[i])
                    continue;

                if (!TryResolveDrivenChildJoint(binding, (VisionHumanoidJoint)i, out VisionHumanoidJoint childJoint))
                    continue;

                int to = (int)childJoint;
                if (!hasPose[to])
                    continue;

                Vector3 localDirection = poseLookup[to].position - poseLookup[i].position;
                if (localDirection.sqrMagnitude < 0.000001f)
                    continue;

                Vector3 worldDirection = poseSpaceRoot.TransformDirection(localDirection.normalized);
                Quaternion target = Quaternion.FromToRotation(binding.bindWorldDirection, worldDirection) * binding.bindWorldRotation;
                float confidence = Mathf.Min(poseLookup[i].confidence, poseLookup[to].confidence);
                float weight = Mathf.Clamp01(blend * Mathf.Max(0.05f, confidence));
                binding.transform.rotation = Quaternion.Slerp(binding.transform.rotation, target, weight);
            }
        }

        private bool TryResolveDrivenChildJoint(
            VisionHumanoidRigJointBinding binding,
            VisionHumanoidJoint joint,
            out VisionHumanoidJoint child)
        {
            VisionHumanoidBone[] bones = VisionHumanoidJointUtility.Bones;
            if (binding.child != null && TryFindChildBindingJoint(binding.child, joint, bones, out child))
                return true;

            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i].from != joint)
                    continue;

                int childIndex = (int)bones[i].to;
                VisionHumanoidRigJointBinding childBinding = bindingLookup[childIndex];
                if (childBinding != null && childBinding.HasTransform)
                {
                    child = bones[i].to;
                    return true;
                }
            }

            child = default;
            return false;
        }

        private bool TryFindChildBindingJoint(
            Transform childTransform,
            VisionHumanoidJoint joint,
            VisionHumanoidBone[] bones,
            out VisionHumanoidJoint child)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i].from != joint)
                    continue;

                int childIndex = (int)bones[i].to;
                VisionHumanoidRigJointBinding childBinding = bindingLookup[childIndex];
                if (childBinding != null && childBinding.transform == childTransform)
                {
                    child = bones[i].to;
                    return true;
                }
            }

            child = default;
            return false;
        }
    }
}
