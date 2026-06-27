using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionResultAdapterTests
    {
        [Test]
        public void ConvertDetection_MapsNormalizedRectToSourcePixels()
        {
            var box = new BBoxData
            {
                id = 12,
                classId = 0,
                className = "person",
                confidence = 0.75f,
                rect = new Rect(0.25f, 0.1f, 0.5f, 0.4f)
            };

            var detection = VisionResultAdapter.ConvertDetection(box, new Vector2Int(1280, 720));

            Assert.That(detection.trackId, Is.EqualTo(12));
            Assert.That(detection.classId, Is.EqualTo(0));
            Assert.That(detection.label, Is.EqualTo("person"));
            Assert.That(detection.confidence, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(detection.normalizedRect, Is.EqualTo(new Rect(0.25f, 0.1f, 0.5f, 0.4f)));
            AssertRect(detection.sourceRect, new Rect(320f, 72f, 640f, 288f), 0.001f);
            Assert.That(detection.sourceCenter, Is.EqualTo(new Vector2(640f, 216f)));
            Assert.That(detection.trackState, Is.EqualTo(VisionTrackState.Tracking));
        }

        [Test]
        public void FromInferenceResult_MapsAllResultKinds()
        {
            var result = new InferenceResult
            {
                success = true,
                inferenceTime = 3.5f,
                boundingBoxes = new[]
                {
                    new BBoxData { id = 1, classId = 0, className = "person", confidence = 0.9f, rect = new Rect(0.1f, 0.2f, 0.3f, 0.4f) }
                },
                keypoints = new[]
                {
                    new KeypointData
                    {
                        personId = 1,
                        points = new[] { new Vector2(0.5f, 0.25f), new Vector2(0.6f, 0.35f) },
                        confidences = new[] { 0.8f, 0f },
                        connections = new[] { 0, 1 }
                    }
                },
                masks = new[]
                {
                    new MaskData { objectId = 1, classId = 0, className = "person", confidence = 0.7f, bbox = new Rect(0.1f, 0.2f, 0.3f, 0.4f) }
                },
                classifications = new[]
                {
                    new ClassificationData { classId = 3, className = "cat", confidence = 0.66f }
                }
            };

            var frame = VisionResultAdapter.FromInferenceResult(result, new Vector2Int(1000, 500), 42, 12.25);

            Assert.That(frame.frameIndex, Is.EqualTo(42));
            Assert.That(frame.timestamp, Is.EqualTo(12.25));
            Assert.That(frame.detections, Has.Length.EqualTo(1));
            Assert.That(frame.poses, Has.Length.EqualTo(1));
            Assert.That(frame.masks, Has.Length.EqualTo(1));
            Assert.That(frame.classifications, Has.Length.EqualTo(1));
            Assert.That(frame.stats.inferenceMs, Is.EqualTo(3.5f).Within(0.0001f));
            Assert.That(frame.poses[0].personId, Is.EqualTo(1));
            Assert.That(frame.poses[0].VisibleKeypointCount, Is.EqualTo(1));
            Assert.That(frame.poses[0].keypoints[0].name, Is.EqualTo("nose"));
            Assert.That(frame.poses[0].keypoints[0].sourcePosition, Is.EqualTo(new Vector2(500f, 125f)));
            Assert.That(frame.poses[0].skeleton.bones, Has.Length.EqualTo(1));
            Assert.That(frame.classifications[0].label, Is.EqualTo("cat"));
        }

        [Test]
        public void FromInferenceResult_NullResult_ReturnsEmptyFrame()
        {
            var frame = VisionResultAdapter.FromInferenceResult(null, new Vector2Int(320, 240), 5, 8.0);

            Assert.That(frame.frameIndex, Is.EqualTo(5));
            Assert.That(frame.sourceSize, Is.EqualTo(new Vector2Int(320, 240)));
            Assert.That(frame.HasAnyResult, Is.False);
        }

        [Test]
        public void ConvertDetection_ClampsOutOfRangeNormalizedRect()
        {
            var box = new BBoxData
            {
                id = -1,
                rect = new Rect(-0.25f, 0.5f, 1.5f, 0.75f)
            };

            var detection = VisionResultAdapter.ConvertDetection(box, new Vector2Int(100, 100));

            AssertRect(detection.normalizedRect, new Rect(0f, 0.5f, 1f, 0.5f), 0.001f);
            AssertRect(detection.sourceRect, new Rect(0f, 50f, 100f, 50f), 0.001f);
            Assert.That(detection.trackState, Is.EqualTo(VisionTrackState.None));
        }

        private static void AssertRect(Rect actual, Rect expected, float tolerance)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(tolerance));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(tolerance));
            Assert.That(actual.width, Is.EqualTo(expected.width).Within(tolerance));
            Assert.That(actual.height, Is.EqualTo(expected.height).Within(tolerance));
        }
    }
}

