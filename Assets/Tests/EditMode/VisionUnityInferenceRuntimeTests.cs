using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionUnityInferenceRuntimeTests
    {
        [Test]
        public void YoloModelAdapter_CreatesUnityInferenceRuntimeAdapter()
        {
            var profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            profile.family = VisionModelFamily.YOLO;
            profile.runtimeKind = VisionRuntimeKind.UnityInferenceEngine;
            profile.capabilities = VisionModelCapability.Detection;
            profile.output = new VisionOutputSchema
            {
                tensors = new[]
                {
                    new VisionTensorSchema { name = "output0", shape = new[] { 1, 6 }, semantic = "detections" }
                }
            };

            var adapter = new YoloModelAdapter();
            IVisionRuntimeAdapter runtime = adapter.CreateRuntime(profile);

            Assert.That(runtime, Is.TypeOf<UnityInferenceRuntimeAdapter>());
            Assert.That(runtime.RuntimeKind, Is.EqualTo(VisionRuntimeKind.UnityInferenceEngine));
            runtime.Dispose();
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void DefaultAdapterRegistry_UsesProductionYoloAdapter()
        {
            var registry = VisionAdapterRegistry.CreateDefault();

            Assert.That(registry.Adapters[0], Is.TypeOf<YoloModelAdapter>());
            Assert.That(registry.Adapters[0].AdapterId, Is.EqualTo("proanima.yolo.unity-inference"));
        }

        [Test]
        public void UnityInferenceRuntimeAdapter_UsesRawOutputParserPath()
        {
            var texture = new Texture2D(100, 50);
            var profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            profile.confidenceThreshold = 0.2f;
            profile.nmsThreshold = 0.5f;
            var provider = new FakeRawOutputProvider();
            var runtime = new UnityInferenceRuntimeAdapter(
                _ => provider,
                new YoloDetectionOutputParser(),
                VisionModelCapability.Detection);
            var frame = VisionFrame.FromTexture(texture, 3, 1.25);

            runtime.Initialize(profile);
            VisionFrameResult result = runtime.ProcessFrame(frame);
            runtime.Dispose();

            Assert.That(provider.InitializeCount, Is.EqualTo(1));
            Assert.That(provider.ExecuteCount, Is.EqualTo(1));
            Assert.That(provider.Disposed, Is.True);
            Assert.That(result.frameIndex, Is.EqualTo(3));
            Assert.That(result.detections.Length, Is.EqualTo(1));
            Assert.That(result.detections[0].sourceRect.x, Is.EqualTo(40f).Within(0.0001f));
            Assert.That(result.detections[0].sourceRect.y, Is.EqualTo(20f).Within(0.0001f));

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void UnityInferenceRawOutputProvider_NullProfile_Throws()
        {
            Assert.That(() => new UnityInferenceRawOutputProvider(null), Throws.ArgumentNullException);
        }

        [Test]
        public void UnityInferenceRawOutputProvider_InitializeWithoutModelAsset_Throws()
        {
            var profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            using var provider = new UnityInferenceRawOutputProvider(profile);

            Assert.That(
                () => provider.Initialize(profile),
                Throws.InvalidOperationException.With.Message.Contains("ModelAsset"));

            Object.DestroyImmediate(profile);
        }

        private sealed class FakeRawOutputProvider : IVisionRawOutputProvider
        {
            public bool IsInitialized { get; private set; }
            public int InitializeCount { get; private set; }
            public int ExecuteCount { get; private set; }
            public bool Disposed { get; private set; }

            public void Initialize(VisionModelProfile profile)
            {
                InitializeCount++;
                IsInitialized = true;
            }

            public VisionRawModelOutput Execute(Texture inputTexture)
            {
                ExecuteCount++;
                return VisionRawModelOutput.Single(
                    "output0",
                    new[] { 0.5f, 0.5f, 0.2f, 0.2f, 0.9f, 0.8f },
                    1,
                    6);
            }

            public void Dispose()
            {
                Disposed = true;
                IsInitialized = false;
            }
        }
    }
}
