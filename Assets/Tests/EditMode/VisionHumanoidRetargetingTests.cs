using NUnit.Framework;
using UniversalTracker.Core;
using UnityEngine;

namespace UniversalTracker.Tests
{
    public sealed class VisionHumanoidRetargetingTests
    {
        [Test]
        public void CocoPoseRetargeterBuildsHumanoidTorsoAndLimbs()
        {
            var retargeter = new VisionCocoHumanoidPoseRetargeter();
            bool ok = retargeter.TryRetarget(CreatePose(0.9f), 1f / 30f, out VisionHumanoidPose pose);

            Assert.IsTrue(ok);
            Assert.GreaterOrEqual(pose.joints.Length, 19);
            Assert.IsTrue(pose.TryGetJoint(VisionHumanoidJoint.Hips, out VisionHumanoidJointPose hips));
            Assert.IsTrue(pose.TryGetJoint(VisionHumanoidJoint.Head, out VisionHumanoidJointPose head));
            Assert.IsTrue(pose.TryGetJoint(VisionHumanoidJoint.LeftHand, out VisionHumanoidJointPose leftHand));
            Assert.Greater(head.position.y, hips.position.y);
            Assert.Less(leftHand.position.x, hips.position.x);
        }

        [Test]
        public void CocoPoseRetargeterPredictsShortKeypointDropout()
        {
            var options = new VisionPoseRetargetingOptions
            {
                keypointConfidenceThreshold = 0.35f,
                dropoutHoldSeconds = 0.2f,
                smoothing = 0f,
                minimumPoseQuality = 0.1f
            };
            var retargeter = new VisionCocoHumanoidPoseRetargeter(options);

            Assert.IsTrue(retargeter.TryRetarget(CreatePose(0.9f), 1f / 30f, out _));
            Assert.IsTrue(retargeter.TryRetarget(CreatePose(0.05f), 1f / 30f, out VisionHumanoidPose pose));

            Assert.IsTrue(pose.TryGetJoint(VisionHumanoidJoint.LeftHand, out VisionHumanoidJointPose hand));
            Assert.IsTrue(hand.predicted);
            Assert.Greater(hand.confidence, 0f);
        }

        [Test]
        public void CocoPoseRetargeterRejectsPoseWithoutTorso()
        {
            var retargeter = new VisionCocoHumanoidPoseRetargeter();
            VisionPose pose = CreatePose(0.9f);
            pose.keypoints[5].confidence = 0f;
            pose.keypoints[5].isVisible = false;

            bool ok = retargeter.TryRetarget(pose, 1f / 30f, out _);

            Assert.IsFalse(ok);
        }

        private static VisionPose CreatePose(float leftWristConfidence)
        {
            var keypoints = new VisionKeypoint[17];
            Set(keypoints, 0, 0.5f, 0.19f, 0.95f);
            Set(keypoints, 5, 0.39f, 0.31f, 0.95f);
            Set(keypoints, 6, 0.61f, 0.31f, 0.95f);
            Set(keypoints, 7, 0.33f, 0.43f, 0.9f);
            Set(keypoints, 8, 0.67f, 0.43f, 0.9f);
            Set(keypoints, 9, 0.27f, 0.57f, leftWristConfidence);
            Set(keypoints, 10, 0.73f, 0.57f, 0.9f);
            Set(keypoints, 11, 0.43f, 0.55f, 0.95f);
            Set(keypoints, 12, 0.57f, 0.55f, 0.95f);
            Set(keypoints, 13, 0.42f, 0.76f, 0.9f);
            Set(keypoints, 14, 0.58f, 0.76f, 0.9f);
            Set(keypoints, 15, 0.41f, 0.95f, 0.9f);
            Set(keypoints, 16, 0.59f, 0.95f, 0.9f);

            return new VisionPose
            {
                personId = 42,
                confidence = 0.95f,
                keypoints = keypoints
            };
        }

        private static void Set(VisionKeypoint[] keypoints, int index, float x, float y, float confidence)
        {
            keypoints[index] = new VisionKeypoint
            {
                index = index,
                normalizedPosition = new Vector2(x, y),
                confidence = confidence,
                isVisible = confidence > 0.01f
            };
        }
    }
}
