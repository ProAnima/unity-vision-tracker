using NUnit.Framework;
using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.Tests
{
    public sealed class VisionPipelineTests
    {
        [Test]
        public void Start_WithoutConfiguration_FailsWithError()
        {
            using var pipeline = new VisionPipeline();
            VisionError error = null;
            pipeline.ErrorReceived += e => error = e;

            bool started = pipeline.Start();

            Assert.That(started, Is.False);
            Assert.That(pipeline.IsRunning, Is.False);
            Assert.That(pipeline.Context.HealthState, Is.EqualTo(VisionHealthState.Failed));
            Assert.That(error.code, Is.EqualTo(VisionErrorCode.ModelNotConfigured));
        }

        [Test]
        public void Start_InitializesSourceAndRuntime()
        {
            using var pipeline = new VisionPipeline();
            var source = new FakeFrameSource();
            var runtime = new FakeRuntimeAdapter();
            pipeline.Configure(null, source, runtime);

            bool started = pipeline.Start();

            Assert.That(started, Is.True);
            Assert.That(source.InitializeCount, Is.EqualTo(1));
            Assert.That(runtime.InitializeCount, Is.EqualTo(1));
            Assert.That(pipeline.Context.HealthState, Is.EqualTo(VisionHealthState.Running));
        }

        [Test]
        public void TryProcessNext_EmitsFrameResult()
        {
            using var pipeline = new VisionPipeline();
            var source = new FakeFrameSource();
            var runtime = new FakeRuntimeAdapter();
            VisionFrameResult emitted = null;
            pipeline.FrameProcessed += result => emitted = result;
            pipeline.Configure(null, source, runtime);
            pipeline.Start();

            bool processed = pipeline.TryProcessNext(out VisionFrameResult result);

            Assert.That(processed, Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.frameIndex, Is.EqualTo(1));
            Assert.That(pipeline.LastResult, Is.SameAs(result));
            Assert.That(emitted, Is.SameAs(result));
        }

        [Test]
        public void TryProcessNext_SourceNotReady_ReportsRecoverableError()
        {
            using var pipeline = new VisionPipeline();
            var source = new FakeFrameSource { Ready = false };
            var runtime = new FakeRuntimeAdapter();
            VisionError error = null;
            pipeline.ErrorReceived += e => error = e;
            pipeline.Configure(null, source, runtime);
            pipeline.Start();

            bool processed = pipeline.TryProcessNext(out VisionFrameResult result);

            Assert.That(processed, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error.code, Is.EqualTo(VisionErrorCode.SourceNotReady));
            Assert.That(error.isRecoverable, Is.True);
        }

        [Test]
        public void TryProcessNext_NullRuntimeResult_ReportsPostprocessError()
        {
            using var pipeline = new VisionPipeline();
            var source = new FakeFrameSource();
            var runtime = new FakeRuntimeAdapter { ReturnNullResult = true };
            VisionError error = null;
            pipeline.ErrorReceived += e => error = e;
            pipeline.Configure(null, source, runtime);
            pipeline.Start();

            bool processed = pipeline.TryProcessNext(out VisionFrameResult result);

            Assert.That(processed, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error.code, Is.EqualTo(VisionErrorCode.PostprocessFailed));
        }

        [Test]
        public void Dispose_ReleasesSourceAndRuntime()
        {
            var pipeline = new VisionPipeline();
            var source = new FakeFrameSource();
            var runtime = new FakeRuntimeAdapter();
            pipeline.Configure(null, source, runtime);
            pipeline.Start();

            pipeline.Dispose();

            Assert.That(source.Disposed, Is.True);
            Assert.That(runtime.Disposed, Is.True);
            Assert.That(pipeline.IsRunning, Is.False);
        }

        [Test]
        public void PipelineProfile_ReturnsClampedDefaultModel()
        {
            var first = ScriptableObject.CreateInstance<VisionModelProfile>();
            var second = ScriptableObject.CreateInstance<VisionModelProfile>();
            var profile = ScriptableObject.CreateInstance<VisionPipelineProfile>();
            profile.models = new[] { first, second };
            profile.defaultModelIndex = 99;

            Assert.That(profile.GetDefaultModel(), Is.SameAs(second));

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        private sealed class FakeFrameSource : IVisionFrameSource
        {
            private Texture2D texture;
            private int frameIndex;

            public bool Ready = true;
            public int InitializeCount { get; private set; }
            public bool Disposed { get; private set; }
            public bool IsReady => Ready;
            public Vector2Int SourceSize => new Vector2Int(16, 16);
            public VisionFrameSourceType SourceType => VisionFrameSourceType.Texture;

            public void Initialize()
            {
                InitializeCount++;
                texture = new Texture2D(16, 16);
            }

            public bool TryGetFrame(out VisionFrame frame)
            {
                frame = VisionFrame.FromTexture(texture, ++frameIndex, frameIndex);
                return frame.IsValid;
            }

            public void Dispose()
            {
                Disposed = true;
                if (texture != null)
                    Object.DestroyImmediate(texture);
            }
        }

        private sealed class FakeRuntimeAdapter : IVisionRuntimeAdapter
        {
            public int InitializeCount { get; private set; }
            public bool ReturnNullResult { get; set; }
            public bool Disposed { get; private set; }
            public VisionRuntimeKind RuntimeKind => VisionRuntimeKind.Mock;
            public VisionModelCapability Capabilities => VisionModelCapability.Detection;
            public bool IsInitialized { get; private set; }

            public void Initialize(VisionModelProfile profile)
            {
                InitializeCount++;
                IsInitialized = true;
            }

            public VisionFrameResult ProcessFrame(VisionFrame frame)
            {
                if (ReturnNullResult)
                    return null;

                return VisionFrameResult.Empty(frame.frameIndex, frame.timestamp, frame.sourceSize);
            }

            public void Dispose()
            {
                Disposed = true;
                IsInitialized = false;
            }
        }
    }
}
