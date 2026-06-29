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
        public void FrameResult_CanCarryGpuOutputHandlesWithoutCpuMaskTexture()
        {
            var result = VisionFrameResult.Empty(2, 0, new Vector2Int(640, 480));
            var maskTexture = new RenderTexture(32, 24, 0, RenderTextureFormat.ARGB32);

            result.gpuOutputs = new VisionGpuOutputHandles
            {
                maskTexture = maskTexture,
                maskSize = new Vector2Int(32, 24),
                keypointCount = 17
            };

            Assert.That(result.gpuOutputs.HasMaskTexture, Is.True);
            Assert.That(result.gpuOutputs.HasAny, Is.True);
            Assert.That(result.gpuOutputs.maskSize, Is.EqualTo(new Vector2Int(32, 24)));
            Assert.That(result.masks, Is.Empty);

            maskTexture.Release();
            Object.DestroyImmediate(maskTexture);
        }

        [Test]
        public void ParsedOutput_ToFrameResult_PreservesGpuOutputHandles()
        {
            var parsed = new VisionParsedOutput
            {
                gpuOutputs = new VisionGpuOutputHandles
                {
                    keypointCount = 17
                }
            };

            VisionFrameResult frame = parsed.ToFrameResult(3, 1d, new Vector2Int(320, 240));

            Assert.That(frame.gpuOutputs, Is.SameAs(parsed.gpuOutputs));
            Assert.That(frame.gpuOutputs.keypointCount, Is.EqualTo(17));
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
