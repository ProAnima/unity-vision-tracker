using System;
using UnityEngine;

namespace UniversalTracker.Core
{
    public interface IVisionFrameSource : IDisposable
    {
        bool IsReady { get; }
        Vector2Int SourceSize { get; }
        VisionFrameSourceType SourceType { get; }

        void Initialize();
        bool TryGetFrame(out VisionFrame frame);
    }

    public interface IVisionRuntimeAdapter : IDisposable
    {
        VisionRuntimeKind RuntimeKind { get; }
        VisionModelCapability Capabilities { get; }
        bool IsInitialized { get; }

        void Initialize(VisionModelProfile profile);
        VisionFrameResult ProcessFrame(VisionFrame frame);
    }

    public interface IVisionModelAdapter
    {
        string AdapterId { get; }
        VisionModelFamily Family { get; }
        VisionModelCapability Capabilities { get; }

        bool CanHandle(VisionModelProfile profile);
        IVisionRuntimeAdapter CreateRuntime();
    }

    public sealed class VisionPipelineContext
    {
        public VisionModelProfile ActiveModelProfile { get; private set; }
        public IVisionFrameSource FrameSource { get; private set; }
        public IVisionRuntimeAdapter Runtime { get; private set; }
        public VisionHealthState HealthState { get; private set; } = VisionHealthState.NotInitialized;
        public VisionError LastError { get; private set; }

        public void Bind(VisionModelProfile profile, IVisionFrameSource source, IVisionRuntimeAdapter runtime)
        {
            ActiveModelProfile = profile;
            FrameSource = source;
            Runtime = runtime;
            HealthState = VisionHealthState.Initializing;
            LastError = null;
        }

        public void MarkRunning()
        {
            HealthState = VisionHealthState.Running;
            LastError = null;
        }

        public void MarkStopped()
        {
            HealthState = VisionHealthState.Stopped;
        }

        public void MarkFailed(VisionError error)
        {
            HealthState = VisionHealthState.Failed;
            LastError = error;
        }
    }
}
