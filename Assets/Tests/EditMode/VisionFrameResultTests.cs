using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionFrameResultTests
    {
        [Test]
        public void EmptyResult_HasNoResults()
        {
            var result = VisionFrameResult.Empty(7, 12.5, new Vector2Int(640, 480));

            Assert.That(result.frameIndex, Is.EqualTo(7));
            Assert.That(result.timestamp, Is.EqualTo(12.5));
            Assert.That(result.sourceSize, Is.EqualTo(new Vector2Int(640, 480)));
            Assert.That(result.HasAnyResult, Is.False);
            Assert.That(result.TotalResultCount, Is.EqualTo(0));
            Assert.That(result.detections, Is.Not.Null);
            Assert.That(result.poses, Is.Not.Null);
        }

        [Test]
        public void TotalResultCount_IncludesAllResultKinds()
        {
            var result = VisionFrameResult.Empty(1, 0, new Vector2Int(1, 1));
            result.detections = new VisionDetection[2];
            result.poses = new VisionPose[1];
            result.masks = new VisionMask[3];
            result.classifications = new VisionClassification[4];

            Assert.That(result.TotalResultCount, Is.EqualTo(10));
            Assert.That(result.HasAnyResult, Is.True);
        }

        [Test]
        public void PoseVisibleKeypointCount_CountsOnlyVisiblePoints()
        {
            var pose = new VisionPose
            {
                keypoints = new[]
                {
                    new VisionKeypoint { isVisible = true },
                    new VisionKeypoint { isVisible = false },
                    new VisionKeypoint { isVisible = true }
                }
            };

            Assert.That(pose.VisibleKeypointCount, Is.EqualTo(2));
        }

        [Test]
        public void Detection_IsTracked_WhenTrackIdAndStateArePresent()
        {
            var detection = new VisionDetection
            {
                trackId = 42,
                trackState = VisionTrackState.Tracking
            };

            Assert.That(detection.IsTracked, Is.True);
        }

        [Test]
        public void PerformanceStats_TotalIsSumOfStages()
        {
            var stats = VisionPerformanceStats.FromStages(1.5f, 4f, 2.25f, 0.25f);

            Assert.That(stats.totalMs, Is.EqualTo(8f).Within(0.0001f));
        }
    }
}

