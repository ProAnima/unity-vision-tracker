using System.Collections.Generic;
using UniversalTracker.Core;
using UnityEngine;

namespace UniversalTracker.Samples
{
    internal sealed class ProAnimaVisionGeneratedHumanoidRig
    {
        private readonly Dictionary<VisionHumanoidJoint, Transform> joints =
            new Dictionary<VisionHumanoidJoint, Transform>();

        private readonly List<SegmentView> segments = new List<SegmentView>();
        private readonly Material bodyMaterial;
        private readonly Material predictedMaterial;
        private readonly float radius;

        private ProAnimaVisionGeneratedHumanoidRig(Material bodyMaterial, Material predictedMaterial, float radius)
        {
            this.bodyMaterial = bodyMaterial;
            this.predictedMaterial = predictedMaterial;
            this.radius = radius;
        }

        public VisionHumanoidRigJointBinding[] Bindings { get; private set; }

        public static ProAnimaVisionGeneratedHumanoidRig Create(
            Transform parent,
            Material bodyMaterial,
            Material predictedMaterial,
            float radius)
        {
            var rig = new ProAnimaVisionGeneratedHumanoidRig(bodyMaterial, predictedMaterial, radius);
            Transform root = rig.CreateJoint(parent, VisionHumanoidJoint.Hips, new Vector3(0f, 0f, 0f));
            Transform spine = rig.CreateJoint(root, VisionHumanoidJoint.Spine, new Vector3(0f, 0.32f, 0f));
            Transform chest = rig.CreateJoint(spine, VisionHumanoidJoint.Chest, new Vector3(0f, 0.36f, 0f));
            Transform neck = rig.CreateJoint(chest, VisionHumanoidJoint.Neck, new Vector3(0f, 0.22f, 0f));
            rig.CreateJoint(neck, VisionHumanoidJoint.Head, new Vector3(0f, 0.24f, 0f));

            Transform leftShoulder = rig.CreateJoint(chest, VisionHumanoidJoint.LeftShoulder, new Vector3(-0.16f, 0.16f, 0f));
            Transform leftUpperArm = rig.CreateJoint(leftShoulder, VisionHumanoidJoint.LeftUpperArm, new Vector3(-0.14f, 0f, 0f));
            Transform leftLowerArm = rig.CreateJoint(leftUpperArm, VisionHumanoidJoint.LeftLowerArm, new Vector3(-0.34f, -0.11f, 0f));
            rig.CreateJoint(leftLowerArm, VisionHumanoidJoint.LeftHand, new Vector3(-0.3f, -0.08f, 0f));

            Transform rightShoulder = rig.CreateJoint(chest, VisionHumanoidJoint.RightShoulder, new Vector3(0.16f, 0.16f, 0f));
            Transform rightUpperArm = rig.CreateJoint(rightShoulder, VisionHumanoidJoint.RightUpperArm, new Vector3(0.14f, 0f, 0f));
            Transform rightLowerArm = rig.CreateJoint(rightUpperArm, VisionHumanoidJoint.RightLowerArm, new Vector3(0.34f, -0.11f, 0f));
            rig.CreateJoint(rightLowerArm, VisionHumanoidJoint.RightHand, new Vector3(0.3f, -0.08f, 0f));

            Transform leftUpperLeg = rig.CreateJoint(root, VisionHumanoidJoint.LeftUpperLeg, new Vector3(-0.11f, -0.08f, 0f));
            Transform leftLowerLeg = rig.CreateJoint(leftUpperLeg, VisionHumanoidJoint.LeftLowerLeg, new Vector3(-0.03f, -0.48f, 0f));
            rig.CreateJoint(leftLowerLeg, VisionHumanoidJoint.LeftFoot, new Vector3(0.02f, -0.46f, 0.1f));

            Transform rightUpperLeg = rig.CreateJoint(root, VisionHumanoidJoint.RightUpperLeg, new Vector3(0.11f, -0.08f, 0f));
            Transform rightLowerLeg = rig.CreateJoint(rightUpperLeg, VisionHumanoidJoint.RightLowerLeg, new Vector3(0.03f, -0.48f, 0f));
            rig.CreateJoint(rightLowerLeg, VisionHumanoidJoint.RightFoot, new Vector3(-0.02f, -0.46f, 0.1f));
            rig.CreateSegments(parent);
            rig.Bindings = rig.CreateBindings();
            rig.UpdateVisuals(default, false);
            return rig;
        }

        public void UpdateVisuals(VisionHumanoidPose pose, bool hasPose)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                SegmentView segment = segments[i];
                Vector3 from = joints[segment.from].position;
                Vector3 to = joints[segment.to].position;
                Vector3 direction = to - from;
                float length = direction.magnitude;
                if (length < 0.0001f)
                    continue;

                segment.visual.position = (from + to) * 0.5f;
                segment.visual.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                segment.visual.localScale = new Vector3(radius, length * 0.5f, radius);
                segment.renderer.sharedMaterial = IsPredicted(pose, hasPose, segment.to) ? predictedMaterial : bodyMaterial;
            }
        }

        private Transform CreateJoint(Transform parent, VisionHumanoidJoint joint, Vector3 localPosition)
        {
            var node = new GameObject(joint.ToString());
            node.transform.SetParent(parent, false);
            node.transform.localPosition = localPosition;
            joints[joint] = node.transform;
            return node.transform;
        }

        private void CreateSegments(Transform parent)
        {
            VisionHumanoidBone[] bones = VisionHumanoidJointUtility.Bones;
            for (int i = 0; i < bones.Length; i++)
            {
                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                segment.name = $"{bones[i].from}_to_{bones[i].to}_Body";
                segment.transform.SetParent(parent, false);
                Renderer renderer = segment.GetComponent<Renderer>();
                renderer.sharedMaterial = bodyMaterial;
                segments.Add(new SegmentView(bones[i].from, bones[i].to, segment.transform, renderer));
            }
        }

        private VisionHumanoidRigJointBinding[] CreateBindings()
        {
            var result = new VisionHumanoidRigJointBinding[joints.Count];
            int index = 0;
            foreach (KeyValuePair<VisionHumanoidJoint, Transform> pair in joints)
            {
                result[index++] = new VisionHumanoidRigJointBinding
                {
                    joint = pair.Key,
                    transform = pair.Value,
                    child = ResolveChild(pair.Key),
                    localAimAxis = Vector3.up
                };
            }

            return result;
        }

        private Transform ResolveChild(VisionHumanoidJoint joint)
        {
            VisionHumanoidBone[] bones = VisionHumanoidJointUtility.Bones;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i].from == joint && joints.TryGetValue(bones[i].to, out Transform child))
                    return child;
            }

            return null;
        }

        private static bool IsPredicted(VisionHumanoidPose pose, bool hasPose, VisionHumanoidJoint joint)
        {
            return hasPose && pose.TryGetJoint(joint, out VisionHumanoidJointPose jointPose) && jointPose.predicted;
        }

        private readonly struct SegmentView
        {
            public readonly VisionHumanoidJoint from;
            public readonly VisionHumanoidJoint to;
            public readonly Transform visual;
            public readonly Renderer renderer;

            public SegmentView(VisionHumanoidJoint from, VisionHumanoidJoint to, Transform visual, Renderer renderer)
            {
                this.from = from;
                this.to = to;
                this.visual = visual;
                this.renderer = renderer;
            }
        }
    }
}
