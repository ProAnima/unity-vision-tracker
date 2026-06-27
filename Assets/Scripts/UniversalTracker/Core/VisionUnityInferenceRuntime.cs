using System;
using UnityEngine;
using UniversalTracker.Models;

namespace UniversalTracker.Core
{
    public sealed class UnityInferenceRuntimeAdapter : IVisionRuntimeAdapter
    {
        private readonly Func<VisionModelProfile, ModelConfig> configFactory;
        private readonly Func<ModelConfig, IInferenceModel> modelFactory;
        private readonly Func<VisionModelProfile, IVisionRawOutputProvider> rawProviderFactory;
        private readonly IVisionOutputParser outputParser;
        private readonly VisionModelCapability capabilities;
        private IInferenceModel model;
        private IVisionRawOutputProvider rawProvider;
        private ModelConfig config;
        private VisionModelProfile activeProfile;
        private string[] labels = Array.Empty<string>();

        public UnityInferenceRuntimeAdapter(
            Func<VisionModelProfile, ModelConfig> configFactory,
            Func<ModelConfig, IInferenceModel> modelFactory,
            VisionModelCapability capabilities)
        {
            this.configFactory = configFactory ?? throw new ArgumentNullException(nameof(configFactory));
            this.modelFactory = modelFactory ?? throw new ArgumentNullException(nameof(modelFactory));
            this.capabilities = capabilities;
        }

        public UnityInferenceRuntimeAdapter(
            Func<VisionModelProfile, IVisionRawOutputProvider> rawProviderFactory,
            IVisionOutputParser outputParser,
            VisionModelCapability capabilities)
        {
            this.rawProviderFactory = rawProviderFactory ?? throw new ArgumentNullException(nameof(rawProviderFactory));
            this.outputParser = outputParser ?? throw new ArgumentNullException(nameof(outputParser));
            this.capabilities = capabilities;
        }

        public VisionRuntimeKind RuntimeKind => VisionRuntimeKind.UnityInferenceEngine;
        public VisionModelCapability Capabilities => capabilities;
        public bool IsInitialized =>
            rawProvider != null ? rawProvider.IsInitialized : model != null && model.IsInitialized;

        public void Initialize(VisionModelProfile profile)
        {
            activeProfile = profile;
            labels = ParseLabels(profile?.labels);

            if (rawProviderFactory != null)
            {
                rawProvider = rawProviderFactory(profile);
                if (rawProvider == null)
                    throw new InvalidOperationException("Unity inference raw output provider factory returned null.");

                rawProvider.Initialize(profile);
                return;
            }

            config = configFactory(profile);
            if (config == null)
                throw new InvalidOperationException("Unity inference runtime config factory returned null.");

            model = modelFactory(config);
            if (model == null)
                throw new InvalidOperationException("Unity inference model factory returned null.");

            model.Initialize(config);
        }

        public VisionFrameResult ProcessFrame(VisionFrame frame)
        {
            if (!frame.IsValid)
                return VisionFrameResult.Empty(frame.frameIndex, frame.timestamp, frame.sourceSize);

            if (rawProvider != null)
            {
                VisionRawModelOutput rawOutput = rawProvider.Execute(frame.texture);
                var context = new VisionOutputParserContext(
                    frame.sourceSize,
                    activeProfile != null ? activeProfile.confidenceThreshold : 0.5f,
                    activeProfile != null ? activeProfile.nmsThreshold : 0.45f,
                    labels);
                VisionParsedOutput parsed = outputParser.Parse(rawOutput, context);
                return parsed.ToFrameResult(frame.frameIndex, frame.timestamp, frame.sourceSize);
            }

            InferenceResult result = model.RunInference(frame.texture);
            return VisionResultAdapter.FromInferenceResult(result, frame.texture, frame.frameIndex, frame.timestamp);
        }

        public void Dispose()
        {
            rawProvider?.Dispose();
            rawProvider = null;
            model?.Dispose();
            model = null;
            config = null;
            activeProfile = null;
            labels = Array.Empty<string>();
        }

        private static string[] ParseLabels(TextAsset labelsAsset)
        {
            if (labelsAsset == null || string.IsNullOrWhiteSpace(labelsAsset.text))
                return Array.Empty<string>();

            string[] lines = labelsAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].Trim();

            return lines;
        }
    }

    public class YoloModelAdapter : IVisionModelAdapter
    {
        public virtual string AdapterId => "proanima.yolo.unity-inference";
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

        public IVisionRuntimeAdapter CreateRuntime(VisionModelProfile profile)
        {
            VisionOutputParserRegistry parserRegistry = VisionOutputParserRegistry.CreateDefault();
            Func<VisionModelProfile, IVisionRawOutputProvider> rawProviderFactory = CreateRawOutputProviderFactory(profile);
            if (rawProviderFactory != null && parserRegistry.TryGetParser(profile, out IVisionOutputParser parser))
            {
                return new UnityInferenceRuntimeAdapter(
                    rawProviderFactory,
                    parser,
                    InferCapabilities(profile));
            }

            return new UnityInferenceRuntimeAdapter(
                ToModelConfig,
                YOLOModelFactory.CreateModel,
                InferCapabilities(profile));
        }

        protected virtual Func<VisionModelProfile, IVisionRawOutputProvider> CreateRawOutputProviderFactory(VisionModelProfile profile)
        {
            return null;
        }

        public static ModelConfig ToModelConfig(VisionModelProfile profile)
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

        public static VisionModelCapability InferCapabilities(VisionModelProfile profile)
        {
            if (profile == null)
                return VisionModelCapability.None;

            VisionModelCapability capabilities = profile.capabilities;
            if (capabilities != VisionModelCapability.None)
                return capabilities;

            string name = $"{profile.displayName} {profile.name}".ToLowerInvariant();
            return InferCapabilities(name);
        }

        public static VisionModelCapability InferCapabilities(ModelConfig config)
        {
            return InferCapabilities(config?.modelName);
        }

        private static VisionModelCapability InferCapabilities(string modelName)
        {
            string name = modelName?.ToLowerInvariant() ?? string.Empty;
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

    public sealed class YoloLegacyModelAdapter : YoloModelAdapter
    {
        public override string AdapterId => "proanima.yolo.legacy";

        public static ModelConfig ToLegacyConfig(VisionModelProfile profile)
        {
            return ToModelConfig(profile);
        }
    }
}
