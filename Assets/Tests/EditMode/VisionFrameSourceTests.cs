using NUnit.Framework;
using UnityEngine;
using UnityEngine.Video;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionFrameSourceTests
    {
        [Test]
        public void TextureFrameSource_ProducesTextureFrame()
        {
            var texture = new Texture2D(32, 24);
            using var source = new TextureFrameSource(texture);

            source.Initialize();
            bool hasFrame = source.TryGetFrame(out VisionFrame frame);

            Assert.That(source.IsReady, Is.True);
            Assert.That(hasFrame, Is.True);
            Assert.That(frame.texture, Is.SameAs(texture));
            Assert.That(frame.sourceSize, Is.EqualTo(new Vector2Int(32, 24)));
            Assert.That(frame.sourceType, Is.EqualTo(VisionFrameSourceType.Texture));

            UnityEngine.Object.DestroyImmediate(texture);
        }

        [Test]
        public void TextureFrameSource_NullTexture_ReturnsNoFrame()
        {
            using var source = new TextureFrameSource(null);

            bool hasFrame = source.TryGetFrame(out VisionFrame frame);

            Assert.That(source.IsReady, Is.False);
            Assert.That(hasFrame, Is.False);
            Assert.That(frame.IsValid, Is.False);
        }

        [Test]
        public void RenderTextureFrameSource_CreatesRenderTextureOnInitialize()
        {
            var renderTexture = new RenderTexture(40, 30, 0);
            using var source = new RenderTextureFrameSource(renderTexture);

            source.Initialize();
            bool hasFrame = source.TryGetFrame(out VisionFrame frame);

            Assert.That(renderTexture.IsCreated(), Is.True);
            Assert.That(source.IsReady, Is.True);
            Assert.That(hasFrame, Is.True);
            Assert.That(frame.sourceSize, Is.EqualTo(new Vector2Int(40, 30)));
            Assert.That(frame.sourceType, Is.EqualTo(VisionFrameSourceType.RenderTexture));

            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }

        [Test]
        public void UnityCameraFrameSource_AssignsAndRestoresTargetTexture()
        {
            var go = new GameObject("VisionCameraSourceTest");
            var camera = go.AddComponent<Camera>();
            var previous = new RenderTexture(8, 8, 0);
            var target = new RenderTexture(16, 16, 16);
            camera.targetTexture = previous;
            using var source = new UnityCameraFrameSource(camera, target);

            source.Initialize();

            Assert.That(target.IsCreated(), Is.True);
            Assert.That(source.IsReady, Is.True);
            Assert.That(camera.targetTexture, Is.SameAs(target));

            source.Dispose();

            Assert.That(camera.targetTexture, Is.SameAs(previous));
            camera.targetTexture = null;
            previous.Release();
            target.Release();
            UnityEngine.Object.DestroyImmediate(previous);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void VideoFrameSource_NullVideoPlayer_Throws()
        {
            Assert.That(() => new VideoFrameSource(null), Throws.ArgumentNullException);
        }

        [Test]
        public void VideoFrameSource_UnpreparedPlayer_ReturnsNoFrame()
        {
            var go = new GameObject("VisionVideoSourceTest");
            var player = go.AddComponent<VideoPlayer>();
            using var source = new VideoFrameSource(player, false);

            bool hasFrame = source.TryGetFrame(out VisionFrame frame);

            Assert.That(source.IsReady, Is.False);
            Assert.That(hasFrame, Is.False);
            Assert.That(frame.IsValid, Is.False);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
