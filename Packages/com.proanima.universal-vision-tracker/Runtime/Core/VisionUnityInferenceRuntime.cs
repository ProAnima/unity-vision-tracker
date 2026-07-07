using System;
using UnityEngine;

namespace UniversalTracker.Core
{
    public sealed class UnityInferenceRuntimeAdapter : IVisionRuntimeAdapter
    {
        private readonly Func<VisionModelProfile, IVisionRawOutputProvider> rawProviderFactory;
        private readonly IVisionOutputParser outputParser;
        private readonly VisionModelCapability capabilities;
        private IVisionRawOutputProvider rawProvider;
        private VisionModelProfile activeProfile;
        private string[] labels = Array.Empty<string>();

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
        public bool IsInitialized => rawProvider != null && rawProvider.IsInitialized;

        public void Initialize(VisionModelProfile profile)
        {
            activeProfile = profile;
            labels = ParseLabels(profile?.labels);
            rawProvider = rawProviderFactory(profile);
            if (rawProvider == null)
                throw new InvalidOperationException("Unity inference raw output provider factory returned null.");

            rawProvider.Initialize(profile);
        }

        public VisionFrameResult ProcessFrame(VisionFrame frame)
        {
            if (!frame.IsValid)
                return VisionFrameResult.Empty(frame.frameIndex, frame.timestamp, frame.sourceSize);

            VisionRawModelOutput rawOutput = rawProvider.Execute(frame.texture);
            var context = new VisionOutputParserContext(
                frame.sourceSize,
                activeProfile != null ? activeProfile.confidenceThreshold : 0.5f,
                activeProfile != null ? activeProfile.nmsThreshold : 0.45f,
                labels,
                ResolveModelInputSize(activeProfile),
                activeProfile != null ? activeProfile.outputTransform : VisionOutputCoordinateTransform.Identity);
            VisionParsedOutput parsed = outputParser.Parse(rawOutput, context);
            return parsed.ToFrameResult(frame.frameIndex, frame.timestamp, frame.sourceSize);
        }

        public void Dispose()
        {
            rawProvider?.Dispose();
            rawProvider = null;
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

        private static Vector2Int ResolveModelInputSize(VisionModelProfile profile)
        {
            if (profile == null)
                return new Vector2Int(640, 640);

            int width = profile.input.width > 0 ? profile.input.width : 640;
            int height = profile.input.height > 0 ? profile.input.height : 640;
            return new Vector2Int(width, height);
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
                   profile.modelAsset != null &&
                   profile.output.IsValid;
        }

        public IVisionRuntimeAdapter CreateRuntime(VisionModelProfile profile)
        {
            VisionOutputParserRegistry parserRegistry = VisionOutputParserRegistry.CreateDefault();
            if (!parserRegistry.TryGetParser(profile, out IVisionOutputParser parser))
                throw new InvalidOperationException($"No output parser registered for profile '{profile?.name}'.");

            return new UnityInferenceRuntimeAdapter(
                modelProfile => new UnityInferenceRawOutputProvider(modelProfile),
                parser,
                InferCapabilities(profile));
        }

        public static VisionModelCapability InferCapabilities(VisionModelProfile profile)
        {
            if (profile == null)
                return VisionModelCapability.None;

            if (profile.capabilities != VisionModelCapability.None)
                return profile.capabilities;

            string name = $"{profile.displayName} {profile.name}".ToLowerInvariant();
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
