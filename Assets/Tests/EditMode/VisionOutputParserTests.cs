using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionOutputParserTests
    {
        [Test]
        public void YoloDetectionParser_ParsesRowFixtureToDetections()
        {
            var parser = new YoloDetectionOutputParser();
            VisionRawModelOutput raw = VisionRawModelOutput.Single(
                "output0",
                new[]
                {
                    0.50f, 0.50f, 0.20f, 0.40f, 0.90f, 0.10f, 0.80f,
                    0.20f, 0.25f, 0.10f, 0.10f, 0.70f, 0.90f, 0.05f
                },
                2,
                7);
            var context = new VisionOutputParserContext(
                new Vector2Int(1000, 500),
                0.25f,
                0.5f,
                new[] { "person", "tool" });

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections.Length, Is.EqualTo(2));
            Assert.That(parsed.detections[0].label, Is.EqualTo("tool"));
            Assert.That(parsed.detections[0].confidence, Is.EqualTo(0.72f).Within(0.0001f));
            AssertRect(parsed.detections[0].sourceRect, new Rect(400, 150, 200, 200));
            Assert.That(parsed.detections[1].label, Is.EqualTo("person"));
            Assert.That(parsed.detections[1].sourceCenter, Is.EqualTo(new Vector2(200, 125)));
        }

        [Test]
        public void YoloDetectionParser_FiltersLowConfidenceRows()
        {
            var parser = new YoloDetectionOutputParser();
            VisionRawModelOutput raw = VisionRawModelOutput.Single(
                "output0",
                new[] { 0.5f, 0.5f, 0.2f, 0.2f, 0.5f, 0.5f },
                1,
                6);
            var context = new VisionOutputParserContext(new Vector2Int(100, 100), 0.3f, 0.5f);

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections, Is.Empty);
        }

        [Test]
        public void YoloDetectionParser_AppliesClassAwareNms()
        {
            var parser = new YoloDetectionOutputParser();
            VisionRawModelOutput raw = VisionRawModelOutput.Single(
                "output0",
                new[]
                {
                    0.50f, 0.50f, 0.40f, 0.40f, 0.95f, 0.90f, 0.01f,
                    0.52f, 0.50f, 0.40f, 0.40f, 0.90f, 0.85f, 0.01f,
                    0.52f, 0.50f, 0.40f, 0.40f, 0.90f, 0.01f, 0.85f
                },
                3,
                7);
            var context = new VisionOutputParserContext(new Vector2Int(100, 100), 0.2f, 0.5f);

            VisionParsedOutput parsed = parser.Parse(raw, context);

            Assert.That(parsed.detections.Length, Is.EqualTo(2));
            Assert.That(parsed.detections[0].classId, Is.EqualTo(0));
            Assert.That(parsed.detections[1].classId, Is.EqualTo(1));
        }

        [Test]
        public void ParsedOutput_ToFrameResult_PreservesMetadata()
        {
            var parsed = new VisionParsedOutput
            {
                detections = new[]
                {
                    new VisionDetection { classId = 3, confidence = 0.8f }
                }
            };

            VisionFrameResult frame = parsed.ToFrameResult(9, 4.5, new Vector2Int(320, 240));

            Assert.That(frame.frameIndex, Is.EqualTo(9));
            Assert.That(frame.timestamp, Is.EqualTo(4.5));
            Assert.That(frame.sourceSize, Is.EqualTo(new Vector2Int(320, 240)));
            Assert.That(frame.detections.Length, Is.EqualTo(1));
            Assert.That(frame.TotalResultCount, Is.EqualTo(1));
        }

        private static void AssertRect(Rect actual, Rect expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.width, Is.EqualTo(expected.width).Within(0.0001f));
            Assert.That(actual.height, Is.EqualTo(expected.height).Within(0.0001f));
        }
    }
}
