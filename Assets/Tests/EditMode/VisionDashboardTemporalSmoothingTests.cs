using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;

namespace UniversalTracker.Tests
{
    public sealed class VisionDashboardTemporalSmoothingTests
    {
        [Test]
        public void SmoothRect_BlendsWithPreviousRectForSameKey()
        {
            var cache = new Dictionary<int, Rect>();
            int key = VisionDashboardTemporalSmoothing.DetectionKey(
                new VisionDetection { trackId = 4, trackState = VisionTrackState.Tracking },
                0);

            VisionDashboardTemporalSmoothing.SmoothRect(cache, key, new Rect(0.1f, 0.1f, 0.2f, 0.2f), 0.5f);
            Rect smoothed = VisionDashboardTemporalSmoothing.SmoothRect(cache, key, new Rect(0.2f, 0.2f, 0.2f, 0.2f), 0.5f);

            Assert.That(smoothed.x, Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(smoothed.y, Is.EqualTo(0.15f).Within(0.0001f));
        }

        [Test]
        public void SmoothRect_ResetsOnLargeJump()
        {
            var cache = new Dictionary<int, Rect>();
            int key = VisionDashboardTemporalSmoothing.MaskKey(new VisionMask { trackId = 8 }, 0);

            VisionDashboardTemporalSmoothing.SmoothRect(cache, key, new Rect(0.05f, 0.05f, 0.1f, 0.1f), 0.8f);
            Rect smoothed = VisionDashboardTemporalSmoothing.SmoothRect(cache, key, new Rect(0.8f, 0.8f, 0.1f, 0.1f), 0.8f);

            Assert.That(smoothed.x, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(smoothed.y, Is.EqualTo(0.8f).Within(0.0001f));
        }

        [Test]
        public void SmoothPoints_ReusesPreviousArrayWhenTopologyMatches()
        {
            var cache = new Dictionary<int, Vector2[]>();
            Vector2[] first = { new Vector2(0f, 0f), new Vector2(1f, 1f) };
            Vector2[] second = { new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f) };

            Vector2[] initial = VisionDashboardTemporalSmoothing.SmoothPoints(cache, 12, first, 0.5f);
            Vector2[] smoothed = VisionDashboardTemporalSmoothing.SmoothPoints(cache, 12, second, 0.5f);

            Assert.That(smoothed, Is.SameAs(initial));
            Assert.That(smoothed[0].x, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(smoothed[1].x, Is.EqualTo(0.9f).Within(0.0001f));
        }

        [Test]
        public void SmoothPoints_ResetsWhenTopologyChanges()
        {
            var cache = new Dictionary<int, Vector2[]>();
            Vector2[] first = { Vector2.zero, Vector2.one };
            Vector2[] changed = { Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f) };

            Vector2[] initial = VisionDashboardTemporalSmoothing.SmoothPoints(cache, 12, first, 0.5f);
            Vector2[] reset = VisionDashboardTemporalSmoothing.SmoothPoints(cache, 12, changed, 0.5f);

            Assert.That(reset, Is.Not.SameAs(initial));
            Assert.That(reset.Length, Is.EqualTo(3));
        }

        [Test]
        public void SmoothPoint_ResetsOnLargeJump()
        {
            var cache = new Dictionary<int, Vector2>();

            VisionDashboardTemporalSmoothing.SmoothPoint(cache, 12, Vector2.zero, 0.8f, 10f);
            Vector2 reset = VisionDashboardTemporalSmoothing.SmoothPoint(cache, 12, new Vector2(100f, 0f), 0.8f, 10f);

            Assert.That(reset, Is.EqualTo(new Vector2(100f, 0f)));
        }

        [Test]
        public void PoseKey_UsesTrackedPersonIdAcrossPoseOrderChanges()
        {
            var pose = new VisionPose
            {
                personId = 7,
                normalizedRect = new Rect(0.1f, 0.1f, 0.2f, 0.4f)
            };

            int first = VisionDashboardTemporalSmoothing.PoseKey(pose, 0, 5);
            int reordered = VisionDashboardTemporalSmoothing.PoseKey(pose, 4, 5);

            Assert.That(reordered, Is.EqualTo(first));
        }
    }
}
