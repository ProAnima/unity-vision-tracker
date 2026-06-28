using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionPerformanceBudgetTests
    {
        [Test]
        public void DefaultBudget_AcceptsNormalParserSample()
        {
            VisionPerformanceBudget budget = VisionPerformanceBudget.Default;
            var sample = new VisionPerformanceBudgetSample(
                VisionPerformanceBudgetStage.Parser,
                elapsedMs: 1.5f,
                allocatedBytes: 8 * 1024,
                itemCount: 12);

            VisionPerformanceBudgetResult result = budget.Evaluate(sample);

            Assert.That(result.status, Is.EqualTo(VisionPerformanceBudgetStatus.WithinBudget));
            Assert.That(result.IsWithinBudget, Is.True);
        }

        [Test]
        public void Evaluate_FlagsElapsedTimeAllocationAndItemOverruns()
        {
            VisionPerformanceBudget budget = VisionPerformanceBudget.Default;
            var sample = new VisionPerformanceBudgetSample(
                VisionPerformanceBudgetStage.Overlay,
                elapsedMs: budget.overlay.maxMilliseconds + 0.5f,
                allocatedBytes: budget.overlay.maxAllocatedBytes + 1,
                itemCount: budget.overlay.maxItems + 1);

            VisionPerformanceBudgetResult result = budget.Evaluate(sample);

            Assert.That(result.status, Is.EqualTo(VisionPerformanceBudgetStatus.Exceeded));
            Assert.That(result.exceededTime, Is.True);
            Assert.That(result.exceededAllocation, Is.True);
            Assert.That(result.exceededItems, Is.True);
            Assert.That(result.IsWithinBudget, Is.False);
        }

        [Test]
        public void DisabledBudget_ReturnsDisabledResult()
        {
            VisionPerformanceBudget budget = VisionPerformanceBudget.Default;
            budget.enabled = false;
            var sample = new VisionPerformanceBudgetSample(
                VisionPerformanceBudgetStage.Tracking,
                elapsedMs: 999f,
                allocatedBytes: 999999,
                itemCount: 999);

            VisionPerformanceBudgetResult result = budget.Evaluate(sample);

            Assert.That(result.status, Is.EqualTo(VisionPerformanceBudgetStatus.Disabled));
            Assert.That(result.IsWithinBudget, Is.True);
        }

        [Test]
        public void DisabledStageBudget_ReturnsDisabledResult()
        {
            VisionPerformanceBudget budget = VisionPerformanceBudget.Default;
            budget.tracking.enabled = false;
            var sample = new VisionPerformanceBudgetSample(
                VisionPerformanceBudgetStage.Tracking,
                elapsedMs: 999f,
                allocatedBytes: 999999,
                itemCount: 999);

            VisionPerformanceBudgetResult result = budget.Evaluate(sample);

            Assert.That(result.status, Is.EqualTo(VisionPerformanceBudgetStatus.Disabled));
            Assert.That(result.stage, Is.EqualTo(VisionPerformanceBudgetStage.Tracking));
        }

        [Test]
        public void PipelineProfile_CreatesDefaultPerformanceBudget()
        {
            var profile = ScriptableObject.CreateInstance<VisionPipelineProfile>();

            Assert.That(profile.performanceBudget, Is.Not.Null);
            Assert.That(profile.performanceBudget.parser.enabled, Is.True);
            Assert.That(profile.performanceBudget.overlay.maxItems, Is.GreaterThan(0));
            Assert.That(profile.performanceBudget.tracking.maxMilliseconds, Is.GreaterThan(0f));

            Object.DestroyImmediate(profile);
        }
    }
}
