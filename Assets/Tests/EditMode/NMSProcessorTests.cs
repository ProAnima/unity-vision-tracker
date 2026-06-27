using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;
using UniversalTracker.Processing;

namespace UniversalTracker.Tests
{
    public sealed class NMSProcessorTests
    {
        [Test]
        public void CalculateIoU_IdenticalRects_ReturnsOne()
        {
            var processor = new NMSProcessor();
            var rect = new Rect(0.1f, 0.2f, 0.4f, 0.3f);

            float iou = processor.CalculateIoU(rect, rect);

            Assert.That(iou, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void CalculateIoU_NonOverlappingRects_ReturnsZero()
        {
            var processor = new NMSProcessor();

            float iou = processor.CalculateIoU(
                new Rect(0f, 0f, 0.2f, 0.2f),
                new Rect(0.8f, 0.8f, 0.1f, 0.1f));

            Assert.That(iou, Is.EqualTo(0f));
        }

        [Test]
        public void ApplyNMS_RemovesLowerConfidenceOverlappingBox()
        {
            var processor = new NMSProcessor();
            var boxes = new[]
            {
                Box(1, 0, 0.95f, new Rect(0.1f, 0.1f, 0.5f, 0.5f)),
                Box(2, 0, 0.60f, new Rect(0.12f, 0.12f, 0.5f, 0.5f)),
                Box(3, 0, 0.70f, new Rect(0.75f, 0.75f, 0.1f, 0.1f))
            };

            var result = processor.ApplyNMS(boxes, 0.5f);

            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result[0].trackId, Is.EqualTo(1));
            Assert.That(result[1].trackId, Is.EqualTo(3));
        }

        [Test]
        public void ApplyPerClassNMS_KeepsOverlappingBoxesFromDifferentClasses()
        {
            var processor = new NMSProcessor();
            var boxes = new[]
            {
                Box(1, 0, 0.95f, new Rect(0.1f, 0.1f, 0.5f, 0.5f)),
                Box(2, 1, 0.60f, new Rect(0.12f, 0.12f, 0.5f, 0.5f))
            };

            var result = processor.ApplyPerClassNMS(boxes, 0.5f);

            Assert.That(result, Has.Length.EqualTo(2));
        }

        [Test]
        public void ApplyNMS_UsesSourceRectWhenNormalizedRectIsEmpty()
        {
            var processor = new NMSProcessor();
            var boxes = new[]
            {
                SourceOnlyBox(1, 0, 0.95f, new Rect(10f, 10f, 50f, 50f)),
                SourceOnlyBox(2, 0, 0.60f, new Rect(12f, 12f, 50f, 50f))
            };

            var result = processor.ApplyNMS(boxes, 0.5f);

            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result[0].trackId, Is.EqualTo(1));
        }

        [Test]
        public void ApplyNMS_NullInput_ReturnsNull()
        {
            var processor = new NMSProcessor();

            Assert.That(processor.ApplyNMS(null, 0.5f), Is.Null);
        }

        private static VisionDetection Box(int id, int classId, float confidence, Rect rect)
        {
            return new VisionDetection
            {
                trackId = id,
                classId = classId,
                label = $"class_{classId}",
                confidence = confidence,
                normalizedRect = rect,
                sourceRect = rect,
                sourceCenter = rect.center
            };
        }

        private static VisionDetection SourceOnlyBox(int id, int classId, float confidence, Rect sourceRect)
        {
            return new VisionDetection
            {
                trackId = id,
                classId = classId,
                label = $"class_{classId}",
                confidence = confidence,
                sourceRect = sourceRect,
                sourceCenter = sourceRect.center
            };
        }
    }
}
