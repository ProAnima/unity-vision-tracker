using NUnit.Framework;
using UnityEngine;
using Unity.InferenceEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionRuntimeAdapterTests
    {
        [Test]
        public void VisionModelProfile_SupportsRequiredCapabilities()
        {
            var profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            profile.capabilities = VisionModelCapability.Detection | VisionModelCapability.Pose2D;

            Assert.That(profile.Supports(VisionModelCapability.Detection), Is.True);
            Assert.That(profile.Supports(VisionModelCapability.Detection | VisionModelCapability.Pose2D), Is.True);
            Assert.That(profile.Supports(VisionModelCapability.Segmentation), Is.False);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void InputSchema_ImageSquareCreatesValidDefault()
        {
            VisionInputSchema schema = VisionInputSchema.ImageSquare(640);

            Assert.That(schema.IsValid, Is.True);
            Assert.That(schema.width, Is.EqualTo(640));
            Assert.That(schema.height, Is.EqualTo(640));
            Assert.That(schema.channels, Is.EqualTo(3));
            Assert.That(schema.preserveAspectRatio, Is.True);
        }

        [Test]
        public void YoloLegacyAdapter_ToLegacyConfigCopiesProfileSettings()
        {
            var profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            profile.displayName = "yolo26-pose";
            profile.backend = BackendType.CPU;
            profile.input = VisionInputSchema.ImageSquare(512);
            profile.confidenceThreshold = 0.35f;
            profile.nmsThreshold = 0.4f;

            ModelConfig config = YoloLegacyModelAdapter.ToLegacyConfig(profile);

            Assert.That(config.modelName, Is.EqualTo("yolo26-pose"));
            Assert.That(config.backend, Is.EqualTo(BackendType.CPU));
            Assert.That(config.inputSize, Is.EqualTo(512));
            Assert.That(config.confidenceThreshold, Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(config.nmsThreshold, Is.EqualTo(0.4f).Within(0.0001f));

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void YoloLegacyAdapter_InferCapabilitiesFromModelName()
        {
            var config = new ModelConfig { modelName = "yolo26n-pose-person" };

            VisionModelCapability capabilities = YoloLegacyModelAdapter.InferCapabilities(config);

            Assert.That((capabilities & VisionModelCapability.Detection) != 0, Is.True);
            Assert.That((capabilities & VisionModelCapability.Pose2D) != 0, Is.True);
            Assert.That((capabilities & VisionModelCapability.HumanDetection) != 0, Is.True);
        }

        [Test]
        public void LegacyInputProviderFrameSource_ProducesVisionFrame()
        {
            var texture = new Texture2D(32, 24);
            var provider = new FakeInputProvider(texture);
            var source = new LegacyInputProviderFrameSource(provider, VisionFrameSourceType.Texture);

            source.Initialize();
            bool hasFrame = source.TryGetFrame(out VisionFrame frame);

            Assert.That(hasFrame, Is.True);
            Assert.That(frame.IsValid, Is.True);
            Assert.That(frame.texture, Is.SameAs(texture));
            Assert.That(frame.sourceSize, Is.EqualTo(new Vector2Int(32, 24)));
            Assert.That(frame.sourceType, Is.EqualTo(VisionFrameSourceType.Texture));

            source.Dispose();
            Object.DestroyImmediate(texture);
        }

        private sealed class FakeInputProvider : IInputProvider
        {
            private readonly Texture texture;

            public FakeInputProvider(Texture texture)
            {
                this.texture = texture;
            }

            public bool IsReady { get; private set; }
            public Texture CurrentTexture => texture;
            public Vector2Int Resolution => new Vector2Int(texture.width, texture.height);

            public event System.Action OnTextureUpdated;

            public void Initialize()
            {
                IsReady = true;
            }

            public void UpdateTexture()
            {
                OnTextureUpdated?.Invoke();
            }

            public void Release()
            {
                IsReady = false;
            }
        }
    }
}
