using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;
using UniversalTracker.OutputReceivers;

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
        public void EventOutputReceiver_ReceivesVisionFrameResultDirectly()
        {
            var go = new GameObject("EventOutputReceiverTest");
            var receiver = go.AddComponent<EventOutputReceiver>();
            VisionFrameResult received = null;
            receiver.OnVisionFrameReceived.AddListener(result => received = result);
            var result = VisionFrameResult.Empty(3, 1.5, new Vector2Int(16, 16));

            receiver.ReceiveVisionResult(result);

            Assert.That(received, Is.SameAs(result));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void VisionAdapterRegistry_RegisterDuplicateAdapterId_Throws()
        {
            var registry = new VisionAdapterRegistry();
            registry.Register(new FakeModelAdapter("fake.adapter"));

            Assert.That(
                () => registry.Register(new FakeModelAdapter("fake.adapter")),
                Throws.InvalidOperationException);
        }

        [Test]
        public void VisionAdapterRegistry_TryGetAdapter_ReturnsFirstMatchingAdapter()
        {
            var profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            profile.family = VisionModelFamily.Custom;
            profile.runtimeKind = VisionRuntimeKind.Mock;
            var registry = new VisionAdapterRegistry();
            var adapter = new FakeModelAdapter("fake.adapter");
            registry.Register(adapter);

            bool found = registry.TryGetAdapter(profile, out IVisionModelAdapter resolved);

            Assert.That(found, Is.True);
            Assert.That(resolved, Is.SameAs(adapter));
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void VisionAdapterRegistry_TryCreateRuntime_NoAdapter_ReturnsError()
        {
            var profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            profile.family = VisionModelFamily.SAM;
            profile.runtimeKind = VisionRuntimeKind.Mock;
            var registry = new VisionAdapterRegistry();

            bool created = registry.TryCreateRuntime(profile, out IVisionRuntimeAdapter runtime, out string error);

            Assert.That(created, Is.False);
            Assert.That(runtime, Is.Null);
            Assert.That(error, Does.Contain("No adapter registered"));
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void VisionAdapterRegistry_TryCreateRuntime_UsesRegisteredAdapter()
        {
            var profile = ScriptableObject.CreateInstance<VisionModelProfile>();
            profile.family = VisionModelFamily.Custom;
            profile.runtimeKind = VisionRuntimeKind.Mock;
            var registry = new VisionAdapterRegistry();
            registry.Register(new FakeModelAdapter("fake.adapter"));

            bool created = registry.TryCreateRuntime(profile, out IVisionRuntimeAdapter runtime, out string error);

            Assert.That(created, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.RuntimeKind, Is.EqualTo(VisionRuntimeKind.Mock));
            runtime.Dispose();
            Object.DestroyImmediate(profile);
        }

        private sealed class FakeModelAdapter : IVisionModelAdapter
        {
            public FakeModelAdapter(string adapterId)
            {
                AdapterId = adapterId;
            }

            public string AdapterId { get; }
            public VisionModelFamily Family => VisionModelFamily.Custom;
            public VisionModelCapability Capabilities => VisionModelCapability.Detection;

            public bool CanHandle(VisionModelProfile profile)
            {
                return profile != null &&
                       profile.family == VisionModelFamily.Custom &&
                       profile.runtimeKind == VisionRuntimeKind.Mock;
            }

            public IVisionRuntimeAdapter CreateRuntime(VisionModelProfile profile)
            {
                return new FakeRuntimeAdapter();
            }
        }

        private sealed class FakeRuntimeAdapter : IVisionRuntimeAdapter
        {
            public VisionRuntimeKind RuntimeKind => VisionRuntimeKind.Mock;
            public VisionModelCapability Capabilities => VisionModelCapability.Detection;
            public bool IsInitialized { get; private set; }

            public void Initialize(VisionModelProfile profile)
            {
                IsInitialized = true;
            }

            public VisionFrameResult ProcessFrame(VisionFrame frame)
            {
                return VisionFrameResult.Empty(frame.frameIndex, frame.timestamp, frame.sourceSize);
            }

            public void Dispose()
            {
                IsInitialized = false;
            }
        }
    }
}
