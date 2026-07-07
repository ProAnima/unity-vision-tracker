using System;
using UnityEngine;

namespace UniversalTracker.Core
{
    public sealed class VisionCocoHumanoidPoseRetargeter
    {
        private readonly VisionPoseTemporalFilter temporalFilter = new VisionPoseTemporalFilter();
        private VisionPoseRetargetingOptions options;

        public VisionCocoHumanoidPoseRetargeter(VisionPoseRetargetingOptions options = null)
        {
            this.options = options ?? VisionPoseRetargetingOptions.Default;
        }

        public VisionPoseRetargetingOptions Options
        {
            get => options;
            set => options = value ?? VisionPoseRetargetingOptions.Default;
        }

        public bool TryRetarget(VisionPose sourcePose, float deltaTime, out VisionHumanoidPose humanoidPose)
        {
            VisionKeypoint[] keypoints = temporalFilter.Update(sourcePose, deltaTime, options);
            if (!TryCreateLookup(keypoints, options.keypointConfidenceThreshold, out CocoKeypoints coco))
            {
                humanoidPose = default;
                return false;
            }

            Vector2 pelvis = Average(coco.leftHip.point, coco.rightHip.point);
            float scale = EstimateScale(coco);
            StabilizeMissingLimbs(ref coco, scale, options);

            var builder = new JointBuilder(sourcePose.personId, pelvis, scale, options.bodyHeightMeters);

            AddTorso(builder, coco);
            AddLimbs(builder, coco);

            humanoidPose = builder.Build();
            return humanoidPose.trackingQuality >= options.minimumPoseQuality;
        }

        public void Reset()
        {
            temporalFilter.Reset();
        }

        private static bool TryCreateLookup(VisionKeypoint[] keypoints, float threshold, out CocoKeypoints coco)
        {
            coco = default;
            if (keypoints == null || keypoints.Length < 17)
                return false;

            coco.nose = Point(keypoints[0], threshold);
            coco.leftShoulder = Point(keypoints[5], threshold);
            coco.rightShoulder = Point(keypoints[6], threshold);
            coco.leftElbow = Point(keypoints[7], threshold);
            coco.rightElbow = Point(keypoints[8], threshold);
            coco.leftWrist = Point(keypoints[9], threshold);
            coco.rightWrist = Point(keypoints[10], threshold);
            coco.leftHip = Point(keypoints[11], threshold);
            coco.rightHip = Point(keypoints[12], threshold);
            coco.leftKnee = Point(keypoints[13], threshold);
            coco.rightKnee = Point(keypoints[14], threshold);
            coco.leftAnkle = Point(keypoints[15], threshold);
            coco.rightAnkle = Point(keypoints[16], threshold);

            return coco.leftHip.available &&
                   coco.rightHip.available &&
                   coco.leftShoulder.available &&
                   coco.rightShoulder.available;
        }

        private static void StabilizeMissingLimbs(
            ref CocoKeypoints coco,
            float scale,
            VisionPoseRetargetingOptions options)
        {
            float armLength = scale * 0.45f;
            float legLength = scale * 0.55f;

            coco.leftElbow = EnsureChild(coco.leftShoulder, coco.leftElbow, new Vector2(-0.45f, 0.65f), armLength, options);
            coco.rightElbow = EnsureChild(coco.rightShoulder, coco.rightElbow, new Vector2(0.45f, 0.65f), armLength, options);
            coco.leftWrist = EnsureChild(coco.leftElbow, coco.leftWrist, Direction(coco.leftShoulder, coco.leftElbow, new Vector2(-0.35f, 0.7f)), armLength, options);
            coco.rightWrist = EnsureChild(coco.rightElbow, coco.rightWrist, Direction(coco.rightShoulder, coco.rightElbow, new Vector2(0.35f, 0.7f)), armLength, options);
            coco.leftKnee = EnsureChild(coco.leftHip, coco.leftKnee, new Vector2(-0.05f, 1f), legLength, options);
            coco.rightKnee = EnsureChild(coco.rightHip, coco.rightKnee, new Vector2(0.05f, 1f), legLength, options);
            coco.leftAnkle = EnsureChild(coco.leftKnee, coco.leftAnkle, Direction(coco.leftHip, coco.leftKnee, new Vector2(-0.02f, 1f)), legLength, options);
            coco.rightAnkle = EnsureChild(coco.rightKnee, coco.rightAnkle, Direction(coco.rightHip, coco.rightKnee, new Vector2(0.02f, 1f)), legLength, options);
        }

