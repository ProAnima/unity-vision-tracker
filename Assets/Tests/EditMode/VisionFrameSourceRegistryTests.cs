using System;
using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionFrameSourceRegistryTests
    {
        [Test]
        public void CreateDefault_MapsBuiltInInputTypesToSourceTypes()
        {
            var registry = VisionFrameSourceRegistry.CreateDefault();

            Assert.That(registry.TryGetSourceType(InputProviderType.WebCam, out VisionFrameSourceType webCam), Is.True);
            Assert.That(webCam, Is.EqualTo(VisionFrameSourceType.WebCam));
            Assert.That(registry.TryGetSourceType(InputProviderType.Camera, out VisionFrameSourceType camera), Is.True);
            Assert.That(camera, Is.EqualTo(VisionFrameSourceType.UnityCamera));
            Assert.That(registry.TryGetSourceType(InputProviderType.Texture, out VisionFrameSourceType texture), Is.True);
            Assert.That(texture, Is.EqualTo(VisionFrameSourceType.Texture));
            Assert.That(registry.TryGetSourceType(InputProviderType.Video, out VisionFrameSourceType video), Is.True);
            Assert.That(video, Is.EqualTo(VisionFrameSourceType.Video));
        }

        [Test]
        public void TryCreateProvider_UsesRegisteredFactory()
        {
            var host = new GameObject("FrameSourceRegistryTest");
            var registry = new VisionFrameSourceRegistry();
            registry.Register(InputProviderType.Texture, VisionFrameSourceType.Texture, _ => new FakeInputProvider());

            bool created = registry.TryCreateProvider(
                InputProviderType.Texture,
                host,
                out IInputProvider provider,
                out VisionFrameSourceType sourceType,
                out string error);

            Assert.That(created, Is.True);
            Assert.That(provider, Is.TypeOf<FakeInputProvider>());
            Assert.That(sourceType, Is.EqualTo(VisionFrameSourceType.Texture));
            Assert.That(error, Is.Null);
            UnityEngine.Object.DestroyImmediate(host);
        }

        [Test]
        public void TryCreateProvider_UnknownInputType_ReturnsError()
        {
            var host = new GameObject("FrameSourceRegistryTest");
            var registry = new VisionFrameSourceRegistry();

            bool created = registry.TryCreateProvider(
                InputProviderType.Custom,
                host,
                out IInputProvider provider,
                out VisionFrameSourceType sourceType,
                out string error);

            Assert.That(created, Is.False);
            Assert.That(provider, Is.Null);
            Assert.That(sourceType, Is.EqualTo(VisionFrameSourceType.Unknown));
            Assert.That(error, Does.Contain("No frame source registered"));
            UnityEngine.Object.DestroyImmediate(host);
        }

        [Test]
        public void TryCreateProvider_NullHost_ReturnsError()
        {
            var registry = new VisionFrameSourceRegistry();
            registry.Register(InputProviderType.Texture, VisionFrameSourceType.Texture, _ => new FakeInputProvider());

            bool created = registry.TryCreateProvider(
                InputProviderType.Texture,
                null,
                out IInputProvider provider,
                out VisionFrameSourceType sourceType,
                out string error);

            Assert.That(created, Is.False);
            Assert.That(provider, Is.Null);
            Assert.That(sourceType, Is.EqualTo(VisionFrameSourceType.Unknown));
            Assert.That(error, Does.Contain("Host GameObject is null"));
        }

        [Test]
        public void Register_NullFactory_Throws()
        {
            var registry = new VisionFrameSourceRegistry();

            Assert.That(
                () => registry.Register(InputProviderType.Texture, VisionFrameSourceType.Texture, null),
                Throws.ArgumentNullException);
        }

        private sealed class FakeInputProvider : IInputProvider
        {
            public bool IsReady { get; private set; }
            public Texture CurrentTexture => null;
            public Vector2Int Resolution => Vector2Int.zero;

            public event Action OnTextureUpdated;

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
