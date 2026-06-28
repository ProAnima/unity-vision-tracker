using System;
using UnityEngine;

namespace UniversalTracker.Core
{
    [CreateAssetMenu(menuName = "ProAnima Vision/Pipeline Profile", fileName = "VisionPipelineProfile")]
    public sealed class VisionPipelineProfile : ScriptableObject
    {
        [Header("Models")]
        public VisionModelProfile[] models = Array.Empty<VisionModelProfile>();
        public int defaultModelIndex;

        [Header("Runtime")]
        [Range(1, 120)] public int targetFps = 30;
        public bool enableTracking = true;
        public bool enableDebugOverlay = true;

        [Header("Health")]
        [Range(1, 100)] public int maxConsecutiveRecoverableErrors = 10;

        [Header("Performance Budgets")]
        public VisionPerformanceBudget performanceBudget = VisionPerformanceBudget.Default;

        public bool HasModels => models != null && models.Length > 0;

        public VisionModelProfile GetDefaultModel()
        {
            if (!HasModels)
                return null;

            int index = Mathf.Clamp(defaultModelIndex, 0, models.Length - 1);
            return models[index];
        }
    }
}
