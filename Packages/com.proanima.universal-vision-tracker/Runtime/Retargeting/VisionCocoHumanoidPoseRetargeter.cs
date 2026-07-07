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
                predicted = a.predicted || b.predicted
            };
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
            public bool predicted;
        }

        private sealed class JointBuilder
        {
            private readonly VisionHumanoidJointPose[] joints = new VisionHumanoidJointPose[19];
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
                    confidence = point.confidence,
                    observed = point.available && !point.predicted,
                    predicted = point.predicted
                };

                confidenceSum += point.confidence;
                if (point.available && !point.predicted)
                    observedCount++;
            }

            public VisionHumanoidPose Build()
            {
                Array.Resize(ref joints, count);
                float quality = count == 0 ? 0f : observedCount / (float)count;
                return new VisionHumanoidPose
                {
                    personId = personId,
                    confidence = count == 0 ? 0f : confidenceSum / count,
                    trackingQuality = quality,
                    joints = joints
                };
            }

            private Vector3 ToBodySpace(Vector2 point)
            {
                Vector2 centered = (point - pelvis) / scale;
                return new Vector3(centered.x * bodyHeight, -centered.y * bodyHeight, 0f);
            }
        }
    }
}
