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
            var budgetLabel = new Label();
            var detectionLabel = new Label();
            var poseLabel = new Label();
            var errorLabel = new Label();
            var statusLabel = new Label();
            var view = new VisionToolkitDashboardView
            {
                frameLabel = frameLabel,
                fpsLabel = fpsLabel,
                inferenceLabel = inferenceLabel,
                budgetLabel = budgetLabel,
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
            Assert.That(budgetLabel.text, Is.EqualTo("-"));
            Assert.That(detectionLabel.text, Is.EqualTo("1"));
            Assert.That(poseLabel.text, Is.EqualTo("1"));
            Assert.That(errorLabel.text, Is.EqualTo("0"));
            Assert.That(statusLabel.text, Is.EqualTo("Running"));
        }

        [Test]
        public void StatsBinder_ReportsPipelineBudgetStatus()
        {
            var go = new GameObject("BudgetDashboardBinderTest");
            try
            {
                UniversalTrackerManager manager = go.AddComponent<UniversalTrackerManager>();
                manager.pipelineProfile = ScriptableObject.CreateInstance<VisionPipelineProfile>();
                manager.pipelineProfile.performanceBudget.pipeline = new VisionStagePerformanceBudget(1f, 0, 1);
                var budgetLabel = new Label();
                var view = new VisionToolkitDashboardView
                {
                    frameLabel = new Label(),
                    fpsLabel = new Label(),
                    inferenceLabel = new Label(),
                    budgetLabel = budgetLabel,
                    detectionCountLabel = new Label(),
                    poseCountLabel = new Label(),
                    errorLabel = new Label(),
                    statusLabel = new Label()
                };
                var result = VisionFrameResult.Empty(1, 0d, new Vector2Int(640, 480));
                result.stats = VisionPerformanceStats.FromStages(0f, 2f, 0f, 0f);
                result.detections = new[] { new VisionDetection(), new VisionDetection() };
                var binder = new VisionToolkitDashboardStatsBinder(() => manager);
                binder.Bind(view);

                binder.UpdateStats(result);

                Assert.That(budgetLabel.text, Is.EqualTo("Over"));
            }
            finally
            {
                UniversalTrackerManager manager = go.GetComponent<UniversalTrackerManager>();
                if (manager != null && manager.pipelineProfile != null)
                    Object.DestroyImmediate(manager.pipelineProfile);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void StatsBinder_ReportsRuntimeContextAndLastError()
        {
            var go = new GameObject("RuntimeContextDashboardBinderTest");
            try
            {
                UniversalTrackerManager manager = go.AddComponent<UniversalTrackerManager>();
                manager.inputType = InputProviderType.RenderTexture;
                manager.pipelineProfile = ScriptableObject.CreateInstance<VisionPipelineProfile>();
                manager.pipelineProfile.models = new[]
                {
                    ScriptableObject.CreateInstance<VisionModelProfile>()
                };
                manager.pipelineProfile.models[0].displayName = "Pose Runtime";
                manager.pipelineProfile.models[0].runtimeKind = VisionRuntimeKind.Mock;

                var sourceLabel = new Label();
                var modelLabel = new Label();
                var runtimeLabel = new Label();
                var lastErrorLabel = new Label();
                var view = new VisionToolkitDashboardView
                {
                    frameLabel = new Label(),
                    fpsLabel = new Label(),
                    inferenceLabel = new Label(),
                    budgetLabel = new Label(),
                    sourceLabel = sourceLabel,
                    modelLabel = modelLabel,
                    runtimeLabel = runtimeLabel,
                    detectionCountLabel = new Label(),
                    poseCountLabel = new Label(),
                    errorLabel = new Label(),
                    lastErrorLabel = lastErrorLabel,
                    statusLabel = new Label()
                };
                var binder = new VisionToolkitDashboardStatsBinder(() => manager);
                binder.Bind(view);

                binder.UpdateStats(VisionFrameResult.Empty(1, 0d, new Vector2Int(640, 480)));
                binder.UpdateHealth(VisionHealthStatus.Create(
                    VisionHealthState.Degraded,
                    VisionHealthState.Running,
                    VisionHealthEvent.Degraded,
                    "source failed",
                    new VisionError(VisionErrorCode.SourceNotReady, "Camera is warming up.")));

                Assert.That(sourceLabel.text, Is.EqualTo("RenderTexture"));
                Assert.That(modelLabel.text, Is.EqualTo("Pose Runtime"));
                Assert.That(runtimeLabel.text, Is.EqualTo("Mock"));
                Assert.That(lastErrorLabel.text, Does.Contain("SourceNotReady"));
            }
            finally
            {
                UniversalTrackerManager manager = go.GetComponent<UniversalTrackerManager>();
                if (manager != null && manager.pipelineProfile != null)
                {
                    if (manager.pipelineProfile.models != null)
                    {
                        foreach (VisionModelProfile profile in manager.pipelineProfile.models)
                            Object.DestroyImmediate(profile);
                    }

                    Object.DestroyImmediate(manager.pipelineProfile);
                }

                Object.DestroyImmediate(go);
            }
        }
    }
}
