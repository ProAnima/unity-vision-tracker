using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;
using UniversalTracker.Tracking;

namespace UniversalTracker.Tests
{
    public sealed class TrackingTests
    {
        [Test]
        public void IOUTracker_OverlappingDetection_PreservesTrackId()
        {
            var tracker = new IOUTracker(iouThreshold: 0.3f, maxMissedFrames: 2);

            var first = tracker.Update(new[] { Box(0, 0.9f, new Rect(0.1f, 0.1f, 0.3f, 0.3f)) }, 0.1f);
            var second = tracker.Update(new[] { Box(0, 0.8f, new Rect(0.12f, 0.1f, 0.3f, 0.3f)) }, 0.1f);

            Assert.That(first, Has.Length.EqualTo(1));
            Assert.That(second, Has.Length.EqualTo(1));
            Assert.That(second[0].id, Is.EqualTo(first[0].id));
            Assert.That(second[0].missedFrames, Is.EqualTo(0));
        }

        [Test]
        public void IOUTracker_NonOverlappingDetection_CreatesNewTrack()
        {
            var tracker = new IOUTracker(iouThreshold: 0.3f, maxMissedFrames: 2);

            var first = tracker.Update(new[] { Box(0, 0.9f, new Rect(0.1f, 0.1f, 0.2f, 0.2f)) }, 0.1f);
            var second = tracker.Update(new[] { Box(0, 0.8f, new Rect(0.7f, 0.7f, 0.2f, 0.2f)) }, 0.1f);

            Assert.That(first, Has.Length.EqualTo(1));
            Assert.That(second, Has.Length.EqualTo(2));
            Assert.That(second[0].id, Is.Not.EqualTo(second[1].id));
        }

        [Test]
        public void IOUTracker_MissingDetections_DeactivatesAfterMaxMissedFrames()
        {
            var tracker = new IOUTracker(iouThreshold: 0.3f, maxMissedFrames: 1);

            tracker.Update(new[] { Box(0, 0.9f, new Rect(0.1f, 0.1f, 0.2f, 0.2f)) }, 0.1f);
            var afterOneMiss = tracker.Update(null, 0.1f);
            var afterTwoMisses = tracker.Update(null, 0.1f);

            Assert.That(afterOneMiss, Has.Length.EqualTo(1));
            Assert.That(afterTwoMisses, Is.Empty);
        }

        [Test]
        public void SORTTracker_RequiresMinHitsBeforeConfirmingTrack()
        {
            var tracker = new SORTTracker(iouThreshold: 0.3f, maxMissedFrames: 2);

            var first = tracker.Update(new[] { Box(0, 0.9f, new Rect(0.1f, 0.1f, 0.3f, 0.3f)) }, 0.1f);
            var second = tracker.Update(new[] { Box(0, 0.9f, new Rect(0.11f, 0.1f, 0.3f, 0.3f)) }, 0.1f);
            var third = tracker.Update(new[] { Box(0, 0.9f, new Rect(0.12f, 0.1f, 0.3f, 0.3f)) }, 0.1f);
            var fourth = tracker.Update(new[] { Box(0, 0.9f, new Rect(0.13f, 0.1f, 0.3f, 0.3f)) }, 0.1f);

            Assert.That(first, Is.Empty);
            Assert.That(second, Is.Empty);
            Assert.That(third, Is.Empty);
            Assert.That(fourth, Has.Length.EqualTo(1));
        }

        [Test]
        public void Reset_ClearsTracksAndRestartsIds()
        {
            var tracker = new IOUTracker();

            var first = tracker.Update(new[] { Box(0, 0.9f, new Rect(0.1f, 0.1f, 0.2f, 0.2f)) }, 0.1f);
            tracker.Reset();
            var afterReset = tracker.Update(new[] { Box(0, 0.9f, new Rect(0.6f, 0.6f, 0.2f, 0.2f)) }, 0.1f);

            Assert.That(first[0].id, Is.EqualTo(0));
            Assert.That(afterReset[0].id, Is.EqualTo(0));
        }

        private static BBoxData Box(int classId, float confidence, Rect rect)
        {
            return new BBoxData
            {
                id = -1,
                classId = classId,
                className = $"class_{classId}",
                confidence = confidence,
                rect = rect,
                center = rect.center
            };
        }
    }
}