        private static void AddTorso(JointBuilder builder, CocoKeypoints coco)
        {
            KeypointPoint shoulders = Average(coco.leftShoulder, coco.rightShoulder);
            KeypointPoint hips = Average(coco.leftHip, coco.rightHip);
            KeypointPoint spine = Lerp(hips, shoulders, 0.45f);
            KeypointPoint chest = Lerp(hips, shoulders, 0.78f);
            KeypointPoint neck = Lerp(hips, shoulders, 1.05f);

            builder.Add(VisionHumanoidJoint.Hips, hips);
            builder.Add(VisionHumanoidJoint.Spine, spine);
            builder.Add(VisionHumanoidJoint.Chest, chest);
            builder.Add(VisionHumanoidJoint.Neck, neck);
            builder.Add(VisionHumanoidJoint.Head, coco.nose.available ? coco.nose : Lerp(hips, shoulders, 1.25f));
            builder.Add(VisionHumanoidJoint.LeftShoulder, coco.leftShoulder);
            builder.Add(VisionHumanoidJoint.RightShoulder, coco.rightShoulder);
        }

        private static void AddLimbs(JointBuilder builder, CocoKeypoints coco)
        {
            builder.Add(VisionHumanoidJoint.LeftUpperArm, coco.leftShoulder);
            builder.Add(VisionHumanoidJoint.LeftLowerArm, coco.leftElbow);
            builder.Add(VisionHumanoidJoint.LeftHand, coco.leftWrist);
            builder.Add(VisionHumanoidJoint.RightUpperArm, coco.rightShoulder);
            builder.Add(VisionHumanoidJoint.RightLowerArm, coco.rightElbow);
            builder.Add(VisionHumanoidJoint.RightHand, coco.rightWrist);
            builder.Add(VisionHumanoidJoint.LeftUpperLeg, coco.leftHip);
            builder.Add(VisionHumanoidJoint.LeftLowerLeg, coco.leftKnee);
            builder.Add(VisionHumanoidJoint.LeftFoot, coco.leftAnkle);
            builder.Add(VisionHumanoidJoint.RightUpperLeg, coco.rightHip);
            builder.Add(VisionHumanoidJoint.RightLowerLeg, coco.rightKnee);
            builder.Add(VisionHumanoidJoint.RightFoot, coco.rightAnkle);
        }

        private static KeypointPoint Point(VisionKeypoint keypoint, float threshold)
        {
            return new KeypointPoint
            {
                point = keypoint.normalizedPosition,
                confidence = Mathf.Clamp01(keypoint.confidence),
                available = keypoint.isVisible,
                observed = keypoint.isVisible && keypoint.confidence >= threshold,
                predicted = keypoint.isVisible && keypoint.confidence < threshold
            };
        }

        private static KeypointPoint Average(KeypointPoint a, KeypointPoint b)
        {
            return new KeypointPoint
            {
                point = Average(a.point, b.point),
                confidence = (a.confidence + b.confidence) * 0.5f,
                available = a.available && b.available,
                observed = a.observed && b.observed,
                predicted = a.predicted || b.predicted
            };
        }

        private static Vector2 Average(Vector2 a, Vector2 b) => (a + b) * 0.5f;

        private static KeypointPoint Lerp(KeypointPoint a, KeypointPoint b, float t)
        {
            return new KeypointPoint
            {
                point = Vector2.LerpUnclamped(a.point, b.point, t),
                confidence = Mathf.Min(a.confidence, b.confidence),
                available = a.available && b.available,
                observed = a.observed && b.observed,
                predicted = a.predicted || b.predicted
            };
        }

        private static KeypointPoint EnsureChild(
            KeypointPoint parent,
            KeypointPoint child,
            Vector2 fallbackDirection,
            float length,
            VisionPoseRetargetingOptions options)
        {
            if (child.available)
                return child;

            Vector2 direction = SafeDirection(fallbackDirection, Vector2.down);
            return new KeypointPoint
            {
                point = parent.point + direction * length,
                confidence = Mathf.Clamp01(options.missingJointConfidence),
                available = parent.available,
                observed = false,
                predicted = true
            };
        }

