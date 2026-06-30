using System;
using UnityEngine;

namespace UniversalTracker.Core
{
    [CreateAssetMenu(menuName = "ProAnima Vision/Pipeline Profile", fileName = "VisionPipelineProfile")]
    public sealed class VisionPipelineProfile : ScriptableObject
    {
        [Header("Models")]
        [Tooltip("Ordered model profiles available to this pipeline.")]
        public VisionModelProfile[] models = Array.Empty<VisionModelProfile>();
        [Tooltip("Model index selected when the pipeline starts.")]
        public int defaultModelIndex;

        [Header("Runtime")]
        [Tooltip("Default target processing rate for managers using this profile.")]
        [Range(1, 120)] public int targetFps = 30;
        [Tooltip("Enable tracking by default for managers using this profile.")]
        public bool enableTracking = true;
        [Tooltip("Enable debug overlay output by default for sample dashboards.")]
        public bool enableDebugOverlay = true;

        [Header("Health")]
        [Tooltip("Recoverable error limit before the pipeline should be considered failed.")]
        [Range(1, 100)] public int maxConsecutiveRecoverableErrors = 10;

        [Header("Performance Budgets")]
        [Tooltip("Runtime budgets used to flag slow or noisy parser, overlay, tracking, and pipeline stages.")]
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
