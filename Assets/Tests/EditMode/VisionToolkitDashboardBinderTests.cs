using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;

namespace UniversalTracker.Tests
{
    public sealed class VisionToolkitDashboardBinderTests
    {
        [Test]
        public void ResultListBinder_WithNoResults_ShowsEmptyRow()
        {
            var list = new VisualElement();
            var binder = new VisionToolkitDashboardResultListBinder();
            binder.Bind(list);

            binder.UpdateRows(
                VisionFrameResult.Empty(1, 0d, new Vector2Int(640, 480)),
                new VisionDashboardResultListSettings(true, true, true, 8));

            Assert.That(list.childCount, Is.EqualTo(1));
            Assert.That(((Label)list[0]).text, Is.EqualTo("No active results"));
            Assert.That(list[0].style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void ResultListBinder_RespectsMaxRows()
        {
            var list = new VisualElement();
            var binder = new VisionToolkitDashboardResultListBinder();
            var result = VisionFrameResult.Empty(1, 0d, new Vector2Int(640, 480));
            result.detections = new[]
            {
                new VisionDetection { classId = 1, label = "Person", confidence = 0.9f },
                new VisionDetection { classId = 2, label = "Prop", confidence = 0.8f }
            };
            binder.Bind(list);

            binder.UpdateRows(result, new VisionDashboardResultListSettings(true, false, false, 1));

            Assert.That(list.childCount, Is.EqualTo(1));
            Assert.That(((Label)list[0]).text, Does.Contain("Person"));
        }

        [Test]
        public void StatsBinder_UpdatesHealthAndFrameLabels()
        {
            var frameLabel = new Label();
            var fpsLabel = new Label();
            var inferenceLabel = new Label();
            var detectionLabel = new Label();
            var poseLabel = new Label();
            var errorLabel = new Label();
            var statusLabel = new Label();
            var view = new VisionToolkitDashboardView
            {
                frameLabel = frameLabel,
                fpsLabel = fpsLabel,
                inferenceLabel = inferenceLabel,
                detectionCountLabel = detectionLabel,
                poseCountLabel = poseLabel,
                errorLabel = errorLabel,
                statusLabel = statusLabel
            };
            var binder = new VisionToolkitDashboardStatsBinder(() => null);
            var result = VisionFrameResult.Empty(7, 0d, new Vector2Int(640, 480));
            result.stats.inferenceMs = 12.5f;
            result.detections = new[] { new VisionDetection() };
            result.poses = new[] { new VisionPose() };
            binder.Bind(view);

            binder.UpdateStats(result);
            binder.UpdateHealth(VisionHealthStatus.Create(VisionHealthState.Running, VisionHealthState.Initializing, VisionHealthEvent.Started, "running"));

            Assert.That(frameLabel.text, Is.EqualTo("7"));
            Assert.That(fpsLabel.text, Is.EqualTo("-"));
            Assert.That(inferenceLabel.text, Is.EqualTo("12.5 ms"));
            Assert.That(detectionLabel.text, Is.EqualTo("1"));
            Assert.That(poseLabel.text, Is.EqualTo("1"));
            Assert.That(errorLabel.text, Is.EqualTo("0"));
            Assert.That(statusLabel.text, Is.EqualTo("Running"));
        }
    }
}