        private static Vector2 Direction(KeypointPoint from, KeypointPoint to, Vector2 fallback)
        {
            if (!from.available || !to.available)
                return SafeDirection(fallback, Vector2.down);

            return SafeDirection(to.point - from.point, fallback);
        }

        private static Vector2 SafeDirection(Vector2 direction, Vector2 fallback)
        {
            if (direction.sqrMagnitude > 0.000001f)
                return direction.normalized;

            return fallback.sqrMagnitude > 0.000001f ? fallback.normalized : Vector2.down;
        }

        private static float EstimateScale(CocoKeypoints coco)
        {
            float torso = Vector2.Distance(Average(coco.leftHip.point, coco.rightHip.point), Average(coco.leftShoulder.point, coco.rightShoulder.point));
            float shoulders = Vector2.Distance(coco.leftShoulder.point, coco.rightShoulder.point);
            return Mathf.Max(0.0001f, Mathf.Max(torso, shoulders));
        }

        private struct CocoKeypoints
        {
            public KeypointPoint nose;
            public KeypointPoint leftShoulder;
            public KeypointPoint rightShoulder;
            public KeypointPoint leftElbow;
            public KeypointPoint rightElbow;
            public KeypointPoint leftWrist;
            public KeypointPoint rightWrist;
            public KeypointPoint leftHip;
            public KeypointPoint rightHip;
            public KeypointPoint leftKnee;
            public KeypointPoint rightKnee;
            public KeypointPoint leftAnkle;
            public KeypointPoint rightAnkle;
        }

        private struct KeypointPoint
        {
            public Vector2 point;
            public float confidence;
            public bool available;
            public bool observed;
            public bool predicted;
        }

        private sealed class JointBuilder
        {
            private readonly VisionHumanoidJointPose[] joints = new VisionHumanoidJointPose[VisionHumanoidJointUtility.JointCount];
            private readonly int personId;
            private readonly Vector2 pelvis;
            private readonly float scale;
            private readonly float bodyHeight;
            private int count;
            private float confidenceSum;
            private int observedCount;

            public JointBuilder(int personId, Vector2 pelvis, float scale, float bodyHeight)
            {
                this.personId = personId;
                this.pelvis = pelvis;
                this.scale = scale;
                this.bodyHeight = bodyHeight;
            }

            public void Add(VisionHumanoidJoint joint, KeypointPoint point)
            {
                joints[count++] = new VisionHumanoidJointPose
                {
                    joint = joint,
                    position = ToBodySpace(point.point),
                    rotation = Quaternion.identity,
                    confidence = point.confidence,
                    observed = point.observed,
                    predicted = point.predicted,
                    hasRotation = false
                };

                confidenceSum += point.confidence;
                if (point.observed)
                    observedCount++;
            }

            public VisionHumanoidPose Build()
            {
                ApplyRotations();
                var output = new VisionHumanoidJointPose[count];
                Array.Copy(joints, output, count);
                float quality = count == 0 ? 0f : observedCount / (float)count;
                return new VisionHumanoidPose
                {
                    personId = personId,
                    confidence = count == 0 ? 0f : confidenceSum / count,
                    trackingQuality = quality,
                    joints = output
                };
            }

            private void ApplyRotations()
            {
                VisionHumanoidBone[] bones = VisionHumanoidJointUtility.Bones;
                for (int i = 0; i < bones.Length; i++)
                {
                    if (!TryFind(bones[i].from, out int fromIndex) ||
                        !TryFind(bones[i].to, out int toIndex))
                    {
                        continue;
                    }

                    Vector3 direction = joints[toIndex].position - joints[fromIndex].position;
                    if (direction.sqrMagnitude < 0.000001f)
                        continue;

                    joints[fromIndex].rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                    joints[fromIndex].hasRotation = true;
                }
            }

            private bool TryFind(VisionHumanoidJoint joint, out int index)
            {
                for (int i = 0; i < count; i++)
                {
                    if (joints[i].joint == joint)
                    {
                        index = i;
                        return true;
                    }
                }

                index = -1;
                return false;
            }

            private Vector3 ToBodySpace(Vector2 point)
            {
                Vector2 centered = (point - pelvis) / scale;
                return new Vector3(centered.x * bodyHeight, -centered.y * bodyHeight, 0f);
            }
        }
    }
}
