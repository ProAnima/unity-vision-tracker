using System;
using UnityEngine;

namespace UniversalTracker.Core
{
    public enum VisionPerformanceBudgetStage
    {
        Parser,
        Overlay,
        Tracking,
        Pipeline
    }

    public enum VisionPerformanceBudgetStatus
    {
        Disabled,
        WithinBudget,
        Exceeded
    }

    [Serializable]
    public struct VisionPerformanceBudgetSample
    {
        public VisionPerformanceBudgetStage stage;
        public float elapsedMs;
        public int allocatedBytes;
        public int itemCount;

        public VisionPerformanceBudgetSample(VisionPerformanceBudgetStage stage, float elapsedMs, int allocatedBytes, int itemCount)
        {
            this.stage = stage;
            this.elapsedMs = Mathf.Max(0f, elapsedMs);
            this.allocatedBytes = Mathf.Max(0, allocatedBytes);
            this.itemCount = Mathf.Max(0, itemCount);
        }
    }

    [Serializable]
    public struct VisionStagePerformanceBudget
    {
        public bool enabled;
        [Min(0f)] public float maxMilliseconds;
        [Min(0)] public int maxAllocatedBytes;
        [Min(0)] public int maxItems;

        public VisionStagePerformanceBudget(float maxMilliseconds, int maxAllocatedBytes, int maxItems)
        {
            enabled = true;
            this.maxMilliseconds = maxMilliseconds;
            this.maxAllocatedBytes = maxAllocatedBytes;
            this.maxItems = maxItems;
        }

        public VisionPerformanceBudgetResult Evaluate(VisionPerformanceBudgetStage stage, VisionPerformanceBudgetSample sample)
        {
            if (!enabled)
                return VisionPerformanceBudgetResult.Disabled(stage, "Stage budget is disabled.");

            bool exceededTime = maxMilliseconds > 0f && sample.elapsedMs > maxMilliseconds;
            bool exceededAllocation = maxAllocatedBytes > 0 && sample.allocatedBytes > maxAllocatedBytes;
            bool exceededItems = maxItems > 0 && sample.itemCount > maxItems;

            if (!exceededTime && !exceededAllocation && !exceededItems)
                return VisionPerformanceBudgetResult.WithinBudget(stage);

            return VisionPerformanceBudgetResult.Exceeded(stage, exceededTime, exceededAllocation, exceededItems);
        }
    }

    [Serializable]
    public struct VisionPerformanceBudgetResult
    {
        public VisionPerformanceBudgetStage stage;
        public VisionPerformanceBudgetStatus status;
        public bool exceededTime;
        public bool exceededAllocation;
        public bool exceededItems;
        public string message;

        public bool IsWithinBudget => status == VisionPerformanceBudgetStatus.WithinBudget ||
                                      status == VisionPerformanceBudgetStatus.Disabled;

        public static VisionPerformanceBudgetResult Disabled(VisionPerformanceBudgetStage stage, string message)
        {
            return new VisionPerformanceBudgetResult
            {
                stage = stage,
                status = VisionPerformanceBudgetStatus.Disabled,
                message = message
            };
        }

        public static VisionPerformanceBudgetResult WithinBudget(VisionPerformanceBudgetStage stage)
        {
            return new VisionPerformanceBudgetResult
            {
                stage = stage,
                status = VisionPerformanceBudgetStatus.WithinBudget,
                message = "Within budget."
            };
        }

        public static VisionPerformanceBudgetResult Exceeded(
            VisionPerformanceBudgetStage stage,
            bool exceededTime,
            bool exceededAllocation,
            bool exceededItems)
        {
            return new VisionPerformanceBudgetResult
            {
                stage = stage,
                status = VisionPerformanceBudgetStatus.Exceeded,
                exceededTime = exceededTime,
                exceededAllocation = exceededAllocation,
                exceededItems = exceededItems,
                message = "Performance budget exceeded."
            };
        }
    }

    [Serializable]
    public sealed class VisionPerformanceBudget
    {
        public bool enabled = true;
        public VisionStagePerformanceBudget parser = new VisionStagePerformanceBudget(4f, 64 * 1024, 512);
        public VisionStagePerformanceBudget overlay = new VisionStagePerformanceBudget(3f, 32 * 1024, 1024);
        public VisionStagePerformanceBudget tracking = new VisionStagePerformanceBudget(2f, 16 * 1024, 256);
        public VisionStagePerformanceBudget pipeline = new VisionStagePerformanceBudget(33.3f, 128 * 1024, 1024);

        public static VisionPerformanceBudget Default => new VisionPerformanceBudget();

        public VisionPerformanceBudgetResult Evaluate(VisionPerformanceBudgetSample sample)
        {
            if (!enabled)
                return VisionPerformanceBudgetResult.Disabled(sample.stage, "Performance budget is disabled.");

            return sample.stage switch
            {
                VisionPerformanceBudgetStage.Parser => parser.Evaluate(sample.stage, sample),
                VisionPerformanceBudgetStage.Overlay => overlay.Evaluate(sample.stage, sample),
                VisionPerformanceBudgetStage.Tracking => tracking.Evaluate(sample.stage, sample),
                VisionPerformanceBudgetStage.Pipeline => pipeline.Evaluate(sample.stage, sample),
                _ => VisionPerformanceBudgetResult.Exceeded(sample.stage, true, false, false)
            };
        }
    }
}
