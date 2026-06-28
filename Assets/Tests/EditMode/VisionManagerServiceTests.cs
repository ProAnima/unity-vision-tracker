using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionManagerServiceTests
    {
        [Test]
        public void ProfileResolver_PrefersPipelineProfileModels()
        {
            var fallback = ScriptableObject.CreateInstance<VisionModelProfile>();
            var first = ScriptableObject.CreateInstance<VisionModelProfile>();
            var second = ScriptableObject.CreateInstance<VisionModelProfile>();
            var pipeline = ScriptableObject.CreateInstance<VisionPipelineProfile>();
            pipeline.models = new[] { first, second };
            int activeIndex = 99;

            VisionModelProfile resolved = VisionModelProfileResolver.Resolve(pipeline, new[] { fallback }, ref activeIndex);

            Assert.That(resolved, Is.SameAs(first));
            Assert.That(activeIndex, Is.EqualTo(0));

            Object.DestroyImmediate(pipeline);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(fallback);
        }

        [Test]
        public void ProfileResolver_UsesFallbackProfilesWithoutPipeline()
        {
            var fallback = ScriptableObject.CreateInstance<VisionModelProfile>();
            int activeIndex = 0;

            VisionModelProfile resolved = VisionModelProfileResolver.Resolve(null, new[] { fallback }, ref activeIndex);

            Assert.That(resolved, Is.SameAs(fallback));
            Assert.That(activeIndex, Is.EqualTo(0));

            Object.DestroyImmediate(fallback);
        }

        [Test]
        public void HealthEventHub_StoresStatusAndDispatchesTypedEvents()
        {
            var hub = new VisionHealthEventHub();
            VisionHealthStatus changed = null;
            VisionHealthStatus failed = null;
            hub.Changed += status => changed = status;
            hub.Failed += status => failed = status;

            var status = VisionHealthStatus.Create(
                VisionHealthState.Failed,
                VisionHealthState.Running,
                VisionHealthEvent.Failed,
                "failed",
                new VisionError(VisionErrorCode.InferenceFailed, "boom", false));

            hub.Emit(status);

            Assert.That(hub.Status, Is.SameAs(status));
            Assert.That(hub.State, Is.EqualTo(VisionHealthState.Failed));
            Assert.That(hub.LastError.code, Is.EqualTo(VisionErrorCode.InferenceFailed));
            Assert.That(changed, Is.SameAs(status));
            Assert.That(failed, Is.SameAs(status));
        }

        [Test]
        public void FrameSourceFactory_CreatesTextureSourceFromTexture()
        {
            var texture = new Texture2D(8, 4);
            var request = new VisionFrameSourceRequest(
                InputProviderType.Texture,
                null,
                texture,
                null,
                null,
                null,
                null,
                null,
                1280,
                720,
                30,
                null,
                null);

            IVisionFrameSource source = VisionFrameSourceFactory.Create(request);

            Assert.That(source, Is.TypeOf<TextureFrameSource>());
            Assert.That(source.IsReady, Is.True);
            Assert.That(source.SourceSize, Is.EqualTo(new Vector2Int(8, 4)));

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void TrackingStage_WhenDisabled_DoesNotMutateDetections()
        {
            var stage = new VisionTrackingStage();
            var detection = new VisionDetection
            {
                classId = 1,
                confidence = 0.9f,
                sourceRect = new Rect(0f, 0f, 10f, 10f)
            };
            var result = VisionFrameResult.Empty(1, 0d, new Vector2Int(100, 100));
            result.detections = new[] { detection };

            stage.Configure(false, TrackerType.IOU, 0.3f, 10);
            stage.Apply(result, 0.016f);

            Assert.That(stage.TrackedObjects, Is.Empty);
            Assert.That(result.detections, Has.Length.EqualTo(1));
            Assert.That(result.detections[0].classId, Is.EqualTo(1));
            Assert.That(result.detections[0].IsTracked, Is.False);
        }
    }
}
