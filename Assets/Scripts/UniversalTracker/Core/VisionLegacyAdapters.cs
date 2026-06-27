using System;
using UnityEngine;
using UniversalTracker.Models;

namespace UniversalTracker.Core
{
    public sealed class LegacyInputProviderFrameSource : IVisionFrameSource
    {
        private readonly IInputProvider provider;
        private readonly VisionFrameSourceType sourceType;
        private readonly bool initializeProvider;
        private int frameIndex;

        public LegacyInputProviderFrameSource(
            IInputProvider provider,
            VisionFrameSourceType sourceType = VisionFrameSourceType.Custom,
            bool initializeProvider = true)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.sourceType = sourceType;
            this.initializeProvider = initializeProvider;
        }

        public bool IsReady => provider.IsReady;
        public Vector2Int SourceSize => provider.Resolution;
        public VisionFrameSourceType SourceType => sourceType;

        public void Initialize()
        {
            if (initializeProvider)
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

}
