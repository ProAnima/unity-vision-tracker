using System;
using UnityEngine;
using UniversalTracker.Models;

namespace UniversalTracker.Core
{
    public sealed class LegacyInputProviderFrameSource : IVisionFrameSource
    {
        private readonly IInputProvider provider;
        private readonly VisionFrameSourceType sourceType;
        private int frameIndex;

        public LegacyInputProviderFrameSource(IInputProvider provider, VisionFrameSourceType sourceType = VisionFrameSourceType.Custom)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.sourceType = sourceType;
        }

        public bool IsReady => provider.IsReady;
        public Vector2Int SourceSize => provider.Resolution;
        public VisionFrameSourceType SourceType => sourceType;

        public void Initialize()
        {
            provider.Initialize();
        }

        public bool TryGetFrame(out VisionFrame frame)
        {
            frame = default;
            provider.UpdateTexture();

            Texture texture = provider.CurrentTexture;
            if (!provider.IsReady || texture == null)
                return false;

            Vector2Int size = provider.Resolution;
            if (size.x <= 0 || size.y <= 0)
                size = new Vector2Int(texture.width, texture.height);

            frame = new VisionFrame(
                texture,
                ++frameIndex,
                Time.realtimeSinceStartupAsDouble,
                size,
                sourceType);

            return frame.IsValid;
        }

        public void Dispose()
        {
            provider.Release();
        }
    }

    public sealed class LegacyInferenceRuntimeAdapter : IVisionRuntimeAdapter
    {
        private readonly IInferenceModel model;
        private readonly ModelConfig legacyConfig;
        private readonly VisionModelCapability capabilities;

        public LegacyInferenceRuntimeAdapter(
            IInferenceModel model,
            ModelConfig legacyConfig,
            VisionModelCapability capabilities = VisionModelCapability.Detection)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.legacyConfig = legacyConfig ?? throw new ArgumentNullException(nameof(legacyConfig));
            this.capabilities = capabilities;
        }

        public VisionRuntimeKind RuntimeKind => VisionRuntimeKind.UnityInferenceEngine;
        public VisionModelCapability Capabilities => capabilities;
        public bool IsInitialized => model.IsInitialized;

        public void Initialize(VisionModelProfile profile)
        {
            model.Initialize(legacyConfig);
        }

        public VisionFrameResult ProcessFrame(VisionFrame frame)
        {
            if (!frame.IsValid)
                return VisionFrameResult.Empty(frame.frameIndex, frame.timestamp, frame.sourceSize);

            InferenceResult result = model.RunInference(frame.texture);
            return VisionResultAdapter.FromInferenceResult(result, frame.texture, frame.frameIndex, frame.timestamp);
        }

        public void Dispose()
        {
            model.Dispose();
        }
    }

    public sealed class YoloLegacyModelAdapter : IVisionModelAdapter
    {
        public string AdapterId => "proanima.yolo.legacy";
        public VisionModelFamily Family => VisionModelFamily.YOLO;
        public VisionModelCapability Capabilities =>
            VisionModelCapability.Detection |
            VisionModelCapability.HumanDetection |
            VisionModelCapability.Pose2D |
            VisionModelCapability.Segmentation;

        public bool CanHandle(VisionModelProfile profile)
        {
            return profile != null &&
                   profile.runtimeKind == VisionRuntimeKind.UnityInferenceEngine &&
                   profile.family == VisionModelFamily.YOLO &&
                   profile.modelAsset != null;
        }

        public IVisionRuntimeAdapter CreateRuntime()
        {
            throw new InvalidOperationException("Use CreateRuntime(ModelConfig) for the legacy YOLO adapter.");
        }

        public IVisionRuntimeAdapter CreateRuntime(ModelConfig config)
        {
            IInferenceModel model = YOLOModelFactory.CreateModel(config);
            VisionModelCapability inferred = InferCapabilities(config);
            return new LegacyInferenceRuntimeAdapter(model, config, inferred);
        }

        public static ModelConfig ToLegacyConfig(VisionModelProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            return new ModelConfig
            {
                modelName = string.IsNullOrWhiteSpace(profile.displayName) ? profile.name : profile.displayName,
                modelAsset = profile.modelAsset,
                backend = profile.backend,
                inputSize = profile.input.IsValid ? Mathf.Max(profile.input.width, profile.input.height) : 640,
                confidenceThreshold = profile.confidenceThreshold,
                nmsThreshold = profile.nmsThreshold
            };
        }

        public static VisionModelCapability InferCapabilities(ModelConfig config)
        {
            string name = config?.modelName?.ToLowerInvariant() ?? string.Empty;
            VisionModelCapability capabilities = VisionModelCapability.Detection;

            if (name.Contains("pose"))
                capabilities |= VisionModelCapability.Pose2D | VisionModelCapability.HumanDetection;
            if (name.Contains("seg"))
                capabilities |= VisionModelCapability.Segmentation;
            if (name.Contains("person") || name.Contains("human"))
                capabilities |= VisionModelCapability.HumanDetection;

            return capabilities;
        }
    }
}
