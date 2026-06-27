using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Visualization;

namespace UniversalTracker.Tests
{
    public sealed class VisionDashboardGeometryTests
    {
        [Test]
        public void CalculateScaleToFitRect_WideSource_AddsVerticalPadding()
        {
            Rect rect = VisionDashboardGeometry.CalculateScaleToFitRect(
                new Vector2(1920, 1080),
                new Vector2(640, 640));

            Assert.That(rect.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(rect.y, Is.EqualTo(140f).Within(0.001f));
            Assert.That(rect.width, Is.EqualTo(640f).Within(0.001f));
            Assert.That(rect.height, Is.EqualTo(360f).Within(0.001f));
        }

        [Test]
        public void NormalizedToViewportRect_MapsInsideFittedPreview()
        {
            Rect rect = VisionDashboardGeometry.NormalizedToViewportRect(
                new Rect(0.25f, 0.25f, 0.5f, 0.5f),
                new Vector2(1920, 1080),
                new Vector2(640, 640));

            Assert.That(rect.x, Is.EqualTo(160f).Within(0.001f));
            Assert.That(rect.y, Is.EqualTo(230f).Within(0.001f));
            Assert.That(rect.width, Is.EqualTo(320f).Within(0.001f));
            Assert.That(rect.height, Is.EqualTo(180f).Within(0.001f));
        }

        [Test]
        public void NormalizedToViewportPoint_UsesSameFitAsRect()
        {
            Vector2 point = VisionDashboardGeometry.NormalizedToViewportPoint(
                new Vector2(0.5f, 0.5f),
                new Vector2(1920, 1080),
                new Vector2(640, 640));

            Assert.That(point.x, Is.EqualTo(320f).Within(0.001f));
            Assert.That(point.y, Is.EqualTo(320f).Within(0.001f));
        }

        [Test]
        public void CalculateBoneLine_ReturnsCenterLengthAndAngle()
        {
            BoneLine line = VisionDashboardGeometry.CalculateBoneLine(
                new Vector2(10, 20),
                new Vector2(30, 20));

            Assert.That(line.center.x, Is.EqualTo(20f).Within(0.001f));
            Assert.That(line.center.y, Is.EqualTo(20f).Within(0.001f));
            Assert.That(line.length, Is.EqualTo(20f).Within(0.001f));
            Assert.That(line.angleDegrees, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateScaleToFitRect_InvalidSource_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                VisionDashboardGeometry.CalculateScaleToFitRect(Vector2.zero, new Vector2(100, 100)));
        }

        [Test]
        public void ClampRectToViewport_KeepsRectInsideViewport()
        {
            Rect rect = VisionDashboardGeometry.ClampRectToViewport(
                new Rect(-10, 20, 80, 100),
                new Vector2(64, 64));

            Assert.That(rect.xMin, Is.EqualTo(0f).Within(0.001f));
            Assert.That(rect.yMin, Is.EqualTo(20f).Within(0.001f));
            Assert.That(rect.xMax, Is.EqualTo(64f).Within(0.001f));
            Assert.That(rect.yMax, Is.EqualTo(64f).Within(0.001f));
        }

        [Test]
        public void ClampLabelPosition_FallsBackBelowTopEdge()
        {
            Vector2 point = VisionDashboardGeometry.ClampLabelPosition(
                new Rect(10, 4, 100, 80),
                new Vector2(80, 20),
                new Vector2(200, 120));

            Assert.That(point.x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(point.y, Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void CalculateAdaptiveStroke_ClampsToReadableRange()
        {
            Assert.That(VisionDashboardGeometry.CalculateAdaptiveStroke(new Vector2(100, 100)), Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(VisionDashboardGeometry.CalculateAdaptiveStroke(new Vector2(4000, 3000)), Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void StableColor_IsDeterministic()
        {
            Color first = VisionDashboardGeometry.StableColor(42);
            Color second = VisionDashboardGeometry.StableColor(42);

            Assert.That(first.r, Is.EqualTo(second.r).Within(0.0001f));
            Assert.That(first.g, Is.EqualTo(second.g).Within(0.0001f));
            Assert.That(first.b, Is.EqualTo(second.b).Within(0.0001f));
        }
    }
}
