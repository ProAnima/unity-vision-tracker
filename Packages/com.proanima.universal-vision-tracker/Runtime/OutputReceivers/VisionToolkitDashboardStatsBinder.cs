using System;
using System.Globalization;
using UnityEngine.UIElements;
using UniversalTracker.Core;

namespace UniversalTracker.OutputReceivers
{
    internal sealed class VisionToolkitDashboardStatsBinder
    {
        private readonly Func<UniversalTrackerManager> managerProvider;
        private Label statusLabel;
        private Label frameLabel;
        private Label fpsLabel;
        private Label inferenceLabel;
        private Label budgetLabel;
        private Label sourceLabel;
        private Label modelLabel;
        private Label runtimeLabel;
        private Label detectionCountLabel;
        private Label poseCountLabel;
        private Label errorLabel;
        private Label lastErrorLabel;

        public VisionToolkitDashboardStatsBinder(Func<UniversalTrackerManager> managerProvider)
        {
            this.managerProvider = managerProvider;
        }

        public void Bind(VisionToolkitDashboardView view)
        {
            statusLabel = view.statusLabel;
            frameLabel = view.frameLabel;
            fpsLabel = view.fpsLabel;
            inferenceLabel = view.inferenceLabel;
            budgetLabel = view.budgetLabel;
            sourceLabel = view.sourceLabel;
            modelLabel = view.modelLabel;
            runtimeLabel = view.runtimeLabel;
            detectionCountLabel = view.detectionCountLabel;
            poseCountLabel = view.poseCountLabel;
            errorLabel = view.errorLabel;
            lastErrorLabel = view.lastErrorLabel;
        }

        public void UpdateStats(VisionFrameResult result)
        {
            UniversalTrackerManager manager = managerProvider?.Invoke();
            UpdateHealth(manager != null ? manager.HealthStatus : null);

            frameLabel.text = result.frameIndex.ToString();
            fpsLabel.text = manager != null ? manager.CurrentFPS.ToString("F1", CultureInfo.InvariantCulture) : "-";
            inferenceLabel.text = result.stats.inferenceMs > 0f
                ? $"{result.stats.inferenceMs.ToString("F1", CultureInfo.InvariantCulture)} ms"
                : "-";
            UpdateBudget(manager, result);
            UpdateRuntimeLabels(manager);
            detectionCountLabel.text = (result.detections?.Length ?? 0).ToString();
            poseCountLabel.text = (result.poses?.Length ?? 0).ToString();
            errorLabel.text = manager != null ? manager.ConsecutiveErrors.ToString() : "0";
            UpdateLastError(manager?.LastError);
        }

        public void UpdateHealth(VisionHealthStatus status)
        {
            if (statusLabel == null)
                return;

            UniversalTrackerManager manager = managerProvider?.Invoke();
            VisionHealthState state = status?.state ?? VisionHealthState.NotInitialized;
            statusLabel.text = state.ToString();
            VisionDashboardTheme.SetPillColor(statusLabel, VisionDashboardTheme.HealthColor(state));

            if (errorLabel != null)
                errorLabel.text = manager != null ? manager.ConsecutiveErrors.ToString() : "0";

            UpdateLastError(status?.lastError);
        }

        private void UpdateRuntimeLabels(UniversalTrackerManager manager)
        {
            if (sourceLabel != null)
                sourceLabel.text = manager != null ? manager.inputType.ToString() : "-";

            VisionModelProfile profile = manager?.ActiveModelProfile ?? manager?.pipelineProfile?.GetDefaultModel();
            if (modelLabel != null)
                modelLabel.text = profile != null ? CreateModelLabel(profile) : "-";

            if (runtimeLabel != null)
                runtimeLabel.text = profile != null ? profile.runtimeKind.ToString() : "-";
        }

        private static string CreateModelLabel(VisionModelProfile profile)
        {
            if (!string.IsNullOrWhiteSpace(profile.displayName))
                return profile.displayName;

            return !string.IsNullOrWhiteSpace(profile.profileId) ? profile.profileId : profile.primaryTask.ToString();
        }

        private void UpdateLastError(VisionError error)
        {
            if (lastErrorLabel == null)
                return;

            lastErrorLabel.text = error == null ? "-" : $"{error.code}: {error.message}";
        }

        private void UpdateBudget(UniversalTrackerManager manager, VisionFrameResult result)
        {
            if (budgetLabel == null)
                return;

            VisionPerformanceBudget budget = manager?.pipelineProfile?.performanceBudget;
            if (budget == null)
            {
                budgetLabel.text = "-";
                return;
            }

            float totalMs = result.stats.totalMs > 0f
                ? result.stats.totalMs
                : result.stats.preprocessMs + result.stats.inferenceMs + result.stats.postprocessMs + result.stats.trackingMs;
            var sample = new VisionPerformanceBudgetSample(
                VisionPerformanceBudgetStage.Pipeline,
                totalMs,
                0,
                result.TotalResultCount);
            VisionPerformanceBudgetResult budgetResult = budget.Evaluate(sample);
            budgetLabel.text = budgetResult.status switch
            {
                VisionPerformanceBudgetStatus.Disabled => "Off",
                VisionPerformanceBudgetStatus.Exceeded => "Over",
                _ => "OK"
            };
            budgetLabel.style.color = budgetResult.status switch
            {
                VisionPerformanceBudgetStatus.Exceeded => VisionDashboardTheme.Warning,
                VisionPerformanceBudgetStatus.Disabled => VisionDashboardTheme.MutedText,
                _ => VisionDashboardTheme.Good
            };
        }
    }
}
