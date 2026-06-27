using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Processing;

namespace UniversalTracker.Tests
{
    public sealed class VisionImageTransformTests
    {
        [Test]
        public void Letterbox_WideSource_ComputesExpectedScaleAndVerticalPadding()
        {
            var transform = VisionImageTransform.Letterbox(
                new Vector2Int(1280, 720),
                new Vector2Int(640, 640));

            Assert.That(transform.preservesAspectRatio, Is.True);
            Assert.That(transform.scale.x, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(transform.scale.y, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(transform.padding.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(transform.padding.y, Is.EqualTo(140f).Within(0.0001f));
            Assert.That(transform.scaledSourceSize.x, Is.EqualTo(640f).Within(0.0001f));
            Assert.That(transform.scaledSourceSize.y, Is.EqualTo(360f).Within(0.0001f));
        }

        [Test]
        public void Letterbox_SourceCenter_MapsToModelCenter()
        {
            var transform = VisionImageTransform.Letterbox(
                new Vector2Int(1280, 720),
                new Vector2Int(640, 640));

            var modelPoint = transform.SourceToModelPoint(new Vector2(640, 360));

            Assert.That(modelPoint.x, Is.EqualTo(320f).Within(0.0001f));
            Assert.That(modelPoint.y, Is.EqualTo(320f).Within(0.0001f));
        }

        [Test]
        public void Letterbox_RectRoundTrip_PreservesSourceRect()
        {
            var transform = VisionImageTransform.Letterbox(
                new Vector2Int(1920, 1080),
                new Vector2Int(640, 640));

            var sourceRect = new Rect(100, 50, 800, 400);
            var modelRect = transform.SourceToModelRect(sourceRect);
            var roundTrip = transform.ModelToSourceRect(modelRect);

            AssertRect(roundTrip, sourceRect, 0.001f);
        }

        [Test]
        public void Stretch_SourceBottomRight_MapsToModelBottomRight()
        {
            var transform = VisionImageTransform.Stretch(
                new Vector2Int(800, 600),
                new Vector2Int(320, 320));

            var modelPoint = transform.SourceToModelPoint(new Vector2(800, 600));

            Assert.That(modelPoint.x, Is.EqualTo(320f).Within(0.0001f));
            Assert.That(modelPoint.y, Is.EqualTo(320f).Within(0.0001f));
        }

        [Test]
        public void NormalizedRectRoundTrip_PreservesSourceRect()
        {
            var transform = VisionImageTransform.Stretch(
                new Vector2Int(800, 600),
                new Vector2Int(320, 320));

            var sourceRect = new Rect(80, 60, 400, 300);
            var normalized = transform.SourceToNormalizedRect(sourceRect);
            var roundTrip = transform.NormalizedToSourceRect(normalized);

            AssertRect(roundTrip, sourceRect, 0.001f);
        }

        [Test]
        public void InvalidSourceSize_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                VisionImageTransform.Letterbox(new Vector2Int(0, 720), new Vector2Int(640, 640)));
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

