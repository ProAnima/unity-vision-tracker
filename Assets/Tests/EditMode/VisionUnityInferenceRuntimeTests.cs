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
        public void UnityInferenceRuntimeAdapter_InitializesProcessesAndDisposesModel()
        {
            var texture = new Texture2D(16, 16);
            var fakeModel = new FakeInferenceModel();
            var runtime = new UnityInferenceRuntimeAdapter(
                _ => new ModelConfig { modelName = "fake-yolo", inputSize = 16 },
                _ => fakeModel,
                VisionModelCapability.Detection);
            var frame = VisionFrame.FromTexture(texture, 7, 2.5);

            runtime.Initialize(null);
            VisionFrameResult result = runtime.ProcessFrame(frame);
            runtime.Dispose();

            Assert.That(fakeModel.InitializeCount, Is.EqualTo(1));
            Assert.That(fakeModel.RunInferenceCount, Is.EqualTo(1));
            Assert.That(fakeModel.Disposed, Is.True);
            Assert.That(result.frameIndex, Is.EqualTo(7));
            Assert.That(result.sourceSize, Is.EqualTo(new Vector2Int(16, 16)));
            Assert.That(result.detections.Length, Is.EqualTo(1));
            Object.DestroyImmediate(texture);
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

        private sealed class FakeInferenceModel : IInferenceModel
        {
            public ModelType ModelType => ModelType.Detection;
            public bool IsInitialized { get; private set; }
            public int InitializeCount { get; private set; }
            public int RunInferenceCount { get; private set; }
            public bool Disposed { get; private set; }

            public void Initialize(ModelConfig config)
            {
                InitializeCount++;
                IsInitialized = true;
            }

            public InferenceResult RunInference(Texture inputTexture)
            {
                RunInferenceCount++;
                return new InferenceResult
                {
                    success = true,
                    detectionCount = 1,
                    boundingBoxes = new[]
                    {
                        new BBoxData
                        {
                            id = 1,
                            classId = 0,
                            className = "object",
                            confidence = 0.9f,
                            rect = new Rect(0.1f, 0.2f, 0.3f, 0.4f)
                        }
                    }
                };
            }

            public void Dispose()
            {
                Disposed = true;
                IsInitialized = false;
            }
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
